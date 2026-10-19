#include <iostream>
#include <iomanip>
#include <algorithm>
#include <vector>
#include "renderer/P3DLoader.h"
#include "game/LevelData.h"

int main() {
    std::cout << "=== Inspecting Level Meshes for Underground Objects ===" << std::endl;

    std::vector<std::string> archives = {
        "game_data/fairy/fairy1/art/levels/fairy1/track_english.p3d"
    };

    std::string path = "game_data/fairy/fairy1/art/levels/fairy1/track_english.p3d";
    CTTR::Parser::P3DParser parser;
    if (!parser.ParseFile(path)) {
        std::cerr << "Failed to parse " << path << std::endl;
        return 1;
    }

    const auto& parsed = parser.GetParsedFile();
    std::cout << "Parsed " << parsed.skeletons.size() << " skeletons, "
              << parsed.meshes.size() << " meshes." << std::endl;

    // Test waterFX skeleton joint 5 & 6
    for (const auto& skel : parsed.skeletons) {
        std::string sname = skel.name;
        while (!sname.empty() && (sname.back() == ' ' || sname.back() == '\0')) sname.pop_back();
        if (sname == "waterFX") {
            std::cout << "Found waterFX skeleton with " << skel.joints.size() << " joints." << std::endl;
            std::vector<glm::mat4> worldMats(skel.joints.size(), glm::mat4(1.0f));
            for (size_t i = 0; i < skel.joints.size(); ++i) {
                int32_t p = skel.joints[i].parentIndex;
                if (p == (int32_t)i || p < 0 || p >= (int32_t)skel.joints.size()) {
                    worldMats[i] = skel.joints[i].localTransform;
                } else {
                    worldMats[i] = worldMats[p] * skel.joints[i].localTransform;
                }
                std::cout << "  Joint [" << i << "] '" << skel.joints[i].name << "' parent=" << p
                          << " World Trans: (" << worldMats[i][3][0] << ", " << worldMats[i][3][1] << ", " << worldMats[i][3][2] << ")" << std::endl;
            }
        }
    }
    return 0;
}
