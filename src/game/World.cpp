#include "World.h"
#include "physics/KartContacts.h"
#include "ai/AIMotifs.h"
#include <iostream>
#include <filesystem>
#include <algorithm>
#include <limits>
#include <cmath>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/constants.hpp>

namespace fs = std::filesystem;

namespace CTTR {
namespace Game {

    namespace {
        void ApplySkeletalDriverPose(Renderer::P3DMesh& mesh, const Renderer::P3DSkeleton& skel) {
            if (skel.bones.empty()) return;

            // 1. World bind matrices in GL coordinate space (negating Z for coordinate conversion)
            std::vector<glm::mat4> worldBind(skel.bones.size(), glm::mat4(1.0f));
            for (size_t i = 0; i < skel.bones.size(); ++i) {
                glm::mat4 local = skel.bones[i].localBindTransform;
                local[3][2] = -local[3][2];
                local[0][2] = -local[0][2];
                local[1][2] = -local[1][2];
                local[2][0] = -local[2][0];
                local[2][1] = -local[2][1];

                if (skel.bones[i].parentIndex >= 0 && skel.bones[i].parentIndex < static_cast<int>(i)) {
                    worldBind[i] = worldBind[skel.bones[i].parentIndex] * local;
                } else {
                    worldBind[i] = local;
                }
            }

            // 2. Local posed transforms
            std::vector<glm::mat4> localPosed(skel.bones.size());
            for (size_t i = 0; i < skel.bones.size(); ++i) {
                glm::mat4 local = skel.bones[i].localBindTransform;
                local[3][2] = -local[3][2];
                local[0][2] = -local[0][2];
                local[1][2] = -local[1][2];
                local[2][0] = -local[2][0];
                local[2][1] = -local[2][1];
                localPosed[i] = local;
            }

            // 3. Driver articulation:
            // Left arm reaching forward to hold left side of steering wheel:
            glm::mat4 rotShoulderL = glm::rotate(glm::mat4(1.0f), glm::radians(-72.0f), glm::vec3(0.0f, 1.0f, 0.0f));
            rotShoulderL = glm::rotate(rotShoulderL, glm::radians(-32.0f), glm::vec3(1.0f, 0.0f, 0.0f));
            rotShoulderL = glm::rotate(rotShoulderL, glm::radians(-12.0f), glm::vec3(0.0f, 0.0f, 1.0f));
            localPosed[35] = localPosed[35] * rotShoulderL;

            glm::mat4 rotElbowL = glm::rotate(glm::mat4(1.0f), glm::radians(-55.0f), glm::vec3(0.0f, 1.0f, 0.0f));
            rotElbowL = glm::rotate(rotElbowL, glm::radians(10.0f), glm::vec3(1.0f, 0.0f, 0.0f));
            localPosed[36] = localPosed[36] * rotElbowL;

            // Right arm reaching forward symmetrically to hold right side of steering wheel:
            glm::mat4 rotShoulderR = glm::rotate(glm::mat4(1.0f), glm::radians(-20.0f), glm::vec3(0.0f, 1.0f, 0.0f));
            rotShoulderR = glm::rotate(rotShoulderR, glm::radians(-120.0f), glm::vec3(1.0f, 0.0f, 0.0f));
            rotShoulderR = glm::rotate(rotShoulderR, glm::radians(-90.0f), glm::vec3(0.0f, 0.0f, 1.0f));
            localPosed[41] = localPosed[41] * rotShoulderR;

            glm::mat4 rotElbowR = glm::rotate(glm::mat4(1.0f), glm::radians(40.0f), glm::vec3(0.0f, 1.0f, 0.0f));
            localPosed[42] = localPosed[42] * rotElbowR;

            // Legs positioned forward into cockpit footwell / pedals
            glm::mat4 rotHipL = glm::rotate(glm::mat4(1.0f), glm::radians(-82.0f), glm::vec3(1.0f, 0.0f, 0.0f));
            rotHipL = glm::rotate(rotHipL, glm::radians(6.0f), glm::vec3(0.0f, 1.0f, 0.0f));
            localPosed[21] = localPosed[21] * rotHipL;

            glm::mat4 rotKneeL = glm::rotate(glm::mat4(1.0f), glm::radians(80.0f), glm::vec3(1.0f, 0.0f, 0.0f));
            localPosed[22] = localPosed[22] * rotKneeL;

            glm::mat4 rotHipR = glm::rotate(glm::mat4(1.0f), glm::radians(-82.0f), glm::vec3(1.0f, 0.0f, 0.0f));
            rotHipR = glm::rotate(rotHipR, glm::radians(-6.0f), glm::vec3(0.0f, 1.0f, 0.0f));
            localPosed[24] = localPosed[24] * rotHipR;

            glm::mat4 rotKneeR = glm::rotate(glm::mat4(1.0f), glm::radians(80.0f), glm::vec3(1.0f, 0.0f, 0.0f));
            localPosed[25] = localPosed[25] * rotKneeR;

            // Torso / Spine natural driving lean into bucket seat
            localPosed[27] = localPosed[27] * glm::rotate(glm::mat4(1.0f), glm::radians(-8.0f), glm::vec3(1.0f, 0.0f, 0.0f));

            // Head / Neck looking slightly up & alert through windshield
            localPosed[28] = localPosed[28] * glm::rotate(glm::mat4(1.0f), glm::radians(6.0f), glm::vec3(1.0f, 0.0f, 0.0f));

            // 4. World posed transforms
            std::vector<glm::mat4> worldPosed(skel.bones.size(), glm::mat4(1.0f));
            for (size_t i = 0; i < skel.bones.size(); ++i) {
                if (skel.bones[i].parentIndex >= 0 && skel.bones[i].parentIndex < static_cast<int>(i)) {
                    worldPosed[i] = worldPosed[skel.bones[i].parentIndex] * localPosed[i];
                } else {
                    worldPosed[i] = localPosed[i];
                }
            }

            // 5. Skinning transformation matrices
            std::vector<glm::mat4> skinMatrices(skel.bones.size());
            for (size_t i = 0; i < skel.bones.size(); ++i) {
                skinMatrices[i] = worldPosed[i] * glm::inverse(worldBind[i]);
            }

            // 6. Deform mesh vertices rigidly using bone indices
            for (auto& v : mesh.vertices) {
                int bIdx = v.boneIndices.x;
                if (bIdx >= 0 && bIdx < static_cast<int>(skel.bones.size())) {
                    glm::vec4 p4 = skinMatrices[bIdx] * glm::vec4(v.position, 1.0f);
                    v.position = glm::vec3(p4);
                }
            }
        }
    }

