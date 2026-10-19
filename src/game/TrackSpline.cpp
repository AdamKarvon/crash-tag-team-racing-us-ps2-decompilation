#include "TrackSpline.h"
#include <fstream>
#include <iostream>
#include <cmath>
#include <algorithm>
#include <cstring>

namespace CTTR {
namespace Game {

    bool TrackSpline::LoadFromMid(const std::string& midFilePath) {
        m_sections.clear();
        m_totalLength = 0.0f;

        std::ifstream file(midFilePath, std::ios::binary);
        if (!file.is_open()) {
            std::cerr << "[TrackSpline] Failed to open: " << midFilePath << std::endl;
            return false;
        }

        std::vector<uint8_t> data((std::istreambuf_iterator<char>(file)),
                                   std::istreambuf_iterator<char>());

        if (data.size() < 0x08a0 + 56) {
            std::cerr << "[TrackSpline] File too small: " << midFilePath << " (" << data.size() << " bytes)" << std::endl;
            return false;
        }

        // Search for primary road network records (standard offset: 0x08a0)
        size_t startOffset = 0x08a0;
        const size_t recSize = 56;

        auto readFloats = [&](size_t off) {
            float flts[14];
            std::memcpy(flts, data.data() + off, sizeof(flts));
            return std::vector<float>(flts, flts + 14);
        };

        // If standard offset doesn't match, scan for candidate start
        bool foundStart = false;
        if (startOffset + recSize <= data.size()) {
            auto f = readFloats(startOffset);
            if (std::abs(f[6] - 0.5f) < 1e-3f && f[7] > 100.0f) {
                foundStart = true;
            }
        }

        if (!foundStart) {
            for (size_t off = 0x100; off + recSize * 2 <= data.size(); off += 4) {
                auto f = readFloats(off);
                if (std::abs(f[6] - 0.5f) < 1e-3f && f[7] > 500.0f) {
                    auto f2 = readFloats(off + recSize);
                    if (std::abs(f2[6] - 0.5f) < 1e-3f && f2[7] < f[7]) {
                        startOffset = off;
                        foundStart = true;
                        break;
                    }
                }
            }
        }

        if (!foundStart) {
            std::cerr << "[TrackSpline] Could not locate road cross-section stream in: " << midFilePath << std::endl;
            return false;
        }

        struct RawPoint {
            glm::vec3 left;
            glm::vec3 right;
            float distRem;
            float centerRatio;
        };
        std::vector<RawPoint> rawPoints;

        size_t currentOff = startOffset;
        while (currentOff + recSize <= data.size()) {
            auto f = readFloats(currentOff);
            float lx = f[0], ly = f[1], lz = f[2];
            float rx = f[3], ry = f[4], rz = f[5];
            float centerRatio = f[6];
            float distRem = f[7];

            // In Radical's cross-sections, centerRatio is between 0.1 and 0.9 across road curves
            if (centerRatio <= 0.05f || centerRatio >= 0.95f) {
                break; // End of main road network
            }

            // Convert PS2 coordinates to OpenGL: Z is negated
            glm::vec3 glLeft(lx, ly, -lz);
            glm::vec3 glRight(rx, ry, -rz);
            rawPoints.push_back({glLeft, glRight, distRem, centerRatio});

            currentOff += recSize;
            if (distRem <= 0.0f) {
                break; // Reached loop closure
            }
        }

        if (rawPoints.size() < 3) {
            std::cerr << "[TrackSpline] Insufficient road points parsed: " << rawPoints.size() << std::endl;
            return false;
        }

        m_totalLength = rawPoints[0].distRem;
        m_sections.resize(rawPoints.size());

        for (size_t i = 0; i < rawPoints.size(); ++i) {
            const auto& rp = rawPoints[i];
            RoadCrossSection& sec = m_sections[i];
            sec.left = rp.left;
            sec.right = rp.right;
            sec.center = rp.left + rp.centerRatio * (rp.right - rp.left);
            sec.width = glm::distance(rp.left, rp.right);
            sec.arcLength = std::clamp(m_totalLength - rp.distRem, 0.0f, m_totalLength);
        }

        // Calculate smooth tangents and surface normals
        const size_t n = m_sections.size();
        for (size_t i = 0; i < n; ++i) {
            size_t nextIdx = (i + 1) % n;
            size_t prevIdx = (i + n - 1) % n;

            glm::vec3 fwd = m_sections[nextIdx].center - m_sections[prevIdx].center;
            if (glm::length(fwd) < 1e-4f) {
                fwd = m_sections[nextIdx].center - m_sections[i].center;
            }
            sec_tangent:
            if (glm::length(fwd) > 1e-4f) {
                m_sections[i].tangent = glm::normalize(fwd);
            } else {
                m_sections[i].tangent = glm::vec3(0.0f, 0.0f, -1.0f);
            }

            glm::vec3 lateral = m_sections[i].right - m_sections[i].left;
            if (glm::length(lateral) > 1e-4f) {
                lateral = glm::normalize(lateral);
            } else {
                lateral = glm::vec3(1.0f, 0.0f, 0.0f);
            }

            glm::vec3 norm = glm::cross(m_sections[i].tangent, lateral);
            if (norm.y < 0.0f) norm = -norm;
            if (glm::length(norm) > 1e-4f) {
                m_sections[i].normal = glm::normalize(norm);
            } else {
                m_sections[i].normal = glm::vec3(0.0f, 1.0f, 0.0f);
            }
        }

        std::cout << "[TrackSpline] Successfully loaded " << m_sections.size() 
                  << " cross-sections from " << midFilePath 
                  << " (Total Circuit Length: " << m_totalLength << " m)" << std::endl;
        return true;
    }

