#pragma once
// =============================================================================
//  LevelData.h  —  Level configuration and spawn extraction
//  Extracts CarLocation0..7, cameras and level parameters from objects.god
// =============================================================================

#include <string>
#include <vector>
#include <glm/glm.hpp>

namespace CTTR {
namespace Game {

    struct SpawnPoint {
        int index = 0;
        std::string name;
        glm::vec3 position{0.0f}; // In OpenGL coordinates (Z negated from PS2)
        float yaw = 3.14159265f;  // In OpenGL radians (facing along track)
    };

    struct CameraPreset {
        int id = 1;
        std::string name;
        glm::vec3 position{0.0f};
        float pitch = 0.0f;
        float yaw = 0.0f;
    };

    class LevelData {
    public:
        LevelData() = default;

        // Loads and parses level configuration from objects.god
        bool Load(const std::string& levelName, const std::string& gameDataDir);

        const std::vector<SpawnPoint>& GetSpawnPoints() const { return m_spawnPoints; }
        const SpawnPoint* GetSpawnPoint(int index) const;

        const std::vector<CameraPreset>& GetCameraPresets() const { return m_cameraPresets; }
        const CameraPreset* GetCameraPreset(int id) const;

        const std::string& GetLevelName() const { return m_levelName; }
        bool IsLoaded() const { return m_loaded; }

    private:
        std::string m_levelName;
        bool m_loaded = false;
        std::vector<SpawnPoint> m_spawnPoints;
        std::vector<CameraPreset> m_cameraPresets;

        void InitializeDefaultPresets();
    };

} // namespace Game
} // namespace CTTR