    World::World() {}
    World::~World() {}

    bool World::LoadLevel(const std::string& gameDataPath, const std::string& levelName, Renderer::GLRenderer& renderer) {
        m_gameDataPath = gameDataPath;
        m_levelName = levelName;
        m_trackMeshes.clear();

        // 1. Load Level Data (CarLocations & Presets) from objects.god
        m_levelData.Load(levelName, gameDataPath);

        // 2. Select track archive to load world-space track geometry.
        // NOTE: props.p3d contains local-space archetype models (prince & princess, sheep targets, roadblocks, etc.)
        // which must NOT be added to m_trackMeshes directly at identity transform (0,0,0) or they will cluster under the track.
        std::vector<std::vector<std::string>> archiveCategories = {
            {
                gameDataPath + "/fairy/" + levelName + "/art/levels/" + levelName + "/track_english.p3d",
                gameDataPath + "/adefault/default/art/levels/" + levelName + "/track_english.p3d",
                gameDataPath + "/art/levels/" + levelName + "/track_english.p3d"
            }
        };

        std::vector<std::string> chosenArchives;
        for (const auto& cat : archiveCategories) {
            for (const auto& path : cat) {
                if (fs::exists(path)) {
                    chosenArchives.push_back(path);
                    break;
                }
            }
        }

        glm::vec3 minBounds(std::numeric_limits<float>::max());
        glm::vec3 maxBounds(std::numeric_limits<float>::lowest());
        size_t loadedArchives = 0;

        Renderer::P3DLoader loader;
        for (const auto& path : chosenArchives) {
            std::cout << "[World] Loading Track P3D: " << path << std::endl;
            if (loader.LoadModel(path)) {
                loadedArchives++;
                auto meshes = loader.GetMeshes();
                for (auto& mesh : meshes) {
                    for (const auto& v : mesh.vertices) {
                        minBounds = glm::min(minBounds, v.position);
                        maxBounds = glm::max(maxBounds, v.position);
                    }
                    if (mesh.blendMode == Renderer::BlendMode::Additive) {
                        std::cout << "[World] Caustic Mesh: '" << mesh.name << "' center: ("
                                  << mesh.boundsCenter.x << ", " << mesh.boundsCenter.y << ", " << mesh.boundsCenter.z << ")" << std::endl;
                    }
                    renderer.BufferMesh(mesh);
                    m_trackMeshes.push_back(mesh);
                }
            }
        }

        m_sceneMin = minBounds;
        m_sceneMax = maxBounds;
        std::cout << "[World] Loaded " << m_trackMeshes.size() << " track meshes from " 
                  << loadedArchives << " archives." << std::endl;

        // 3. Build spatial grid collider
        m_collider.Build(m_trackMeshes);

        // 4. Load road network spline (roads.mid) and initialize RaceController
        std::vector<std::string> roadPaths = {
            gameDataPath + "/adefault/default/design/levels/" + levelName + "/roads.mid",
            gameDataPath + "/design/levels/" + levelName + "/roads.mid",
            gameDataPath + "/fairy/" + levelName + "/design/levels/" + levelName + "/roads.mid"
        };
        for (const auto& rpath : roadPaths) {
            if (fs::exists(rpath)) {
                if (m_trackSpline.LoadFromMid(rpath)) {
                    m_raceController.Initialize(&m_trackSpline, 32, 3);
                    m_raceController.RegisterVehicle(0);
                    m_raceController.StartCountdown();
                    break;
                }
            }
        }

        return !m_trackMeshes.empty();
    }

