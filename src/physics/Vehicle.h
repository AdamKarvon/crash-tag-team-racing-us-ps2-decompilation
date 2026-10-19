#pragma once
#include <cstdint>
#include <memory>
#include <vector>
#include <string>
#include "Driveline.h"
#include "../game/GameEnums.h"

namespace CTTR {
namespace Physics {

    struct Vector3 {
        float x, y, z;
    };

    enum class ClashRole {
        NONE,
        DRIVER,
        GUNNER
    };

    struct VehicleStateSnapshot {
        uint32_t id;
        Vector3 position;
        Vector3 rotation;
        Vector3 velocity;
        float throttle;
        float steering;
        ClashRole clashRole;
        uint32_t clashPartnerId;
        float turretYaw;
        float turretPitch;
        int currentGear;
        float engineRPM;
        float turboLevel;
    };

    class Vehicle {
    public:
        Vehicle(uint32_t id);
        ~Vehicle() = default;

        // Core Physics Update (60Hz deterministic tick)
        void UpdatePhysics(float deltaTime);
        
        // Input Handling
        void ApplyInput(float throttle, float steering, bool brake);
        void SetPowerslideInput(bool powerslide);
        void ActivateTurbo();

        // Clashing / Merging System
        bool CanClashWith(const Vehicle& other) const;
        void InitiateClash(std::shared_ptr<Vehicle> targetVehicle);
        void SetClashState(ClashRole role, std::shared_ptr<Vehicle> partner);
        void BreakClash();

        // Weapon System (Active when Gunner)
        void AimTurret(float yaw, float pitch);
        void FireWeapon();

        // State Retrieval for Network Sync & Physics Validation
        Vector3 GetPosition() const { return m_position; }
        Vector3 GetVelocity() const { return m_velocity; }
        Vector3 GetRotation() const { return m_rotation; }
        uint32_t GetId() const { return m_id; }
        bool IsMerged() const { return m_clashRole != ClashRole::NONE; }
        ClashRole GetClashRole() const { return m_clashRole; }
        uint32_t GetClashPartnerId() const;
        float GetTurretYaw() const { return m_turretYaw; }
        float GetTurretPitch() const { return m_turretPitch; }
        float GetMass() const { return m_mass; }
        float GetTurboAmount() const { return m_turboAmount; }
        void AddTurbo(float amount) { m_turboAmount = std::clamp(m_turboAmount + amount, 0.0f, 100.0f); }
        bool IsTurboActive() const { return m_turboActive; }
        bool IsPowersliding() const { return m_powersliding; }
        float GetSpeedKPH() const;

        void SetPosition(const Vector3& pos) { m_position = pos; }
        void SetVelocity(const Vector3& vel) { m_velocity = vel; }
        void SetRotation(const Vector3& rot) { m_rotation = rot; }
        void SetMass(float mass) { m_mass = mass; }
        void SetGroundHeight(float groundY) { m_groundY = groundY; }
        float GetGroundHeight() const { return m_groundY; }

        // Driveline Component Access
        const Transmission& GetTransmission() const { return m_transmission; }
        const Wheel& GetWheel(size_t index) const { return m_wheels[index]; }
        const Differential& GetDifferential() const { return m_differential; }
        const SteeringWheel& GetSteeringWheel() const { return m_steering; }

        VehicleStateSnapshot CaptureState() const;
        uint64_t GetDeterministicStateHash() const;

    private:
        void InitializeDriveline();

        uint32_t m_id;
        
        // Transform State
        Vector3 m_position;
        Vector3 m_rotation;
        Vector3 m_velocity;
        Vector3 m_angularVelocity;

        // Vehicle Chassis Parameters
        float m_mass; // 1,000 kg standard, 1,500 kg Uber
        float m_centerOfMassZOffset; // 0.2 standard, 0.3 Uber
        
        // Input State
        float m_currentThrottle;
        float m_currentSteering;
        bool m_currentBrake;
        bool m_powerslideInput;

        // Turbo & Drift Dynamics
        float m_turboAmount; // 0..100
        bool m_turboActive;
        bool m_powersliding;
        float m_powerslideStageTimer;
        TurboIncreaseEvent m_lastTurboEvent;

        // Clash State
        ClashRole m_clashRole;
        std::weak_ptr<Vehicle> m_clashPartner;

        // Turret State
        float m_turretYaw;
        float m_turretPitch;

        // Driveline Components (cttr-main architecture)
        Transmission m_transmission;
        Differential m_differential;
        SteeringWheel m_steering;
        std::vector<Wheel> m_wheels; // 5 wheels (FL, FR, RL, RR, Virtual 3rd)
        std::shared_ptr<BrakeMotif> m_brakeMotif;
        std::shared_ptr<SuspensionMotif> m_suspensionMotif;
        float m_groundY = 0.0f;
    };

} // namespace Physics
} // namespace CTTR
