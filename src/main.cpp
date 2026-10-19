// =============================================================================
//  main.cpp  —  CTTR Online  (Driveable 3D Kart Simulator & Track Inspector)
// =============================================================================

#include <iostream>
#include <iomanip>
#include <thread>
#include <chrono>
#include <string>
#include <filesystem>
#include <vector>
#include <algorithm>
#include <cmath>
#include <fstream>
#include <memory>

#include "network/LanGameSync.h"
#include "renderer/GLRenderer.h"
#include "renderer/P3DLoader.h"
#include "renderer/DebugDraw.h"
#include "renderer/FrontendRenderer.h"
#include "game/MenuState.h"
#include "game/GroundContact.h"
#include "game/InputSource.h"
#include "game/LevelData.h"
#include "game/World.h"
#include "physics/Vehicle.h"
#include "physics/TrackCollider.h"

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/constants.hpp>

namespace fs = std::filesystem;

// ---------------------------------------------------------------------------
//  Camera Modes
// ---------------------------------------------------------------------------
enum class CameraMode {
    CHASE_CAM,  // 3rd-person dynamic chase camera following the driveable kart
    FREE_CAM    // Detached free-flying camera for inspecting track & models
};

static CameraMode g_CameraMode = CameraMode::CHASE_CAM;

// ---------------------------------------------------------------------------
//  Free Camera Structure (Track Inspector)
// ---------------------------------------------------------------------------
struct FreeCamera {
    glm::vec3 position = glm::vec3(0.0f, 10.0f, -200.0f);
    glm::vec3 front    = glm::vec3(0.0f, 0.0f, 1.0f);
    glm::vec3 up       = glm::vec3(0.0f, 1.0f, 0.0f);
    glm::vec3 right    = glm::vec3(1.0f, 0.0f, 0.0f);
    glm::vec3 worldUp  = glm::vec3(0.0f, 1.0f, 0.0f);

    float yaw   = 90.0f;
    float pitch = -10.0f;

    float speed       = 50.0f;
    float sensitivity = 0.15f;
    float fov         = 60.0f;

    bool firstMouse = true;
    double lastX = 640.0;
    double lastY = 360.0;
    bool isDragging = false;

    void UpdateCameraVectors() {
        glm::vec3 f;
        f.x = cos(glm::radians(yaw)) * cos(glm::radians(pitch));
        f.y = sin(glm::radians(pitch));
        f.z = sin(glm::radians(yaw)) * cos(glm::radians(pitch));
        front = glm::normalize(f);
        right = glm::normalize(glm::cross(front, worldUp));
        up    = glm::normalize(glm::cross(right, front));
    }

    glm::mat4 GetViewMatrix() const {
        return glm::lookAt(position, position + front, up);
    }

    void ProcessKeyboard(int key, float dt, bool shiftPressed) {
        float velocity = speed * dt * (shiftPressed ? 3.5f : 1.0f);
        if (key == GLFW_KEY_W) position += front * velocity;
        if (key == GLFW_KEY_S) position -= front * velocity;
        if (key == GLFW_KEY_A) position -= right * velocity;
        if (key == GLFW_KEY_D) position += right * velocity;
        if (key == GLFW_KEY_E || key == GLFW_KEY_SPACE) position += worldUp * velocity;
        if (key == GLFW_KEY_Q || key == GLFW_KEY_LEFT_CONTROL) position -= worldUp * velocity;
        
        // Arrow keys for keyboard rotation
        if (key == GLFW_KEY_LEFT)  yaw -= 60.0f * dt;
        if (key == GLFW_KEY_RIGHT) yaw += 60.0f * dt;
        if (key == GLFW_KEY_UP)    pitch += 40.0f * dt;
        if (key == GLFW_KEY_DOWN)  pitch -= 40.0f * dt;
        if (pitch > 89.0f) pitch = 89.0f;
        if (pitch < -89.0f) pitch = -89.0f;

        UpdateCameraVectors();
    }