    TrackProjection TrackSpline::Project(const glm::vec3& worldPos) const {
        TrackProjection proj{};
        if (m_sections.size() < 2) return proj;

        float bestDistSq = 1e18f;
        size_t bestSeg = 0;
        float bestT = 0.0f;
        glm::vec3 bestProjPoint{0.0f};

        const size_t n = m_sections.size();
        for (size_t i = 0; i < n; ++i) {
            size_t next = (i + 1) % n;
            const glm::vec3& p0 = m_sections[i].center;
            const glm::vec3& p1 = m_sections[next].center;

            glm::vec3 segVec = p1 - p0;
            float lenSq = glm::dot(segVec, segVec);
            if (lenSq < 1e-6f) continue;

            float t = glm::clamp(glm::dot(worldPos - p0, segVec) / lenSq, 0.0f, 1.0f);
            glm::vec3 pt = p0 + t * segVec;
            float dSq = glm::dot(worldPos - pt, worldPos - pt);

            if (dSq < bestDistSq) {
                bestDistSq = dSq;
                bestSeg = i;
                bestT = t;
                bestProjPoint = pt;
            }
        }

        size_t next = (bestSeg + 1) % n;
        float s0 = m_sections[bestSeg].arcLength;
        float s1 = m_sections[next].arcLength;
        if (next == 0) s1 = m_totalLength; // Loop wrap

        float s = s0 + bestT * (s1 - s0);
        proj.arcLength = s;
        proj.normalizedProgress = (m_totalLength > 0.0f) ? std::clamp(s / m_totalLength, 0.0f, 0.999999f) : 0.0f;
        proj.projectedPoint = bestProjPoint;
        proj.segmentIndex = bestSeg;

        // Interpolated tangent and width
        proj.tangent = glm::normalize(glm::mix(m_sections[bestSeg].tangent, m_sections[next].tangent, bestT));
        float width = glm::mix(m_sections[bestSeg].width, m_sections[next].width, bestT);

        // Lateral offset: compute signed distance across track
        glm::vec3 roadRight = glm::normalize(m_sections[bestSeg].right - m_sections[bestSeg].left);
        proj.lateralOffset = glm::dot(worldPos - bestProjPoint, roadRight);
        proj.isInsideTrack = std::abs(proj.lateralOffset) <= (width * 0.5f + 1.0f); // 1m margin

        return proj;
    }

    glm::vec3 TrackSpline::SampleCenterline(float s) const {
        if (m_sections.empty()) return glm::vec3(0.0f);
        if (m_sections.size() == 1 || m_totalLength <= 0.0f) return m_sections[0].center;

        // Wrap s into [0, totalLength)
        while (s < 0.0f) s += m_totalLength;
        while (s >= m_totalLength) s -= m_totalLength;

        // Find segment
        for (size_t i = 0; i < m_sections.size(); ++i) {
            size_t next = (i + 1) % m_sections.size();
            float s0 = m_sections[i].arcLength;
            float s1 = (next == 0) ? m_totalLength : m_sections[next].arcLength;

            if (s >= s0 && s <= s1) {
                float t = (s1 > s0) ? (s - s0) / (s1 - s0) : 0.0f;
                return glm::mix(m_sections[i].center, m_sections[next].center, t);
            }
        }
        return m_sections.back().center;
    }

    glm::vec3 TrackSpline::SampleTangent(float s) const {
        if (m_sections.empty()) return glm::vec3(0.0f, 0.0f, -1.0f);
        if (m_totalLength <= 0.0f) return m_sections[0].tangent;

        while (s < 0.0f) s += m_totalLength;
        while (s >= m_totalLength) s -= m_totalLength;

        for (size_t i = 0; i < m_sections.size(); ++i) {
            size_t next = (i + 1) % m_sections.size();
            float s0 = m_sections[i].arcLength;
            float s1 = (next == 0) ? m_totalLength : m_sections[next].arcLength;

            if (s >= s0 && s <= s1) {
                float t = (s1 > s0) ? (s - s0) / (s1 - s0) : 0.0f;
                return glm::normalize(glm::mix(m_sections[i].tangent, m_sections[next].tangent, t));
            }
        }
        return m_sections.back().tangent;
    }

    float TrackSpline::SampleWidth(float s) const {
        if (m_sections.empty()) return 30.0f;
        while (s < 0.0f) s += m_totalLength;
        while (s >= m_totalLength) s -= m_totalLength;

        for (size_t i = 0; i < m_sections.size(); ++i) {
            size_t next = (i + 1) % m_sections.size();
            float s0 = m_sections[i].arcLength;
            float s1 = (next == 0) ? m_totalLength : m_sections[next].arcLength;

            if (s >= s0 && s <= s1) {
                float t = (s1 > s0) ? (s - s0) / (s1 - s0) : 0.0f;
                return glm::mix(m_sections[i].width, m_sections[next].width, t);
            }
        }
        return m_sections.back().width;
    }

} // namespace Game
} // namespace CTTR
