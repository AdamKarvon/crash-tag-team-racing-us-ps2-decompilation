#pragma once
#include <vector>
#include <string>
#include <cstdint>
#include <map>
#include <memory>
#include <glm/glm.hpp>

namespace CTTR {
namespace Parser {

    // Intermediate Representation (IR) structures for Pure3D data.
    struct ParsedVertex {
        glm::vec3 position{0.0f};
        glm::vec3 normal{0.0f, 1.0f, 0.0f};
        glm::vec2 uv{0.0f};
        glm::vec4 color{1.0f};
        
        // Skinning data (if skinned mesh)
        glm::vec4 boneWeights{0.0f};
        glm::ivec4 boneIndices{0};

        // Debug/Verification fields: Keeping raw packed data
        int16_t rawPosX = 0, rawPosY = 0, rawPosZ = 0;
        uint32_t packedNormal = 0;
    };

    struct ParsedTexture {
        std::string name;
        uint32_t width = 0;
        uint32_t height = 0;
        uint32_t bpp = 0;
        // Pure3D image format (13 = PSMT4, 14 = PSMT8 in CTTR assets).
        uint32_t format = 0;
        std::vector<uint8_t> rgbaData; // Decoded RGBA32 pixels (width * height * 4)
    };

    struct ParsedShader {
        std::string name;
        std::string textureName; // Map from TEX parameter
        std::string shaderDefinition;
        std::map<std::string, std::string> textureParams;
        std::map<std::string, float> floatParams;
        std::map<std::string, uint32_t> intParams;
        std::map<std::string, glm::vec4> colorParams;
    };

    struct ParsedMesh {
        std::string name;
        std::string shaderName;
        std::string textureName;
        std::vector<ParsedVertex> vertices;
        std::vector<uint32_t> indices;
        std::vector<uint32_t> stripStarts;
        
        // Store primitive topology as found in file (e.g., TriangleStrip vs TriangleList)
        uint32_t primitiveType = 0; 
        uint32_t numVertices = 0;
        bool hasBounds = false;
        glm::vec3 boundsMin = glm::vec3(0.0f);
        glm::vec3 boundsMax = glm::vec3(0.0f);
        bool isSkinned = false;
        std::string skeletonName;
        std::vector<uint32_t> matrixPalette;
    };

    struct ParsedBone {
        std::string name;
        int32_t parentIndex = -1;
        glm::mat4 restPoseMatrix{1.0f};
        glm::mat4 localTransform{1.0f};
        glm::mat4 worldTransform{1.0f};
    };

    struct ParsedSkeleton {
        std::string name;
        std::vector<ParsedBone> joints;
    };

    struct ParsedLocator {
        std::string name;
        uint32_t type = 0;
        glm::vec3 position{0.0f};
        glm::mat4 transform{1.0f};
        std::vector<float> data;
        
        // Convenience flags
        bool isWeaponMount = false;
        bool isClashAnchor = false;
    };

    // Collision structures
    struct ParsedCollisionSphere {
        glm::vec3 center{0.0f};
        float radius = 0.0f;
    };

    struct ParsedCollisionOBB {
        glm::vec3 center{0.0f};
        glm::vec3 halfExtents{0.0f};
        glm::mat3 orientation{1.0f};
    };

    struct ParsedCollisionAABB {
        glm::vec3 minBound{0.0f};
        glm::vec3 maxBound{0.0f};
    };

    struct ParsedCollisionTriangle {
        glm::vec3 v0{0.0f};
        glm::vec3 v1{0.0f};
        glm::vec3 v2{0.0f};
        glm::vec3 normal{0.0f, 1.0f, 0.0f};
    };

    struct ParsedCollisionObject {
        std::string name;
        std::vector<ParsedCollisionSphere> spheres;
        std::vector<ParsedCollisionOBB> obbs;
        std::vector<ParsedCollisionAABB> aabbs;
        std::vector<ParsedCollisionTriangle> triangles;
    };

    struct ParsedCompositeDrawableProp {
        std::string name;
        uint32_t jointIndex = 0;
    };

    struct ParsedCompositeDrawable {
        std::string name;
        std::string skeletonName;
        std::vector<ParsedCompositeDrawableProp> props;
    };

    struct ParsedP3DFile {
        std::vector<ParsedMesh> meshes;
        std::vector<ParsedBone> skeleton;
        std::vector<ParsedSkeleton> skeletons;
        std::vector<ParsedCompositeDrawable> compositeDrawables;
        std::vector<ParsedLocator> locators;
        std::vector<ParsedTexture> textures;
        std::vector<ParsedShader> shaders;
        std::vector<ParsedCollisionObject> collisionObjects;

        // Statistics
        size_t totalChunks = 0;
        std::map<uint32_t, size_t> chunkTypeCounts;
    };

} // namespace Parser
} // namespace CTTR
