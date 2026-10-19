#include <iostream>
#include <iomanip>
#include <vector>
#include "renderer/P3DLoader.h"

int main() {
    std::string path = "game_data/fairy/fairy1/art/levels/fairy1/track_english.p3d";
    CTTR::Renderer::P3DLoader loader;
    if (!loader.LoadModel(path)) {
        std::cerr << "Failed to load " << path << std::endl;
        return 1;
    }

    const auto& meshes = loader.GetMeshes();
    std::cout << "Loaded " << meshes.size() << " meshes from " << path << std::endl;

    for (size_t i = 0; i < meshes.size(); ++i) {
        const auto& m = meshes[i];
        glm::vec3 minB(1e9f);
        glm::vec3 maxB(-1e9f);
        for (const auto& v : m.vertices) {
            minB = glm::min(minB, v.position);
            maxB = glm::max(maxB, v.position);
        }

        // Check if bounds center is near (0, 0, 0) - e.g. within 30m of origin
        glm::vec3 center = (minB + maxB) * 0.5f;
        if (std::abs(center.x) < 40.0f && std::abs(center.z) < 40.0f) {
            std::cout << "Mesh [" << i << "] Name: '" << m.name << "' Shader: '" << m.shaderName 
                      << "' Tex: '" << m.textureName << "'"
                      << "\n    Bounds: [" << minB.x << ", " << minB.y << ", " << minB.z << "] to ["
                      << maxB.x << ", " << maxB.y << ", " << maxB.z << "]"
                      << "\n    Center: (" << center.x << ", " << center.y << ", " << center.z << ")" << std::endl;
        }
    }
    return 0;
}
