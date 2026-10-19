// =============================================================================
//  AIDriver.cpp  —  Autonomous AI Driver implementation
// =============================================================================

#include "AIDriver.h"
#include <cmath>
#include <algorithm>
#include <glm/gtc/constants.hpp>

namespace CTTR {
namespace AI {

    AIDriver::AIDriver(int vehicleId, const AIPersonality& personality)
        : m_vehicleId(vehicleId), m_personality(personality) {}

    Game::DriverInput AIDriver::ComputeInput(const Physics::Vehicle& vehicle, 
                                            const Game::TrackSpline& spline, 
                                            float playerOverallProgress) {
        Game::DriverInput input{};
        if (!spline.IsLoaded()) return input;

        auto pos = vehicle.GetPosition();
        auto rot = vehicle.GetRotation();
        float speedKph = vehicle.GetSpeedKPH();
        float speedMs = std::max(speedKph / 3.6f, 5.0f);
        float kartYaw = rot.y;

        // 1. Current track projection
        auto proj = spline.Project(glm::vec3(pos.x, pos.y, pos.z));
        float totalL = spline.GetTotalLength();

        // 2. Look-ahead distance scaled by vehicle speed
        float speedRatio = std::clamp(speedKph / 150.0f, 0.0f, 1.0f);
        float lookAheadTime = glm::mix(m_personality.driving.lookAheadNearSeconds, 
                                       m_personality.driving.lookAheadFarSeconds, 
                                       speedRatio);
        float lookAheadDist = std::max(16.0f, speedMs * lookAheadTime);

        float targetS = proj.arcLength + lookAheadDist;
        while (targetS >= totalL) targetS -= totalL;

        // 3. Target point with individual personality lane offset
        glm::vec3 center = spline.SampleCenterline(targetS);
        glm::vec3 tangent = spline.SampleTangent(targetS);
        glm::vec3 right = glm::cross(tangent, glm::vec3(0.0f, 1.0f, 0.0f));
        if (glm::length(right) > 1e-4f) {
            right = glm::normalize(right);
        } else {
            right = glm::vec3(1.0f, 0.0f, 0.0f);
        }

        float roadWidth = spline.SampleWidth(targetS);
        float targetLane = std::clamp(m_personality.laneOffset, -roadWidth * 0.4f, roadWidth * 0.4f);
        glm::vec3 targetPos = center + right * targetLane;

        // 4. Pure-pursuit steering in local kart coordinates
        glm::vec3 toTarget = targetPos - glm::vec3(pos.x, pos.y, pos.z);
        float cosYaw = std::cos(kartYaw);
        float sinYaw = std::sin(kartYaw);

        float localForward = toTarget.z * cosYaw + toTarget.x * sinYaw;
        float localLateral = -toTarget.z * sinYaw + toTarget.x * cosYaw;

        float steerAngle = std::atan2(localLateral, localForward);
        // Map steerAngle to steering [-1, 1], with max lock at ~20 degrees (0.35 rad)
        float steer = std::clamp(steerAngle / 0.35f, -1.0f, 1.0f);

        // Lateral track error correction
        float lateralErr = (targetLane - proj.lateralOffset);
        steer += std::clamp(lateralErr * 0.03f, -0.15f, 0.15f);
        input.steering = std::clamp(steer, -1.0f, 1.0f);

        // 5. Corner anticipation and speed control
        float dotTan = std::clamp(glm::dot(proj.tangent, tangent), -1.0f, 1.0f);
        float cornerAngleDeg = glm::degrees(std::acos(dotTan));

        // Catchup / Rubber-banding multiplier
        float topSpeedMult = (m_personality.catchup.basePercentTopSpeed / 100.0f);
        if (playerOverallProgress > 0.0f) {
            // My approximate overall progress
            float myProgress = proj.arcLength; // relative in lap
            float diff = playerOverallProgress - myProgress;
            if (diff > m_personality.catchup.distMinCatchupNear) {
                // Player ahead -> boost AI
                topSpeedMult = (m_personality.catchup.enhancedPercentTopSpeed / 100.0f);
            } else if (diff < -m_personality.catchup.distMaxCatchupNear) {
                // AI far ahead -> ease off slightly
                topSpeedMult = (m_personality.catchup.impairedPercentTopSpeed / 100.0f);
            }
        }

        float targetMaxSpeed = 160.0f * topSpeedMult;

        if (cornerAngleDeg > m_personality.driving.cornerAngle) {
            if (speedKph > m_personality.driving.cornerBrakeSpeed * topSpeedMult) {
                input.throttle = 0.0f;
                input.brake = true;
            } else if (speedKph > m_personality.driving.cornerOffGasSpeed * topSpeedMult) {
                input.throttle = 0.2f * m_personality.aggression;
                input.brake = false;
            } else {
                input.throttle = 0.85f * m_personality.aggression;
                input.brake = false;
            }

            // Trigger powerslide drift on sharp corners
            if (cornerAngleDeg > m_personality.driving.powerslideAngle && 
                speedKph > m_personality.driving.powerslideMinSpeed) {
                input.powerslide = true;
            }
        } else {
            if (speedKph < targetMaxSpeed) {
                input.throttle = 1.0f * m_personality.aggression;
            } else {
                input.throttle = 0.4f;
            }
            input.brake = false;
            input.powerslide = false;

            // Trigger turbo on straightaways if energy available
            if (vehicle.GetTurboAmount() >= 50.0f && cornerAngleDeg < 12.0f && speedKph > 50.0f) {
                input.turbo = true;
            }
        }

        m_lastInput = input;
        return input;
    }

} // namespace AI
} // namespace CTTR
