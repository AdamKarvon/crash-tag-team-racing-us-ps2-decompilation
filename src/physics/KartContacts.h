#pragma once
// =============================================================================
//  KartContacts.h  —  Kart-vs-Kart Contact & Momentum Exchange
//  Resolves vehicle-to-vehicle overlap and impulses in the fixed 60Hz tick
// =============================================================================

#include <vector>
#include <memory>
#include <cmath>
#include <algorithm>
#include <glm/glm.hpp>
#include "Vehicle.h"

namespace CTTR {
namespace Physics {

    inline void ResolveKartContacts(std::vector<std::unique_ptr<Vehicle>>& vehicles, float kartRadius = 1.35f, float restitution = 0.35f) {
        if (vehicles.size() < 2) return;

        const float minDistance = kartRadius * 2.0f;
        const float minDistanceSq = minDistance * minDistance;

        for (size_t i = 0; i < vehicles.size(); ++i) {
            for (size_t j = i + 1; j < vehicles.size(); ++j) {
                auto* vA = vehicles[i].get();
                auto* vB = vehicles[j].get();
                if (!vA || !vB) continue;

                // If merged/clashed, contact is managed by clash socket
                if (vA->IsMerged() && vB->IsMerged() && vA->GetClashPartnerId() == vB->GetId()) {
                    continue;
                }

                auto posA = vA->GetPosition();
                auto posB = vB->GetPosition();

                // Check distance in horizontal (XZ) plane
                float dx = posA.x - posB.x;
                float dz = posA.z - posB.z;
                float distSq = dx * dx + dz * dz;

                if (distSq < minDistanceSq && distSq > 1e-6f) {
                    float dist = std::sqrt(distSq);
                    float nx = dx / dist;
                    float nz = dz / dist;
                    float penetration = minDistance - dist;

                    float massA = vA->GetMass();
                    float massB = vB->GetMass();
                    float invMassA = 1.0f / massA;
                    float invMassB = 1.0f / massB;
                    float invMassSum = invMassA + invMassB;

                    // 1. Positional Push-Out (prevent overlap)
                    float pushA = penetration * (invMassA / invMassSum);
                    float pushB = penetration * (invMassB / invMassSum);

                    posA.x += nx * pushA;
                    posA.z += nz * pushA;
                    posB.x -= nx * pushB;
                    posB.z -= nz * pushB;

                    vA->SetPosition(posA);
                    vB->SetPosition(posB);

                    // 2. Velocity Momentum Exchange
                    auto velA = vA->GetVelocity();
                    auto velB = vB->GetVelocity();

                    float relVx = velA.x - velB.x;
                    float relVz = velA.z - velB.z;
                    float normVel = relVx * nx + relVz * nz;

                    if (normVel < 0.0f) { // Moving towards each other
                        float impulse = -(1.0f + restitution) * normVel / invMassSum;

                        velA.x += (nx * impulse) * invMassA;
                        velA.z += (nz * impulse) * invMassA;
                        velB.x -= (nx * impulse) * invMassB;
                        velB.z -= (nz * impulse) * invMassB;

                        // Tangential friction damping
                        float tx = -nz;
                        float tz = nx;
                        float tangVel = relVx * tx + relVz * tz;
                        float friction = 0.2f;
                        float frictionImpulse = -tangVel * friction / invMassSum;

                        velA.x += (tx * frictionImpulse) * invMassA;
                        velA.z += (tz * frictionImpulse) * invMassA;
                        velB.x -= (tx * frictionImpulse) * invMassB;
                        velB.z -= (tz * frictionImpulse) * invMassB;

                        vA->SetVelocity(velA);
                        vB->SetVelocity(velB);
                    }
                }
            }
        }
    }

} // namespace Physics
} // namespace CTTR