    bool World::LoadPlayerKart(const std::string& gameDataPath, Renderer::GLRenderer& renderer) {
        m_playerKartVisuals.chassisMeshes.clear();
        for (int i = 0; i < 4; ++i) m_playerKartVisuals.wheelMeshes[i].clear();
        m_playerKartVisuals.driverMeshes.clear();

        // 1. Load Player Kart (CrashStang)
        std::vector<std::string> kartPaths = {
            gameDataPath + "/adefault/default/art/cars/CrashStang/CrashStang.p3d",
            gameDataPath + "/art/cars/CrashStang/CrashStang.p3d"
        };

        Renderer::P3DLoader loader;
        for (const auto& kartPath : kartPaths) {
            if (fs::exists(kartPath)) {
                std::cout << "[World] Loading Player Kart: " << kartPath << std::endl;
                if (loader.LoadModel(kartPath)) {
                    auto rawMeshes = loader.GetMeshes();
                    for (auto& mesh : rawMeshes) {
                        renderer.BufferMesh(mesh);
                        if (mesh.name.find("wShape0") != std::string::npos) {
                            m_playerKartVisuals.wheelMeshes[0].push_back(mesh); // FR
                        } else if (mesh.name.find("wShape1") != std::string::npos) {
                            m_playerKartVisuals.wheelMeshes[1].push_back(mesh); // FL
                        } else if (mesh.name.find("wShape2") != std::string::npos) {
                            m_playerKartVisuals.wheelMeshes[2].push_back(mesh); // RL
                        } else if (mesh.name.find("wShape3") != std::string::npos) {
                            m_playerKartVisuals.wheelMeshes[3].push_back(mesh); // RR
                        } else if (mesh.name.find("chassis_weld") != std::string::npos) {
                            // Skip composite whole-vehicle LOD mesh to prevent Z-fighting
                        } else {
                            m_playerKartVisuals.chassisMeshes.push_back(mesh);
                        }
                    }
                    std::cout << "[World] Buffered CrashStang chassis (" 
                              << m_playerKartVisuals.chassisMeshes.size() << " meshes) and 4 wheels." << std::endl;
                    break;
                }
            }
        }

        // 2. Load Driver Model (Crash Bandicoot)
        std::vector<std::string> driverPaths = {
            gameDataPath + "/fairy/fairy1/art/characters/crash_model.p3d",
            gameDataPath + "/adefault/default/art/characters/crash_model.p3d",
            gameDataPath + "/art/characters/crash_model.p3d"
        };

        for (const auto& driverPath : driverPaths) {
            if (fs::exists(driverPath)) {
                std::cout << "[World] Loading Driver Model: " << driverPath << std::endl;
                if (loader.LoadModel(driverPath)) {
                    m_playerKartVisuals.driverMeshes = loader.GetMeshes();
                    const auto& driverSkel = loader.GetSkeleton();
                    for (auto& mesh : m_playerKartVisuals.driverMeshes) {
                        ApplySkeletalDriverPose(mesh, driverSkel);
                        renderer.BufferMesh(mesh);
                    }
                    std::cout << "[World] Buffered Crash Bandicoot driver model (" 
                              << m_playerKartVisuals.driverMeshes.size() << " meshes)." << std::endl;
                    break;
                }
            }
        }

        // 3. Create Player Vehicle Instance
        if (m_vehicles.empty()) {
            m_vehicles.push_back(std::make_unique<Physics::Vehicle>(0));
        }

        ResetVehicle(0, 0);
        return true;
    }

