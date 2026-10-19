#pragma once
// =============================================================================
//  AIDriver.h  —  Autonomous AI Driver for CTTR Opponents
//  Pure-pursuit steering, cornering, powerslide drift & rubber-banding
// =============================================================================

#include <glm/glm.hpp>
#include "AIMotifs.h"
#include "game/TrackSpline.h"
#include "game/InputSource.h"
#include "physics/Vehicle.h"

namespace CTTR {
namespace AI {

    class AIDriver : public Game::InputSource {
    public:
        AIDriver(int vehicleId, const AIPersonality& personality);
        ~AIDriver() override = default;

        // Computes driver inputs for this fixed tick
        Game::DriverInput ComputeInput(const Physics::Vehicle& vehicle, 
                                       const Game::TrackSpline& spline, 
                                       float playerOverallProgress);

        Game::DriverInput Poll() override { return m_lastInput; }

        int GetVehicleId() const { return m_vehicleId; }
        const AIPersonality& GetPersonality() const { return m_personality; }
        void SetPersonality(const AIPersonality& p) { m_personality = p; }

    private:
        int m_vehicleId;
        AIPersonality m_personality;
        Game::DriverInput m_lastInput{};
        float m_driftCooldown = 0.0f;
    };

} // namespace AI
} // namespace CTTR
