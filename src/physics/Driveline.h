#pragma once
#include <cstdint>
#include <string>
#include <vector>
#include <memory>
#include <cmath>
#include <algorithm>

namespace CTTR {
namespace Physics {

// =============================================================================
//  DrivelineComponentMotif & DrivelineComponent
//  Directly matching CTTR GameCube reverse-engineered architecture (cttr-main)
// =============================================================================

struct DrivelineComponentMotif {
    virtual ~DrivelineComponentMotif() = default;
};

class DrivelineComponent {
public:
    virtual ~DrivelineComponent() = default;
    virtual float GetOutputTorque() const { return 0.0f; }
    virtual float GetAngularVelocity() const { return 0.0f; }
};

// =============================================================================
//  BrakeMotif
// =============================================================================
//  BrakeMotif (Decompiled from SLES_534.39 @ 0x00159b48)
// =============================================================================
struct BrakeMotif : public DrivelineComponentMotif {
    float maxBrakeTorque = 1600.0f; // Nm (SetBrakeScale @ 0x00159d08)
};

// =============================================================================
//  SuspensionMotif (Decompiled from SLES_534.39 @ 0x002a5c68, ctor 0x002a5f00)
// =============================================================================
struct SuspensionMotif {
    float criticalDamping = 0.0f;  // +0x0c: c_crit = 2*sqrt(k*m)*0.25 (quarter-car damping @ 0x002a5fe4)
    float springStiffness = 0.0f;  // +0x10: k = -(m*g_y) / (0.5 * maxExtension) (@ 0x002a5fd8)
    float damperTune     = 0.6f;  // +0x14: Normalised damping multiplier (SetDamperTune @ 0x002a6008)
    float maxExtension   = 0.25f; // +0x18: Suspension max travel in metres (SetMaxExtension @ 0x002a6010)
    float restPosition   = 0.30f; // +0x1c: Resting position in metres (SetRestingPosition @ 0x002a6018)
    float springTune     = 0.5f;  // +0x20: Normalised spring multiplier (SetSpringTune @ 0x002a6020)
    float vehicleMass    = 1000.0f; // +0x24: Vehicle mass cached in Initialize (@ 0x002a5fc0)

    // Exact PS2 R5900 Suspension Initialization (0x002a5f70)
    void Initialize(float mass, float gravityY = -9.81f) {
        vehicleMass = mass;
        float halfMaxExt = 0.5f * maxExtension;
        if (halfMaxExt > 1e-4f) {
            springStiffness = -(vehicleMass * gravityY) / halfMaxExt; // 0x002a5fd8
        }
        criticalDamping = 2.0f * std::sqrt(springStiffness * vehicleMass) * 0.25f; // 0x002a5fe4..0x002a5fec
    }
};

// =============================================================================
//  SteeringWheel (Decompiled from SLES_534.39 @ 0x00299430 / 0x002995e8 / 0x002999b8)
// =============================================================================
struct SteeringWheel {
    // Canonical CTTR kart properties
    float maxSteerAngleDegrees = 20.0f; // Standard CTTR kart limit: 20 deg
    float timeToLimit          = 0.20f; // 0.20s to full lock
    float timeToCenter         = 0.17f; // 0.17s return to center
    float currentAngleDegrees  = 0.0f;

    // Decompiled SLES_534.39 parameters from SteeringWheelMotif (0x002999b8)
    float lowSpeedLimitRadians  = 0.5235988f; // 30.0 deg (0x3f060a93 @ 0x002999e0)
    float highSpeedLimitRadians = 0.43633235f; // 25.0 deg (0x3edf66f4 @ 0x00299a00)
    float lowSpeedTimeToCenter  = 0.075f;      // (0x3d99999a @ 0x002999f0)
    float lowSpeedTimeToLimit   = 0.10f;       // (0x3dcccccd @ 0x002999d0)
    float timeToCenterDecomp    = 0.10f;       // (0x3dcccccd @ 0x00299a1c)
    float timeToLimitDecomp     = 0.10f;       // (0x3dcccccd @ 0x00299a20)

