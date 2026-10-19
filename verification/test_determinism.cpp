#include <iostream>
#include <cassert>
#include <cmath>
#include <vector>
#include <iomanip>
#include "../src/physics/Vehicle.h"
#include "../src/game/GameEnums.h"

using namespace CTTR::Physics;

bool FloatNear(float a, float b, float eps = 0.05f) {
    return std::fabs(a - b) <= eps;
}

int main() {
    std::cout << "=====================================================" << std::endl;
    std::cout << "=== CTTR Vehicle Simulation Determinism Test Suite ===" << std::endl;
    std::cout << "=====================================================" << std::endl;

    int totalTests = 0;
    int passedTests = 0;

    // -------------------------------------------------------------
    // TEST 1: Driveline Architecture & Canonical Configuration
    // -------------------------------------------------------------
    std::cout << "\n[TEST 1] Verifying Driveline Architecture & Wheel Layout..." << std::endl;
    totalTests++;
    {
        Vehicle veh(1);
        assert(veh.GetMass() == 1000.0f);

        // Verify Front Left & Right (Steering wheels)
        const auto& w0 = veh.GetWheel(0);
        const auto& w1 = veh.GetWheel(1);
        auto m0 = w0.GetWheelMotif();
        auto m1 = w1.GetWheelMotif();
        assert(m0 != nullptr && m1 != nullptr);
        assert(m0->mSteering == true);
        assert(m0->mDriving == false);
        assert(FloatNear(m0->mBrakeBalance, 0.6f));
        assert(FloatNear(m0->mSlipAngleInDegrees, 85.0f));
        assert(FloatNear(m0->mSlipTransition, 0.35f));

        // Verify Rear Left & Right (Driving wheels)
        const auto& w2 = veh.GetWheel(2);
        const auto& w3 = veh.GetWheel(3);
        auto m2 = w2.GetWheelMotif();
        auto m3 = w3.GetWheelMotif();
        assert(m2 != nullptr && m3 != nullptr);
        assert(m2->mSteering == false);
        assert(m2->mDriving == true);
        assert(FloatNear(m2->mBrakeBalance, 0.4f));
        assert(FloatNear(m2->mLongtitudinalGrip, 2.15f));
        assert(FloatNear(m2->mSlipAngleInDegrees, 75.0f));
        assert(FloatNear(m2->mSlipTransition, 0.25f));

        // Verify Virtual 3rd Drive Wheel
        const auto& w4 = veh.GetWheel(4);
        auto m4 = w4.GetWheelMotif();
        assert(m4 != nullptr);
        assert(m4->mPhysical == false);
        assert(m4->mDriving == true);

        // Verify Steering Wheel Lock
        assert(FloatNear(veh.GetSteeringWheel().maxSteerAngleDegrees, 20.0f));
        assert(FloatNear(veh.GetSteeringWheel().timeToLimit, 0.20f));
        assert(FloatNear(veh.GetSteeringWheel().timeToCenter, 0.17f));

        std::cout << "  ✓ All 5 wheels, differential, and steering rack match CTTR specifications." << std::endl;
        passedTests++;
    }

    // -------------------------------------------------------------
    // TEST 2: Acceleration & 3-Speed Automatic Transmission Shifting
    // -------------------------------------------------------------
    std::cout << "\n[TEST 2] Verifying Engine Acceleration & Gear Shifting (60Hz)..." << std::endl;
    totalTests++;
    {
        Vehicle veh(1);
        veh.ApplyInput(1.0f, 0.0f, false); // Full throttle, straight

        const float dt = 1.0f / 60.0f; // 60Hz tick
        int initialGear = veh.GetTransmission().currentGear;
        assert(initialGear == 1); // Starts in 1st gear

        bool shiftedTo2nd = false;
        bool shiftedTo3rd = false;

        for (int frame = 0; frame < 360; ++frame) { // 6 seconds simulation
            veh.UpdatePhysics(dt);
            int currentGear = veh.GetTransmission().currentGear;
            if (currentGear == 2) shiftedTo2nd = true;
            if (currentGear == 3) shiftedTo3rd = true;
        }

        float finalSpeed = veh.GetSpeedKPH();
        std::cout << "  ✓ Final Speed after 6s: " << std::fixed << std::setprecision(1) << finalSpeed << " kph" << std::endl;
        std::cout << "  ✓ Final Engine RPM:     " << veh.GetTransmission().currentRPM << " RPM" << std::endl;
        std::cout << "  ✓ Final Transmission Gear: Gear " << veh.GetTransmission().currentGear << std::endl;

        assert(shiftedTo2nd);
        assert(shiftedTo3rd);
        assert(finalSpeed > 100.0f);
        passedTests++;
    }

    // -------------------------------------------------------------
    // TEST 3: Braking Deceleration & 60/40 Balance
    // -------------------------------------------------------------
    std::cout << "\n[TEST 3] Verifying Braking Dynamics & Deceleration..." << std::endl;
    totalTests++;
    {
        Vehicle veh(1);
        veh.ApplyInput(1.0f, 0.0f, false);
        const float dt = 1.0f / 60.0f;

        // Accelerate for 2 seconds
        for (int f = 0; f < 120; ++f) veh.UpdatePhysics(dt);
        float topSpeed = veh.GetSpeedKPH();
        assert(topSpeed > 50.0f);

        // Apply full brake
        veh.ApplyInput(0.0f, 0.0f, true);
        for (int f = 0; f < 120; ++f) {
            veh.UpdatePhysics(dt);
            if (f % 20 == 0 || f == 119) {
                std::cout << "    [Brake f=" << f << "] speed=" << veh.GetSpeedKPH() 
                          << " vx=" << veh.GetVelocity().x 
                          << " vz=" << veh.GetVelocity().z 
                          << " vy=" << veh.GetVelocity().y << std::endl;
            }
        }

        float stoppedSpeed = veh.GetSpeedKPH();
        std::cout << "  ✓ Speed before brake: " << topSpeed << " kph -> Speed after brake: " << stoppedSpeed << " kph" << std::endl;
        assert(stoppedSpeed < 1.0f);
        passedTests++;
    }

    // -------------------------------------------------------------
    // TEST 4: Steering Rate & Limit Tracking
    // -------------------------------------------------------------
    std::cout << "\n[TEST 4] Verifying Steering Limits & Centering Rates..." << std::endl;
    totalTests++;
    {
        Vehicle veh(1);
        const float dt = 1.0f / 60.0f;

        // Apply full left steer for 0.25 seconds
        veh.ApplyInput(0.5f, 1.0f, false);
        for (int f = 0; f < 15; ++f) veh.UpdatePhysics(dt); // 0.25s >= 0.20s limit time
        assert(FloatNear(veh.GetSteeringWheel().currentAngleDegrees, 20.0f, 0.5f));

        // Release steering for 0.25 seconds
        veh.ApplyInput(0.5f, 0.0f, false);
        for (int f = 0; f < 15; ++f) veh.UpdatePhysics(dt); // 0.25s >= 0.17s center time
        assert(FloatNear(veh.GetSteeringWheel().currentAngleDegrees, 0.0f, 0.5f));

        std::cout << "  ✓ Steering reached 20.0° lock and returned to 0.0° center on schedule." << std::endl;
        passedTests++;
    }

    // -------------------------------------------------------------
    // TEST 5: Powerslide Drift & Turbo Energy Accumulation
    // -------------------------------------------------------------
    std::cout << "\n[TEST 5] Verifying Powerslide Drift & Turbo Boost Accumulation..." << std::endl;
    totalTests++;
    {
        Vehicle veh(1);
        const float dt = 1.0f / 60.0f;

        // Accelerate up to drift entry speed (>21.6 kph)
        veh.ApplyInput(1.0f, 0.0f, false);
        for (int f = 0; f < 60; ++f) veh.UpdatePhysics(dt);

        // Enter powerslide with steering
        veh.ApplyInput(1.0f, 0.8f, false);
        veh.SetPowerslideInput(true);

        for (int f = 0; f < 120; ++f) { // 2.0s drift
            veh.UpdatePhysics(dt);
        }

        assert(veh.IsPowersliding());
        float turboAccum = veh.GetTurboAmount();
        std::cout << "  ✓ Accumulated Turbo Energy: " << turboAccum << " / 100.0" << std::endl;
        assert(turboAccum >= 50.0f); // Reached minimum activation threshold

        // Release powerslide to boost straight ahead and verify consumption
        veh.SetPowerslideInput(false);
        veh.ApplyInput(1.0f, 0.0f, false);
        veh.ActivateTurbo();
        assert(veh.IsTurboActive());

        for (int f = 0; f < 60; ++f) { // 1.0s of turbo use
            veh.UpdatePhysics(dt);
        }
        float turboRemaining = veh.GetTurboAmount();
        std::cout << "  ✓ Turbo Remaining after 1s burn: " << turboRemaining << std::endl;
        assert(FloatNear(turboAccum - turboRemaining, 14.0f, 1.0f)); // 14 units/sec drain

        passedTests++;
    }

    // -------------------------------------------------------------
    // TEST 6: Clash/Merge Handshake, Uber Transformation & Weapons
    // -------------------------------------------------------------
    std::cout << "\n[TEST 6] Verifying Clash State Machine & Mass Alteration..." << std::endl;
    totalTests++;
    {
        auto driver = std::make_shared<Vehicle>(1);
        auto gunner = std::make_shared<Vehicle>(2);

        driver->SetPosition({0.0f, 0.0f, 0.0f});
        gunner->SetPosition({5.0f, 0.0f, 10.0f}); // 11.18 units apart (<60 range)

        assert(driver->CanClashWith(*gunner));
        assert(gunner->CanClashWith(*driver));

        // Merge handshake
        driver->SetClashState(ClashRole::DRIVER, gunner);
        gunner->SetClashState(ClashRole::GUNNER, driver);

        assert(driver->IsMerged());
        assert(gunner->IsMerged());
        assert(driver->GetMass() == 1500.0f); // Uber vehicle mass 1,500 kg
        assert(gunner->GetClashPartnerId() == 1);

        // Update physics: Gunner must follow Driver position
        const float dt = 1.0f / 60.0f;
        driver->ApplyInput(1.0f, 0.0f, false);
        for (int f = 0; f < 30; ++f) {
            driver->UpdatePhysics(dt);
            gunner->UpdatePhysics(dt);
        }

        assert(FloatNear(gunner->GetPosition().x, driver->GetPosition().x));
        assert(FloatNear(gunner->GetPosition().z, driver->GetPosition().z));
        assert(FloatNear(gunner->GetPosition().y, driver->GetPosition().y + 1.5f));

        // Gunner weapon aiming
        gunner->AimTurret(45.0f, 15.0f);
        assert(gunner->GetTurretYaw() == 45.0f);
        assert(gunner->GetTurretPitch() == 15.0f);

        // Break clash
        driver->BreakClash();
        gunner->BreakClash();
        assert(!driver->IsMerged());
        assert(!gunner->IsMerged());
        assert(driver->GetMass() == 1000.0f);

        std::cout << "  ✓ Clash Handshake, 1,500kg Uber mass adjustment, deck snapping, and unclash passed." << std::endl;
        passedTests++;
    }

    // -------------------------------------------------------------
    // TEST 7: Strict Bit-Exact Simulation Determinism (600 Ticks)
    // -------------------------------------------------------------
    std::cout << "\n[TEST 7] Verifying 600-Tick Bit-Exact Simulation Determinism..." << std::endl;
    totalTests++;
    {
        Vehicle runA(10);
        Vehicle runB(10);

        const float dt = 1.0f / 60.0f;
        bool allHashesMatched = true;

        for (int frame = 0; frame < 600; ++frame) {
            // Apply identical procedural input streams
            float throttle = 0.5f + 0.5f * std::sin(frame * 0.05f);
            float steer = 0.6f * std::sin(frame * 0.08f);
            bool brake = (frame % 150 > 135);
            bool powerslide = (frame % 200 > 100);

            runA.ApplyInput(throttle, steer, brake);
            runA.SetPowerslideInput(powerslide);

            runB.ApplyInput(throttle, steer, brake);
            runB.SetPowerslideInput(powerslide);

            runA.UpdatePhysics(dt);
            runB.UpdatePhysics(dt);

            uint64_t hashA = runA.GetDeterministicStateHash();
            uint64_t hashB = runB.GetDeterministicStateHash();

            if (hashA != hashB) {
                std::cerr << "  ✗ Determinism failure at tick " << frame 
                          << ": Hash A = 0x" << std::hex << hashA 
                          << " != Hash B = 0x" << hashB << std::dec << std::endl;
                allHashesMatched = false;
                break;
            }

            if (frame == 60 || frame == 180 || frame == 360 || frame == 599) {
                std::cout << "  [Tick " << std::setw(3) << frame + 1 << "/600] State Hash: 0x" 
                          << std::hex << hashA << std::dec << " (Match OK)" << std::endl;
            }
        }

        assert(allHashesMatched);
        std::cout << "  ✓ Bit-exact determinism confirmed over 600 frames (10 seconds) with zero drift!" << std::endl;
        passedTests++;
    }

    std::cout << "\n=====================================================" << std::endl;
    std::cout << "=== ALL TESTS COMPLETED: " << passedTests << " / " << totalTests << " PASSED! ===" << std::endl;
    std::cout << "=====================================================" << std::endl;

    return (passedTests == totalTests) ? 0 : 1;
}
