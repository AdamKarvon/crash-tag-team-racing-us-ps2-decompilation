#pragma once
// =============================================================================
//  World.h  —  Game World & Simulation Manager
//  Encapsulates track geometry, spatial collision grid, karts and drivers.
// =============================================================================

#include <vector>
#include <memory>
#include <string>
#include <glm/glm.hpp>

#include "renderer/GLRenderer.h"
#include "renderer/P3DLoader.h"
#include "physics/Vehicle.h"
#include "physics/TrackCollider.h"
#include "game/GroundContact.h"
#include "game/InputSource.h"
#include "game/LevelData.h"
#include "game/TrackSpline.h"
#include "game/RaceController.h"
#include "ai/AIDriver.h"
#include <array>

namespace CTTR {
namespace Game {

    struct KartVisuals {
        std::vector<Renderer::P3DMesh> chassisMeshes;
        std::vector<Renderer::P3DMesh> wheelMeshes[4]; // 0: FR, 1: FL, 2: RL, 3: RR
        std::vector<Renderer::P3DMesh> driverMeshes;
        float wheelSpinAngles[4] = {0.0f, 0.0f, 0.0f, 0.0f};
    };

    class World {
    public:
        World();
        ~World();

        // Loads track archives and builds spatial grid collider
        bool LoadLevel(const std::string& gameDataPath, const std::string& levelName, Renderer::GLRenderer& renderer);

        // Loads player kart (CrashStang) and driver (Crash Bandicoot)
        bool LoadPlayerKart(const std::string& gameDataPath, Renderer::GLRenderer& renderer);

        // Spawns AI opponent karts (default: 7 opponents for full 8-kart grid)
        bool SpawnOpponents(int numOpponents = 7);

        // Resets vehicle position to the specified spawn point index (default: CarLocation0)
        void ResetVehicle(size_t vehicleIndex, int spawnIndex = 0);

        // Fixed 60Hz physics step
        void StepPhysics(float fixedDt, const DriverInput& playerInput);

        // Renders all world geometry and vehicles
        void Render(Renderer::GLRenderer& renderer, const glm::mat4& view, const glm::mat4& proj, float currentTime);

        // Accessors
        Physics::Vehicle* GetPlayerVehicle() const;
        size_t GetVehicleCount() const { return m_vehicles.size(); }
        Physics::Vehicle* GetVehicle(size_t index) const;

        const Physics::TrackCollider& GetCollider() const { return m_collider; }
        const LevelData& GetLevelData() const { return m_levelData; }
        const std::vector<Renderer::P3DMesh>& GetTrackMeshes() const { return m_trackMeshes; }
        const TrackSpline& GetTrackSpline() const { return m_trackSpline; }
        TrackSpline& GetTrackSpline() { return m_trackSpline; }
        const RaceController& GetRaceController() const { return m_raceController; }
        RaceController& GetRaceController() { return m_raceController; }

        glm::vec3 GetSceneMin() const { return m_sceneMin; }
        glm::vec3 GetSceneMax() const { return m_sceneMax; }

    private:
        std::string m_gameDataPath;
        std::string m_levelName;

        LevelData m_levelData;
        Physics::TrackCollider m_collider;
        TrackSpline m_trackSpline;
        RaceController m_raceController;
        std::vector<Renderer::P3DMesh> m_trackMeshes;

        glm::vec3 m_sceneMin{0.0f};
        glm::vec3 m_sceneMax{0.0f};

        std::vector<std::unique_ptr<Physics::Vehicle>> m_vehicles;
        std::vector<std::unique_ptr<AI::AIDriver>> m_aiDrivers;
        std::vector<std::array<float, 4>> m_wheelSpinAngles;
        KartVisuals m_playerKartVisuals;
    };

} // namespace Game
} // namespace CTTR