    void Update(float inputSteer, float dt) {
        float targetAngle = inputSteer * maxSteerAngleDegrees;
        float rate = (std::abs(targetAngle) > std::abs(currentAngleDegrees)) 
                   ? (maxSteerAngleDegrees / std::max(timeToLimit, 0.01f))
                   : (maxSteerAngleDegrees / std::max(timeToCenter, 0.01f));

        float delta = targetAngle - currentAngleDegrees;
        float maxStep = rate * dt;
        if (std::abs(delta) <= maxStep) {
            currentAngleDegrees = targetAngle;
        } else {
            currentAngleDegrees += (delta > 0.0f ? maxStep : -maxStep);
        }
    }
};

// =============================================================================
//  WheelMotif (Decompiled from SLES_534.39 @ 0x002e81d8 / 0x002e89e0..0x002e8ca0)
// =============================================================================
struct WheelMotif : public DrivelineComponentMotif {
    float mDefaultRadius = 0.4f;            // +0x0c: Default radius in metres (0x002e88c0)
    float mBrakeBalance = 0.5f;             // +0x10: [0.0 - 1.0] (SetBrakeBalance @ 0x002e89a8)
    float mCamber = 0.0f;                   // +0x14: Reserved camber angle (0x002e88d0)
    float mSlipAngleInDegrees = 45.0f;      // +0x18: [0.0 - 180.0] (SetSlipAngleInDegrees @ 0x002e8a20)
    float mSlipTransition = 0.5f;           // +0x1c: [0.0 - 1.0] (SetSlipTransition @ 0x002e8b58)
    float mCosSlipStart = 0.70710678f;      // +0x20: cos(slipAngle * transition) (@ 0x002e8af8 / 0x002e8c30)
    float mCosSlipEnd = 0.70710678f;        // +0x24: cos(slipAngle) (@ 0x002e8b4c / 0x002e8c84)
    float mRotationalInertia = 1.76f;       // +0x28: 0.5 * mass * radius^2 (computed in SetMass @ 0x002e89e0)

    bool mDriving = false;                  // +0x2c: SetDriving @ 0x002e89b8
    bool mPowerslideEffectVisibleOnHiddenWheel = false; // +0x30: SetPowerslideEffectVisibleOnHiddenWheel @ 0x002e8a08
    bool mPhysical = true;                  // +0x34: SetPhysical @ 0x002e8a10
    bool mSteering = false;                 // +0x38: SetSteering @ 0x002e8c90
    bool mSteeringVisual = true;            // +0x3c: SetSteeringVisual @ 0x002e8c98
    bool mVisible = true;                   // +0x40: SetVisible @ 0x002e8ca0
    bool mVisualInverseSteering = false;    // +0x44: SetVisualInverseSteering @ 0x002e89c0

    float mLateralGripInSlip = 5.5f;        // +0x48: [0.0 - 20.0] (SetLateralGripInSlip @ 0x002e89c8)
    float mLateralGripNoSlip = 0.6f;        // +0x4c: [0.0 - 20.0] (SetLateralGripNoSlip @ 0x002e89d0)
    float mLongtitudinalGrip = 2.15f;       // +0x50: [0.0 - 20.0] (SetLongtitudinalGrip @ 0x002e89d8)
    float mMass = 22.0f;                    // +0x54: kg [0.0 - 10000.0] (SetMass @ 0x002e89e0)
    float mRadius = 0.4f;                   // +0x58: Physical radius in metres (GetRadius @ 0x002e8960)
    float mWidth = 0.0f;                    // +0x5c: Width parameter

    // Backward compatibility aliases
    float unkC = 0.4f;
    float unk14 = 0.0f;
    float unk20 = 0.0f;
    float unk24 = 0.0f;
    float unk28 = 0.0f;
    float unk44 = 0.4f;
    float unk48 = 0.0f;