    void ProcessMouseMovement(float xoffset, float yoffset, bool constrainPitch = true) {
        xoffset *= sensitivity;
        yoffset *= sensitivity;

        yaw   += xoffset;
        pitch += yoffset;

        if (constrainPitch) {
            if (pitch > 89.0f) pitch = 89.0f;
            if (pitch < -89.0f) pitch = -89.0f;
        }

        UpdateCameraVectors();
    }

    void FocusOnBounds(const glm::vec3& minBounds, const glm::vec3& maxBounds) {
        glm::vec3 center = (minBounds + maxBounds) * 0.5f;
        float span = glm::length(maxBounds - minBounds);
        if (span < 1.0f) span = 10.0f;

        position = center + glm::vec3(0.0f, std::max(span * 0.2f, 3.0f), -std::max(span * 0.6f, 5.0f));
        front = glm::normalize(center - position);
        right = glm::normalize(glm::cross(front, worldUp));
        up    = glm::normalize(glm::cross(right, front));

        pitch = glm::degrees(asin(front.y));
        yaw   = glm::degrees(atan2(front.z, front.x));
        UpdateCameraVectors();
    }
};

// ---------------------------------------------------------------------------
//  Chase Camera Structure (Kart Racing 3rd Person)
// ---------------------------------------------------------------------------
struct ChaseCamera {
    glm::vec3 position = glm::vec3(-6.36f, -6.2f, 216.5f);
    glm::vec3 target   = glm::vec3(-6.36f, -9.0f, 201.0f);
    float distance = 10.5f;
    float height   = 3.8f;
    float yawOffset = 0.0f;

    void Reset(const glm::vec3& kartPos, float kartYaw) {
        // Vehicle forward heading: (sin(yaw), 0, cos(yaw))
        // Behind vector: (-sin(yaw), 0, -cos(yaw))
        glm::vec3 behind(-std::sin(kartYaw), 0.0f, -std::cos(kartYaw));
        glm::vec3 fwd(std::sin(kartYaw), 0.0f, std::cos(kartYaw));
        position = kartPos + behind * distance + glm::vec3(0.0f, height, 0.0f);
        target   = kartPos + fwd * 2.5f + glm::vec3(0.0f, 1.2f, 0.0f);
        yawOffset = 0.0f;
    }

    void Update(const glm::vec3& kartPos, float kartYaw, float speedKPH, float dt) {
        float dynamicDist = distance + std::clamp(speedKPH * 0.03f, 0.0f, 3.0f);
        float dynamicHeight = height + std::clamp(speedKPH * 0.008f, 0.0f, 1.2f);

        float camYaw = kartYaw + glm::radians(yawOffset);
        glm::vec3 behind(-std::sin(camYaw), 0.0f, -std::cos(camYaw));
        glm::vec3 fwd(std::sin(camYaw), 0.0f, std::cos(camYaw));
        glm::vec3 desiredPos = kartPos + behind * dynamicDist + glm::vec3(0.0f, dynamicHeight, 0.0f);
        glm::vec3 desiredTarget = kartPos + fwd * 2.5f + glm::vec3(0.0f, 1.2f, 0.0f);

        float followTightness = std::clamp(dt * 7.0f, 0.0f, 1.0f);
        float targetTightness = std::clamp(dt * 12.0f, 0.0f, 1.0f);

        position = glm::mix(position, desiredPos, followTightness);
        target   = glm::mix(target, desiredTarget, targetTightness);
    }

    glm::mat4 GetViewMatrix() const {
        return glm::lookAt(position, target, glm::vec3(0.0f, 1.0f, 0.0f));
    }
};

// ---------------------------------------------------------------------------
//  Global State
// ---------------------------------------------------------------------------
static FreeCamera  g_Camera;
static ChaseCamera g_ChaseCamera;
static bool g_WireframeMode = false;
static bool g_KeysPressed[1024] = {false};

// Simulation World
static CTTR::Game::World g_World;
static std::unique_ptr<CTTR::Game::KeyboardInput> g_KeyboardInput;

static void ResetPlayerVehicle() {
    g_World.ResetVehicle(0, 0);
    for (size_t i = 1; i < g_World.GetVehicleCount(); ++i) {
        g_World.ResetVehicle(i, static_cast<int>(i));
    }
    g_World.GetRaceController().StartCountdown();
    auto* v = g_World.GetPlayerVehicle();
    if (v) {
        auto pos = v->GetPosition();
        auto rot = v->GetRotation();
        g_ChaseCamera.Reset({pos.x, pos.y, pos.z}, rot.y);
    }
}

