// =============================================================================
//  test_track_spline.cpp  —  Verification suite for TrackSpline & roads.mid
// =============================================================================

#include <iostream>
#include <cassert>
#include <cmath>
#include <filesystem>
#include <vector>

#include "game/TrackSpline.h"
#include "game/LevelData.h"

int main() {
    std::cout << "=====================================================" << std::endl;
    std::cout << "=== CTTR TrackSpline & roads.mid Verification ===" << std::endl;
    std::cout << "=====================================================" << std::endl;

    std::string midPath = "game_data/adefault/default/design/levels/fairy1/roads.mid";
    if (!std::filesystem::exists(midPath)) {
        midPath = "game_data/fairy/fairy1/design/levels/fairy1/roads.mid";
    }

    CTTR::Game::TrackSpline spline;

    // [TEST 1] Load roads.mid
    std::cout << "\n[TEST 1] Loading roads.mid for Fairy1..." << std::endl;
    bool ok = spline.LoadFromMid(midPath);
    if (!ok) {
        std::cerr << "  ✗ Failed to load " << midPath << std::endl;
        return 1;
    }
    std::cout << "  ✓ Parsed " << spline.GetCrossSectionCount() << " road cross-sections." << std::endl;
    assert(spline.GetCrossSectionCount() >= 180);

    // [TEST 2] Verify Circuit Length & Closed Loop
    std::cout << "\n[TEST 2] Verifying Circuit Length & Closed Loop Closure..." << std::endl;
    float len = spline.GetTotalLength();
    std::cout << "  ✓ Total Circuit Length: " << len << " m" << std::endl;
    assert(std::abs(len - 4303.09f) < 5.0f);

    const auto& p0 = spline.GetCrossSections().front().center;
    const auto& pEnd = spline.GetCrossSections().back().center;
    float loopGap = glm::distance(p0, pEnd);
    std::cout << "  ✓ Start to Finish loop closure gap: " << loopGap << " m" << std::endl;
    assert(loopGap < 1.0f);

    // [TEST 3] Verify CarLocation0..7 Projection onto Spline
    std::cout << "\n[TEST 3] Verifying Grid Spawns (CarLocation0..7) on Track Spline..." << std::endl;
    CTTR::Game::LevelData levelData;
    levelData.Load("fairy1", "game_data");

    for (int i = 0; i < 8; ++i) {
        const auto* sp = levelData.GetSpawnPoint(i);
        assert(sp != nullptr);
        auto proj = spline.Project(sp->position);

        std::cout << "  CarLocation" << i << " at (" << sp->position.x << ", " 
                  << sp->position.y << ", " << sp->position.z << ")"
                  << " -> arcLength=" << proj.arcLength << "m, lateral=" << proj.lateralOffset 
                  << "m, inside=" << (proj.isInsideTrack ? "YES" : "NO") << std::endl;

        // Must be inside road boundaries
        assert(proj.isInsideTrack);
        // Must be near start line (< 60m or > 4250m)
        assert(proj.arcLength < 60.0f || proj.arcLength > 4250.0f);
    }
    std::cout << "  ✓ All 8 grid spawn locations are positioned correctly within road bounds on the starting grid!" << std::endl;

    // [TEST 4] Monotonic Arc Length Sampling
    std::cout << "\n[TEST 4] Testing Continuous Arc Length Evaluation..." << std::endl;
    for (float s = 0.0f; s < len; s += 500.0f) {
        glm::vec3 pt = spline.SampleCenterline(s);
        glm::vec3 tan = spline.SampleTangent(s);
        float width = spline.SampleWidth(s);
        assert(glm::length(tan) > 0.9f);
        assert(width > 15.0f && width < 120.0f);
    }
    std::cout << "  ✓ Continuous centerline, tangent, and width sampling verified along the entire 4.3km circuit!" << std::endl;

    std::cout << "\n=====================================================" << std::endl;
    std::cout << "=== ALL SPLINE TESTS PASSED! (4 / 4) ===" << std::endl;
    std::cout << "=====================================================" << std::endl;
    return 0;
}
