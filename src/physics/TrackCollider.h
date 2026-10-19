#pragma once

#include <vector>
#include <cmath>
#include <algorithm>
#include <unordered_map>
#include <iostream>
#include <glm/glm.hpp>
#include "../renderer/P3DLoader.h"

namespace CTTR {
namespace Physics {

struct Triangle2D {
    glm::vec3 p0, p1, p2;
    glm::vec3 normal;
};

class TrackCollider {
public:
    void Build(const std::vector<CTTR::Renderer::P3DMesh>& meshes) {
        m_triangles.clear();
        for (const auto& m : meshes) {
            // Include track road surfaces
            if (m.name.find("trackShape") != std::string::npos ||
                m.shaderName.find("drt_") != std::string::npos ||
                m.shaderName.find("brk_") != std::string::npos ||
                m.shaderName.find("checkered") != std::string::npos ||
                m.shaderName.find("wud_bridge") != std::string::npos) {
                
                for (size_t i = 0; i + 2 < m.indices.size(); i += 3) {
                    const auto& a = m.vertices[m.indices[i]].position;
                    const auto& b = m.vertices[m.indices[i+1]].position;
                    const auto& c = m.vertices[m.indices[i+2]].position;

                    glm::vec3 n = glm::cross(b - a, c - a);
                    if (glm::length(n) > 1e-4f) {
                        n = glm::normalize(n);
                        if (n.y < 0.0f) {
                            n = -n;
                        }
                        // Upward-facing driving surfaces
                        if (n.y > 0.2f) {
                            m_triangles.push_back({a, b, c, n});
                        }
                    }
                }
            }
        }
        std::cout << "[TrackCollider] Built with " << m_triangles.size() << " drivable road triangles." << std::endl;
        BuildGrid(10.0f);
    }

    void BuildGrid(float cellSize = 10.0f) {
        m_cellSize = cellSize;
        m_grid.clear();
        for (size_t i = 0; i < m_triangles.size(); ++i) {
            const auto& tri = m_triangles[i];
            float minX = std::min({tri.p0.x, tri.p1.x, tri.p2.x});
            float maxX = std::max({tri.p0.x, tri.p1.x, tri.p2.x});
            float minZ = std::min({tri.p0.z, tri.p1.z, tri.p2.z});
            float maxZ = std::max({tri.p0.z, tri.p1.z, tri.p2.z});

            int x0 = static_cast<int>(std::floor(minX / m_cellSize));
            int x1 = static_cast<int>(std::floor(maxX / m_cellSize));
            int z0 = static_cast<int>(std::floor(minZ / m_cellSize));
            int z1 = static_cast<int>(std::floor(maxZ / m_cellSize));

            for (int cx = x0; cx <= x1; ++cx) {
                for (int cz = z0; cz <= z1; ++cz) {
                    uint64_t key = (static_cast<uint64_t>(static_cast<uint32_t>(cx)) << 32) | static_cast<uint32_t>(cz);
                    m_grid[key].push_back(i);
                }
            }
        }
        std::cout << "[TrackCollider] Spatial grid built with " << m_grid.size() << " active cells (cell size " << m_cellSize << "m)." << std::endl;
    }

    bool SampleHeight(float x, float z, float currentY, float& outY, glm::vec3& outNormal) const {
        int cx = static_cast<int>(std::floor(x / m_cellSize));
        int cz = static_cast<int>(std::floor(z / m_cellSize));
        uint64_t key = (static_cast<uint64_t>(static_cast<uint32_t>(cx)) << 32) | static_cast<uint32_t>(cz);

        auto it = m_grid.find(key);
        if (it == m_grid.end()) {
            return false;
        }

        float bestDist = 1e9f;
        bool found = false;

        for (size_t idx : it->second) {
            const auto& tri = m_triangles[idx];
            // 2D Barycentric coordinates in XZ
            float denom = (tri.p1.z - tri.p2.z) * (tri.p0.x - tri.p2.x) + (tri.p2.x - tri.p1.x) * (tri.p0.z - tri.p2.z);
            if (std::abs(denom) < 1e-5f) continue;

            float w0 = ((tri.p1.z - tri.p2.z) * (x - tri.p2.x) + (tri.p2.x - tri.p1.x) * (z - tri.p2.z)) / denom;
            float w1 = ((tri.p2.z - tri.p0.z) * (x - tri.p2.x) + (tri.p0.x - tri.p2.x) * (z - tri.p2.z)) / denom;
            float w2 = 1.0f - w0 - w1;

            if (w0 >= -0.05f && w1 >= -0.05f && w2 >= -0.05f) {
                float y = w0 * tri.p0.y + w1 * tri.p1.y + w2 * tri.p2.y;
                float dist = std::abs(y - currentY);
                if (dist < bestDist) {
                    bestDist = dist;
                    outY = y;
                    outNormal = tri.normal;
                    found = true;
                }
            }
        }
        return found;
    }

    const std::vector<Triangle2D>& GetTriangles() const { return m_triangles; }

private:
    std::vector<Triangle2D> m_triangles;
    float m_cellSize = 10.0f;
    std::unordered_map<uint64_t, std::vector<size_t>> m_grid;
};

} // namespace Physics
} // namespace CTTR