    void RecalculateSlipCosines() {
        constexpr float DEG_TO_RAD = 0.0174532925f; // 0x3c8efa36 in PS2 R5900 asm
        mCosSlipStart = std::cos(mSlipAngleInDegrees * mSlipTransition * DEG_TO_RAD);
        mCosSlipEnd   = std::cos(mSlipAngleInDegrees * DEG_TO_RAD);
        unk20 = mCosSlipStart;
        unk24 = mCosSlipEnd;
    }

    void SetMass(float mass) {
        mMass = mass;
        mRotationalInertia = 0.5f * mMass * mRadius * mRadius; // 0x002e89e0
        unk28 = mRotationalInertia;
    }

    void SetSlipAngleInDegrees(float angle) {
        mSlipAngleInDegrees = angle;
        RecalculateSlipCosines();
    }

    void SetSlipTransition(float trans) {
        mSlipTransition = trans;
        RecalculateSlipCosines();
    }
};

// =============================================================================
//  Wheel Component
// =============================================================================
class Wheel : public DrivelineComponent {
public:
    Wheel() = default;
    ~Wheel() override = default;

    void SetWheelMotif(const std::shared_ptr<WheelMotif>& motif) { m_motif = motif; }
    std::shared_ptr<WheelMotif> GetWheelMotif() const { return m_motif; }

    void SetSuspensionMotif(const std::shared_ptr<SuspensionMotif>& s) { m_suspension = s; }
    std::shared_ptr<SuspensionMotif> GetSuspensionMotif() const { return m_suspension; }

    void SetBrakeMotif(const std::shared_ptr<BrakeMotif>& b) { m_brake = b; }
    std::shared_ptr<BrakeMotif> GetBrakeMotif() const { return m_brake; }

    void SetSteeringWheel(const std::shared_ptr<SteeringWheel>& sw) { m_steeringWheel = sw; }
    std::shared_ptr<SteeringWheel> GetSteeringWheel() const { return m_steeringWheel; }

    void SetInput(DrivelineComponent* input) { m_input = input; }
    DrivelineComponent* GetInput() const { return m_input; }

    // Physical calculations
    float radius = 0.32f; // Metres (~12.6 inches)
    float angularVelocity = 0.0f; // rad/s
    float driveTorque = 0.0f;
    float brakeTorque = 0.0f;
    float lateralForce = 0.0f;
    float longitudinalForce = 0.0f;
    float slipRatio = 0.0f;
    float slipAngle = 0.0f; // radians

    void ApplyTorque(float torque) {
        driveTorque = torque;
    }

    void ApplyBraking(float brakeInput) {
        if (!m_motif || !m_brake) return;
        brakeTorque = brakeInput * m_brake->maxBrakeTorque * m_motif->mBrakeBalance;
    }

