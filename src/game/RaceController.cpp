// =============================================================================
//  RaceController.cpp  —  Lap tracking, checkpoint gates, race timing & standings
// =============================================================================

#include "RaceController.h"
#include <iostream>
#include <algorithm>
#include <cmath>

namespace CTTR {
namespace Game {

    void RaceController::Initialize(const TrackSpline* spline, int numCheckpoints, int totalLaps) {
        m_spline = spline;
        m_totalLaps = (totalLaps > 0) ? totalLaps : 3;
        m_state = RaceState::PreRace;
        m_countdownTimer = 3.0f;
        m_raceTimer = 0.0f;
        m_nextFinishPosition = 1;
        m_checkpoints.clear();
        m_progress.clear();

        if (!m_spline || !m_spline->IsLoaded() || numCheckpoints < 4) {
            std::cerr << "[RaceController] Invalid spline or checkpoint count" << std::endl;
            return;
        }

        float totalL = m_spline->GetTotalLength();
        m_checkpoints.reserve(numCheckpoints);

        for (int i = 0; i < numCheckpoints; ++i) {
            float s = (static_cast<float>(i) / static_cast<float>(numCheckpoints)) * totalL;
            glm::vec3 center = m_spline->SampleCenterline(s);
            glm::vec3 tangent = m_spline->SampleTangent(s);
            float width = m_spline->SampleWidth(s);

            glm::vec3 right = glm::cross(tangent, glm::vec3(0.0f, 1.0f, 0.0f));
            if (glm::length(right) > 1e-4f) {
                right = glm::normalize(right);
            } else {
                right = glm::vec3(1.0f, 0.0f, 0.0f);
            }

            Checkpoint cp;
            cp.index = static_cast<size_t>(i);
            cp.center = center;
            cp.normal = tangent;
            cp.right = right;
            cp.width = width;
            cp.arcLength = s;
            m_checkpoints.push_back(cp);
        }

        std::cout << "[RaceController] Initialized with " << m_checkpoints.size() 
                  << " checkpoints across " << totalL << " m circuit (" << m_totalLaps << " laps)" << std::endl;
    }

    void RaceController::RegisterVehicle(int vehicleId) {
        for (const auto& p : m_progress) {
            if (p.vehicleId == vehicleId) return;
        }

        VehicleRaceProgress prog{};
        prog.vehicleId = vehicleId;
        prog.currentLap = 1;
        prog.nextCheckpoint = 0;
        prog.checkpointsPassedInLap = 0;
        prog.hasStartedRace = false;
        prog.finished = false;
        prog.finishPosition = 0;
        prog.currentPosition = static_cast<int>(m_progress.size()) + 1;
        m_progress.push_back(prog);
    }

    void RaceController::StartCountdown() {
        m_state = RaceState::Countdown;
        m_countdownTimer = 3.0f;
        m_raceTimer = 0.0f;
        m_nextFinishPosition = 1;

        for (auto& p : m_progress) {
            p.currentLap = 1;
            p.nextCheckpoint = 0;
            p.checkpointsPassedInLap = 0;
            p.hasStartedRace = false;
            p.finished = false;
            p.finishPosition = 0;
            p.currentLapTime = 0.0f;
            p.bestLapTime = 0.0f;
            p.lastLapTime = 0.0f;
            p.totalRaceTime = 0.0f;
            p.turboStartTriggered = false;
            p.turboAwarded = false;
        }
    }

    void RaceController::StartRaceImmediate() {
        m_state = RaceState::Racing;
        m_countdownTimer = 0.0f;
        m_raceTimer = 0.0f;
        m_nextFinishPosition = 1;

        for (auto& p : m_progress) {
            p.currentLap = 1;
            p.nextCheckpoint = 0;
            p.checkpointsPassedInLap = 0;
            p.hasStartedRace = false;
            p.finished = false;
            p.finishPosition = 0;
            p.currentLapTime = 0.0f;
            p.bestLapTime = 0.0f;
            p.lastLapTime = 0.0f;
            p.totalRaceTime = 0.0f;
        }
    }

