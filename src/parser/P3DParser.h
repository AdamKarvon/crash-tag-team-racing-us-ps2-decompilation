#pragma once
#include <vector>
#include <cstdint>
#include <string>
#include "ChunkReader.h"
#include "ParsedP3D.h"
#include <stdexcept>
#include <map>

namespace CTTR {
namespace Parser {

    class P3DParseException : public std::runtime_error {
    public:
        P3DParseException(const std::string& message, uint32_t chunkId = 0, size_t offset = 0)
            : std::runtime_error(message + " (Chunk ID: 0x" + std::to_string(chunkId) + ", Offset: " + std::to_string(offset) + ")"),
              chunkId(chunkId), offset(offset) {}
        uint32_t chunkId;
        size_t offset;
    };

    // Known Chunk Types in Radical Pure3D format (Verified against PS2 SLES_534.39 decompilation)
    // Core Geometry & Drawables (Registered in 0x0031ed20 via tP3DFileHandler::AddHandler @ 0x003bef28)
    constexpr uint32_t CHUNK_MESH                                = 0x10000;  // pure3d::GeometryLoader (ctor 0x00314e40, vtable 0x0057fbd0)
    constexpr uint32_t CHUNK_SKIN                                = 0x10001;  // pure3d::PolySkinLoader (ctor 0x0032fca0, vtable 0x005820a0)
    constexpr uint32_t CHUNK_OLD_PRIM_GROUP                      = 0x10002;
    constexpr uint32_t CHUNK_BOUNDING_BOX                        = 0x10003;
    constexpr uint32_t CHUNK_BOUNDING_SPHERE                     = 0x10004;
    constexpr uint32_t CHUNK_POSITION_LIST                       = 0x10005;
    constexpr uint32_t CHUNK_NORMAL_LIST                         = 0x10006;
    constexpr uint32_t CHUNK_UV_LIST                             = 0x10007;
    constexpr uint32_t CHUNK_COLOUR_LIST                         = 0x10008;
    constexpr uint32_t CHUNK_INDEX_LIST                          = 0x1000A;
    constexpr uint32_t CHUNK_MATRIX_LIST                         = 0x1000B;
    constexpr uint32_t CHUNK_WEIGHT_LIST                         = 0x1000C;
    constexpr uint32_t CHUNK_MATRIX_PALETTE                      = 0x1000D;
    constexpr uint32_t CHUNK_PACKED_NORMAL_LIST                  = 0x10010;
    constexpr uint32_t CHUNK_MEMORY_IMAGE_VERTEX_LIST            = 0x10012;
    constexpr uint32_t CHUNK_MEMORY_IMAGE_VERTEX_DESCRIPTION     = 0x10014;
    constexpr uint32_t CHUNK_PRIM_GROUP                          = 0x10020;

    // Skeletons & Locators
    constexpr uint32_t CHUNK_SKELETON                            = 0x4500;   // pure3d::SkeletonLoader v1 (ctor 0x0036b9e8)
    constexpr uint32_t CHUNK_SKELETON_JOINT                      = 0x4501;
    constexpr uint32_t CHUNK_SKELETON_2                          = 0x23000;  // pure3d::SkeletonLoader v2 (ctor 0x0036b9b0)
    constexpr uint32_t CHUNK_SKELETON_JOINT_2                    = 0x23001;
    constexpr uint32_t CHUNK_OLD_LOCATOR                         = 0x14000;  // pure3d::LocatorLoader (ctor 0x0031f750)
    constexpr uint32_t CHUNK_LOCATOR                             = 0x3000005; // LocatorChunkLoader (ctor 0x001f4ee0 @ 0x0027df34)
    constexpr uint32_t CHUNK_LOCATOR_MATRIX                      = 0x300000C;

    // Shaders, Textures, Sprites & Fonts
    constexpr uint32_t CHUNK_SHADER                              = 0x11000;  // pure3d::ShaderLoader (ctor 0x003641c0, vtable 0x00585bd0)
    constexpr uint32_t CHUNK_SHADER_TEXTURE_PARAMETER            = 0x11002;
    constexpr uint32_t CHUNK_SHADER_INT_PARAMETER                = 0x11003;
    constexpr uint32_t CHUNK_SHADER_FLOAT_PARAMETER              = 0x11004;
    constexpr uint32_t CHUNK_SHADER_COLOUR_PARAMETER             = 0x11005;
    constexpr uint32_t CHUNK_TEXTURE                             = 0x19000;  // pure3d::TextureLoader (ctor 0x00372a40, vtable 0x00587200)
    constexpr uint32_t CHUNK_IMAGE                               = 0x19001;  // pure3d::ImageLoader (ctor 0x00316678, vtable 0x0057fe58)
    constexpr uint32_t CHUNK_IMAGE_DATA                          = 0x19002;
    constexpr uint32_t CHUNK_VOLUME_IMAGE                        = 0x19004;
    constexpr uint32_t CHUNK_SPRITE                              = 0x19005;  // pure3d::SpriteLoader (ctor 0x0036e648, vtable 0x00586d60)
    constexpr uint32_t CHUNK_TEXTURE_FONT                        = 0x22000;  // pure3d::TextureFontLoader (ctor 0x00371cb0)
    constexpr uint32_t CHUNK_IMAGE_FONT                          = 0x22001;  // pure3d::ImageFontLoader (ctor 0x00318238)

