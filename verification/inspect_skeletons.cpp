#include <iostream>
#include <iomanip>
#include "parser/P3DParser.h"

int main() {
    std::string path = "game_data/fairy/fairy1/art/levels/fairy1/track_english.p3d";
    CTTR::Parser::P3DParser parser;
    if (!parser.ParseFile(path)) {
        std::cerr << "Failed to parse " << path << std::endl;
        return 1;
    }

    const auto& parsed = parser.GetParsedFile();
    std::cout << "Parsed " << parsed.skeletons.size() << " skeletons." << std::endl;

    for (const auto& skel : parsed.skeletons) {
        std::cout << "\nSkeleton: '" << skel.name << "' (" << skel.joints.size() << " joints)" << std::endl;
        for (size_t i = 0; i < skel.joints.size(); ++i) {
            const auto& j = skel.joints[i];
            const auto& m = j.localTransform;
            std::cout << "  Joint [" << i << "] '" << j.name << "' parent=" << j.parentIndex
                      << " trans=(" << m[3][0] << ", " << m[3][1] << ", " << m[3][2] << ")"
                      << " row3=(" << m[0][3] << ", " << m[1][3] << ", " << m[2][3] << ")" << std::endl;
        }
    }
    return 0;
}