    int RaceController::GetCountdownDisplayNumber() const {
        if (m_countdownTimer <= 0.0f) return 0; // GO!
        int num = static_cast<int>(std::ceil(m_countdownTimer));
        return std::clamp(num, 1, 3);
    }

    const VehicleRaceProgress* RaceController::GetProgress(int vehicleId) const {
        for (const auto& p : m_progress) {
            if (p.vehicleId == vehicleId) return &p;
        }
        return nullptr;
    }

    VehicleRaceProgress* RaceController::GetProgressMut(int vehicleId) {
        for (auto& p : m_progress) {
            if (p.vehicleId == vehicleId) return &p;
        }
        return nullptr;
    }

    bool RaceController::HasVehicleFinished(int vehicleId) const {
        const auto* p = GetProgress(vehicleId);
        return p ? p->finished : false;
    }

    bool RaceController::ConsumeTurboStart(int vehicleId) {
        auto* p = GetProgressMut(vehicleId);
        if (p && p->turboStartTriggered && !p->turboAwarded) {
            p->turboAwarded = true;
            return true;
        }
        return false;
    }

    void RaceController::Update(float dt,
                                const std::vector<std::pair<int, glm::vec3>>& vehiclePositions,
                                const std::vector<std::pair<int, bool>>& vehicleThrottles) {
        if (m_state == RaceState::PreRace) return;

        if (m_state == RaceState::Countdown) {
            m_countdownTimer -= dt;

            // Golden turbo-start window: while countdown displays "1" (0.15s .. 0.85s remaining)
            for (const auto& [vid, throttle] : vehicleThrottles) {
                auto* p = GetProgressMut(vid);
                if (p) {
                    if (m_countdownTimer >= 0.15f && m_countdownTimer <= 0.85f) {
                        if (throttle) {
                            p->turboStartTriggered = true;
                        }
                    } else if (m_countdownTimer > 0.85f && throttle) {
                        // Jumped the gun early — penalty/forfeits turbo
                        p->turboStartTriggered = false;
                    }
                }
            }

            if (m_countdownTimer <= 0.0f) {
                m_countdownTimer = 0.0f;
                m_state = RaceState::Racing;
            }
            return;
        }

        if (m_state == RaceState::Racing || m_state == RaceState::Finished) {
            if (m_state == RaceState::Racing) {
                m_raceTimer += dt;
            }

            for (auto& prog : m_progress) {
                glm::vec3 pos{0.0f};
                bool throttle = false;

                for (const auto& [vid, vpos] : vehiclePositions) {
                    if (vid == prog.vehicleId) { pos = vpos; break; }
                }
                for (const auto& [vid, vth] : vehicleThrottles) {
                    if (vid == prog.vehicleId) { throttle = vth; break; }
                }

                UpdateVehicle(prog, pos, throttle, dt);
            }

            UpdateStandings();

            if (m_state == RaceState::Racing) {
                bool allDone = true;
                for (const auto& p : m_progress) {
                    if (!p.finished) {
                        allDone = false;
                        break;
                    }
                }
                if (allDone && !m_progress.empty()) {
                    m_state = RaceState::Finished;
                }
            }
        }
    }