    bool World::SpawnOpponents(int numOpponents) {
        if (numOpponents <= 0) return true;
        int count = std::min(numOpponents, 7);

        // Ensure player is at index 0
        if (m_vehicles.empty()) {
            m_vehicles.push_back(std::make_unique<Physics::Vehicle>(0));
            ResetVehicle(0, 0);
        }

        m_aiDrivers.clear();

        for (int i = 1; i <= count; ++i) {
            if (m_vehicles.size() <= static_cast<size_t>(i)) {
                m_vehicles.push_back(std::make_unique<Physics::Vehicle>(i));
            }
            ResetVehicle(i, i);
            auto personality = AI::GetDefaultPersonality(i);
            m_aiDrivers.push_back(std::make_unique<AI::AIDriver>(i, personality));
            m_raceController.RegisterVehicle(i);
        }

        m_wheelSpinAngles.resize(m_vehicles.size(), {0.0f, 0.0f, 0.0f, 0.0f});
        std::cout << "[World] Spawned " << m_aiDrivers.size() << " AI opponents. Total grid size: " 
                  << m_vehicles.size() << " karts." << std::endl;
        return true;
    }

    void World::ResetVehicle(size_t vehicleIndex, int spawnIndex) {
        if (vehicleIndex >= m_vehicles.size()) return;
        auto& v = m_vehicles[vehicleIndex];

        const auto* sp = m_levelData.GetSpawnPoint(spawnIndex);
        glm::vec3 spawnPos = sp ? sp->position : glm::vec3(-6.36f, -9.87f, 205.73f);
        float spawnYaw = sp ? sp->yaw : glm::pi<float>();

        // Sample ground height using unified dual-axle ground contact
        auto gc = SampleGroundContact(m_collider, spawnPos, spawnYaw);
        float startY = gc.hit ? gc.y : spawnPos.y;

        v->SetGroundHeight(startY);
        v->SetPosition({spawnPos.x, startY, spawnPos.z});
        v->SetRotation({0.0f, spawnYaw, 0.0f});
        v->SetVelocity({0.0f, 0.0f, 0.0f});

        std::cout << "[World] Reset Vehicle " << vehicleIndex << " to spawn " << spawnIndex 
                  << " (" << spawnPos.x << ", " << startY << ", " << spawnPos.z 
                  << "), Yaw=" << spawnYaw << std::endl;
    }

