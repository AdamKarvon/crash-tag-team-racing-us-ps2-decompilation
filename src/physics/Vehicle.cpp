#include "Vehicle.h"
#include <iostream>
#include <cmath>
#include <cstring>
#include <algorithm>

namespace CTTR {
namespace Physics {

    namespace {
        uint32_t FloatBits(float value) {
            uint32_t bits = 0;
            std::memcpy(&bits, &value, sizeof(float));
            return bits;
        }

        void HashU32(uint64_t& hash, uint32_t value) {
            constexpr uint64_t FNV_PRIME = 1099511628211ull;
            hash ^= value;
            hash *= FNV_PRIME;
        }

        void HashFloat(uint64_t& hash, float value) {
            HashU32(hash, FloatBits(value));
        }

        void HashVector(uint64_t& hash, const Vector3& value) {
            HashFloat(hash, value.x);
            HashFloat(hash, value.y);
            HashFloat(hash, value.z);
        }
    }

    Vehicle::Vehicle(uint32_t id)
        : m_id(id),
          m_position{0.0f, 0.0f, 0.0f},
          m_rotation{0.0f, 0.0f, 0.0f},
          m_velocity{0.0f, 0.0f, 0.0f},
          m_angularVelocity{0.0f, 0.0f, 0.0f},
          m_mass(1000.0f),                // 1,000 kg standard CrashTripod mass
          m_centerOfMassZOffset(0.2f),    // 0.2 rear-biased CM
          m_currentThrottle(0.0f),
          m_currentSteering(0.0f),
          m_currentBrake(false),
          m_powerslideInput(false),
          m_turboAmount(0.0f),
          m_turboActive(false),
          m_powersliding(false),
          m_powerslideStageTimer(0.0f),
          m_lastTurboEvent(TurboIncrease_Powerslide_Stage_01),
          m_clashRole(ClashRole::NONE),
          m_turretYaw(0.0f),
          m_turretPitch(0.0f)
    {
        InitializeDriveline();
    }

    void Vehicle::InitializeDriveline() {
        m_brakeMotif = std::make_shared<BrakeMotif>();
        m_brakeMotif->maxBrakeTorque = 1600.0f;

        m_suspensionMotif = std::make_shared<SuspensionMotif>();
        m_suspensionMotif->springTune = 0.5f;
        m_suspensionMotif->damperTune = 0.6f;
        m_suspensionMotif->maxExtension = 0.25f;
        m_suspensionMotif->restPosition = 0.30f;

        auto steeringPtr = std::make_shared<SteeringWheel>();
        steeringPtr->maxSteerAngleDegrees = 20.0f;
        steeringPtr->timeToLimit = 0.20f;
        steeringPtr->timeToCenter = 0.17f;
        m_steering = *steeringPtr;

        // --- Wheel 0: Front Left ---
        auto wMotifFL = std::make_shared<WheelMotif>();
        wMotifFL->mSteering = true;
        wMotifFL->mDriving = false;
        wMotifFL->mBrakeBalance = 0.6f;
        wMotifFL->mLateralGripNoSlip = 0.23f;
        wMotifFL->mLateralGripInSlip = 5.5f;
        wMotifFL->mLongtitudinalGrip = 1.0f;
        wMotifFL->mSlipAngleInDegrees = 85.0f;
        wMotifFL->mSlipTransition = 0.35f;
        wMotifFL->mMass = 22.0f;

        // --- Wheel 1: Front Right ---
        auto wMotifFR = std::make_shared<WheelMotif>(*wMotifFL);

        // --- Wheel 2: Rear Left ---
        auto wMotifRL = std::make_shared<WheelMotif>();
        wMotifRL->mSteering = false;
        wMotifRL->mDriving = true;
        wMotifRL->mBrakeBalance = 0.4f;
        wMotifRL->mLateralGripNoSlip = 0.6f;
        wMotifRL->mLateralGripInSlip = 5.5f;
        wMotifRL->mLongtitudinalGrip = 2.15f;
        wMotifRL->mSlipAngleInDegrees = 75.0f;
        wMotifRL->mSlipTransition = 0.25f;
        wMotifRL->mMass = 22.0f;

        // --- Wheel 3: Rear Right ---
        auto wMotifRR = std::make_shared<WheelMotif>(*wMotifRL);

        // --- Wheel 4: Virtual 3rd Drive Wheel ---
        auto wMotif3rd = std::make_shared<WheelMotif>();
        wMotif3rd->mPhysical = false;
        wMotif3rd->mDriving = true;
        wMotif3rd->mSteering = false;
        wMotif3rd->mLateralGripNoSlip = 0.6f;
        wMotif3rd->mLateralGripInSlip = 7.0f;
        wMotif3rd->mLongtitudinalGrip = 2.15f;
        wMotif3rd->mSlipAngleInDegrees = 70.0f;
        wMotif3rd->mSlipTransition = 0.25f;

        m_wheels.resize(5);
        m_wheels[0].SetWheelMotif(wMotifFL);
        m_wheels[0].SetBrakeMotif(m_brakeMotif);
        m_wheels[0].SetSuspensionMotif(m_suspensionMotif);
        m_wheels[0].SetSteeringWheel(steeringPtr);

        m_wheels[1].SetWheelMotif(wMotifFR);
        m_wheels[1].SetBrakeMotif(m_brakeMotif);
        m_wheels[1].SetSuspensionMotif(m_suspensionMotif);
        m_wheels[1].SetSteeringWheel(steeringPtr);

        m_wheels[2].SetWheelMotif(wMotifRL);
        m_wheels[2].SetBrakeMotif(m_brakeMotif);
        m_wheels[2].SetSuspensionMotif(m_suspensionMotif);

        m_wheels[3].SetWheelMotif(wMotifRR);
        m_wheels[3].SetBrakeMotif(m_brakeMotif);
        m_wheels[3].SetSuspensionMotif(m_suspensionMotif);

        m_wheels[4].SetWheelMotif(wMotif3rd);

        // Connect differential to rear wheels
        auto diffMotif = std::make_shared<DifferentialMotif>();
        m_differential.SetMotif(diffMotif);
        m_differential.SetOutput(0, &m_wheels[2]);
        m_differential.SetOutput(1, &m_wheels[3]);
    }