    void RaceController::UpdateVehicle(VehicleRaceProgress& prog, const glm::vec3& worldPos, bool throttlePressed, float dt) {
        if (prog.finished || !m_spline) return;

        prog.currentLapTime += dt;
        prog.totalRaceTime += dt;

        TrackProjection proj = m_spline->Project(worldPos);
        prog.arcLength = proj.arcLength;
        prog.lateralOffset = proj.lateralOffset;

        if (m_checkpoints.empty()) return;

        const size_t N = m_checkpoints.size();
        size_t k = static_cast<size_t>(prog.nextCheckpoint);
        const Checkpoint& cp = m_checkpoints[k];

        float totalL = m_spline->GetTotalLength();
        float step = totalL / static_cast<float>(N);

        glm::vec3 toCar = worldPos - cp.center;
        float distForward = glm::dot(toCar, cp.normal);
        float distLateral = std::abs(glm::dot(toCar, cp.right));

        bool reached = false;

        if (k == 0) {
            // Start/Finish checkpoint
            if (!prog.hasStartedRace) {
                // Crossing start line at the race beginning
                // Car positions near the start straight: arcLength > totalL - 60m or arcLength < step * 0.75m
                if (proj.arcLength < step * 0.75f || (proj.arcLength > totalL - 60.0f && distForward >= 0.0f)) {
                    if (distLateral <= cp.width * 0.6f + 5.0f) {
                        reached = true;
                    }
                }
            } else {
                // Completing a lap — anti-shortcut: must have visited >= N - 2 checkpoints
                if (prog.checkpointsPassedInLap >= static_cast<int>(N) - 2) {
                    if ((proj.arcLength < step * 0.75f || (proj.arcLength > totalL - 60.0f && distForward >= 0.0f)) &&
                        distLateral <= cp.width * 0.6f + 5.0f) {
                        reached = true;
                    }
                }
            }
        } else {
            // Intermediate checkpoint k
            float diff = proj.arcLength - cp.arcLength;
            if (diff >= -10.0f && diff <= step * 0.85f && distLateral <= cp.width * 0.6f + 5.0f) {
                reached = true;
            }
        }

        if (reached) {
            if (k == 0 && !prog.hasStartedRace) {
                prog.hasStartedRace = true;
                prog.checkpointsPassedInLap = 1;
                prog.nextCheckpoint = 1;
                prog.currentLapTime = 0.0f; // Start lap timer upon crossing start line
            } else if (k == 0 && prog.hasStartedRace) {
                prog.lastLapTime = prog.currentLapTime;
                if (prog.bestLapTime <= 0.0f || prog.lastLapTime < prog.bestLapTime) {
                    prog.bestLapTime = prog.lastLapTime;
                }
                if (prog.currentLap >= m_totalLaps) {
                    prog.finished = true;
                    prog.finishPosition = m_nextFinishPosition++;
                } else {
                    prog.currentLap++;
                    prog.checkpointsPassedInLap = 1;
                    prog.nextCheckpoint = 1;
                    prog.currentLapTime = 0.0f;
                }
            } else {
                prog.checkpointsPassedInLap++;
                prog.nextCheckpoint = static_cast<int>((k + 1) % N);
            }
        }

        // Calculate continuous overall progress for sorting standings
        if (prog.finished) {
            prog.overallProgress = static_cast<float>(m_totalLaps + 1) * totalL + 10000.0f - static_cast<float>(prog.finishPosition) * 100.0f;
        } else {
            prog.overallProgress = static_cast<float>(prog.currentLap - 1) * totalL + proj.arcLength;
        }
    }

    void RaceController::UpdateStandings() {
        if (m_progress.empty()) return;

        std::vector<size_t> order(m_progress.size());
        for (size_t i = 0; i < order.size(); ++i) order[i] = i;

        std::sort(order.begin(), order.end(), [&](size_t a, size_t b) {
            const auto& pa = m_progress[a];
            const auto& pb = m_progress[b];

            // 1. Finished vehicles rank ahead
            if (pa.finished != pb.finished) {
                return pa.finished;
            }
            if (pa.finished && pb.finished) {
                return pa.finishPosition < pb.finishPosition;
            }

            // 2. Otherwise sort by overallProgress descending
            return pa.overallProgress > pb.overallProgress;
        });

        for (size_t rank = 0; rank < order.size(); ++rank) {
            m_progress[order[rank]].currentPosition = static_cast<int>(rank + 1);
        }
    }

} // namespace Game
} // namespace CTTR