static void SetCameraPreset(int preset) {
    g_CameraMode = CameraMode::FREE_CAM;
    if (preset == 1) {
        g_Camera.FocusOnBounds(g_World.GetSceneMin(), g_World.GetSceneMax());
        std::cout << "[FreeCam] Preset 1: Full Level Overview" << std::endl;
        return;
    }

    const auto* cp = g_World.GetLevelData().GetCameraPreset(preset);
    if (cp) {
        g_Camera.position = cp->position;
        g_Camera.pitch = cp->pitch;
        g_Camera.yaw = cp->yaw;
        g_Camera.UpdateCameraVectors();
        std::cout << "[FreeCam] Preset " << preset << ": " << cp->name << std::endl;
    } else {
        std::cout << "[FreeCam] Preset " << preset << " not defined." << std::endl;
    }
}

static void ToggleCameraMode() {
    if (g_CameraMode == CameraMode::CHASE_CAM) {
        g_CameraMode = CameraMode::FREE_CAM;
        g_Camera.position = g_ChaseCamera.position;
        glm::vec3 toKart = g_ChaseCamera.target - g_Camera.position;
        if (glm::length(toKart) > 0.01f) {
            toKart = glm::normalize(toKart);
            g_Camera.pitch = glm::degrees(asin(toKart.y));
            g_Camera.yaw   = glm::degrees(atan2(toKart.z, toKart.x));
            g_Camera.UpdateCameraVectors();
        }
        if (g_KeyboardInput) g_KeyboardInput->SetEnabled(false);
        std::cout << "[Camera] Switched to FREE CAM (Track & Asset Inspection Mode)" << std::endl;
    } else {
        g_CameraMode = CameraMode::CHASE_CAM;
        auto* v = g_World.GetPlayerVehicle();
        if (v) {
            auto pos = v->GetPosition();
            auto rot = v->GetRotation();
            g_ChaseCamera.Reset({pos.x, pos.y, pos.z}, rot.y);
        }
        if (g_KeyboardInput) g_KeyboardInput->SetEnabled(true);
        std::cout << "[Camera] Switched to CHASE CAM (Driveable Kart Mode)" << std::endl;
    }
}

static void KeyCallback(GLFWwindow* window, int key, int scancode, int action, int mods)
{
    if (key >= 0 && key < 1024) {
        if (action == GLFW_PRESS) g_KeysPressed[key] = true;
        else if (action == GLFW_RELEASE) g_KeysPressed[key] = false;
    }

    if (action == GLFW_PRESS) {
        if (key == GLFW_KEY_TAB) ToggleCameraMode();
        if (key == GLFW_KEY_R)   ResetPlayerVehicle();
        if (key == GLFW_KEY_1)   SetCameraPreset(1);
        if (key == GLFW_KEY_2)   SetCameraPreset(2);
        if (key == GLFW_KEY_3)   SetCameraPreset(3);
        if (key == GLFW_KEY_4)   SetCameraPreset(4);
    }

    if (key == GLFW_KEY_ESCAPE && action == GLFW_PRESS) {
        glfwSetWindowShouldClose(window, GLFW_TRUE);
    }

    if (key == GLFW_KEY_F1 && action == GLFW_PRESS) {
        g_WireframeMode = !g_WireframeMode;
        glPolygonMode(GL_FRONT_AND_BACK, g_WireframeMode ? GL_LINE : GL_FILL);
        std::cout << "[Renderer] Wireframe Mode: " << (g_WireframeMode ? "ON" : "OFF") << std::endl;
    }
}

static void MouseButtonCallback(GLFWwindow* window, int button, int action, int mods) {
    if (button == GLFW_MOUSE_BUTTON_LEFT || button == GLFW_MOUSE_BUTTON_RIGHT) {
        if (action == GLFW_PRESS) {
            g_Camera.isDragging = true;
            g_Camera.firstMouse = true;
        } else if (action == GLFW_RELEASE) {
            g_Camera.isDragging = false;
        }
    }
}