    void Vehicle::UpdatePhysics(float deltaTime) {
        if (deltaTime <= 0.0f) return;

        // 1. Clashing / Merged Gunner Snap
        if (m_clashRole == ClashRole::GUNNER) {
            if (auto partner = m_clashPartner.lock()) {
                m_position = partner->GetPosition();
                m_position.y += 1.5f; // Sits elevated on rear gunner deck
                m_velocity = partner->GetVelocity();
                m_rotation = partner->GetRotation();
            }
            return;
        }

        // 2. Steering input dynamics
        m_steering.Update(m_currentSteering, deltaTime);
        float steerRad = m_steering.currentAngleDegrees * (3.14159265f / 180.0f);

        // 3. Forward & Lateral speed in kart local frame
        float cosYaw = std::cos(m_rotation.y);
        float sinYaw = std::sin(m_rotation.y);
        float localForwardSpeed = m_velocity.z * cosYaw + m_velocity.x * sinYaw;
        float localLateralSpeed = -m_velocity.z * sinYaw + m_velocity.x * cosYaw;

        // 4. Powerslide / Drift simulation
        float speedKPH = std::abs(localForwardSpeed) * 3.6f;
        if (m_powerslideInput && speedKPH >= 21.6f && std::abs(m_currentSteering) >= 0.2f) {
            m_powersliding = true;
            m_powerslideStageTimer += deltaTime;

            // Power slide stages awards turbo (only when not actively boosting)
            if (!m_turboActive) {
                if (m_powerslideStageTimer >= 0.5f && m_powerslideStageTimer < 1.2f) {
                    m_turboAmount = std::min(m_turboAmount + 25.0f * deltaTime, 100.0f);
                    m_lastTurboEvent = TurboIncrease_Powerslide_Stage_01;
                } else if (m_powerslideStageTimer >= 1.2f && m_powerslideStageTimer < 2.0f) {
                    m_turboAmount = std::min(m_turboAmount + 45.0f * deltaTime, 100.0f);
                    m_lastTurboEvent = TurboIncrease_Powerslide_Stage_02;
                } else if (m_powerslideStageTimer >= 2.0f) {
                    m_turboAmount = std::min(m_turboAmount + 60.0f * deltaTime, 100.0f);
                    m_lastTurboEvent = TurboIncrease_Powerslide_Stage_03;
                }
            }
        } else {
            if (m_powersliding && m_powerslideStageTimer >= 0.5f) {
                // Powerslide boost kickout impulse on release
                localForwardSpeed += 4.5f; // Power slide boost kickout
            }
            m_powersliding = false;
            m_powerslideStageTimer = 0.0f;
        }

        // 5. Turbo consumption & boost
        if (m_turboActive) {
            m_turboAmount -= 14.0f * deltaTime; // 14.0/s usage
            if (m_turboAmount <= 0.0f) {
                m_turboAmount = 0.0f;
                m_turboActive = false;
            }
        }

        // 6. Transmission & Differential Update
        float rearWheelRadS = (m_wheels[2].angularVelocity + m_wheels[3].angularVelocity) * 0.5f;
        bool reverseReq = (m_currentBrake && localForwardSpeed < 0.2f && m_currentThrottle > 0.1f);
        m_transmission.Update(m_currentThrottle, reverseReq, m_turboActive, rearWheelRadS, deltaTime);

        float driveTorque = m_transmission.GetOutputTorque();
        m_differential.DistributeTorque(driveTorque);

        // Apply braking to all 4 wheels
        if (m_currentBrake && !reverseReq) {
            for (int i = 0; i < 4; ++i) {
                m_wheels[i].ApplyBraking(1.0f);
            }
        }

        // 7. Wheel dynamics & tire force integration
        float wheelNormalLoad = (m_mass * 9.81f) * 0.25f; // ~2450 N per wheel
        float totalLongForce = 0.0f;
        float totalFrontLatForce = 0.0f;
        float totalRearLatForce = 0.0f;

        for (int i = 0; i < 4; ++i) {
            float wFwd = localForwardSpeed;
            float wLat = localLateralSpeed;

            if (i < 2) {
                // Front wheels: account for steering angle and yaw rate
                float cosS = std::cos(steerRad);
                float sinS = std::sin(steerRad);
                float steeredFwd = wFwd * cosS + wLat * sinS;
                float steeredLat = -wFwd * sinS + wLat * cosS;
                m_wheels[i].Update(steeredFwd, steeredLat, wheelNormalLoad, deltaTime);
                totalFrontLatForce += m_wheels[i].lateralForce;
                totalLongForce += m_wheels[i].longitudinalForce;
            } else {
                // Rear drive wheels
                m_wheels[i].Update(wFwd, wLat, wheelNormalLoad, deltaTime);
                totalRearLatForce += m_wheels[i].lateralForce;
                totalLongForce += m_wheels[i].longitudinalForce;
            }
        }

        // 8. Vehicle Body Acceleration & Yaw Dynamics
        // Aerodynamic drag
        float aeroDragCoeff = (m_clashRole == ClashRole::DRIVER ? 0.38f : 0.32f);
        float aeroDrag = 0.5f * 1.225f * aeroDragCoeff * localForwardSpeed * std::abs(localForwardSpeed);

        float netLongAccel = (totalLongForce - aeroDrag) / m_mass;
        if (m_currentBrake && !reverseReq) {
            if (localForwardSpeed > 0.0f) {
                localForwardSpeed = std::max(localForwardSpeed + netLongAccel * deltaTime, 0.0f);
            } else if (localForwardSpeed < 0.0f) {
                localForwardSpeed = std::min(localForwardSpeed + netLongAccel * deltaTime, 0.0f);
            }
            if (std::abs(localForwardSpeed) < 0.1f) {
                localForwardSpeed = 0.0f;
            }
        } else {
            localForwardSpeed += netLongAccel * deltaTime;
        }

        // Steering yaw rate from wheelbase (~2.4m) and lateral forces
        constexpr float WHEELBASE = 2.4f;
        float baseYawRate = (localForwardSpeed / WHEELBASE) * std::sin(steerRad);
        if (m_powersliding) {
            baseYawRate *= 1.65f; // Extra rotation authority in drift
        }

        m_rotation.y += baseYawRate * deltaTime;

        // Convert updated local velocities back to world space
        cosYaw = std::cos(m_rotation.y);
        sinYaw = std::sin(m_rotation.y);
        if (std::abs(localForwardSpeed) < 1e-4f && std::abs(m_currentThrottle) < 1e-4f) {
            m_velocity.x = 0.0f;
            m_velocity.z = 0.0f;
        } else {
            m_velocity.x = localForwardSpeed * sinYaw + (totalRearLatForce / m_mass * deltaTime) * cosYaw;
            m_velocity.z = localForwardSpeed * cosYaw - (totalRearLatForce / m_mass * deltaTime) * sinYaw;
        }

        // Gravity & ground constraint
        m_velocity.y -= 9.81f * deltaTime;
        m_position.y += m_velocity.y * deltaTime;
        if (m_position.y <= m_groundY) {
            m_position.y = m_groundY;
            m_velocity.y = 0.0f;
        }

        // World position integration
        m_position.x += m_velocity.x * deltaTime;
        m_position.z += m_velocity.z * deltaTime;
    }

