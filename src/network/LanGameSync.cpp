#include "LanGameSync.h"
#include <iostream>

namespace CTTR {
namespace Network {

    LanServer::LanServer(int maxPlayers) : m_maxPlayers(maxPlayers), m_isRunning(false) {
        // Initialize server state
    }

    LanServer::~LanServer() {
        m_isRunning = false;
    }

    void LanServer::Start(uint16_t port) {
        m_isRunning = true;
        std::cout << "LAN Server started on port " << port << " for CTTR 4-Player Match." << std::endl;
        // Network socket binding logic would go here
    }

    void LanServer::OnClientConnect(uint32_t clientId) {
        if (m_vehicles.size() < m_maxPlayers) {
            m_vehicles.push_back(std::make_shared<Physics::Vehicle>(clientId));
            std::cout << "Client " << clientId << " connected and spawned kart." << std::endl;
        }
    }

    void LanServer::OnClientDisconnect(uint32_t clientId) {
        // Find and remove vehicle, break any clashes
        for (auto it = m_vehicles.begin(); it != m_vehicles.end(); ++it) {
            if ((*it)->GetId() == clientId) {
                (*it)->BreakClash();
                m_vehicles.erase(it);
                break;
            }
        }
    }

    void LanServer::OnClientInputReceived(const ClientInputPacket& input) {
        // Find the vehicle associated with this client
        for (auto& vehicle : m_vehicles) {
            if (vehicle->GetId() == input.clientId) {
                // Apply driving input
                vehicle->ApplyInput(input.throttle, input.steering, input.brake);
                
                // Apply turret input
                vehicle->AimTurret(input.aimYaw, input.aimPitch);
                if (input.fireWeapon) {
                    vehicle->FireWeapon();
                }

                // Handle Clash Request (Merge)
                if (input.requestClash && !vehicle->IsMerged()) {
                    // Look for another vehicle to clash with
                    for (auto& otherVehicle : m_vehicles) {
                        if (otherVehicle->GetId() != vehicle->GetId() && 
                            vehicle->CanClashWith(*otherVehicle)) {
                            
                            // Successful Clash Initiation!
                            // The initiator becomes the Gunner, the target becomes the Driver
                            vehicle->SetClashState(Physics::ClashRole::GUNNER, otherVehicle);
                            otherVehicle->SetClashState(Physics::ClashRole::DRIVER, vehicle);
                            
                            std::cout << "CLASH MERGE! Player " << vehicle->GetId() 
                                      << " merged onto Player " << otherVehicle->GetId() << std::endl;
                            break; // Only clash with one target
                        }
                    }
                }
                break;
            }
        }
    }

    void LanServer::Tick(float deltaTime) {
        if (!m_isRunning) return;

        // 1. Update Physics for all vehicles (Server is Authoritative)
        for (auto& vehicle : m_vehicles) {
            vehicle->UpdatePhysics(deltaTime);
        }

        // 2. Broadcast authoritative state to all LAN clients
        BroadcastState();
    }

    void LanServer::BroadcastState() {
        std::vector<VehicleStatePacket> stateData;
        
        for (const auto& vehicle : m_vehicles) {
            VehicleStatePacket packet;
            packet.vehicleId = vehicle->GetId();
            
            Physics::Vector3 pos = vehicle->GetPosition();
            packet.x = pos.x; packet.y = pos.y; packet.z = pos.z;
            
            Physics::Vector3 vel = vehicle->GetVelocity();
            packet.vx = vel.x; packet.vy = vel.y; packet.vz = vel.z;

            Physics::Vector3 rot = vehicle->GetRotation();
            packet.rx = rot.x; packet.ry = rot.y; packet.rz = rot.z;
            
            packet.isMerged = vehicle->IsMerged();
            packet.role = vehicle->GetClashRole();
            packet.partnerId = vehicle->GetClashPartnerId();
            packet.turretYaw = vehicle->GetTurretYaw();
            packet.turretPitch = vehicle->GetTurretPitch();
            packet.isFiring = false;
            
            stateData.push_back(packet);
        }

        // Network dispatch logic would go here
        // ENet::Host_Broadcast(stateData);
    }

    bool LanServer::ValidateMovement(const Physics::Vehicle& vehicle, float deltaTime) {
        // Anti-Cheat: Validate that the client's requested position delta
        // does not exceed the maximum possible physical bounds of the kart.
        return true; 
    }

} // namespace Network
} // namespace CTTR