    void Update(float forwardSpeed, float lateralSpeed, float normalLoad, float dt) {
        if (!m_motif || !m_motif->mPhysical) {
            driveTorque = 0.0f;
            brakeTorque = 0.0f;
            lateralForce = 0.0f;
            longitudinalForce = 0.0f;
            return;
        }

        // 1. Wheel rotational dynamics & coupling to vehicle road speed
        float roadAngVel = forwardSpeed / radius;
        float wheelInertia = 0.5f * m_motif->mMass * radius * radius;
        if (wheelInertia < 0.05f) wheelInertia = 0.05f;

        if (!m_motif->mDriving && brakeTorque <= 0.0f) {
            // Free-rolling non-driven wheel spins at vehicle road speed
            angularVelocity = roadAngVel;
            slipRatio = 0.0f;
            longitudinalForce = 0.0f;
        } else if (driveTorque <= 0.0f && brakeTorque > 0.0f) {
            // Pure braking: decelerate wheel towards 0, cannot exceed road speed
            if (forwardSpeed >= 0.0f) {
                angularVelocity = std::min(angularVelocity, roadAngVel);
                float decel = (brakeTorque / wheelInertia) * dt;
                angularVelocity = std::max(angularVelocity - decel, 0.0f);
            } else {
                angularVelocity = std::max(angularVelocity, roadAngVel);
                float decel = (brakeTorque / wheelInertia) * dt;
                angularVelocity = std::min(angularVelocity + decel, 0.0f);
            }

            float linearWheelSpeed = angularVelocity * radius;
            float denom = std::max(std::abs(forwardSpeed), 1.0f);
            slipRatio = (linearWheelSpeed - forwardSpeed) / denom;
            slipRatio = std::clamp(slipRatio, -1.0f, 1.0f);
            longitudinalForce = slipRatio * m_motif->mLongtitudinalGrip * normalLoad;
        } else if (driveTorque <= 0.0f && brakeTorque <= 0.0f) {
            // Coasting: drive wheel rapidly couples to road speed
            angularVelocity = roadAngVel;
            slipRatio = 0.0f;
            longitudinalForce = 0.0f;
        } else {
            // Driven wheel with active drive torque
            if (forwardSpeed >= 0.0f) {
                angularVelocity = std::max(angularVelocity, roadAngVel);
            } else {
                angularVelocity = std::min(angularVelocity, roadAngVel);
            }

            float netTorque = driveTorque;
            if (angularVelocity > 0.0f) {
                netTorque -= brakeTorque;
            } else if (angularVelocity < 0.0f) {
                netTorque += brakeTorque;
            }

            // Cap maximum burnout slip delta so wheel angular velocity remains physically bounded
            constexpr float maxSlipDelta = 35.0f; // rad/s
            angularVelocity += (netTorque / wheelInertia) * dt;
            if (forwardSpeed >= 0.0f) {
                angularVelocity = std::min(angularVelocity, roadAngVel + maxSlipDelta);
            } else {
                angularVelocity = std::max(angularVelocity, roadAngVel - maxSlipDelta);
            }

            // 2. Slip calculation
            float linearWheelSpeed = angularVelocity * radius;
            float denom = std::max(std::abs(forwardSpeed), 1.0f);
            slipRatio = (linearWheelSpeed - forwardSpeed) / denom;
            slipRatio = std::clamp(slipRatio, -1.0f, 1.0f);
            longitudinalForce = slipRatio * m_motif->mLongtitudinalGrip * normalLoad;
        }

        slipAngle = std::atan2(lateralSpeed, std::max(std::abs(forwardSpeed), 0.5f));

        // 3. Grip and Force calculation via recovered CTTR parameters
        float slipAngleDeg = std::abs(slipAngle) * (180.0f / 3.14159265f);
        float threshold = m_motif->mSlipAngleInDegrees;
        float transition = std::max(m_motif->mSlipTransition, 0.05f);

        // Blending between NoSlip and Slip grip
        float blend = std::clamp((slipAngleDeg - (threshold * (1.0f - transition))) / (threshold * transition + 1e-4f), 0.0f, 1.0f);
        float effectiveLatGrip = m_motif->mLateralGripNoSlip * (1.0f - blend) + m_motif->mLateralGripInSlip * blend;

        lateralForce = -std::sin(slipAngle) * effectiveLatGrip * normalLoad;

        // Clear per-frame applied inputs
        driveTorque = 0.0f;
        brakeTorque = 0.0f;
    }

private:
    std::shared_ptr<WheelMotif> m_motif;
    std::shared_ptr<SuspensionMotif> m_suspension;
    std::shared_ptr<BrakeMotif> m_brake;
    std::shared_ptr<SteeringWheel> m_steeringWheel;
    DrivelineComponent* m_input = nullptr;
};

// =============================================================================
//  DifferentialMotif & Differential (Decompiled from SLES_534.39 @ 0x001a43a8 / 0x001a4660 / 0x001a47e8)
// =============================================================================
struct DifferentialMotif : public DrivelineComponentMotif {
    float biasRatio       = 0.5f; // Backward compatibility alias
    float mLock           = 0.5f; // +0x0c: 50/50 open/limited-slip split (0x3f000000 @ 0x001a47fc)
    float mCrownGearRatio = 3.6f; // +0x10: Crown / final drive ratio (0x40666666 @ 0x001a4808)
};

class Differential : public DrivelineComponent {
public:
    Differential() = default;
    ~Differential() override = default;