    void World::StepPhysics(float fixedDt, const DriverInput& playerInput) {
        if (m_vehicles.empty()) return;
        if (m_wheelSpinAngles.size() < m_vehicles.size()) {
            m_wheelSpinAngles.resize(m_vehicles.size(), {0.0f, 0.0f, 0.0f, 0.0f});
        }

        auto raceState = m_raceController.GetState();

        // 1. Process Player Vehicle (index 0)
        auto* playerVeh = m_vehicles[0].get();
        auto pPos = playerVeh->GetPosition();
        auto pRot = playerVeh->GetRotation();
        glm::vec3 playerKartPos(pPos.x, pPos.y, pPos.z);
        float playerKartYaw = pRot.y;

        auto gcPlayer = SampleGroundContact(m_collider, playerKartPos, playerKartYaw);
        float pTargetY = gcPlayer.hit ? gcPlayer.y : pPos.y;
        playerVeh->SetGroundHeight(pTargetY);
        if (pPos.y < pTargetY + 0.05f) {
            pPos.y = pTargetY;
            playerVeh->SetPosition(pPos);
            auto vVel = playerVeh->GetVelocity();
            if (vVel.y < 0.0f) { vVel.y = 0.0f; playerVeh->SetVelocity(vVel); }
        }

        float playerProgress = 0.0f;
        const auto* progPlayer = m_raceController.GetProgress(0);
        if (progPlayer) playerProgress = progPlayer->overallProgress;

        if (raceState == RaceState::Countdown) {
            // Held at grid during countdown, but throttle checked for turbo start
        } else if (raceState == RaceState::Racing || raceState == RaceState::Finished) {
            playerVeh->ApplyInput(playerInput.throttle, playerInput.steering, playerInput.brake);
            playerVeh->SetPowerslideInput(playerInput.powerslide);
            if (playerInput.turbo) playerVeh->ActivateTurbo();
            playerVeh->UpdatePhysics(fixedDt);
        } else {
            playerVeh->ApplyInput(playerInput.throttle, playerInput.steering, playerInput.brake);
            playerVeh->SetPowerslideInput(playerInput.powerslide);
            if (playerInput.turbo) playerVeh->ActivateTurbo();
            playerVeh->UpdatePhysics(fixedDt);
        }

        // 2. Process AI Vehicles (indices 1..N-1)
        for (size_t i = 1; i < m_vehicles.size(); ++i) {
            auto* aiVeh = m_vehicles[i].get();
            auto aiPos = aiVeh->GetPosition();
            auto aiRot = aiVeh->GetRotation();
            glm::vec3 aiKartPos(aiPos.x, aiPos.y, aiPos.z);
            float aiKartYaw = aiRot.y;

            auto gcAI = SampleGroundContact(m_collider, aiKartPos, aiKartYaw);
            float aiTargetY = gcAI.hit ? gcAI.y : aiPos.y;
            aiVeh->SetGroundHeight(aiTargetY);
            if (aiPos.y < aiTargetY + 0.05f) {
                aiPos.y = aiTargetY;
                aiVeh->SetPosition(aiPos);
                auto vVel = aiVeh->GetVelocity();
                if (vVel.y < 0.0f) { vVel.y = 0.0f; aiVeh->SetVelocity(vVel); }
            }

            if (raceState == RaceState::Racing || raceState == RaceState::Finished) {
                if (i - 1 < m_aiDrivers.size()) {
                    auto aiInput = m_aiDrivers[i - 1]->ComputeInput(*aiVeh, m_trackSpline, playerProgress);
                    aiVeh->ApplyInput(aiInput.throttle, aiInput.steering, aiInput.brake);
                    aiVeh->SetPowerslideInput(aiInput.powerslide);
                    if (aiInput.turbo) aiVeh->ActivateTurbo();
                    aiVeh->UpdatePhysics(fixedDt);
                }
            }
        }

        // 3. Resolve Kart-vs-Kart Contacts
        Physics::ResolveKartContacts(m_vehicles, 1.35f, 0.35f);

        // 4. Update RaceController with all vehicle positions and throttles
        std::vector<std::pair<int, glm::vec3>> positions;
        std::vector<std::pair<int, bool>> throttles;
        for (size_t i = 0; i < m_vehicles.size(); ++i) {
            auto pos = m_vehicles[i]->GetPosition();
            positions.push_back({static_cast<int>(i), glm::vec3(pos.x, pos.y, pos.z)});
            bool th = (i == 0) ? (playerInput.throttle > 0.1f) : true;
            throttles.push_back({static_cast<int>(i), th});
        }
        m_raceController.Update(fixedDt, positions, throttles);

        if (raceState == RaceState::Countdown && m_raceController.GetState() == RaceState::Racing) {
            if (m_raceController.ConsumeTurboStart(0)) {
                playerVeh->AddTurbo(100.0f);
                playerVeh->ActivateTurbo();
                std::cout << "[World] TURBO START TRIGGERED!" << std::endl;
            }
        }

        // 5. Update Wheel Spin Angles for all vehicles
        for (size_t vi = 0; vi < m_vehicles.size(); ++vi) {
            for (int w = 0; w < 4; ++w) {
                m_wheelSpinAngles[vi][w] += m_vehicles[vi]->GetWheel(w).angularVelocity * fixedDt;
            }
        }
    }