    // Animation, Scenegraph & Composition
    constexpr uint32_t CHUNK_COMPOSITE_DRAWABLE                  = 0x123000; // pure3d::CompositeDrawableLoader (ctor 0x003001c8, vtable 0x0057d130)
    constexpr uint32_t CHUNK_COMPOSITE_DRAWABLE_PROP             = 0x123001;
    constexpr uint32_t CHUNK_SCENEGRAPH                          = 0x120100; // pure3d::scenegraph::ScenegraphLoader (ctor 0x003629f8, vtable 0x005858d0)
    constexpr uint32_t CHUNK_ANIMATION                           = 0x121000; // pure3d::AnimationLoader (ctor 0x002ece48, vtable 0x0057bb88)
    constexpr uint32_t CHUNK_CHANNEL                             = 0x121100; // pure3d::ChannelLoader (ctor 0x002fb3f8)
    constexpr uint32_t CHUNK_FRAME_CONTROLLER                    = 0x121201; // pure3d::FrameControllerLoader (ctor 0x003097d8)
    constexpr uint32_t CHUNK_ANIMATED_OBJECT_FACTORY             = 0x121301; // pure3d::AnimatedObjectFactoryLoader (ctor 0x002eaec8)
    constexpr uint32_t CHUNK_ANIMATED_OBJECT                     = 0x121302; // pure3d::AnimatedObjectLoader (ctor 0x002eb218)

    // UI, Lighting, Camera, Sound & Road Network
    constexpr uint32_t CHUNK_FRONTEND_PROJECT                    = 0x18000;  // pure3d::frontend::ProjectLoader (ctor 0x0030b948)
    constexpr uint32_t CHUNK_TEXT_BIBLE                          = 0x18100;  // pure3d::TextBibleLoader (ctor 0x003708e0)
    constexpr uint32_t CHUNK_CAMERA                              = 0x2200;   // pure3d::CameraLoader (ctor 0x002f6f00)
    constexpr uint32_t CHUNK_LIGHT                               = 0x13000;  // pure3d::LightLoader (ctor 0x0031e190)
    constexpr uint32_t CHUNK_LIGHT_GROUP                         = 0x13006;  // pure3d::LightGroupLoader (ctor 0x0031ea08)
    constexpr uint32_t CHUNK_ROAD_NETWORK                        = 0x9000001; // RoadNetworkChunkLoader (ctor 0x00257ef0 @ 0x0027df18)
    constexpr uint32_t CHUNK_SOUND_DATA                          = 0x2680019; // SoundDataChunkLoader (ctor 0x00286c60 @ 0x002869a4)

    // RavenPhysics Collision & Rigid Body (Registered in 0x0040bdd0)
    constexpr uint32_t CHUNK_COLLISION_OBJECT                    = 0x7010000; // ravenphysics::CollisionObjectLoader (ctor 0x0040fa00, vtable 0x005974f8)
    constexpr uint32_t CHUNK_WORLD_COLLISION_OBJECT              = 0x7010025; // ravenphysics::WorldCollisionObjectLoader (ctor 0x0040fa28, vtable 0x005974b8)
    constexpr uint32_t CHUNK_RIGID_BODY                          = 0x7011000; // ravenphysics::RigidBodyLoader (ctor 0x0040aaa8, vtable 0x00597108)
    constexpr uint32_t CHUNK_COLLISION_VOLUME                    = 0x7010001;
    constexpr uint32_t CHUNK_COLLISION_SPHERE                    = 0x7010002;
    constexpr uint32_t CHUNK_COLLISION_CYLINDER                  = 0x7010003;
    constexpr uint32_t CHUNK_COLLISION_OBB                       = 0x7010004;
    constexpr uint32_t CHUNK_COLLISION_WALL                      = 0x7010005;
    constexpr uint32_t CHUNK_COLLISION_AABB                      = 0x7010006;
    constexpr uint32_t CHUNK_COLLISION_VECTOR                    = 0x7010007;
    constexpr uint32_t CHUNK_COLLISION_MESH_TREE                 = 0x701000A;
    constexpr uint32_t CHUNK_COLLISION_MESH_VECTOR_LIST          = 0x7000003;
    constexpr uint32_t CHUNK_COLLISION_MESH_TRIANGLE_LIST        = 0x7000006;