    void SetInput(DrivelineComponent* input) { m_input = input; }
    void SetMotif(const std::shared_ptr<DifferentialMotif>& motif) { m_motif = motif; }
    void SetOutput(int index, DrivelineComponent* output) {
        if (index >= 0 && index < 2) {
            m_outputs[index] = output;
        }
    }

    // Exact PS2 Differential update calculation (0x001a4660)
    void Update() {
        if (!m_motif) return;
        float r0 = m_outputs[0] ? m_outputs[0]->GetAngularVelocity() : 0.0f;
        float r1 = m_outputs[1] ? m_outputs[1]->GetAngularVelocity() : 0.0f;
        m_rpm = 0.5f * (r0 + r1) * m_motif->mCrownGearRatio; // 0x001a4668..0x001a4690
        float inTorque = m_input ? m_input->GetOutputTorque() : 0.0f;
        m_torque = 0.5f * inTorque * m_motif->mCrownGearRatio; // 0x001a4694..0x001a46a4
    }

    void DistributeTorque(float inputTorque) {
        float crownRatio = m_motif ? m_motif->mCrownGearRatio : 3.6f;
        float halfTorque = inputTorque * 0.5f;

        if (auto* w0 = dynamic_cast<Wheel*>(m_outputs[0])) {
            w0->ApplyTorque(halfTorque);
        }
        if (auto* w1 = dynamic_cast<Wheel*>(m_outputs[1])) {
            w1->ApplyTorque(halfTorque);
        }
    }

    float GetOutputTorque() const override { return m_torque; }
    float GetAngularVelocity() const override { return m_rpm; }

private:
    DrivelineComponent* m_input = nullptr;
    DrivelineComponent* m_outputs[2] = {nullptr, nullptr};
    std::shared_ptr<DifferentialMotif> m_motif;
    float m_rpm = 0.0f;
    float m_torque = 0.0f;
};

// =============================================================================
//  EngineMotif & Engine (Decompiled from SLES_534.39 @ 0x001c2fd0 / 0x001c31f0)
// =============================================================================
struct EngineMotif : public DrivelineComponentMotif {
    float mMaxRPM             = 7500.0f;  // +0x0c: (0x45ea6000 @ 0x001c3210)
    float mMaxRPMRateOfChange = 15000.0f; // +0x10: (0x466a6000 @ 0x001c3220)
    float mMaxTorque          = 1000.0f;  // +0x14: (0x447a0000 @ 0x001c3204)
    float mMaxTorqueWithTurbo = 1500.0f;  // +0x18: (0x44bb8000 @ 0x001c3230)
    float mMinRPM             = 1000.0f;  // +0x1c: (0x447a0000 @ 0x001c3264)
    float mRPMRange           = 6500.0f;  // +0x20: mMaxRPM - mMinRPM (0x45cb2000 @ 0x001c3240)

    // Exact 10-point normalized torque curve table from .data @ 0x0050ffc0
    float mTorqueCurve[10] = {
        0.30f, 0.51f, 0.67f, 0.72f, 0.93f, 0.99f, 1.00f, 0.94f, 0.89f, 0.81f
    };

