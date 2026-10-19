#pragma once
#include "object_registry.h"
#include <string>
#include <vector>
#include <memory>

namespace CTTR {
namespace Asset {

    struct LevelLoadConfig {
        std::string levelName = "fairy1";
        std::string language = "english";
        std::string skillLevel = "Medium";
        bool isMultiplayer = false;
        bool isPAL = false;
        bool isJapanese = false;
        bool isLowPoly = false;
        int numPlayers = 1;
        int numAI = 7;
        std::string playerVehicle = "CrashStang";
        std::string playerCharacter = "Crash";
    };

    struct StepLog {
        int stepNumber;
        std::string description;
        std::string loadSetName;
        std::string assetPath;
        bool success;
    };

    class LevelLoader {
    public:
        LevelLoader() = default;
        ~LevelLoader() = default;

        // Executes the deterministic 37-step LevelInitialize pipeline
        bool InitializeLevel(const LevelLoadConfig& config);

        // Executes the reverse LevelTerminate unload sequence
        bool TerminateLevel();

        // Level restart logic (ResetLevel)
        bool ResetLevel();

        const std::vector<StepLog>& GetExecutionLog() const { return m_executionLog; }
        const std::string& GetCurrentTheme() const { return m_currentTheme; }
        const std::string& GetCurrentLevel() const { return m_currentLevel; }
        bool IsLevelLoaded() const { return m_isLoaded; }

    private:
        void LogStep(int stepNum, const std::string& desc, const std::string& loadSet = "", const std::string& asset = "", bool success = true);

        std::string m_currentLevel;
        std::string m_currentTheme;
        LevelLoadConfig m_config;
        bool m_isLoaded = false;
        std::vector<StepLog> m_executionLog;
    };

} // namespace Asset
} // namespace CTTR
