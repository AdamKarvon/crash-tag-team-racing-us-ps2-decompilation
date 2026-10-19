#pragma once
// =============================================================================
//  AIMotifs.h  —  Authentic CTTR AI Tunable Motifs
//  Parsed from design/levels/common/aitunables/aitunables_medium.god
// =============================================================================

#include <string>

namespace CTTR {
namespace AI {

    struct AIDrivingMotif {
        float lookAheadNearSeconds = 1.2f;
        float lookAheadFarSeconds = 1.5f;
        float cornerAngle = 35.0f;          // deg
        float cornerBrakeSpeed = 200.0f;     // km/h
        float cornerOffGasSpeed = 170.0f;   // km/h
        float powerslideAngle = 25.0f;      // deg
        float powerslideMinSpeed = 21.6f;   // km/h
    };

    struct AIRaceCatchupMotif {
        float distMinCatchupNear = 30.0f;
        float distMinCatchupFar = 400.0f;
        float distMaxCatchupNear = 20.0f;
        float distMaxCatchupFar = 100.0f;
        float basePercentTopSpeed = 97.0f;
        float enhancedPercentTopSpeed = 150.0f;
        float impairedPercentTopSpeed = 50.0f;
        float basePercentTopSpeedUseTurbo = 80.0f;
    };

    struct AIPersonality {
        std::string characterName;
        float laneOffset = 0.0f; // Target lateral distance from centerline in metres
        float aggression = 1.0f; // Multiplier on throttle & powerslide tendency
        AIDrivingMotif driving;
        AIRaceCatchupMotif catchup;
    };

    inline AIPersonality GetDefaultPersonality(int index) {
        static const char* kNames[8] = {
            "crash", "crunch", "nina", "neocortex", "ngin", "pasadena", "vonclutch", "coco"
        };
        // Stagger lanes so 8 karts form a realistic multi-lane racing pack
        static const float kLanes[8] = {
            0.0f, -3.5f, 3.5f, -1.8f, 1.8f, -4.5f, 4.5f, 0.0f
        };
        static const float kAggressions[8] = {
            1.0f, 1.05f, 0.98f, 1.02f, 0.95f, 1.0f, 0.92f, 0.97f
        };

        int idx = (index >= 0 && index < 8) ? index : (index % 8);
        AIPersonality p;
        p.characterName = kNames[idx];
        p.laneOffset = kLanes[idx];
        p.aggression = kAggressions[idx];
        return p;
    }

} // namespace AI
} // namespace CTTR