    // Exact piecewise linear torque evaluation (0x001c32d0)
    float EvaluateTorqueCurve(float normalizedRPM) const {
        float scaled = std::clamp(normalizedRPM, 0.0f, 1.0f) * 9.0f; // 0..9 index space
        int idx = static_cast<int>(scaled);
        int nextIdx = std::min(idx + 1, 9);
        float frac = scaled - static_cast<float>(idx);
        return mTorqueCurve[idx] + frac * (mTorqueCurve[nextIdx] - mTorqueCurve[idx]);
    }
};

// =============================================================================
//  TransmissionMotif (Decompiled from SLES_534.39 @ 0x002c2a40 / 0x002c3138)
// =============================================================================
struct TransmissionMotif : public DrivelineComponentMotif {
    float mDownShiftRPM          = 2500.0f; // +0x0c: (0x451c4000 @ 0x002c315c)
    float mEfficiency            = 1.0f;    // +0x10: (0x3f800000 @ 0x002c3150)
    
    // Exact 8-ratio gear table from .rodata @ 0x005766b8
    // Index 0 = R (-1.3), 1 = N (0.0), 2 = G1 (2.66), 3 = G2 (1.78), 4 = G3 (1.30), 5 = G4 (1.00), 6 = G5 (0.74), 7 = G6 (0.50)
    float mGearRatios[8] = {
        -1.30f, 0.00f, 2.66f, 1.78f, 1.30f, 1.00f, 0.74f, 0.50f
    };

    bool  mAutomatic             = true;    // +0x34: (SetAutomatic @ 0x002c3218)
    float mMinTimeBetweenShifts  = 1.0f;    // +0x38: (0x3f800000 @ 0x002c3198)
    int32_t mTopGear             = 4;       // +0x3c: Gear 4 (0x002c3178)
    float mUpShiftRPM            = 5000.0f; // +0x40: (0x459c4000 @ 0x002c3168)
};

// =============================================================================
//  CarPhysicsMotif (Decompiled from SLES_534.39 @ 0x00173468 / 0x00173650)
// =============================================================================
struct CarPhysicsMotif {
    float mCmYOffset             = 0.0f;    // +0x08: (SetCmYOffset @ 0x00174938)
    float mCmZOffset             = 0.2f;    // +0x0c: (SetCmZOffset @ 0x00174940)
    float mDragCoeff             = 0.38f;   // +0x1c: Precomputed in Initialize (@ 0x001735c8)
    float mDragCoeffWithTurbo    = 0.45f;   // +0x20: Precomputed in Initialize (@ 0x00173638)
    float mMass                  = 1000.0f; // +0x2c: kg (SetMass @ 0x00174948)
    float mPowerSlideBoost       = 1.0f;    // +0x3c: (SetPowerSlideBoost @ 0x00174950)
    float mPowerSlideKickout     = 4.5f;    // +0x40: (SetPowerSlideKickout @ 0x00174958)
    float mScootchImpulseMagnitude = 60.0f; // +0x44: (SetScootchImpulseMagnitude @ 0x00174960)
    float mScootchImpulseTime    = 0.25f;   // +0x48: (SetScootchImpulseTime @ 0x00174968)
    float mScootchMinTimeBetween = 0.5f;    // +0x4c: (SetScootchMinTimeBetween @ 0x00174970)
    float mScootchTurboUsage     = 10.0f;   // +0x50: (SetScootchTurboUsage @ 0x00174978)
    float mTopSpeedKph           = 145.0f;  // +0x6c: (SetTopSpeedKph @ 0x00174980)
    float mTopSpeedKphWithTurbo  = 195.0f;  // +0x70: (SetTopSpeedKphWithTurbo @ 0x00174988)
    float mTurboMeterCapacity    = 100.0f;  // +0x78: (SetTurboMeterCapacity @ 0x00174620)
    float mTurboMeterMinimumLevelForUsage = 50.0f; // +0x7c: (SetTurboMeterMinimumLevelForUsage @ 0x00174628)
    float mTurboMeterRechargePerSecond = 5.0f;     // +0x80: (SetTurboMeterRechargePerSecond @ 0x00174630)
    float mTurboMeterUsagePerSecond    = 14.0f;    // +0x84: (SetTurboMeterUsagePerSecond @ 0x00174638)
    int32_t mWheelCount          = 5;       // +0x8c: (SetWheelCount @ 0x00174998)
    float mWheelGripLossDueToGas = 0.25f;   // +0x90: (SetWheelGripLossDueToGas @ 0x001749a0)
    float mWheelSlipTendency     = 0.5f;    // +0x98: (SetWheelSlipTendency @ 0x001749a8)
    float mWeebleAmount          = 1.0f;    // +0x9c: (SetWeebleAmount @ 0x00174990)