    void World::Render(Renderer::GLRenderer& renderer, const glm::mat4& view, const glm::mat4& proj, float currentTime) {
        // 1. Render Track & Props (world identity)
        renderer.RenderFrame(m_trackMeshes, view, proj, glm::mat4(1.0f), currentTime);

        // 2. Render All Karts
        if (!m_vehicles.empty() && !m_playerKartVisuals.chassisMeshes.empty()) {
            const glm::vec3 wheelOffsets[4] = {
                glm::vec3( 1.182f, -0.274f, -1.332f), // 0: FR
                glm::vec3(-1.182f, -0.274f, -1.332f), // 1: FL
                glm::vec3(-1.182f, -0.295f,  1.701f), // 2: RL
                glm::vec3( 1.182f, -0.295f,  1.701f)  // 3: RR
            };

            for (size_t vi = 0; vi < m_vehicles.size(); ++vi) {
                auto* v = m_vehicles[vi].get();
                auto vPos = v->GetPosition();
                auto vRot = v->GetRotation();
                glm::vec3 kartPos(vPos.x, vPos.y, vPos.z);
                float kartYaw = vRot.y;

                auto gc = SampleGroundContact(m_collider, kartPos, kartYaw);
                float slopePitch = gc.hit ? gc.pitch : 0.0f;
                if (gc.hit) {
                    kartPos.y = gc.y;
                }

                glm::mat4 kartMatrix = glm::translate(glm::mat4(1.0f), kartPos);
                kartMatrix = glm::rotate(kartMatrix, kartYaw + glm::pi<float>(), glm::vec3(0.0f, 1.0f, 0.0f));
                kartMatrix = glm::rotate(kartMatrix, slopePitch, glm::vec3(1.0f, 0.0f, 0.0f));

                // Chassis
                renderer.RenderFrame(m_playerKartVisuals.chassisMeshes, view, proj, kartMatrix, currentTime);

                // Wheels
                float steerDegrees = v->GetSteeringWheel().currentAngleDegrees;

                for (int w = 0; w < 4; ++w) {
                    if (m_playerKartVisuals.wheelMeshes[w].empty()) continue;
                    glm::mat4 wMat = kartMatrix * glm::translate(glm::mat4(1.0f), wheelOffsets[w]);
                    if (w < 2) {
                        wMat = glm::rotate(wMat, glm::radians(steerDegrees), glm::vec3(0.0f, 1.0f, 0.0f));
                    }
                    float spin = (vi < m_wheelSpinAngles.size()) ? m_wheelSpinAngles[vi][w] : 0.0f;
                    wMat = glm::rotate(wMat, -spin, glm::vec3(1.0f, 0.0f, 0.0f));

                    renderer.RenderFrame(m_playerKartVisuals.wheelMeshes[w], view, proj, wMat, currentTime);
                }

                // Driver Model
                if (!m_playerKartVisuals.driverMeshes.empty()) {
                    glm::mat4 driverMatrix = kartMatrix 
                        * glm::translate(glm::mat4(1.0f), glm::vec3(-0.35f, -0.45f, 0.28f))
                        * glm::rotate(glm::mat4(1.0f), glm::radians(180.0f), glm::vec3(0.0f, 1.0f, 0.0f));

                    renderer.RenderFrame(m_playerKartVisuals.driverMeshes, view, proj, driverMatrix, currentTime);
                }
            }
        }
    }

    Physics::Vehicle* World::GetPlayerVehicle() const {
        return m_vehicles.empty() ? nullptr : m_vehicles[0].get();
    }

    Physics::Vehicle* World::GetVehicle(size_t index) const {
        return index < m_vehicles.size() ? m_vehicles[index].get() : nullptr;
    }

} // namespace Game
} // namespace CTTR
