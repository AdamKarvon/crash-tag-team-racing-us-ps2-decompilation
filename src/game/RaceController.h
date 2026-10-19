#pragma once
// =============================================================================
//  RaceController.h  —  Lap tracking, checkpoint gates, race timing & standings
//  Handles race state machine, start countdown, turbo start, and anti-cut checks
// =============================================================================

#include <vector>
#include <string>
#include <memory>
#include <glm/glm.hpp>

#include "TrackSpline.h"

namespace CTTR {
namespace Game {

    enum class RaceState {
        PreRace,
        Countdown,
        Racing,
        Finished
    };

    struct Checkpoint {
        size_t index = 0;
        glm::vec3 center{0.0f};
        glm::vec3 normal{0.0f};  // Unit forward track tangent
        glm::vec3 right{0.0f};   // Unit lateral direction
        float width = 0.0f;      // Road width in metres
        float arcLength = 0.0f;  // Arc length along track s
    };

    struct VehicleRaceProgress {
        int vehicleId = 0;
        int currentLap = 1;              // 1-based: 1, 2, 3
        int nextCheckpoint = 0;          // Next expected checkpoint index [0 .. N-1]
        int checkpointsPassedInLap = 0;
        bool hasStartedRace = false;     // True once start line crossed
        bool finished = false;
        int finishPosition = 0;

        float currentLapTime = 0.0f;
        float bestLapTime = 0.0f;
        float lastLapTime = 0.0f;
        float totalRaceTime = 0.0f;

        float arcLength = 0.0f;          // Current position along track [0 .. totalLength]
        float lateralOffset = 0.0f;
        float overallProgress = 0.0f;    // Scalar for continuous position sorting
        int currentPosition = 1;         // 1st, 2nd, ..., 8th
        
        bool turboStartTriggered = false; // Set if throttle was pressed during "1" countdown
        bool turboAwarded = false;        // Consumed once race begins
    };

    class RaceController {
    public:
        RaceController() = default;

        // Initialize checkpoints along the given TrackSpline
        void Initialize(const TrackSpline* spline, int numCheckpoints = 32, int totalLaps = 3);

        // Register a vehicle ID for race tracking
        void RegisterVehicle(int vehicleId);

        // Start countdown sequence (3, 2, 1, GO!)
        void StartCountdown();

        // Skip directly to racing (e.g. time trials or debug)
        void StartRaceImmediate();

        // Fixed 60Hz tick
        void Update(float dt, 
                    const std::vector<std::pair<int, glm::vec3>>& vehiclePositions, 
                    const std::vector<std::pair<int, bool>>& vehicleThrottles);

        // State queries
        RaceState GetState() const { return m_state; }
        float GetCountdownTimer() const { return m_countdownTimer; }
        int GetCountdownDisplayNumber() const; // 3, 2, 1, 0 (GO!)
        int GetTotalLaps() const { return m_totalLaps; }
        float GetRaceTime() const { return m_raceTimer; }
        size_t GetCheckpointCount() const { return m_checkpoints.size(); }
        const std::vector<Checkpoint>& GetCheckpoints() const { return m_checkpoints; }

        // Vehicle queries
        const VehicleRaceProgress* GetProgress(int vehicleId) const;
        VehicleRaceProgress* GetProgressMut(int vehicleId);
        const std::vector<VehicleRaceProgress>& GetAllProgress() const { return m_progress; }
        bool HasVehicleFinished(int vehicleId) const;
        bool ConsumeTurboStart(int vehicleId);

    private:
        void UpdateVehicle(VehicleRaceProgress& prog, const glm::vec3& worldPos, bool throttlePressed, float dt);
        void UpdateStandings();

        const TrackSpline* m_spline = nullptr;
        int m_totalLaps = 3;
        RaceState m_state = RaceState::PreRace;
        float m_countdownTimer = 3.0f; // 3.0s total countdown
        float m_raceTimer = 0.0f;
        int m_nextFinishPosition = 1;

        std::vector<Checkpoint> m_checkpoints;
        std::vector<VehicleRaceProgress> m_progress;
    };

} // namespace Game
} // namespace CTTR
