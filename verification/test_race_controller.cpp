// =============================================================================
//  test_race_controller.cpp  —  Verification suite for RaceController
// =============================================================================

#include <iostream>
#include <cassert>
#include <cmath>
#include <filesystem>
#include <vector>

#include "game/TrackSpline.h"
#include "game/LevelData.h"
#include "game/RaceController.h"

int main() {
    std::cout << "=====================================================" << std::endl;
    std::cout << "=== CTTR RaceController & Lap Tracker Verification ===" << std::endl;
    std::cout << "=====================================================" << std::endl;

    std::string midPath = "game_data/adefault/default/design/levels/fairy1/roads.mid";
    if (!std::filesystem::exists(midPath)) {
        midPath = "game_data/fairy/fairy1/design/levels/fairy1/roads.mid";
    }

    CTTR::Game::TrackSpline spline;
    bool ok = spline.LoadFromMid(midPath);
    assert(ok);

    CTTR::Game::RaceController race;
    const int NUM_CHECKPOINTS = 32;
    const int TOTAL_LAPS = 3;
    race.Initialize(&spline, NUM_CHECKPOINTS, TOTAL_LAPS);

    assert(race.GetCheckpointCount() == NUM_CHECKPOINTS);
    assert(race.GetTotalLaps() == TOTAL_LAPS);
    assert(race.GetState() == CTTR::Game::RaceState::PreRace);

    // [TEST 1] Register Vehicles & Pre-Race Status
    std::cout << "\n[TEST 1] Registering Vehicles (1..8) & Pre-Race Status..." << std::endl;
    for (int i = 0; i < 8; ++i) {
        race.RegisterVehicle(i);
        const auto* prog = race.GetProgress(i);
        assert(prog != nullptr);
        assert(prog->currentLap == 1);
        assert(prog->nextCheckpoint == 0);
        assert(!prog->finished);
    }
    std::cout << "  ✓ All 8 vehicles registered cleanly on the grid." << std::endl;

    // [TEST 2] Countdown State Machine & Turbo Start Golden Window
    std::cout << "\n[TEST 2] Testing Countdown Sequence & Golden-Window Turbo Start..." << std::endl;
    race.StartCountdown();
    assert(race.GetState() == CTTR::Game::RaceState::Countdown);

    const float dt = 1.0f / 60.0f;
    // Tick 1.2s down to ~1.8s (displaying "2")
    for (int i = 0; i < 72; ++i) {
        std::vector<std::pair<int, glm::vec3>> pos = {{0, glm::vec3(0.0f)}};
        std::vector<std::pair<int, bool>> th = {{0, false}, {1, true}}; // Vehicle 1 jumps gun at t=2.5s
        race.Update(dt, pos, th);
    }
    assert(race.GetCountdownDisplayNumber() == 2);
    assert(!race.GetProgress(0)->turboStartTriggered);
    assert(!race.GetProgress(1)->turboStartTriggered); // Penalized for early jump

    // Tick another 1.2s down to ~0.6s remaining (displaying "1")
    for (int i = 0; i < 72; ++i) {
        std::vector<std::pair<int, glm::vec3>> pos = {{0, glm::vec3(0.0f)}};
        std::vector<std::pair<int, bool>> th = {{0, true}, {1, false}}; // Vehicle 0 hits throttle during "1"
        race.Update(dt, pos, th);
    }
    assert(race.GetCountdownDisplayNumber() == 1);
    assert(race.GetProgress(0)->turboStartTriggered); // Golden window hit!

    // Tick until countdown finishes -> GO!
    for (int i = 0; i < 50; ++i) {
        std::vector<std::pair<int, glm::vec3>> pos = {{0, glm::vec3(0.0f)}};
        std::vector<std::pair<int, bool>> th = {{0, true}};
        race.Update(dt, pos, th);
    }
    assert(race.GetState() == CTTR::Game::RaceState::Racing);
    assert(race.GetCountdownDisplayNumber() == 0); // GO!
    assert(race.ConsumeTurboStart(0));             // Turbo awarded!
    assert(!race.ConsumeTurboStart(0));            // Consumed once
    std::cout << "  ✓ Countdown 3-2-1-GO state machine and Turbo Start timing verified!" << std::endl;

    // [TEST 3] Anti-Cheat & Corner-Cut Prevention
    std::cout << "\n[TEST 3] Verifying Anti-Corner-Cut Gate Enforcement..." << std::endl;
    {
        // Vehicle 2 crosses start line (Checkpoint 0)
        glm::vec3 startPos = spline.SampleCenterline(0.0f);
        std::vector<std::pair<int, glm::vec3>> pStart = {{2, startPos}};
        std::vector<std::pair<int, bool>> tStart = {{2, true}};
        race.Update(dt, pStart, tStart);
        assert(race.GetProgress(2)->hasStartedRace);
        assert(race.GetProgress(2)->nextCheckpoint == 1);

        // Vehicle 2 visits Checkpoint 1
        glm::vec3 cp1Pos = spline.SampleCenterline(race.GetCheckpoints()[1].arcLength);
        race.Update(dt, {{2, cp1Pos}}, tStart);
        assert(race.GetProgress(2)->nextCheckpoint == 2);

        // Vehicle 2 tries to cut across infield to Checkpoint 15
        glm::vec3 cp15Pos = spline.SampleCenterline(race.GetCheckpoints()[15].arcLength);
        race.Update(dt, {{2, cp15Pos}}, tStart);
        // Assert checkpoint 15 is rejected! Next expected must still be 2!
        assert(race.GetProgress(2)->nextCheckpoint == 2);
        assert(race.GetProgress(2)->checkpointsPassedInLap == 2); // only cp 0 and cp 1

        // Vehicle 2 tries to trigger lap completion by driving straight back to finish line
        race.Update(dt, {{2, startPos}}, tStart);
        // Must NOT complete lap!
        assert(race.GetProgress(2)->currentLap == 1);
        assert(!race.GetProgress(2)->finished);
        std::cout << "  ✓ Corner-cutting attempt successfully blocked: out-of-order gates rejected." << std::endl;
    }

    // [TEST 4] 3-Lap Full Simulation
    std::cout << "\n[TEST 4] Simulating 3-Lap Race Completion along Centerline..." << std::endl;
    {
        float totalL = spline.GetTotalLength();
        float speed = 35.0f; // 35 m/s (~126 km/h)
        float currentS = totalL - 30.0f; // Start on grid 30m behind line

        int ticks = 0;
        while (!race.GetProgress(0)->finished && ticks < 30000) {
            currentS += speed * dt;
            if (currentS >= totalL) currentS -= totalL;

            glm::vec3 pos = spline.SampleCenterline(currentS);
            race.Update(dt, {{0, pos}}, {{0, true}});
            ticks++;
        }

        const auto* prog = race.GetProgress(0);
        assert(prog->finished);
        assert(prog->finishPosition == 1);
        assert(prog->currentLap == 3);
        assert(prog->bestLapTime > 0.0f);
        assert(prog->totalRaceTime > 0.0f);

        std::cout << "  ✓ Finished 3 laps in " << ticks << " ticks (" 
                  << prog->totalRaceTime << "s). Best Lap: " << prog->bestLapTime << "s" << std::endl;
    }

    // [TEST 5] Standings and Multi-Vehicle Progress Ranking
    std::cout << "\n[TEST 5] Verifying Instantaneous Standings Ranking..." << std::endl;
    {
        // Vehicle 0 is finished (1st)
        // Set Vehicle 3 to Lap 2, arcLength 2000m
        // Set Vehicle 4 to Lap 2, arcLength 1500m
        // Set Vehicle 5 to Lap 1, arcLength 3000m
        race.GetProgressMut(3)->currentLap = 2;
        race.GetProgressMut(4)->currentLap = 2;
        glm::vec3 pos3 = spline.SampleCenterline(2000.0f);
        glm::vec3 pos4 = spline.SampleCenterline(1500.0f);
        glm::vec3 pos5 = spline.SampleCenterline(3000.0f);

        // Update positions
        race.Update(dt, {{3, pos3}, {4, pos4}, {5, pos5}}, {{3, true}, {4, true}, {5, true}});

        // Vehicle 0 is finished -> 1st
        assert(race.GetProgress(0)->currentPosition == 1);
        // Vehicle 3 has more progress than Vehicle 4 -> 3 ahead of 4
        assert(race.GetProgress(3)->currentPosition < race.GetProgress(4)->currentPosition);
        // Vehicle 4 is on lap 2 (progress ~1.5 laps) vs Vehicle 5 on lap 1 (progress ~0.7 laps) -> 4 ahead of 5
        assert(race.GetProgress(4)->currentPosition < race.GetProgress(5)->currentPosition);

        std::cout << "  ✓ Standings ranking correctly ordered by finished state, lap count, and track arc length." << std::endl;
    }

    std::cout << "\n=====================================================" << std::endl;
    std::cout << "=== ALL RACE CONTROLLER TESTS PASSED! (5 / 5) ===" << std::endl;
    std::cout << "=====================================================" << std::endl;
    return 0;
}
