#pragma once
#include <string>
#include <vector>
#include <cstdint>
#include <glm/glm.hpp>
#include "../parser/P3DParser.h"

namespace CTTR {
namespace Renderer {

    // Pure3D is a chunk-based format used by Radical Entertainment.
    // Each chunk has an ID and a size.
    enum class P3DChunkID : uint32_t {
        Texture = 0x00019000,
        Material = 0x00010000,
        Mesh = 0x00010001,
        Skeleton = 0x00010002,
        Animation = 0x00010003
    };

    enum class BlendMode {
        Opaque = 0,
        AlphaTest,
        AlphaBlend,
        Additive
    };

    struct Vertex {
        glm::vec3 position;
        glm::vec3 normal;
        glm::vec2 texCoords;
        // Bone weights for skeletal animation
        glm::vec4 boneWeights;
        glm::ivec4 boneIndices;
    };

    struct P3DMesh {
        std::string name;
        std::string shaderName;
        std::string textureName;
        uint32_t textureWidth = 0;
        uint32_t textureHeight = 0;
        std::vector<uint8_t> textureData; // RGBA32
        bool hasTexture = false;
        
        BlendMode blendMode = BlendMode::Opaque;
        glm::vec2 uvScroll{0.0f, 0.0f};
        glm::vec3 boundsCenter{0.0f, 0.0f, 0.0f};

        std::vector<Vertex> vertices;
        std::vector<uint32_t> indices;
        
        // OpenGL buffer IDs
        uint32_t vao = 0, vbo = 0, ebo = 0;
        uint32_t textureId = 0;
    };

    struct P3DSkeleton {
        // Hierarchical bone structure for kart animations (e.g., wheels spinning, character steering)
        struct Bone {
            std::string name;
            int parentIndex;
            glm::mat4 localBindTransform;
            glm::mat4 inverseBindTransform;
        };
        std::vector<Bone> bones;
    };

    class P3DLoader {
    public:
        P3DLoader();
        ~P3DLoader();

        // Loads a .p3d file extracted from the PS2 ISO
        bool LoadModel(const std::string& filepath);

        const std::vector<P3DMesh>& GetMeshes() const { return m_meshes; }
        std::vector<P3DMesh>& GetMeshes() { return m_meshes; }
        const P3DSkeleton& GetSkeleton() const { return m_skeleton; }
        const Parser::ParsedP3DFile& GetParsedFile() const { return m_parser.GetParsedFile(); }

    private:
        void BuildRuntimeMeshes();
        void BuildRuntimeSkeleton();
        void BuildFallbackMesh();

        // Coordinate conversion: PS2 generally uses Left-Handed, OpenGL uses Right-Handed
        glm::vec3 ConvertPS2ToGL(const glm::vec3& ps2Pos);
        glm::vec3 ConvertNormalPS2ToGL(const glm::vec3& ps2Normal);

        std::vector<P3DMesh> m_meshes;
        P3DSkeleton m_skeleton;
        Parser::P3DParser m_parser;
    };

} // namespace Renderer
} // namespace CTTR
