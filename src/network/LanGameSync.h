#pragma once
#include <cstdint>
#include <vector>
#include <memory>
#include "../physics/Vehicle.h"

namespace CTTR {
namespace Network {

    // Defines the network packet for vehicle state updates
    struct VehicleStatePacket {
        uint32_t vehicleId;
        float x, y, z;          // Position
        float vx, vy, vz;       // Velocity
        float rx, ry, rz;       // Rotation
        
        // Clash/Merge Data
        bool isMerged;
        Physics::ClashRole role;
        uint32_t partnerId;
        
        // Turret Data
        float turretYaw;
        float turretPitch;
        bool isFiring;
    };

    // Defines client input to be sent to the server (Authoritative model)
    struct ClientInputPacket {
        uint32_t clientId;
        float throttle;
        float steering;
        bool brake;
        bool requestClash;       // Player hit the "merge" button
        float aimYaw;
        float aimPitch;
        bool fireWeapon;
    };

    class LanServer {
    public:
        LanServer(int maxPlayers = 4);
        ~LanServer();

        void Start(uint16_t port);
        void Tick(float deltaTime); // Called every server frame (e.g., 60Hz)
        
        // Network Event Handlers
        void OnClientConnect(uint32_t clientId);
        void OnClientDisconnect(uint32_t clientId);
        void OnClientInputReceived(const ClientInputPacket& input);

    private:
        void BroadcastState();
        void ProcessClashRequests();

        std::vector<std::shared_ptr<Physics::Vehicle>> m_vehicles;
        int m_maxPlayers;
        bool m_isRunning;
        
        // Anti-cheat / Validation
        bool ValidateMovement(const Physics::Vehicle& vehicle, float deltaTime);
    };

} // namespace Network
} // namespace CTTR