    void Vehicle::ApplyInput(float throttle, float steering, bool brake) {
        if (m_clashRole != ClashRole::GUNNER) {
            m_currentThrottle = std::clamp(throttle, 0.0f, 1.0f);
            m_currentSteering = std::clamp(steering, -1.0f, 1.0f);
            m_currentBrake = brake;
        }
    }

    void Vehicle::SetPowerslideInput(bool powerslide) {
        m_powerslideInput = powerslide;
    }

    void Vehicle::ActivateTurbo() {
        // Can only activate turbo if minimum capacity met (50 units)
        if (m_turboAmount >= 50.0f) {
            m_turboActive = true;
        }
    }

    bool Vehicle::CanClashWith(const Vehicle& other) const {
        if (IsMerged() || other.IsMerged()) return false;
        if (m_id == other.m_id) return false;

        float dx = m_position.x - other.m_position.x;
        float dy = m_position.y - other.m_position.y;
        float dz = m_position.z - other.m_position.z;
        float distSq = dx * dx + dy * dy + dz * dz;

        // Standard clash initiation radius (60 units)
        return distSq <= (60.0f * 60.0f);
    }

    void Vehicle::InitiateClash(std::shared_ptr<Vehicle> targetVehicle) {
        if (CanClashWith(*targetVehicle)) {
            SetClashState(ClashRole::DRIVER, targetVehicle);
            targetVehicle->SetClashState(ClashRole::GUNNER, std::make_shared<Vehicle>(*this));
        }
    }