    // Exact PS2 Top-Speed Drag Derivation Formula (0x001735a4..0x00173638)
    void ComputeDragCoefficients(float maxDriveForce, float maxDriveForceTurbo) {
        constexpr float KPH_TO_MS = 0.2777778f; // 0x3e8e38e4
        float vMax = mTopSpeedKph * KPH_TO_MS;
        float vMaxTurbo = mTopSpeedKphWithTurbo * KPH_TO_MS;
        if (vMax > 1.0f) {
            mDragCoeff = maxDriveForce / (vMax * (vMax + 30.0f)); // 0x001735b0..0x001735c8
        }
        if (vMaxTurbo > 1.0f) {
            mDragCoeffWithTurbo = maxDriveForceTurbo / (vMaxTurbo * (vMaxTurbo + 30.0f)); // 0x00173620..0x00173638
        }
    }
};

// =============================================================================
//  Transmission & Engine (CTTR CrashTripod Specs: 3-Speed + Reverse)
// =============================================================================
class Transmission : public DrivelineComponent {
public:
    // Gear ratios from CTTR game data (Section 5.1 of rebuild specification)
    const float gearRatios[4] = {
        -2.0f,  // Gear 0: Reverse
         3.0f,  // Gear 1: 1st
         1.5f,  // Gear 2: 2nd
         1.0f   // Gear 3: 3rd (Top)
    };

    const float upShiftRPM   = 6500.0f;
    const float downShiftRPM = 4000.0f;
    const float maxRPM       = 8000.0f;
    const float idleRPM      = 1000.0f;

    // Torque ratings: Standard 1000 Nm, Turbo 3000 Nm
    const float maxEngineTorque = 1000.0f;
    const float turboEngineTorque = 3000.0f;

    int currentGear = 1; // Default to 1st
    float currentRPM = 1000.0f;
    float outputTorque = 0.0f;

    void Update(float throttleInput, bool reverseRequest, bool turboActive, float wheelSpeedRadS, float dt) {
        if (reverseRequest) {
            currentGear = 0; // Reverse
        } else if (currentGear == 0) {
            currentGear = 1; // Return to 1st
        }

        float ratio = gearRatios[currentGear];

        // Engine RPM tracks wheel rotational speed through gear ratio
        float targetRPM = std::abs(wheelSpeedRadS) * (ratio * 60.0f / (2.0f * 3.14159265f)) * 3.5f; // Final drive ratio ~3.5
        targetRPM = std::clamp(targetRPM, idleRPM, maxRPM);

        // Smooth RPM transition
        currentRPM += (targetRPM - currentRPM) * std::clamp(dt * 15.0f, 0.0f, 1.0f);
        if (throttleInput > 0.05f && currentRPM < 2500.0f) {
            currentRPM += throttleInput * 3000.0f * dt;
        }

        // Automatic gear shifting
        if (currentGear > 0) {
            if (currentRPM >= upShiftRPM && currentGear < 3) {
                currentGear++;
            } else if (currentRPM <= downShiftRPM && currentGear > 1) {
                currentGear--;
            }
        }

        // Torque delivery
        float peakTorque = turboActive ? turboEngineTorque : maxEngineTorque;
        // Normalized torque curve: peak at mid-range RPM
        float rpmFactor = std::sin((currentRPM / maxRPM) * 3.14159265f * 0.85f);
        outputTorque = throttleInput * peakTorque * std::max(rpmFactor, 0.35f) * ratio * 3.5f;
    }

    float GetOutputTorque() const override { return outputTorque; }
};

} // namespace Physics
} // namespace CTTR