    class P3DParser {
    public:
        P3DParser();
        ~P3DParser();

        // Entry point for parsing
        bool Parse(const std::vector<uint8_t>& fileData);
        bool ParseFile(const std::string& filePath);
        
        const ParsedP3DFile& GetParsedFile() const { return m_parsedFile; }
        void SetVerbose(bool verbose) { m_verbose = verbose; }
        bool IsVerbose() const { return m_verbose; }

    private:
        // Recursive Chunk Walker
        void WalkChunks(ChunkReader& reader, size_t endOffset);
        void WalkMeshChildren(ChunkReader& reader, size_t endOffset, ParsedMesh& mesh);
        void WalkPrimitiveGroupChildren(ChunkReader& reader, size_t endOffset, ParsedMesh& mesh);
        void WalkTextureChildren(ChunkReader& reader, size_t endOffset, ParsedTexture& texture);
        void WalkCollisionObjectChildren(ChunkReader& reader, size_t endOffset, ParsedCollisionObject& colObj);
        void WalkCollisionVolumeChildren(ChunkReader& reader, size_t endOffset, ParsedCollisionObject& colObj);

        // Specific Chunk Handlers
        ParsedMesh ParseMeshChunk(ChunkReader& reader, uint32_t headerDataSize);
        ParsedMesh ParseSkinChunk(ChunkReader& reader, uint32_t headerDataSize);
        void ParsePrimitiveGroupChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh, bool oldFormat);
        void ParsePositionListChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh);
        void ParseNormalListChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh);
        void ParseUVListChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh);
        void ParseColourListChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh);
        void ParseIndexListChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh);
        void ParseWeightListChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh);
        void ParseMatrixPaletteChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh);
        void ParseMemoryImageVertexList(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh);
        void ParseMemoryImageVertexDescription(ChunkReader& reader, uint32_t headerDataSize);
        void ParseBoundingBoxChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh);
        
        void ParseSkeletonChunk(ChunkReader& reader, uint32_t headerDataSize);
        void ParseSkeletonJointChunk(ChunkReader& reader, uint32_t headerDataSize);
        void ParseSkeleton2Chunk(ChunkReader& reader, uint32_t headerDataSize);
        void ParseSkeletonJoint2Chunk(ChunkReader& reader, uint32_t headerDataSize);
        
        void ParseLocatorChunk(ChunkReader& reader, uint32_t headerDataSize);
        void ParseLocatorMatrixChunk(ChunkReader& reader, uint32_t headerDataSize);
        void ParseOldLocatorChunk(ChunkReader& reader, uint32_t headerDataSize);
        
        void ParseShaderChunk(ChunkReader& reader, uint32_t headerDataSize, size_t nextChunkOffset);
        void ParseTextureChunk(ChunkReader& reader, uint32_t headerDataSize, size_t nextChunkOffset);
        void ParseImageChunk(ChunkReader& reader, uint32_t headerDataSize, size_t nextChunkOffset, ParsedTexture& texture);
        void ParseImageDataChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedTexture& texture);
        
        void ParseCollisionObjectChunk(ChunkReader& reader, uint32_t headerDataSize, size_t nextChunkOffset);
        void ParseCollisionSphereChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedCollisionObject& colObj);
        void ParseCollisionOBBChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedCollisionObject& colObj);
        void ParseCollisionAABBChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedCollisionObject& colObj);
        void ParseCollisionMeshChunk(ChunkReader& reader, uint32_t headerDataSize, size_t nextChunkOffset, ParsedCollisionObject& colObj);
        void ParseCompositeDrawableChunk(ChunkReader& reader, uint32_t headerDataSize, size_t nextChunkOffset);

        struct VertexFieldDescription {
            uint32_t type;
            uint32_t dataType;
            uint32_t offset;
            uint32_t stride;
        };

        struct VertexLayoutDescription {
            uint32_t version = 0;
            uint32_t param = 0;
            std::vector<VertexFieldDescription> fields;

            const VertexFieldDescription* FindField(uint32_t type) const {
                for (const auto& field : fields) {
                    if (field.type == type) return &field;
                }
                return nullptr;
            }
        };

        ParsedP3DFile m_parsedFile;
        VertexLayoutDescription m_lastDescription;
        bool m_verbose = false;
    };

} // namespace Parser
} // namespace CTTR