    void Vehicle::SetClashState(ClashRole role, std::shared_ptr<Vehicle> partner) {
        m_clashRole = role;
        m_clashPartner = partner;
        if (role == ClashRole::DRIVER) {
            m_mass = 1500.0f; // Clashed Uber vehicle is 1,500 kg
            m_centerOfMassZOffset = 0.3f;
        } else if (role == ClashRole::NONE) {
            m_mass = 1000.0f; // Return to standard kart mass
            m_centerOfMassZOffset = 0.2f;
        }
    }

    void Vehicle::BreakClash() {
        m_clashRole = ClashRole::NONE;
        m_clashPartner.reset();
        m_mass = 1000.0f;
        m_centerOfMassZOffset = 0.2f;
    }

    void Vehicle::AimTurret(float yaw, float pitch) {
        if (m_clashRole == ClashRole::GUNNER) {
            m_turretYaw = yaw;
            m_turretPitch = std::clamp(pitch, -45.0f, 45.0f);
        }
    }

    void Vehicle::FireWeapon() {
        if (m_clashRole == ClashRole::GUNNER) {
            std::cout << "[Vehicle " << m_id << "] Fired Gunner Turret!" << std::endl;
        }
    }

    uint32_t Vehicle::GetClashPartnerId() const {
        if (auto partner = m_clashPartner.lock()) {
            return partner->GetId();
        }
        return 0;
    }

    float Vehicle::GetSpeedKPH() const {
        float speedSq = (m_velocity.x * m_velocity.x) + (m_velocity.z * m_velocity.z);
        return std::sqrt(speedSq) * 3.6f;
    }

    VehicleStateSnapshot Vehicle::CaptureState() const {
        VehicleStateSnapshot snap;
        snap.id = m_id;
        snap.position = m_position;
        snap.rotation = m_rotation;
        snap.velocity = m_velocity;
        snap.throttle = m_currentThrottle;
        snap.steering = m_currentSteering;
        snap.clashRole = m_clashRole;
        snap.clashPartnerId = GetClashPartnerId();
        snap.turretYaw = m_turretYaw;
        snap.turretPitch = m_turretPitch;
        snap.currentGear = m_transmission.currentGear;
        snap.engineRPM = m_transmission.currentRPM;
        snap.turboLevel = m_turboAmount;
        return snap;
    }

    uint64_t Vehicle::GetDeterministicStateHash() const {
        constexpr uint64_t FNV_OFFSET_BASIS = 14695981039346656037ull;
        uint64_t hash = FNV_OFFSET_BASIS;

        HashU32(hash, m_id);
        HashVector(hash, m_position);
        HashVector(hash, m_rotation);
        HashVector(hash, m_velocity);
        HashFloat(hash, m_currentThrottle);
        HashFloat(hash, m_currentSteering);
        HashFloat(hash, m_steering.currentAngleDegrees);
        HashU32(hash, static_cast<uint32_t>(m_clashRole));
        HashU32(hash, GetClashPartnerId());
        HashFloat(hash, m_turretYaw);
        HashFloat(hash, m_turretPitch);
        HashU32(hash, static_cast<uint32_t>(m_transmission.currentGear));
        HashFloat(hash, m_transmission.currentRPM);
        HashFloat(hash, m_turboAmount);
        HashU32(hash, m_turboActive ? 1 : 0);
        HashU32(hash, m_powersliding ? 1 : 0);

        for (size_t i = 0; i < m_wheels.size(); ++i) {
            HashFloat(hash, m_wheels[i].angularVelocity);
            HashFloat(hash, m_wheels[i].slipRatio);
        }

        return hash;
    }

} // namespace Physics
} // namespace CTTR
