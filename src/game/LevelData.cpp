#include "LevelData.h"
#include "asset/god_parser.h"
#include <iostream>
#include <filesystem>
#include <cmath>
#include <glm/gtc/constants.hpp>

namespace fs = std::filesystem;

namespace CTTR {
namespace Game {

    bool LevelData::Load(const std::string& levelName, const std::string& gameDataDir) {
        m_levelName = levelName;
        m_spawnPoints.clear();
        m_cameraPresets.clear();

        std::vector<std::string> candidatePaths = {
            gameDataDir + "/adefault/default/design/levels/" + levelName + "/objects.god",
            gameDataDir + "/design/levels/" + levelName + "/objects.god",
            gameDataDir + "/fairy/" + levelName + "/design/levels/" + levelName + "/objects.god",
            gameDataDir + "/" + levelName + "/" + levelName + "/design/levels/" + levelName + "/objects.god"
        };

        std::string objectsGodPath;
        for (const auto& path : candidatePaths) {
            if (fs::exists(path)) {
                objectsGodPath = path;
                break;
            }
        }

        if (objectsGodPath.empty()) {
            std::cerr << "[LevelData] Warning: Could not locate objects.god for level '" 
                      << levelName << "'. Using default spawn point." << std::endl;
            SpawnPoint sp0{0, "CarLocation0", glm::vec3(-6.36f, -9.87f, 205.73f), glm::pi<float>()};
            m_spawnPoints.push_back(sp0);
            InitializeDefaultPresets();
            m_loaded = false;
            return false;
        }

        std::cout << "[LevelData] Parsing level objects from: " << objectsGodPath << std::endl;
        Asset::GodParser parser;
        Asset::GodScript script;
        if (!parser.ParseFile(objectsGodPath, script)) {
            std::cerr << "[LevelData] Failed to parse objects.god: " << parser.GetLastError() << std::endl;
            SpawnPoint sp0{0, "CarLocation0", glm::vec3(-6.36f, -9.87f, 205.73f), glm::pi<float>()};
            m_spawnPoints.push_back(sp0);
            InitializeDefaultPresets();
            m_loaded = false;
            return false;
        }

        for (int i = 0; i < 8; ++i) {
            std::string locName = "CarLocation" + std::to_string(i);
            auto locObj = script.FindObject(locName);
            if (!locObj) continue;

            glm::vec3 ps2Pos = locObj->GetPosition();
            // Coordinate conversion: PS2 (left-handed) -> OpenGL (right-handed, Z negated)
            glm::vec3 glPos(ps2Pos.x, ps2Pos.y, -ps2Pos.z);

            // Quaternion extraction:
            // Radical SetQuaternion(w, x, y, z) maps to GodObject m_quaternion:
            // q.x = w, q.y = x_rot, q.z = y_rot, q.w = z_rot
            glm::vec4 q = locObj->GetQuaternion();
            float w = q.x;
            float yRot = q.z;

            float glYaw = glm::pi<float>();
            if (std::abs(w) > 1e-4f || std::abs(yRot) > 1e-4f) {
                float ps2Yaw = 2.0f * std::atan2(yRot, w);
                glYaw = glm::pi<float>() - ps2Yaw;
            }

            // Normalize yaw to [-pi, pi]
            while (glYaw > glm::pi<float>())  glYaw -= glm::two_pi<float>();
            while (glYaw < -glm::pi<float>()) glYaw += glm::two_pi<float>();

            SpawnPoint sp;
            sp.index = i;
            sp.name = locName;
            sp.position = glPos;
            sp.yaw = glYaw;
            m_spawnPoints.push_back(sp);

            std::cout << "[LevelData] Registered " << locName 
                      << " -> Pos: (" << glPos.x << ", " << glPos.y << ", " << glPos.z 
                      << "), Yaw: " << glYaw << " rad (" << glm::degrees(glYaw) << " deg)" << std::endl;
        }

        if (m_spawnPoints.empty()) {
            std::cerr << "[LevelData] Warning: No CarLocation objects found. Using default." << std::endl;
            SpawnPoint sp0{0, "CarLocation0", glm::vec3(-6.36f, -9.87f, 205.73f), glm::pi<float>()};
            m_spawnPoints.push_back(sp0);
        }

        InitializeDefaultPresets();
        m_loaded = true;
        return true;
    }

    const SpawnPoint* LevelData::GetSpawnPoint(int index) const {
        for (const auto& sp : m_spawnPoints) {
            if (sp.index == index) return &sp;
        }
        return m_spawnPoints.empty() ? nullptr : &m_spawnPoints[0];
    }

    const CameraPreset* LevelData::GetCameraPreset(int id) const {
        for (const auto& cp : m_cameraPresets) {
            if (cp.id == id) return &cp;
        }
        return nullptr;
    }

    void LevelData::InitializeDefaultPresets() {
        m_cameraPresets.clear();

        // Preset 1: Full Level Overview (dynamically updated by scene bounds)
        m_cameraPresets.push_back({1, "Full Level Overview", glm::vec3(0.0f, 100.0f, 0.0f), -45.0f, 90.0f});

        // Preset 2: Starting Grid (CarLocation0)
        glm::vec3 gridCamPos = glm::vec3(-6.36f, -2.0f, 175.0f);
        if (!m_spawnPoints.empty()) {
            gridCamPos = m_spawnPoints[0].position + glm::vec3(0.0f, 7.87f, -30.73f);
        }
        m_cameraPresets.push_back({2, "Starting Grid", gridCamPos, -5.0f, 90.0f});

        // Preset 3: Prince & Princess Statues Section
        m_cameraPresets.push_back({3, "Prince & Princess Statues", glm::vec3(5.0f, -2.0f, 410.0f), -5.0f, 90.0f});

        // Preset 4: Castle Courtyard & Glass Slipper (Grand Ballroom)
        m_cameraPresets.push_back({4, "Castle Courtyard & Glass Slipper", glm::vec3(440.0f, 44.0f, -430.0f), -6.0f, -90.0f});
    }

} // namespace Game
} // namespace CTTR