static void CursorPosCallback(GLFWwindow* window, double xpos, double ypos) {
    if (!g_Camera.isDragging) return;

    if (g_Camera.firstMouse) {
        g_Camera.lastX = xpos;
        g_Camera.lastY = ypos;
        g_Camera.firstMouse = false;
    }

    float xoffset = static_cast<float>(xpos - g_Camera.lastX);
    float yoffset = static_cast<float>(g_Camera.lastY - ypos); // Reversed: y-coords go from bottom to top

    g_Camera.lastX = xpos;
    g_Camera.lastY = ypos;

    if (g_CameraMode == CameraMode::FREE_CAM) {
        g_Camera.ProcessMouseMovement(xoffset, yoffset);
    } else {
        // In Chase Cam, mouse dragging lets player look around vehicle
        g_ChaseCamera.yawOffset += xoffset * 0.3f;
    }
}

static std::string ResolveGameDataPath(const std::string& inputPath)
{
    std::cout << "[main] Resolving game_data path for: '" << inputPath << "'..." << std::endl;
    try
    {
        fs::path p = inputPath;
        if (fs::exists(p) && fs::is_directory(p))
        {
            std::string absPath = fs::absolute(p).string();
            std::cout << "[main] Found game_data directly at: " << absPath << std::endl;
            return absPath;
        }

        fs::path current = fs::current_path();
        std::cout << "[main] Searching parent directories starting from: " << current.string() << std::endl;
        
        while (true)
        {
            fs::path candidate = current / p;
            if (fs::exists(candidate) && fs::is_directory(candidate))
            {
                std::string absPath = fs::absolute(candidate).string();
                std::cout << "[main] Auto-resolved game_data path: " << absPath << std::endl;
                return absPath;
            }

            fs::path parent = current.parent_path();
            if (parent == current || !current.has_parent_path()) {
                break; // Prevent infinite loop at drive root
            }
            current = parent;
        }

        // Absolute fallback for repository workspace
        fs::path repoFallback = "C:/Users/Adam/Documents/Project/CTTR_Online/game_data";
        if (fs::exists(repoFallback) && fs::is_directory(repoFallback)) {
            std::cout << "[main] Using repository fallback game_data path: " << repoFallback.string() << std::endl;
            return repoFallback.string();
        }
    }
    catch (const std::exception& e)
    {
        std::cerr << "[main] Error resolving game_data path: " << e.what() << std::endl;
    }

    std::cout << "[main] Warning: Could not resolve game_data directory. Using fallback string: " << inputPath << std::endl;
    return inputPath;
}

