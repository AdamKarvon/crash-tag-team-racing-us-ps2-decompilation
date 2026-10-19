// =============================================================================
//  test_ai_opponents.cpp  —  Verification suite for AI Opponents & Multi-Kart Dynamics
// =============================================================================

#include <iostream>
#include <cassert>
#include <cmath>
#include <filesystem>
#include <vector>
#include <memory>

#include "game/TrackSpline.h"
#include "game/LevelData.h"
#include "game/RaceController.h"
#include "physics/Vehicle.h"
#include "physics/KartContacts.h"
#include "ai/AIMotifs.h"
#include "ai/AIDriver.h"

int main() {
    std::cout << "=====================================================" << std::endl;
    std::cout << "=== CTTR AI Opponents & Multi-Kart Verification ===" << std::endl;
    std::cout << "=====================================================" << std::endl;

    std::string midPath = "game_data/adefault/default/design/levels/fairy1/roads.mid";
    if (!std::filesystem::exists(midPath)) {
        midPath = "game_data/fairy/fairy1/design/levels/fairy1/roads.mid";
    }

    CTTR::Game::TrackSpline spline;
    bool ok = spline.LoadFromMid(midPath);
    assert(ok);

    CTTR::Game::LevelData levelData;
    levelData.Load("fairy1", "game_data");

    // [TEST 1] Kart Contacts (Push-Out & Momentum Exchange)
    std::cout << "\n[TEST 1] Testing Kart-vs-Kart Push-Out & Impulse Exchange..." << std::endl;
    {
        std::vector<std::unique_ptr<CTTR::Physics::Vehicle>> testKarts;
        testKarts.push_back(std::make_unique<CTTR::Physics::Vehicle>(1));
        testKarts.push_back(std::make_unique<CTTR::Physics::Vehicle>(2));

        // Place karts 1.5m apart (overlapping, radius = 1.35m, minDistance = 2.70m)
        testKarts[0]->SetPosition({0.0f, 0.0f, 0.0f});
        testKarts[0]->SetVelocity({0.0f, 0.0f, 10.0f});

        testKarts[1]->SetPosition({0.0f, 0.0f, 1.5f});
        testKarts[1]->SetVelocity({0.0f, 0.0f, -5.0f});

        CTTR::Physics::ResolveKartContacts(testKarts, 1.35f, 0.35f);

        auto p0 = testKarts[0]->GetPosition();
        auto p1 = testKarts[1]->GetPosition();
        float postDist = glm::distance(glm::vec3(p0.x, 0.0f, p0.z), glm::vec3(p1.x, 0.0f, p1.z));

        std::cout << "  ✓ Initial distance: 1.50m -> Post-contact distance: " << postDist << "m (>= 2.70m)" << std::endl;
        assert(postDist >= 2.69f);

        auto v0 = testKarts[0]->GetVelocity();
        auto v1 = testKarts[1]->GetVelocity();
        std::cout << "  ✓ Post-contact velocities: V0.z=" << v0.z << ", V1.z=" << v1.z << std::endl;
        assert(v0.z < 10.0f);  // Pushed backwards
        assert(v1.z > -5.0f);  // Pushed forwards
    }

    // [TEST 2] Spawn 8 Grid Karts with Authentic AI Motifs
    std::cout << "\n[TEST 2] Spawning 8 Grid Karts & Initializing AI Drivers..." << std::endl;
    std::vector<std::unique_ptr<CTTR::Physics::Vehicle>> vehicles;
    std::vector<std::unique_ptr<CTTR::AI::AIDriver>> aiDrivers;
    CTTR::Game::RaceController race;
    race.Initialize(&spline, 32, 3);

    for (int i = 0; i < 8; ++i) {
        vehicles.push_back(std::make_unique<CTTR::Physics::Vehicle>(i));
        const auto* sp = levelData.GetSpawnPoint(i);
        assert(sp != nullptr);

        vehicles[i]->SetPosition({sp->position.x, sp->position.y, sp->position.z});
        vehicles[i]->SetRotation({0.0f, sp->yaw, 0.0f});
        vehicles[i]->SetGroundHeight(sp->position.y);

        auto personality = CTTR::AI::GetDefaultPersonality(i);
        aiDrivers.push_back(std::make_unique<CTTR::AI::AIDriver>(i, personality));
        race.RegisterVehicle(i);
    }
    std::cout << "  ✓ Successfully initialized 8 karts on CarLocation0..7 with distinct lane offsets." << std::endl;

    // [TEST 3] Bit-Exact AI Simulation Determinism over 180 Ticks
    std::cout << "\n[TEST 3] Verifying Multi-Kart Simulation Determinism (180 Ticks)..." << std::endl;
    auto runSimulation = [&](int numTicks) {
        // Clone initial state
        std::vector<std::unique_ptr<CTTR::Physics::Vehicle>> simVehicles;
        std::vector<std::unique_ptr<CTTR::AI::AIDriver>> simAIs;
        for (int i = 0; i < 8; ++i) {
            simVehicles.push_back(std::make_unique<CTTR::Physics::Vehicle>(i));
            const auto* sp = levelData.GetSpawnPoint(i);
            simVehicles[i]->SetPosition({sp->position.x, sp->position.y, sp->position.z});
            simVehicles[i]->SetRotation({0.0f, sp->yaw, 0.0f});
            simVehicles[i]->SetGroundHeight(sp->position.y);
            simAIs.push_back(std::make_unique<CTTR::AI::AIDriver>(i, CTTR::AI::GetDefaultPersonality(i)));
        }

        const float dt = 1.0f / 60.0f;
        for (int t = 0; t < numTicks; ++t) {
            for (int i = 0; i < 8; ++i) {
                auto input = simAIs[i]->ComputeInput(*simVehicles[i], spline, 0.0f);
                simVehicles[i]->ApplyInput(input.throttle, input.steering, input.brake);
                simVehicles[i]->SetPowerslideInput(input.powerslide);
                if (input.turbo) simVehicles[i]->ActivateTurbo();
                simVehicles[i]->UpdatePhysics(dt);
            }
            CTTR::Physics::ResolveKartContacts(simVehicles, 1.35f, 0.35f);
        }

        uint64_t hash = 14695981039346656037ULL;
        for (int i = 0; i < 8; ++i) {
            hash ^= simVehicles[i]->GetDeterministicStateHash();
            hash *= 1099511628211ULL;
        }
        return hash;
    };

    uint64_t hashA = runSimulation(180);
    uint64_t hashB = runSimulation(180);
    std::cout << "  Run A Combined 8-Kart Hash: 0x" << std::hex << hashA << std::dec << std::endl;
    std::cout << "  Run B Combined 8-Kart Hash: 0x" << std::hex << hashB << std::dec << std::endl;
    assert(hashA == hashB);
    std::cout << "  ✓ Multi-kart AI simulation is 100% deterministic across all 8 vehicles!" << std::endl;

    // [TEST 4] Navigation and Lane Offset Integrity
    std::cout << "\n[TEST 4] Verifying Pure-Pursuit Steering on Track Roadway (600 Ticks)..." << std::endl;
    {
        const float dt = 1.0f / 60.0f;
        for (int t = 0; t < 600; ++t) {
            std::vector<std::pair<int, glm::vec3>> positions;
            std::vector<std::pair<int, bool>> throttles;

            for (int i = 0; i < 8; ++i) {
                auto input = aiDrivers[i]->ComputeInput(*vehicles[i], spline, 0.0f);
                vehicles[i]->ApplyInput(input.throttle, input.steering, input.brake);
                vehicles[i]->SetPowerslideInput(input.powerslide);
                if (input.turbo) vehicles[i]->ActivateTurbo();
                vehicles[i]->UpdatePhysics(dt);

                auto p = vehicles[i]->GetPosition();
                positions.push_back({i, glm::vec3(p.x, p.y, p.z)});
                throttles.push_back({i, input.throttle > 0.1f});
            }

            CTTR::Physics::ResolveKartContacts(vehicles, 1.35f, 0.35f);
            race.Update(dt, positions, throttles);
        }

        for (int i = 0; i < 8; ++i) {
            auto pos = vehicles[i]->GetPosition();
            auto proj = spline.Project(glm::vec3(pos.x, pos.y, pos.z));
            float speed = vehicles[i]->GetSpeedKPH();

            std::cout << "  Kart " << i << " (" << aiDrivers[i]->GetPersonality().characterName 
                      << ") -> Speed: " << speed << " km/h, arcLength=" << proj.arcLength 
                      << "m, lateralOffset=" << proj.lateralOffset 
                      << "m, inside=" << (proj.isInsideTrack ? "YES" : "NO") << std::endl;

            // Must have advanced along track
            assert(speed > 40.0f);
            // Must stay inside track boundaries
            assert(proj.isInsideTrack);
        }
        std::cout << "  ✓ All 8 AI karts accelerated cleanly, navigated curves, and remained inside track boundaries!" << std::endl;
    }

    std::cout << "\n=====================================================" << std::endl;
    std::cout << "=== ALL AI OPPONENT TESTS PASSED! (4 / 4) ===" << std::endl;
    std::cout << "=====================================================" << std::endl;
    return 0;
}
