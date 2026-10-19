#pragma once
// =============================================================================
//  GroundContact.h  —  Dual-axle ground sampling for karts
//  Single implementation of the axle-weighted height/pitch sampling that was
//  previously duplicated for spawn, physics tick and rendering in main.cpp.
// =============================================================================

#include <cmath>
#include <algorithm>
#include <glm/glm.hpp>
#include "physics/TrackCollider.h"

namespace CTTR {
namespace Game {

    // CrashStang axle geometry (metres, from bone offsets w0..w3)
    constexpr float kFrontAxleOffset = 1.332f;
    constexpr float kRearAxleOffset  = 1.701f;
    constexpr float kWheelbase       = kFrontAxleOffset + kRearAxleOffset;
    constexpr float kRideHeight      = 0.84f;   // chassis origin above contact patch

    struct GroundContact {
        bool  hit   = false;   // any surface found
        float y     = 0.0f;    // chassis origin height (contact + ride height)
        float pitch = 0.0f;    // nose-up positive, radians
    };

    inline glm::vec3 ForwardFromYaw(float yaw) {
        return glm::vec3(std::sin(yaw), 0.0f, std::cos(yaw));
    }

    // Samples the track under both axles. Preference order:
    //   both axles -> wheelbase-weighted height + axle pitch
    //   front only / rear only -> that axle
    //   neither -> centre sample with pitch derived from the surface normal
    // `pos.y` is the current chassis height, used to pick the nearest layer.
    inline GroundContact SampleGroundContact(const Physics::TrackCollider& collider,
                                             const glm::vec3& pos, float yaw) {
        GroundContact gc;
        const glm::vec3 fwd = ForwardFromYaw(yaw);
        const glm::vec3 frontAxle = pos + fwd * kFrontAxleOffset;
        const glm::vec3 rearAxle  = pos - fwd * kRearAxleOffset;

        float yFront = pos.y - kRideHeight;
        float yRear  = pos.y - kRideHeight;
        glm::vec3 nFront(0.0f, 1.0f, 0.0f), nRear(0.0f, 1.0f, 0.0f);
        const bool hitFront = collider.SampleHeight(frontAxle.x, frontAxle.z, yFront, yFront, nFront);
        const bool hitRear  = collider.SampleHeight(rearAxle.x,  rearAxle.z,  yRear,  yRear,  nRear);

        if (hitFront && hitRear) {
            gc.hit   = true;
            gc.y     = (yFront * kRearAxleOffset + yRear * kFrontAxleOffset) / kWheelbase + kRideHeight;
            gc.pitch = std::atan2(yFront - yRear, kWheelbase);
            return gc;
        }
        if (hitFront) { gc.hit = true; gc.y = yFront + kRideHeight; return gc; }
        if (hitRear)  { gc.hit = true; gc.y = yRear  + kRideHeight; return gc; }

        float groundY = pos.y - kRideHeight;
        glm::vec3 n(0.0f, 1.0f, 0.0f);
        if (collider.SampleHeight(pos.x, pos.z, groundY, groundY, n)) {
            gc.hit = true;
            gc.y   = groundY + kRideHeight;
            const float forwardSlope = n.z * std::cos(yaw) + n.x * std::sin(yaw);
            gc.pitch = -std::asin(std::clamp(forwardSlope, -0.6f, 0.6f));
        }
        return gc;
    }

} // namespace Game
} // namespace CTTR