// ---------------------------------------------------------------------------
//  Main
// ---------------------------------------------------------------------------
int main(int argc, char** argv)
{
    std::cout << "=========================================================" << std::endl;
    std::cout << "--- Crash Tag Team Racing: Driveable Kart Simulator ---" << std::endl;
    std::cout << "=========================================================" << std::endl;

    std::string gameDataPath = "game_data";
    std::string levelName = "fairy1";
    std::string screenshotPath;
    float driveDuration = 0.0f;
    int initialPreset = 0;
    bool forceFreeCam = false;
    std::string listMeshesFilter;

    for (int i = 1; i < argc; ++i) {
        const std::string arg = argv[i];
        if (arg == "--screenshot" && i + 1 < argc) {
            screenshotPath = argv[++i];
        } else if (arg == "--preset" && i + 1 < argc) {
            initialPreset = std::stoi(argv[++i]);
            forceFreeCam = true;
        } else if (arg == "--free-cam") {
            forceFreeCam = true;
        } else if (arg == "--level" && i + 1 < argc) {
            levelName = argv[++i];
        } else if (arg == "--list-meshes") {
            listMeshesFilter = (i + 1 < argc && argv[i + 1][0] != '-') ? argv[++i] : "*";
        } else if (arg == "--cam-pos" && i + 3 < argc) {
            g_Camera.position.x = std::stof(argv[++i]);
            g_Camera.position.y = std::stof(argv[++i]);
            g_Camera.position.z = std::stof(argv[++i]);
            initialPreset = -1;
            forceFreeCam = true;
        } else if (arg == "--cam-rot" && i + 2 < argc) {
            g_Camera.pitch = std::stof(argv[++i]);
            g_Camera.yaw = std::stof(argv[++i]);
            g_Camera.UpdateCameraVectors();
        } else if (arg == "--drive-test" && i + 1 < argc) {
            driveDuration = std::stof(argv[++i]);
        } else if (arg != "--physics-test") {
            gameDataPath = arg;
        }
    }

    gameDataPath = ResolveGameDataPath(gameDataPath);

    // ── 1. Renderer Initialization ─────────────────────────────────────────
    CTTR::Renderer::GLRenderer renderer;
    if (!renderer.Initialize(1280, 720, "CTTR Online - Driveable Kart Simulator")) {
        std::cerr << "Failed to initialize OpenGL Renderer!" << std::endl;
        return 1;
    }

    GLFWwindow* window = renderer.GetWindow();
    glfwSetKeyCallback(window, KeyCallback);
    glfwSetMouseButtonCallback(window, MouseButtonCallback);
    glfwSetCursorPosCallback(window, CursorPosCallback);

    CTTR::Renderer::DebugDraw debugDraw;
    debugDraw.Initialize();

    // ── 2. Initialize Input Source ─────────────────────────────────────────
    CTTR::Game::KeyboardInput::Bindings bindings{};
    bindings.throttle[0] = GLFW_KEY_W;
    bindings.throttle[1] = GLFW_KEY_UP;
    bindings.brake[0]    = GLFW_KEY_S;
    bindings.brake[1]    = GLFW_KEY_DOWN;
    bindings.left[0]     = GLFW_KEY_A;
    bindings.left[1]     = GLFW_KEY_LEFT;
    bindings.right[0]    = GLFW_KEY_D;
    bindings.right[1]    = GLFW_KEY_RIGHT;
    bindings.powerslide  = GLFW_KEY_SPACE;
    bindings.turbo       = GLFW_KEY_LEFT_SHIFT;

    g_KeyboardInput = std::make_unique<CTTR::Game::KeyboardInput>(g_KeysPressed, 1024, bindings);
    if (driveDuration > 0.0f) {
        // Convert drive duration in seconds to 60Hz tick count
        g_KeyboardInput->SetForcedThrottleTicks(static_cast<int>(std::round(driveDuration * 60.0f)));
    }

    // ── 3. Load Level & Track World ────────────────────────────────────────
    if (!g_World.LoadLevel(gameDataPath, levelName, renderer)) {
        std::cerr << "[main] Failed to load level: " << levelName << std::endl;
        return 1;
    }

    if (driveDuration > 0.0f) {
        g_World.GetRaceController().StartRaceImmediate();
    }

    // Diagnostic mesh listing if requested
    if (!listMeshesFilter.empty()) {
        std::cout << "\n--- Track Mesh Inventory (Filter: '" << listMeshesFilter << "') ---" << std::endl;
        for (const auto& m : g_World.GetTrackMeshes()) {
            if (listMeshesFilter == "*" || m.name.find(listMeshesFilter) != std::string::npos ||
                m.shaderName.find(listMeshesFilter) != std::string::npos) {
                std::cout << "Mesh: " << m.name << " | Shader: " << m.shaderName 
                          << " | BlendMode: " << static_cast<int>(m.blendMode)
                          << " | UVScroll: (" << m.uvScroll.x << ", " << m.uvScroll.y << ")"
                          << " | Verts: " << m.vertices.size() << " | Trs: " << m.indices.size() / 3 << std::endl;
            }
        }
        std::cout << "---------------------------------------------------------\n" << std::endl;
    }

    // ── 4. Load Player Kart & Driver Character ─────────────────────────────
    if (!g_World.LoadPlayerKart(gameDataPath, renderer)) {
        std::cerr << "[main] Warning: Player kart could not be fully loaded." << std::endl;
    }

    // ── 5. Spawn 7 AI Opponents for Full 8-Kart Starting Grid ──────────────
    g_World.SpawnOpponents(7);

    auto* playerVehicle = g_World.GetPlayerVehicle();
    if (playerVehicle) {
        auto pos = playerVehicle->GetPosition();
        auto rot = playerVehicle->GetRotation();
        g_ChaseCamera.Reset({pos.x, pos.y, pos.z}, rot.y);
    }

    // Initialize Camera Modes
    if (forceFreeCam) {
        g_CameraMode = CameraMode::FREE_CAM;
        if (g_KeyboardInput) g_KeyboardInput->SetEnabled(false);
        if (initialPreset > 0) {
            SetCameraPreset(initialPreset);
        } else if (initialPreset == 0) {
            g_Camera.FocusOnBounds(g_World.GetSceneMin(), g_World.GetSceneMax());
        } else {
            g_Camera.UpdateCameraVectors();
        }
    } else {
        g_CameraMode = CameraMode::CHASE_CAM;
    }

    std::cout << "\n=========================================================" << std::endl;
    std::cout << "CONTROLS:" << std::endl;
    std::cout << "  W / Up Arrow      : Throttle (Forward Acceleration)" << std::endl;
    std::cout << "  S / Down Arrow    : Brake / Reverse" << std::endl;
    std::cout << "  A / Left Arrow    : Steer Left" << std::endl;
    std::cout << "  D / Right Arrow   : Steer Right" << std::endl;
    std::cout << "  Space (Hold)      : Drift / Powerslide (Builds Turbo Boost!)" << std::endl;
    std::cout << "  Left Shift        : Fire Turbo Boost!" << std::endl;
    std::cout << "  R                 : Reset Kart to Starting Grid" << std::endl;
    std::cout << "  Tab               : Toggle Between Driveable Chase Cam and Free Fly Cam" << std::endl;
    std::cout << "  1 / 2 / 3 / 4     : Camera Bookmarks (1: Overview, 2: Start Line, 3: Statues, 4: Castle)" << std::endl;
    std::cout << "  Mouse Drag        : Orbit / Look Around" << std::endl;
    std::cout << "  F1                : Toggle Wireframe Mode" << std::endl;
    std::cout << "  Esc               : Exit Program" << std::endl;
    std::cout << "=========================================================\n" << std::endl;

    glm::vec3 minBounds = g_World.GetSceneMin();
    glm::vec3 maxBounds = g_World.GetSceneMax();
    float sceneSpan = glm::length(maxBounds - minBounds);
    float farPlane = std::clamp(sceneSpan * 2.0f, 5000.0f, 25000.0f);
    glm::mat4 proj = glm::perspective(glm::radians(45.0f), 1280.0f / 720.0f, 1.5f, farPlane);

    double prevTime = glfwGetTime();
    double lastTitleUpdate = 0.0;
    float physicsAccumulator = 0.0f;

    // ── Main Simulation & Render Loop ─────────────────────────────────────
    while (!renderer.ShouldClose())
    {
        double now = glfwGetTime();
        float dt = static_cast<float>(now - prevTime);
        prevTime = now;

        if (dt > 0.1f) dt = 0.1f;

        // In Free Cam, WASD/QE controls camera movement
        if (g_CameraMode == CameraMode::FREE_CAM) {
            bool shiftPressed = g_KeysPressed[GLFW_KEY_LEFT_SHIFT] || g_KeysPressed[GLFW_KEY_RIGHT_SHIFT];
            for (int key = 0; key < 1024; ++key) {
                if (g_KeysPressed[key]) {
                    g_Camera.ProcessKeyboard(key, dt, shiftPressed);
                }
            }
        }

        // Fixed 60Hz physics timestep integration for bit-exact determinism
        const float FIXED_DT = 1.0f / 60.0f;
        physicsAccumulator += dt;
        while (physicsAccumulator >= FIXED_DT) {
            physicsAccumulator -= FIXED_DT;

            CTTR::Game::DriverInput input{};
            if (g_KeyboardInput) {
                input = g_KeyboardInput->Poll();
            }

            g_World.StepPhysics(FIXED_DT, input);
        }

        // ── 5. Camera Update ───────────────────────────────────────────────
        playerVehicle = g_World.GetPlayerVehicle();
        if (playerVehicle && g_CameraMode == CameraMode::CHASE_CAM) {
            auto vPos = playerVehicle->GetPosition();
            auto vRot = playerVehicle->GetRotation();
            g_ChaseCamera.Update({vPos.x, vPos.y, vPos.z}, vRot.y, playerVehicle->GetSpeedKPH(), dt);
        }

        glm::mat4 view = (g_CameraMode == CameraMode::CHASE_CAM) 
                         ? g_ChaseCamera.GetViewMatrix() 
                         : g_Camera.GetViewMatrix();

        // ── 6. Render Frame ────────────────────────────────────────────────
        renderer.Clear();
        debugDraw.BeginFrame();

        g_World.Render(renderer, view, proj, static_cast<float>(now));

        debugDraw.Render(view, proj);
        renderer.SwapBuffers();

        // ── 7. Real-time Dashboard in Window Title ────────────────────────
        if (now - lastTitleUpdate > 0.05) {
            lastTitleUpdate = now;
            char titleBuffer[256];
            if (g_CameraMode == CameraMode::CHASE_CAM && playerVehicle) {
                const auto* raceProg = g_World.GetRaceController().GetProgress(0);
                auto raceState = g_World.GetRaceController().GetState();
                char raceStatus[80] = "";
                if (raceState == CTTR::Game::RaceState::Countdown) {
                    int num = g_World.GetRaceController().GetCountdownDisplayNumber();
                    if (num > 0) {
                        std::snprintf(raceStatus, sizeof(raceStatus), "COUNTDOWN: %d | ", num);
                    } else {
                        std::snprintf(raceStatus, sizeof(raceStatus), "GO! | ");
                    }
                } else if (raceProg) {
                    if (raceProg->finished) {
                        std::snprintf(raceStatus, sizeof(raceStatus), "FINISHED P%d! Time: %.2fs | ", 
                            raceProg->finishPosition, raceProg->totalRaceTime);
                    } else {
                        std::snprintf(raceStatus, sizeof(raceStatus), "P%d Lap %d/%d (%.1fs) | ", 
                            raceProg->currentPosition, raceProg->currentLap, g_World.GetRaceController().GetTotalLaps(), raceProg->currentLapTime);
                    }
                }
                std::snprintf(titleBuffer, sizeof(titleBuffer),
                    "CTTR Online | %s%.1f km/h | Turbo: %.0f%% %s | [Tab: FreeCam] [R: Reset]",
                    raceStatus,
                    playerVehicle->GetSpeedKPH(),
                    playerVehicle->GetTurboAmount(),
                    (playerVehicle->IsPowersliding() ? "[DRIFT!]" : (playerVehicle->IsTurboActive() ? "[BOOST!]" : "")));
            } else {
                std::snprintf(titleBuffer, sizeof(titleBuffer),
                    "CTTR Online [FREE CAM] | Pos: (%.1f, %.1f, %.1f) | [Tab: ChaseCam] [1-4: Presets] [F1: Wireframe]",
                    g_Camera.position.x, g_Camera.position.y, g_Camera.position.z);
            }
            glfwSetWindowTitle(window, titleBuffer);
        }

        // ── 8. Headless / Test Screenshot Capture ─────────────────────────
        if (!screenshotPath.empty()) {
            static int frameCount = 0;
            frameCount++;
            bool forceDone = !g_KeyboardInput || !g_KeyboardInput->IsForcingThrottle();
            if (forceDone && frameCount >= 10) {
                std::vector<unsigned char> pixels(1280 * 720 * 3);
                glReadPixels(0, 0, 1280, 720, GL_RGB, GL_UNSIGNED_BYTE, pixels.data());
                std::ofstream out(screenshotPath, std::ios::binary);
                if (out.is_open()) {
                    out << "P6\n1280 720\n255\n";
                    for (int y = 719; y >= 0; --y) {
                        out.write(reinterpret_cast<const char*>(pixels.data() + y * 1280 * 3), 1280 * 3);
                    }
                    out.close();
                    std::cout << "[main] Saved screenshot to " << screenshotPath << std::endl;
                }
                break;
            }
        }
    }

    renderer.Shutdown();
    return 0;
}
