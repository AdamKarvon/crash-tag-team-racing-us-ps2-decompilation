#pragma once
// =============================================================================
//  TrackSpline.h  —  Track centerline spline, widths, and progress projection
//  Parses authentic road cross-sections from roads.mid
// =============================================================================

#include <vector>
#include <string>
#include <glm/glm.hpp>

namespace CTTR {
namespace Game {

    struct RoadCrossSection {
        glm::vec3 left{0.0f};    // Left road boundary in OpenGL space
        glm::vec3 right{0.0f};   // Right road boundary in OpenGL space
        glm::vec3 center{0.0f};  // Centerline point
        glm::vec3 tangent{0.0f}; // Unit forward track tangent
        glm::vec3 normal{0.0f};  // Up normal of the road surface
        float width = 0.0f;      // Total road width in metres
        float arcLength = 0.0f;  // Cumulative distance from start line [0 .. totalLength]
    };

    struct TrackProjection {
        float arcLength = 0.0f;          // Distance along circuit s in [0 .. totalLength]
        float normalizedProgress = 0.0f; // s / totalLength in [0.0 .. 1.0)
        float lateralOffset = 0.0f;      // Distance from centerline (positive = right, negative = left)
        glm::vec3 projectedPoint{0.0f};  // Point on centerline
        glm::vec3 tangent{0.0f};         // Track direction at projected point
        size_t segmentIndex = 0;         // Segment index [0 .. N-1]
        bool isInsideTrack = true;       // True if |lateralOffset| <= roadWidth * 0.5
    };

    class TrackSpline {
    public:
        TrackSpline() = default;

        // Loads and parses road network from roads.mid
        bool LoadFromMid(const std::string& midFilePath);

        // Projects any 3D world position onto the track spline
        TrackProjection Project(const glm::vec3& worldPos) const;

        // Evaluates centerline position at arc length s in [0 .. totalLength]
        glm::vec3 SampleCenterline(float s) const;

        // Evaluates track tangent at arc length s
        glm::vec3 SampleTangent(float s) const;

        // Evaluates road width at arc length s
        float SampleWidth(float s) const;

        float GetTotalLength() const { return m_totalLength; }
        size_t GetCrossSectionCount() const { return m_sections.size(); }
        const std::vector<RoadCrossSection>& GetCrossSections() const { return m_sections; }
        bool IsLoaded() const { return !m_sections.empty(); }

    private:
        std::vector<RoadCrossSection> m_sections;
        float m_totalLength = 0.0f;
    };

} // namespace Game
} // namespace CTTR
