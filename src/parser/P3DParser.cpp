#include "P3DParser.h"
#include <iostream>
#include <fstream>
#include <algorithm>
#include <cmath>
#include <cstring>

namespace CTTR {
namespace Parser {

#define P3D_TRACE if (!m_verbose) {} else std::cout

    namespace {
        std::string TrimString(const std::string& str) {
            size_t first = str.find_first_not_of(" \t\r\n\0");
            if (first == std::string::npos) return "";
            size_t last = str.find_last_not_of(" \t\r\n\0");
            return str.substr(first, (last - first + 1));
        }

        constexpr uint8_t kBlock8[32] = {
            0, 1, 4, 5, 16, 17, 20, 21,
            2, 3, 6, 7, 18, 19, 22, 23,
            8, 9, 12, 13, 24, 25, 28, 29,
            10, 11, 14, 15, 26, 27, 30, 31
        };

        inline uint32_t GetPSMT8Address(uint32_t x, uint32_t y, uint32_t storageWidth) {
            const uint32_t blockLocation = (y & ~0x0Fu) * storageWidth
                + (x & ~0x0Fu) * 2u;
            const uint32_t swapSelector = (((y + 2u) >> 2u) & 0x01u) * 4u;
            const uint32_t posY = ((((y & ~0x03u) >> 1u) + (y & 0x01u)) & 0x07u);
            const uint32_t columnLocation = posY * storageWidth * 2u
                + ((x + swapSelector) & 0x07u) * 4u;
            const uint32_t byteNum = ((y >> 1u) & 0x01u) + ((x >> 2u) & 0x02u);
            return blockLocation + columnLocation + byteNum;
        }

        inline uint32_t GetPSMT4Address(uint32_t x, uint32_t y, uint32_t storageWidth) {
            const uint32_t blockX = x / 32;
            const uint32_t blockY = y / 16;
            const uint32_t blocksPerRow = std::max(1u, storageWidth / 32);
            const uint32_t blockOffset = (blockY * blocksPerRow + blockX) * 256;
            const uint32_t lx = x % 32;
            const uint32_t ly = y % 16;
            const uint32_t subBlock = (ly / 8) * 2 + (lx / 16);
            const uint32_t subX = lx % 16;
            const uint32_t subY = ly % 8;
            const uint32_t byteNum = subY * 8 + ((subX / 2) ^ ((subY & 1) * 4));
            return blockOffset + subBlock * 64 + byteNum;
        }

        inline uint32_t GetPSMCT32Address(uint32_t x, uint32_t y, uint32_t width) {
            const uint32_t pagesPerRow = std::max(1u, (width + 63) / 64);
            const uint32_t page = (x / 64) + (y / 32) * pagesPerRow;
            const uint32_t px = x % 64;
            const uint32_t py = y % 32;
            const uint32_t block = kBlock8[(px / 8) + (py / 8) * 8];
            const uint32_t column = (py % 8) / 2;
            const uint32_t columnWord = (px % 8) + (py % 2) * 8;
            static constexpr uint8_t columnWords[16] =
                {0, 1, 4, 5, 8, 9, 12, 13, 2, 3, 6, 7, 10, 11, 14, 15};
            return (page * 2048 + block * 64 + column * 16 + columnWords[columnWord] * 4) * 4;
        }

        inline uint32_t GetCLUTIndex(uint32_t index, uint32_t bpp) {
            if (bpp == 8) {
                return (index & 0xE7) | ((index & 0x08) << 1) | ((index & 0x10) >> 1);
            }
            return index ^ 0x08;
        }
    }

    P3DParser::P3DParser() {}
    P3DParser::~P3DParser() {}

    bool P3DParser::ParseFile(const std::string& filePath) {
        std::ifstream file(filePath, std::ios::binary | std::ios::ate);
        if (!file.is_open()) {
            std::cerr << "P3DParser: Failed to open file: " << filePath << std::endl;
            return false;
        }
        std::streamsize size = file.tellg();
        file.seekg(0, std::ios::beg);
        std::vector<uint8_t> buffer(size);
        if (!file.read(reinterpret_cast<char*>(buffer.data()), size)) {
            std::cerr << "P3DParser: Failed to read file data: " << filePath << std::endl;
            return false;
        }
        return Parse(buffer);
    }

    bool P3DParser::Parse(const std::vector<uint8_t>& fileData) {
        if (fileData.empty()) {
            std::cerr << "P3DParser: Provided file data is empty." << std::endl;
            return false;
        }

        m_parsedFile = ParsedP3DFile{};
        ChunkReader reader(fileData);
        P3D_TRACE << "P3DParser: Beginning Chunk Walk. Total size: " << fileData.size() << " bytes." << std::endl;

        try {
            uint32_t signature = reader.Read<uint32_t>();
            uint32_t headerSize = reader.Read<uint32_t>();
            uint32_t fileSize = reader.Read<uint32_t>();

            if (signature != 0xFF443350 && signature != 0x503344FF) {
                std::cerr << "P3DParser: Unexpected Pure3D signature 0x"
                          << std::hex << signature << std::dec << "." << std::endl;
                return false;
            }

            const size_t parseEnd = std::min<size_t>(fileSize, fileData.size());
            reader.Seek(headerSize);
            WalkChunks(reader, parseEnd);

            // Post-process: resolve mesh texture names via parsed shaders/textures
            for (auto& mesh : m_parsedFile.meshes) {
                std::string cleanShader = TrimString(mesh.shaderName);
                std::string matchedTexName;

                for (const auto& shader : m_parsedFile.shaders) {
                    if (TrimString(shader.name) == cleanShader) {
                        matchedTexName = TrimString(shader.textureName);
                        break;
                    }
                }

                if (matchedTexName.empty()) {
                    for (const auto& tex : m_parsedFile.textures) {
                        std::string cleanTex = TrimString(tex.name);
                        if (cleanTex == cleanShader || cleanShader.find(cleanTex) != std::string::npos) {
                            matchedTexName = cleanTex;
                            break;
                        }
                    }
                }

                if (matchedTexName.empty()) {
                    matchedTexName = cleanShader;
                }

                mesh.textureName = matchedTexName;
            }

            P3D_TRACE << "P3DParser: Walk complete. Found " 
                      << m_parsedFile.meshes.size() << " meshes, "
                      << m_parsedFile.skeleton.size() << " bones, "
                      << m_parsedFile.locators.size() << " locators, "
                      << m_parsedFile.textures.size() << " textures, "
                      << m_parsedFile.shaders.size() << " shaders, "
                      << m_parsedFile.collisionObjects.size() << " collision objects." << std::endl;
            return true;
        } catch (const P3DParseException& e) {
            std::cerr << "P3DParseException: " << e.what() << std::endl;
            return false;
        } catch (const std::exception& e) {
            std::cerr << "P3DParser Exception: " << e.what() << std::endl;
            return false;
        }
    }

    void P3DParser::WalkChunks(ChunkReader& reader, size_t endOffset) {
        while (reader.Tell() + 12 <= endOffset) {
            uint32_t chunkId = reader.Read<uint32_t>();
            uint32_t headerSize = reader.Read<uint32_t>();
            uint32_t chunkSize = reader.Read<uint32_t>();
            
            if (chunkId == 0x00000000 || headerSize < 12 || chunkSize < headerSize) {
                break; 
            }

            m_parsedFile.totalChunks++;
            m_parsedFile.chunkTypeCounts[chunkId]++;

            size_t chunkStart = reader.Tell() - 12;
            size_t headerEnd = chunkStart + headerSize;
            size_t nextChunkOffset = chunkStart + chunkSize;

            if (nextChunkOffset > endOffset) {
                P3D_TRACE << "P3DParser: Chunk 0x" << std::hex << chunkId << std::dec
                          << " extends beyond parent boundary." << std::endl;
                break;
            }

            P3D_TRACE << "Found Chunk: 0x" << std::hex << chunkId << std::dec 
                      << " Header: " << headerSize
                      << " Size: " << chunkSize << " bytes." << std::endl;

            try {
                switch (chunkId) {
                    case CHUNK_MESH: {
                        ParsedMesh mesh = ParseMeshChunk(reader, headerSize - 12);
                        if (headerSize > 12) {
                            reader.Seek(headerEnd);
                        }
                        WalkMeshChildren(reader, nextChunkOffset, mesh);
                        break;
                    }
                    case CHUNK_SKIN: {
                        ParsedMesh skinMesh = ParseSkinChunk(reader, headerSize - 12);
                        if (headerSize > 12) {
                            reader.Seek(headerEnd);
                        }
                        WalkMeshChildren(reader, nextChunkOffset, skinMesh);
                        break;
                    }
                    case CHUNK_SKELETON:
                        ParseSkeletonChunk(reader, headerSize - 12);
                        if (headerSize > 12) {
                            reader.Seek(headerEnd);
                        }
                        WalkChunks(reader, nextChunkOffset); 
                        break;
                    case CHUNK_SKELETON_JOINT:
                        ParseSkeletonJointChunk(reader, headerSize - 12);
                        break;
                    case CHUNK_SKELETON_2:
                        ParseSkeleton2Chunk(reader, headerSize - 12);
                        if (headerSize > 12) {
                            reader.Seek(headerEnd);
                        }
                        WalkChunks(reader, nextChunkOffset);
                        break;
                    case CHUNK_SKELETON_JOINT_2:
                        ParseSkeletonJoint2Chunk(reader, headerSize - 12);
                        break;
                    case CHUNK_LOCATOR:
                        ParseLocatorChunk(reader, headerSize - 12);
                        if (headerSize > 12) {
                            reader.Seek(headerEnd);
                        }
                        WalkChunks(reader, nextChunkOffset);
                        break;
                    case CHUNK_OLD_LOCATOR:
                        ParseOldLocatorChunk(reader, headerSize - 12);
                        break;
                    case CHUNK_LOCATOR_MATRIX:
                        ParseLocatorMatrixChunk(reader, headerSize - 12);
                        break;
                    case CHUNK_SHADER:
                        ParseShaderChunk(reader, headerSize - 12, nextChunkOffset);
                        break;
                    case CHUNK_TEXTURE:
                        ParseTextureChunk(reader, headerSize - 12, nextChunkOffset);
                        break;
                    case CHUNK_COLLISION_OBJECT:
                        ParseCollisionObjectChunk(reader, headerSize - 12, nextChunkOffset);
                        break;
                    case CHUNK_COMPOSITE_DRAWABLE:
                        ParseCompositeDrawableChunk(reader, headerSize - 12, nextChunkOffset);
                        break;
                    default:
                        reader.Seek(headerEnd);
                        if (nextChunkOffset > headerEnd) {
                            WalkChunks(reader, nextChunkOffset);
                        }
                        break;
                }
            } catch (const std::exception& e) {
                throw P3DParseException(std::string("Failed processing chunk: ") + e.what(), chunkId, chunkStart);
            }

            reader.Seek(nextChunkOffset);
        }
    }

    void P3DParser::WalkMeshChildren(ChunkReader& reader, size_t endOffset, ParsedMesh& mesh) {
        while (reader.Tell() + 12 <= endOffset) {
            uint32_t chunkId = reader.Read<uint32_t>();
            uint32_t headerSize = reader.Read<uint32_t>();
            uint32_t chunkSize = reader.Read<uint32_t>();

            if (headerSize < 12 || chunkSize < headerSize) break;

            m_parsedFile.totalChunks++;
            m_parsedFile.chunkTypeCounts[chunkId]++;

            size_t chunkStart = reader.Tell() - 12;
            size_t headerEnd = chunkStart + headerSize;
            size_t nextChunkOffset = chunkStart + chunkSize;
            if (nextChunkOffset > endOffset) break;

            try {
                if (chunkId == CHUNK_OLD_PRIM_GROUP || chunkId == CHUNK_PRIM_GROUP) {
                    ParsedMesh primitiveMesh = mesh;
                    primitiveMesh.name += "_pg" + std::to_string(m_parsedFile.meshes.size());
                    ParsePrimitiveGroupChunk(reader, headerSize - 12, primitiveMesh, chunkId == CHUNK_OLD_PRIM_GROUP);
                    if (headerSize > 12) {
                        reader.Seek(headerEnd);
                    }
                    WalkPrimitiveGroupChildren(reader, nextChunkOffset, primitiveMesh);
                    if (!primitiveMesh.vertices.empty() || primitiveMesh.hasBounds) {
                        m_parsedFile.meshes.push_back(primitiveMesh);
                    }
                } else if (chunkId == CHUNK_BOUNDING_BOX) {
                    ParseBoundingBoxChunk(reader, headerSize - 12, mesh);
                } else {
                    if (headerSize > 12) {
                        reader.Seek(headerEnd);
                    }
                    if (nextChunkOffset > headerEnd) {
                        WalkMeshChildren(reader, nextChunkOffset, mesh);
                    }
                }
            } catch (const std::exception& e) {
                throw P3DParseException(std::string("Failed processing mesh child chunk: ") + e.what(), chunkId, chunkStart);
            }

            reader.Seek(nextChunkOffset);
        }
    }

    void P3DParser::WalkPrimitiveGroupChildren(ChunkReader& reader, size_t endOffset, ParsedMesh& mesh) {
        while (reader.Tell() + 12 <= endOffset) {
            uint32_t chunkId = reader.Read<uint32_t>();
            uint32_t headerSize = reader.Read<uint32_t>();
            uint32_t chunkSize = reader.Read<uint32_t>();

            P3D_TRACE << "      [PrimGroup Child] Chunk ID: 0x" << std::hex << chunkId << std::dec 
                      << " Header: " << headerSize << " Size: " << chunkSize << std::endl;

            if (headerSize < 12 || chunkSize < headerSize) break;

            m_parsedFile.totalChunks++;
            m_parsedFile.chunkTypeCounts[chunkId]++;

            size_t chunkStart = reader.Tell() - 12;
            size_t headerEnd = chunkStart + headerSize;
            size_t nextChunkOffset = chunkStart + chunkSize;
            if (nextChunkOffset > endOffset) break;

            try {
                switch (chunkId) {
                    case CHUNK_POSITION_LIST:
                        ParsePositionListChunk(reader, headerSize - 12, mesh);
                        break;
                    case CHUNK_NORMAL_LIST:
                        ParseNormalListChunk(reader, headerSize - 12, mesh);
                        break;
                    case CHUNK_UV_LIST:
                        ParseUVListChunk(reader, headerSize - 12, mesh);
                        break;
                    case CHUNK_COLOUR_LIST:
                        ParseColourListChunk(reader, headerSize - 12, mesh);
                        break;
                    case CHUNK_INDEX_LIST:
                        ParseIndexListChunk(reader, headerSize - 12, mesh);
                        break;
                    case CHUNK_WEIGHT_LIST:
                        ParseWeightListChunk(reader, headerSize - 12, mesh);
                        break;
                    case CHUNK_MATRIX_PALETTE:
                        ParseMatrixPaletteChunk(reader, headerSize - 12, mesh);
                        break;
                    case CHUNK_BOUNDING_BOX:
                        ParseBoundingBoxChunk(reader, headerSize - 12, mesh);
                        break;
                    case CHUNK_MEMORY_IMAGE_VERTEX_LIST:
                        ParseMemoryImageVertexList(reader, headerSize - 12, mesh);
                        break;
                    case CHUNK_MEMORY_IMAGE_VERTEX_DESCRIPTION:
                        ParseMemoryImageVertexDescription(reader, headerSize - 12);
                        break;
                    case CHUNK_PACKED_NORMAL_LIST:
                        P3D_TRACE << "    -> Packed normals present." << std::endl;
                        break;
                    default:
                        break;
                }
            } catch (const std::exception& e) {
                throw P3DParseException(std::string("Failed processing prim group child chunk: ") + e.what(), chunkId, chunkStart);
            }

            reader.Seek(headerEnd);
            if (nextChunkOffset > headerEnd) {
                WalkPrimitiveGroupChildren(reader, nextChunkOffset, mesh);
            }
            reader.Seek(nextChunkOffset);
        }
    }

    ParsedMesh P3DParser::ParseMeshChunk(ChunkReader& reader, uint32_t headerDataSize) {
        std::string name = reader.ReadP3DString();
        uint32_t version = reader.Read<uint32_t>();
        uint32_t numPrimGroups = reader.Read<uint32_t>();

        P3D_TRACE << "  -> Parsing Mesh: " << name << " (PrimGroups: " << numPrimGroups << ")" << std::endl;

        ParsedMesh mesh;
        mesh.name = name;
        mesh.primitiveType = 0;
        mesh.isSkinned = false;
        return mesh;
    }

    ParsedMesh P3DParser::ParseSkinChunk(ChunkReader& reader, uint32_t headerDataSize) {
        std::string name = reader.ReadP3DString();
        std::string skeletonName = reader.ReadP3DString();
        uint32_t numPrimGroups = reader.Read<uint32_t>();

        P3D_TRACE << "  -> Parsing Skin Mesh: " << name << " (Skeleton: " << skeletonName << ", PrimGroups: " << numPrimGroups << ")" << std::endl;

        ParsedMesh mesh;
        mesh.name = name;
        mesh.skeletonName = skeletonName;
        mesh.primitiveType = 0;
        mesh.isSkinned = true;
        return mesh;
    }

    void P3DParser::ParsePrimitiveGroupChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh, bool oldFormat) {
        uint32_t version = reader.Read<uint32_t>();
        std::string shaderName = reader.ReadP3DString();
        mesh.shaderName = TrimString(shaderName);
        mesh.primitiveType = reader.Read<uint32_t>();
        uint32_t vertexType = reader.Read<uint32_t>();
        uint32_t numVertices = reader.Read<uint32_t>();
        mesh.numVertices = numVertices;
        uint32_t numIndices = reader.Read<uint32_t>();
        uint32_t numMatrices = reader.Read<uint32_t>();

        if (!oldFormat) {
            reader.Read<uint32_t>(); // memory imaged
            reader.Read<uint32_t>(); // optimized
            reader.Read<uint32_t>(); // vertex animated
            reader.Read<uint32_t>(); // vertex animation mask
        }

        P3D_TRACE << "    -> PrimitiveGroup shader=" << shaderName
                  << " version=" << version
                  << " primitive=" << mesh.primitiveType
                  << " vertexType=0x" << std::hex << vertexType << std::dec
                  << " verts=" << numVertices
                  << " indices=" << numIndices
                  << " matrices=" << numMatrices << std::endl;
    }

    void P3DParser::ParsePositionListChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh) {
        uint32_t count = reader.Read<uint32_t>();
        mesh.vertices.resize(std::max<size_t>(mesh.vertices.size(), count));

        for (uint32_t i = 0; i < count; ++i) {
            auto& v = mesh.vertices[i];
            v.position.x = reader.Read<float>();
            v.position.y = reader.Read<float>();
            v.position.z = reader.Read<float>();
            v.rawPosX = static_cast<int16_t>(std::lround(v.position.x));
            v.rawPosY = static_cast<int16_t>(std::lround(v.position.y));
            v.rawPosZ = static_cast<int16_t>(std::lround(v.position.z));
        }

        P3D_TRACE << "      -> Positions: " << count << std::endl;
    }

    void P3DParser::ParseNormalListChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh) {
        uint32_t count = reader.Read<uint32_t>();
        mesh.vertices.resize(std::max<size_t>(mesh.vertices.size(), count));

        for (uint32_t i = 0; i < count; ++i) {
            auto& v = mesh.vertices[i];
            v.normal.x = reader.Read<float>();
            v.normal.y = reader.Read<float>();
            v.normal.z = reader.Read<float>();
        }

        P3D_TRACE << "      -> Normals: " << count << std::endl;
    }

    void P3DParser::ParseUVListChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh) {
        uint32_t count = reader.Read<uint32_t>();
        uint32_t channel = reader.Read<uint32_t>();
        mesh.vertices.resize(std::max<size_t>(mesh.vertices.size(), count));

        for (uint32_t i = 0; i < count; ++i) {
            auto& v = mesh.vertices[i];
            float u = reader.Read<float>();
            float vv = reader.Read<float>();
            if (channel == 0) {
                v.uv = {u, vv};
            }
        }

        P3D_TRACE << "      -> UVs channel " << channel << ": " << count << std::endl;
    }

    void P3DParser::ParseColourListChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh) {
        uint32_t count = reader.Read<uint32_t>();
        mesh.vertices.resize(std::max<size_t>(mesh.vertices.size(), count));

        for (uint32_t i = 0; i < count; ++i) {
            uint32_t col = reader.Read<uint32_t>();
            float b = static_cast<float>(col & 0xFF) / 255.0f;
            float g = static_cast<float>((col >> 8) & 0xFF) / 255.0f;
            float r = static_cast<float>((col >> 16) & 0xFF) / 255.0f;
            float a = static_cast<float>((col >> 24) & 0xFF) / 255.0f;
            mesh.vertices[i].color = glm::vec4(r, g, b, a);
        }
    }

    void P3DParser::ParseIndexListChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh) {
        uint32_t count = reader.Read<uint32_t>();
        mesh.indices.reserve(mesh.indices.size() + count);

        for (uint32_t i = 0; i < count; ++i) {
            mesh.indices.push_back(reader.Read<uint32_t>());
        }

        P3D_TRACE << "      -> Indices: " << count << std::endl;
    }

    void P3DParser::ParseWeightListChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh) {
        uint32_t count = reader.Read<uint32_t>();
        mesh.vertices.resize(std::max<size_t>(mesh.vertices.size(), count));

        for (uint32_t i = 0; i < count; ++i) {
            float w0 = reader.Read<float>();
            float w1 = reader.Read<float>();
            float w2 = reader.Read<float>();
            mesh.vertices[i].boneWeights = glm::vec4(w0, w1, w2, 1.0f - (w0 + w1 + w2));
        }
    }

    void P3DParser::ParseMatrixPaletteChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh) {
        uint32_t count = reader.Read<uint32_t>();
        mesh.matrixPalette.resize(count);
        for (uint32_t i = 0; i < count; ++i) {
            mesh.matrixPalette[i] = reader.Read<uint32_t>();
        }

        // Remap any vertices decoded from VIF before the palette chunk was encountered
        for (auto& v : mesh.vertices) {
            uint32_t palIdx = static_cast<uint32_t>(v.boneIndices.x);
            if (palIdx < mesh.matrixPalette.size()) {
                v.boneIndices.x = static_cast<int>(mesh.matrixPalette[palIdx]);
            }
        }
    }

    void P3DParser::ParseBoundingBoxChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh) {
        mesh.boundsMin.x = reader.Read<float>();
        mesh.boundsMin.y = reader.Read<float>();
        mesh.boundsMin.z = reader.Read<float>();
        mesh.boundsMax.x = reader.Read<float>();
        mesh.boundsMax.y = reader.Read<float>();
        mesh.boundsMax.z = reader.Read<float>();
        mesh.hasBounds = true;

        P3D_TRACE << "      -> Bounds min=("
                  << mesh.boundsMin.x << ", " << mesh.boundsMin.y << ", " << mesh.boundsMin.z
                  << ") max=("
                  << mesh.boundsMax.x << ", " << mesh.boundsMax.y << ", " << mesh.boundsMax.z
                  << ")" << std::endl;
    }

    void P3DParser::ParseSkeletonChunk(ChunkReader& reader, uint32_t headerDataSize) {
        std::string name = reader.ReadP3DString();
        uint32_t version = reader.Read<uint32_t>();
        uint32_t numJoints = 0;
        P3D_TRACE << "  -> Parsing Skeleton: " << name << " (V" << version << ", Joints: " << numJoints << ")" << std::endl;

        ParsedSkeleton skel;
        skel.name = name;
        m_parsedFile.skeletons.push_back(skel);
    }

    void P3DParser::ParseSkeletonJointChunk(ChunkReader& reader, uint32_t headerDataSize) {
        std::string name = reader.ReadP3DString();
        int32_t parentIndex = reader.Read<int32_t>();
        int32_t dof = reader.Read<int32_t>();
        int32_t freeRot = reader.Read<int32_t>();
        int32_t primaryAxis = reader.Read<int32_t>();
        int32_t secondaryAxis = reader.Read<int32_t>();
        int32_t twistAxis = reader.Read<int32_t>();

        ParsedBone bone;
        bone.name = name;
        bone.parentIndex = parentIndex;

        for (int row = 0; row < 4; ++row) {
            for (int col = 0; col < 4; ++col) {
                bone.restPoseMatrix[row][col] = reader.Read<float>();
            }
        }
        bone.localTransform = bone.restPoseMatrix;
        bone.worldTransform = bone.restPoseMatrix;

        m_parsedFile.skeleton.push_back(bone);
        if (!m_parsedFile.skeletons.empty()) {
            m_parsedFile.skeletons.back().joints.push_back(bone);
        }
    }

    void P3DParser::ParseSkeleton2Chunk(ChunkReader& reader, uint32_t headerDataSize) {
        std::string name = reader.ReadP3DString();
        uint32_t version = reader.Read<uint32_t>();
        uint32_t numJoints = reader.Read<uint32_t>();
        uint32_t numPartitions = reader.Read<uint32_t>();
        uint32_t numLimbs = reader.Read<uint32_t>();
        
        P3D_TRACE << "  -> Parsing Skeleton2 (CTTR): " << name 
                  << " (Joints: " << numJoints 
                  << ", Partitions: " << numPartitions 
                  << ", Limbs: " << numLimbs << ")" << std::endl;

        ParsedSkeleton skel;
        skel.name = name;
        m_parsedFile.skeletons.push_back(skel);
    }

    void P3DParser::ParseSkeletonJoint2Chunk(ChunkReader& reader, uint32_t headerDataSize) {
        std::string name = reader.ReadP3DString();
        uint32_t parentIndex = reader.Read<uint32_t>();
        
        ParsedBone bone;
        bone.name = name;
        bone.parentIndex = (int32_t)parentIndex;
        
        for (int row = 0; row < 4; ++row) {
            for (int col = 0; col < 4; ++col) {
                bone.localTransform[row][col] = reader.Read<float>();
            }
        }
        bone.restPoseMatrix = bone.localTransform;
        bone.worldTransform = bone.localTransform;
        
        P3D_TRACE << "    -> Joint: " << name << " Parent: " << (int)parentIndex << std::endl;
        m_parsedFile.skeleton.push_back(bone);
        if (!m_parsedFile.skeletons.empty()) {
            m_parsedFile.skeletons.back().joints.push_back(bone);
        }
    }

    void P3DParser::ParseLocatorChunk(ChunkReader& reader, uint32_t headerDataSize) {
        std::string name = reader.ReadP3DString();
        
        ParsedLocator locator;
        locator.name = name;
        
        uint32_t type = reader.Read<uint32_t>();
        locator.type = type;
        uint32_t dataLen = reader.Read<uint32_t>();
        for (uint32_t i = 0; i < dataLen; ++i) {
            locator.data.push_back(reader.Read<float>());
        }
        
        locator.position.x = reader.Read<float>();
        locator.position.y = reader.Read<float>();
        locator.position.z = reader.Read<float>();
        locator.transform = glm::mat4(1.0f);
        locator.transform[3] = glm::vec4(locator.position, 1.0f);
        locator.isClashAnchor = false;
        locator.isWeaponMount = false;

        P3D_TRACE << "  -> Found Locator: " << name << " at (" 
                  << locator.position.x << ", " << locator.position.y << ", " << locator.position.z 
                  << ") type=" << type << std::endl;

        if (name.find("clash") != std::string::npos || name.find("turret") != std::string::npos || name.find("anchor") != std::string::npos) {
            locator.isClashAnchor = true;
        }
        if (name.find("weapon") != std::string::npos || name.find("gun") != std::string::npos || name.find("fire") != std::string::npos) {
            locator.isWeaponMount = true;
        }

        m_parsedFile.locators.push_back(locator);
    }

    void P3DParser::ParseLocatorMatrixChunk(ChunkReader& reader, uint32_t headerDataSize) {
        if (m_parsedFile.locators.empty()) return;
        auto& loc = m_parsedFile.locators.back();
        for (int row = 0; row < 4; ++row) {
            for (int col = 0; col < 4; ++col) {
                loc.transform[row][col] = reader.Read<float>();
            }
        }
        loc.position = glm::vec3(loc.transform[3]);
    }

    void P3DParser::ParseOldLocatorChunk(ChunkReader& reader, uint32_t headerDataSize) {
        std::string name = reader.ReadP3DString();
        ParsedLocator locator;
        locator.name = name;
        locator.position.x = reader.Read<float>();
        locator.position.y = reader.Read<float>();
        locator.position.z = reader.Read<float>();
        locator.transform = glm::mat4(1.0f);
        locator.transform[3] = glm::vec4(locator.position, 1.0f);
        m_parsedFile.locators.push_back(locator);
    }

    void P3DParser::ParseShaderChunk(ChunkReader& reader, uint32_t headerDataSize, size_t nextChunkOffset) {
        size_t chunkStart = reader.Tell() - 12;
        size_t headerEnd = chunkStart + 12 + headerDataSize;

        std::string name = TrimString(reader.ReadP3DString());
        uint32_t version = reader.Read<uint32_t>();
        std::string pddiShaderName = reader.ReadP3DString();
        uint32_t hasTranslucency = reader.Read<uint32_t>();
        uint32_t vertexNeeds = reader.Read<uint32_t>();
        uint32_t vertexMask = reader.Read<uint32_t>();
        uint32_t numParams = reader.Read<uint32_t>();

        ParsedShader shader;
        shader.name = name;
        shader.shaderDefinition = pddiShaderName;

        P3D_TRACE << "  -> Found Shader: " << name << " (pddi=" << pddiShaderName << ")" << std::endl;

        reader.Seek(headerEnd);

        while (reader.Tell() + 12 <= nextChunkOffset) {
            uint32_t cid = reader.Read<uint32_t>();
            uint32_t hlen = reader.Read<uint32_t>();
            uint32_t clen = reader.Read<uint32_t>();

            if (hlen < 12 || clen < hlen) break;
            size_t cstart = reader.Tell() - 12;
            if (cstart + clen > nextChunkOffset) break;
            size_t cnext = cstart + clen;

            if (cid == CHUNK_SHADER_TEXTURE_PARAMETER) {
                char paramCode[5] = {0};
                for (int i = 0; i < 4; ++i) paramCode[i] = static_cast<char>(reader.Read<uint8_t>());
                std::string texName = TrimString(reader.ReadP3DString());
                P3D_TRACE << "     ShaderParam: " << paramCode << " = " << texName << std::endl;
                shader.textureParams[paramCode] = texName;
                if (std::string(paramCode) == "TEX " || std::string(paramCode) == "TEX") {
                    shader.textureName = texName;
                }
            } else if (cid == CHUNK_SHADER_INT_PARAMETER) {
                char paramCode[5] = {0};
                for (int i = 0; i < 4; ++i) paramCode[i] = static_cast<char>(reader.Read<uint8_t>());
                uint32_t val = reader.Read<uint32_t>();
                shader.intParams[paramCode] = val;
            } else if (cid == CHUNK_SHADER_FLOAT_PARAMETER) {
                char paramCode[5] = {0};
                for (int i = 0; i < 4; ++i) paramCode[i] = static_cast<char>(reader.Read<uint8_t>());
                float val = reader.Read<float>();
                shader.floatParams[paramCode] = val;
            } else if (cid == CHUNK_SHADER_COLOUR_PARAMETER) {
                char paramCode[5] = {0};
                for (int i = 0; i < 4; ++i) paramCode[i] = static_cast<char>(reader.Read<uint8_t>());
                uint32_t col = reader.Read<uint32_t>();
                float b = static_cast<float>(col & 0xFF) / 255.0f;
                float g = static_cast<float>((col >> 8) & 0xFF) / 255.0f;
                float r = static_cast<float>((col >> 16) & 0xFF) / 255.0f;
                float a = static_cast<float>((col >> 24) & 0xFF) / 255.0f;
                shader.colorParams[paramCode] = glm::vec4(r, g, b, a);
            }
            reader.Seek(cnext);
        }

        m_parsedFile.shaders.push_back(shader);
    }

    void P3DParser::ParseTextureChunk(ChunkReader& reader, uint32_t headerDataSize, size_t nextChunkOffset) {
        size_t chunkStart = reader.Tell() - 12;
        size_t headerEnd = chunkStart + 12 + headerDataSize;

        std::string name = TrimString(reader.ReadP3DString());
        uint32_t version = reader.Read<uint32_t>();
        uint32_t width = reader.Read<uint32_t>();
        uint32_t height = reader.Read<uint32_t>();
        uint32_t bpp = reader.Read<uint32_t>();
        uint32_t alphaDepth = reader.Read<uint32_t>();
        uint32_t numMipMaps = reader.Read<uint32_t>();
        uint32_t textureType = reader.Read<uint32_t>();
        uint32_t usageHint = reader.Read<uint32_t>();
        uint32_t priority = reader.Read<uint32_t>();

        P3D_TRACE << "  -> Found Texture: " << name << " (" << width << "x" << height << ", bpp=" << bpp << ")" << std::endl;

        ParsedTexture texture;
        texture.name = name;
        texture.width = width;
        texture.height = height;
        texture.bpp = bpp;

        reader.Seek(headerEnd);
        WalkTextureChildren(reader, nextChunkOffset, texture);

        if (!texture.rgbaData.empty()) {
            m_parsedFile.textures.push_back(texture);
        }
    }

    void P3DParser::WalkTextureChildren(ChunkReader& reader, size_t endOffset, ParsedTexture& texture) {
        while (reader.Tell() + 12 <= endOffset) {
            uint32_t cid = reader.Read<uint32_t>();
            uint32_t hlen = reader.Read<uint32_t>();
            uint32_t clen = reader.Read<uint32_t>();

            if (hlen < 12 || clen < hlen) break;

            m_parsedFile.totalChunks++;
            m_parsedFile.chunkTypeCounts[cid]++;

            size_t chunkStart = reader.Tell() - 12;
            size_t headerEnd = chunkStart + hlen;
            size_t nextChunkOffset = chunkStart + clen;
            if (nextChunkOffset > endOffset) break;

            if (cid == CHUNK_IMAGE) {
                ParseImageChunk(reader, hlen - 12, nextChunkOffset, texture);
            } else if (cid == CHUNK_IMAGE_DATA) {
                ParseImageDataChunk(reader, hlen - 12, texture);
            } else {
                reader.Seek(headerEnd);
                if (nextChunkOffset > headerEnd) {
                    WalkTextureChildren(reader, nextChunkOffset, texture);
                }
            }
            reader.Seek(nextChunkOffset);
        }
    }

    void P3DParser::ParseImageChunk(ChunkReader& reader, uint32_t headerDataSize, size_t nextChunkOffset, ParsedTexture& texture) {
        std::string name = reader.ReadP3DString();
        uint32_t version = reader.Read<uint32_t>();
        uint32_t width = reader.Read<uint32_t>();
        uint32_t height = reader.Read<uint32_t>();
        uint32_t bpp = reader.Read<uint32_t>();
        uint32_t palettized = reader.Read<uint32_t>();
        uint32_t hasAlpha = reader.Read<uint32_t>();
        uint32_t format = reader.Read<uint32_t>();

        if (texture.width == 0) texture.width = width;
        if (texture.height == 0) texture.height = height;
        if (texture.bpp == 0) texture.bpp = bpp;
        texture.format = format;

        size_t headerEnd = reader.Tell();
        WalkTextureChildren(reader, nextChunkOffset, texture);
    }

    void P3DParser::ParseImageDataChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedTexture& texture) {
        uint32_t dataLen = reader.Read<uint32_t>();
        if (dataLen < 28) return;

        char magic[5] = {0};
        for (int i = 0; i < 4; ++i) magic[i] = static_cast<char>(reader.Read<uint8_t>());

        if (std::string(magic) != "P3DI") {
            return;
        }

        uint32_t format = reader.Read<uint32_t>();
        uint32_t width = reader.Read<uint32_t>();
        uint32_t height = reader.Read<uint32_t>();
        uint32_t bpp = reader.Read<uint32_t>();
        uint32_t paletteSize = reader.Read<uint32_t>();
        uint32_t pixelDataSize = reader.Read<uint32_t>();

        std::vector<uint8_t> payload(dataLen - 28);
        for (size_t i = 0; i < payload.size(); ++i) {
            payload[i] = reader.Read<uint8_t>();
        }

        if (payload.size() < pixelDataSize + paletteSize) {
            return;
        }

        // A texture may contain lower-resolution Image_Data mip levels.
        // Retain the largest complete raster as the material's base level.
        if (!texture.rgbaData.empty()
            && static_cast<uint64_t>(width) * height
                <= static_cast<uint64_t>(texture.width) * texture.height) {
            return;
        }

        // Image_Data is the payload's authoritative raster description.
        texture.width = width;
        texture.height = height;
        texture.bpp = bpp;
        texture.format = format;

        const uint8_t* paletteData = (paletteSize > 0) ? payload.data() : nullptr;
        const uint8_t* pixelData = payload.data() + paletteSize;

        texture.rgbaData.resize(width * height * 4);

        if (bpp == 8) {
            const uint32_t storageWidth = width;
            for (uint32_t y = 0; y < height; ++y) {
                for (uint32_t x = 0; x < width; ++x) {
                    uint32_t srcAddr = GetPSMT8Address(x, y, storageWidth);
                    uint8_t colorIdx = (srcAddr < pixelDataSize) ? pixelData[srcAddr] : 0;
                    uint32_t clutIdx = GetCLUTIndex(colorIdx, 8);
                    size_t ci = static_cast<size_t>(clutIdx) * 4;
                    size_t dstOff = (y * width + x) * 4;
                    if (paletteData && ci + 3 < paletteSize) {
                        texture.rgbaData[dstOff + 0] = paletteData[ci + 0];
                        texture.rgbaData[dstOff + 1] = paletteData[ci + 1];
                        texture.rgbaData[dstOff + 2] = paletteData[ci + 2];
                        texture.rgbaData[dstOff + 3] = static_cast<uint8_t>(
                            std::min<uint32_t>(paletteData[ci + 3] * 2u, 255u));
                    } else {
                        texture.rgbaData[dstOff + 0] = 255;
                        texture.rgbaData[dstOff + 1] = 255;
                        texture.rgbaData[dstOff + 2] = 255;
                        texture.rgbaData[dstOff + 3] = 255;
                    }
                }
            }
        } else if (bpp == 4) {
            for (uint32_t i = 0; i < width * height; ++i) {
                size_t byteIdx = i / 2;
                uint8_t byteVal = (byteIdx < pixelDataSize) ? pixelData[byteIdx] : 0;
                uint8_t nibble = (i % 2 == 0) ? (byteVal & 0x0F) : ((byteVal >> 4) & 0x0F);
                size_t ci = static_cast<size_t>(nibble) * 4;
                size_t dstOff = i * 4;
                if (paletteData && ci + 3 < paletteSize) {
                    texture.rgbaData[dstOff + 0] = paletteData[ci + 0];
                    texture.rgbaData[dstOff + 1] = paletteData[ci + 1];
                    texture.rgbaData[dstOff + 2] = paletteData[ci + 2];
                    texture.rgbaData[dstOff + 3] = static_cast<uint8_t>(
                        std::min<uint32_t>(paletteData[ci + 3] * 2u, 255u));
                } else {
                    texture.rgbaData[dstOff + 0] = 255;
                    texture.rgbaData[dstOff + 1] = 255;
                    texture.rgbaData[dstOff + 2] = 255;
                    texture.rgbaData[dstOff + 3] = 255;
                }
            }
        } else if (bpp == 16) {
            uint32_t totalPixels = width * height;
            for (uint32_t i = 0; i < totalPixels; ++i) {
                if (i * 2 + 1 < pixelDataSize) {
                    uint16_t px = pixelData[i * 2] | (pixelData[i * 2 + 1] << 8);
                    size_t dstOff = i * 4;
                    texture.rgbaData[dstOff + 0] = static_cast<uint8_t>((px & 0x1F) << 3);
                    texture.rgbaData[dstOff + 1] = static_cast<uint8_t>(((px >> 5) & 0x1F) << 3);
                    texture.rgbaData[dstOff + 2] = static_cast<uint8_t>(((px >> 10) & 0x1F) << 3);
                    texture.rgbaData[dstOff + 3] = (px & 0x8000) ? 255 : 0;
                }
            }
        } else if (bpp == 32) {
            for (uint32_t y = 0; y < height; ++y) {
                for (uint32_t x = 0; x < width; ++x) {
                    const uint32_t srcAddr = GetPSMCT32Address(x, y, width);
                    const size_t dstOff = (y * width + x) * 4;
                    if (srcAddr + 3 < pixelDataSize) {
                        std::memcpy(texture.rgbaData.data() + dstOff, pixelData + srcAddr, 4);
                    }
                }
            }
        }
    }

    void P3DParser::ParseCollisionObjectChunk(ChunkReader& reader, uint32_t headerDataSize, size_t nextChunkOffset) {
        size_t chunkStart = reader.Tell() - 12;
        size_t headerEnd = chunkStart + 12 + headerDataSize;

        std::string name = reader.ReadP3DString();
        uint32_t version = reader.Read<uint32_t>();
        uint32_t numSubVolumes = reader.Read<uint32_t>();

        ParsedCollisionObject colObj;
        colObj.name = name;

        reader.Seek(headerEnd);
        WalkCollisionObjectChildren(reader, nextChunkOffset, colObj);

        m_parsedFile.collisionObjects.push_back(colObj);
    }

    void P3DParser::WalkCollisionObjectChildren(ChunkReader& reader, size_t endOffset, ParsedCollisionObject& colObj) {
        while (reader.Tell() + 12 <= endOffset) {
            uint32_t cid = reader.Read<uint32_t>();
            uint32_t hlen = reader.Read<uint32_t>();
            uint32_t clen = reader.Read<uint32_t>();

            if (hlen < 12 || clen < hlen) break;

            m_parsedFile.totalChunks++;
            m_parsedFile.chunkTypeCounts[cid]++;

            size_t chunkStart = reader.Tell() - 12;
            size_t headerEnd = chunkStart + hlen;
            size_t nextChunkOffset = chunkStart + clen;
            if (nextChunkOffset > endOffset) break;

            if (cid == CHUNK_COLLISION_VOLUME) {
                WalkCollisionVolumeChildren(reader, nextChunkOffset, colObj);
            } else if (cid == CHUNK_COLLISION_SPHERE) {
                ParseCollisionSphereChunk(reader, hlen - 12, colObj);
            } else if (cid == CHUNK_COLLISION_OBB) {
                ParseCollisionOBBChunk(reader, hlen - 12, colObj);
            } else if (cid == CHUNK_COLLISION_AABB) {
                ParseCollisionAABBChunk(reader, hlen - 12, colObj);
            } else if (cid == CHUNK_COLLISION_MESH_TREE || cid == CHUNK_COLLISION_MESH_TRIANGLE_LIST) {
                ParseCollisionMeshChunk(reader, hlen - 12, nextChunkOffset, colObj);
            } else {
                reader.Seek(headerEnd);
                if (nextChunkOffset > headerEnd) {
                    WalkCollisionObjectChildren(reader, nextChunkOffset, colObj);
                }
            }
            reader.Seek(nextChunkOffset);
        }
    }

    void P3DParser::WalkCollisionVolumeChildren(ChunkReader& reader, size_t endOffset, ParsedCollisionObject& colObj) {
        while (reader.Tell() + 12 <= endOffset) {
            uint32_t cid = reader.Read<uint32_t>();
            uint32_t hlen = reader.Read<uint32_t>();
            uint32_t clen = reader.Read<uint32_t>();

            if (hlen < 12 || clen < hlen) break;

            m_parsedFile.totalChunks++;
            m_parsedFile.chunkTypeCounts[cid]++;

            size_t chunkStart = reader.Tell() - 12;
            size_t headerEnd = chunkStart + hlen;
            size_t nextChunkOffset = chunkStart + clen;
            if (nextChunkOffset > endOffset) break;

            if (cid == CHUNK_COLLISION_SPHERE) {
                ParseCollisionSphereChunk(reader, hlen - 12, colObj);
            } else if (cid == CHUNK_COLLISION_OBB) {
                ParseCollisionOBBChunk(reader, hlen - 12, colObj);
            } else if (cid == CHUNK_COLLISION_AABB) {
                ParseCollisionAABBChunk(reader, hlen - 12, colObj);
            }
            reader.Seek(nextChunkOffset);
        }
    }

    void P3DParser::ParseCollisionSphereChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedCollisionObject& colObj) {
        ParsedCollisionSphere sphere;
        sphere.radius = reader.Read<float>();
        colObj.spheres.push_back(sphere);
    }

    void P3DParser::ParseCollisionOBBChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedCollisionObject& colObj) {
        ParsedCollisionOBB obb;
        obb.halfExtents.x = reader.Read<float>();
        obb.halfExtents.y = reader.Read<float>();
        obb.halfExtents.z = reader.Read<float>();
        colObj.obbs.push_back(obb);
    }

    void P3DParser::ParseCollisionAABBChunk(ChunkReader& reader, uint32_t headerDataSize, ParsedCollisionObject& colObj) {
        ParsedCollisionAABB aabb;
        aabb.minBound.x = reader.Read<float>();
        aabb.minBound.y = reader.Read<float>();
        aabb.minBound.z = reader.Read<float>();
        aabb.maxBound.x = reader.Read<float>();
        aabb.maxBound.y = reader.Read<float>();
        aabb.maxBound.z = reader.Read<float>();
        colObj.aabbs.push_back(aabb);
    }

    void P3DParser::ParseCollisionMeshChunk(ChunkReader& reader, uint32_t headerDataSize, size_t nextChunkOffset, ParsedCollisionObject& colObj) {
        // Read collision triangles if present
        uint32_t count = reader.Read<uint32_t>();
        for (uint32_t i = 0; i < count; ++i) {
            ParsedCollisionTriangle tri;
            tri.v0.x = reader.Read<float>();
            tri.v0.y = reader.Read<float>();
            tri.v0.z = reader.Read<float>();
            tri.v1.x = reader.Read<float>();
            tri.v1.y = reader.Read<float>();
            tri.v1.z = reader.Read<float>();
            tri.v2.x = reader.Read<float>();
            tri.v2.y = reader.Read<float>();
            tri.v2.z = reader.Read<float>();
            colObj.triangles.push_back(tri);
        }
    }

    void P3DParser::ParseMemoryImageVertexList(ChunkReader& reader, uint32_t headerDataSize, ParsedMesh& mesh) {
        uint32_t version = reader.Read<uint32_t>();
        uint32_t param = reader.Read<uint32_t>();
        uint32_t vertDataSize = reader.Read<uint32_t>();

        if (vertDataSize < 12) {
            return;
        }

        size_t payloadStart = reader.Tell();
        std::vector<uint8_t> vifBytes(vertDataSize);
        reader.ReadBytes(vifBytes.data(), vertDataSize);

        struct VifBatch {
            std::vector<glm::vec3> positions;
            std::vector<glm::vec2> uvs;
            std::vector<bool> adcFlags;
            std::vector<uint8_t> boneIndices;
        };
        std::vector<VifBatch> batches;
        VifBatch currentBatch;

        size_t voff = 0;
        while (voff + 4 <= vifBytes.size()) {
            uint32_t word;
            memcpy(&word, &vifBytes[voff], 4);
            uint32_t cmd7 = (word >> 24) & 0x7F;
            uint32_t num  = (word >> 16) & 0xFF;
            uint32_t imm  = word & 0xFFFF;

            if ((cmd7 & 0x60) == 0x60) { // VIF UNPACK
                uint32_t vn = (cmd7 >> 2) & 0x03;
                uint32_t vl = cmd7 & 0x03;
                uint32_t vuAddr = imm & 0x03FF;
                
                if (vuAddr == 0 && !currentBatch.positions.empty()) {
                    batches.push_back(std::move(currentBatch));
                    currentBatch = VifBatch{};
                }

                size_t elemCount = vn + 1;
                size_t rowBytes;
                if (vl == 3 && vn == 3) {
                    rowBytes = 2;
                } else if (vl == 0) {
                    rowBytes = elemCount * 4;
                } else if (vl == 1) {
                    rowBytes = elemCount * 2;
                } else if (vl == 2) {
                    rowBytes = elemCount;
                } else {
                    rowBytes = elemCount * 4;
                }

                size_t totalBytes = num * rowBytes;
                size_t padded = ((totalBytes + 3) / 4) * 4;

                if (voff + 4 + padded <= vifBytes.size()) {
                    const uint8_t* payload = &vifBytes[voff + 4];

                    if (vuAddr == 0x005 && vl == 0 && vn == 2) {
                        const float* fp = reinterpret_cast<const float*>(payload);
                        for (size_t i = 0; i < num; ++i) {
                            float x = fp[i * 3 + 0];
                            float y = fp[i * 3 + 1];
                            float z = fp[i * 3 + 2];
                            currentBatch.positions.push_back(glm::vec3(x, y, z));
                        }
                    } else if (vuAddr == 0x003 && vl == 0 && vn == 1) {
                        const uint32_t* up = reinterpret_cast<const uint32_t*>(payload);
                        for (size_t i = 0; i < num; ++i) {
                            uint32_t rawU = up[i * 2 + 0];
                            uint32_t rawV = up[i * 2 + 1];
                            bool adc = ((rawU & 1u) != 0) || ((rawV & 1u) != 0);
                            currentBatch.adcFlags.push_back(adc);

                            uint32_t cleanU = rawU & ~1u;
                            uint32_t cleanV = rawV & ~1u;
                            float u, v;
                            std::memcpy(&u, &cleanU, 4);
                            std::memcpy(&v, &cleanV, 4);
                            currentBatch.uvs.push_back(glm::vec2(u, v));
                        }
                    } else if (vuAddr == 0x006 && vl == 2 && vn == 3) {
                        for (size_t i = 0; i < num; ++i) {
                            uint8_t palIdx = payload[i * 4] / 4;
                            currentBatch.boneIndices.push_back(palIdx);
                        }
                    }
                }

                voff += 4 + padded;
            } else if (cmd7 == 0x14 || cmd7 == 0x17) {
                if (!currentBatch.positions.empty()) {
                    batches.push_back(std::move(currentBatch));
                    currentBatch = VifBatch{};
                }
                voff += 4;
            } else if (cmd7 == 0x50 || cmd7 == 0x51) {
                voff += 4 + (imm * 16);
            } else if (cmd7 == 0x20) {
                voff += 8;
            } else if (cmd7 == 0x30 || cmd7 == 0x31) {
                voff += 20;
            } else if (cmd7 == 0x01) {
                voff += 4;
            } else {
                voff += 4;
            }
        }
        
        if (!currentBatch.positions.empty()) {
            batches.push_back(std::move(currentBatch));
        }

        std::vector<ParsedVertex> validVertices;
        mesh.stripStarts.clear();
        P3D_TRACE << "      -> Decoding VIF Mesh: " << mesh.name << " (batches: " << batches.size() << ", primType: " << mesh.primitiveType << ")" << std::endl;
        for (size_t bIdx = 0; bIdx < batches.size(); ++bIdx) {
            const auto& batch = batches[bIdx];
            if (batch.positions.empty()) continue;

            size_t startVertIdx = 0;
            bool isContinuation = false;

            // Check PS2 VU1 2-vertex overlap continuation with previous batch
            if (bIdx > 0 && validVertices.size() >= 2 && batch.positions.size() >= 2) {
                const auto& prevP0 = validVertices[validVertices.size() - 2].position;
                const auto& prevP1 = validVertices[validVertices.size() - 1].position;
                if (prevP0 == batch.positions[0] && prevP1 == batch.positions[1]) {
                    startVertIdx = 2;
                    isContinuation = true;
                }
            }

            // If it's a triangle strip and not a seamless VU1 continuation, record a strip boundary
            if (mesh.primitiveType == 1 && !isContinuation) {
                mesh.stripStarts.push_back(static_cast<uint32_t>(validVertices.size()));
            }

            bool prevAdc = false;
            for (size_t i = startVertIdx; i < batch.positions.size(); ++i) {
                // If this vertex marks a strip restart via PS2 ADC (no-kick) flag, register a strip start
                if (mesh.primitiveType == 1 && i < batch.adcFlags.size()) {
                    bool adc = batch.adcFlags[i];
                    if (adc && !prevAdc) {
                        uint32_t curIdx = static_cast<uint32_t>(validVertices.size());
                        if (mesh.stripStarts.empty() || mesh.stripStarts.back() != curIdx) {
                            mesh.stripStarts.push_back(curIdx);
                        }
                    }
                    prevAdc = adc;
                }

                ParsedVertex vert{};
                vert.position = batch.positions[i];
                vert.normal = glm::vec3(0.0f, 1.0f, 0.0f);
                vert.uv = (i < batch.uvs.size()) ? batch.uvs[i] : glm::vec2(0.0f, 0.0f);

                int realBone = 0;
                if (i < batch.boneIndices.size()) {
                    uint8_t palIdx = batch.boneIndices[i];
                    if (palIdx < mesh.matrixPalette.size()) {
                        realBone = static_cast<int>(mesh.matrixPalette[palIdx]);
                    } else {
                        realBone = palIdx;
                    }
                }
                vert.boneIndices = glm::ivec4(realBone, 0, 0, 0);
                vert.boneWeights = glm::vec4(1.0f, 0.0f, 0.0f, 0.0f);

                vert.rawPosX = static_cast<int16_t>(std::clamp(static_cast<int>(std::lround(vert.position.x)), -32768, 32767));
                vert.rawPosY = static_cast<int16_t>(std::clamp(static_cast<int>(std::lround(vert.position.y)), -32768, 32767));
                vert.rawPosZ = static_cast<int16_t>(std::clamp(static_cast<int>(std::lround(vert.position.z)), -32768, 32767));
                validVertices.push_back(vert);
            }
        }

        mesh.vertices = std::move(validVertices);
        reader.Seek(payloadStart + vertDataSize);
        P3D_TRACE << "      -> Decoded PS2 VIF MemoryImageVertexList: " << mesh.vertices.size() << " vertices." << std::endl;
    }

    void P3DParser::ParseMemoryImageVertexDescription(ChunkReader& reader, uint32_t headerDataSize) {
        size_t chunkStart = reader.Tell() - 12;
        reader.Seek(chunkStart + 12);

        m_lastDescription = VertexLayoutDescription{};
        m_lastDescription.version = reader.Read<uint32_t>();
        m_lastDescription.param = reader.Read<uint32_t>();
        uint32_t numFields = reader.Read<uint32_t>();

        uint32_t fieldsToRead = numFields;
        for (uint32_t i = 0; i < fieldsToRead; ++i) {
            VertexFieldDescription field;
            field.type = reader.Read<uint32_t>();
            field.dataType = reader.Read<uint32_t>();
            field.offset = reader.Read<uint32_t>();
            field.stride = reader.Read<uint32_t>();
            m_lastDescription.fields.push_back(field);
        }
    }

    void P3DParser::ParseCompositeDrawableChunk(ChunkReader& reader, uint32_t headerDataSize, size_t nextChunkOffset) {
        uint32_t version = reader.Read<uint32_t>();
        std::string name = reader.ReadP3DString();
        std::string skeletonName = reader.ReadP3DString();
        skeletonName = TrimString(skeletonName);
        uint32_t numElements = reader.Read<uint32_t>();

        ParsedCompositeDrawable compDrawable;
        compDrawable.name = TrimString(name);
        compDrawable.skeletonName = skeletonName;

        while (reader.Tell() + 12 <= nextChunkOffset) {
            uint32_t chunkId = reader.Read<uint32_t>();
            uint32_t headerSize = reader.Read<uint32_t>();
            uint32_t chunkSize = reader.Read<uint32_t>();
            if (headerSize < 12 || chunkSize < headerSize) break;
            size_t chunkStart = reader.Tell() - 12;
            size_t headerEnd = chunkStart + headerSize;
            size_t childNext = chunkStart + chunkSize;
            if (childNext > nextChunkOffset) break;

            if (chunkId == CHUNK_COMPOSITE_DRAWABLE_PROP) {
                uint32_t propVersion = reader.Read<uint32_t>();
                uint32_t isTranslucent = reader.Read<uint32_t>();
                std::string propName = reader.ReadP3DString();
                uint32_t flags = reader.Read<uint32_t>();
                uint32_t jointIndex = reader.Read<uint32_t>();

                ParsedCompositeDrawableProp prop;
                prop.name = TrimString(propName);
                prop.jointIndex = jointIndex;
                compDrawable.props.push_back(prop);
            }
            reader.Seek(childNext);
        }

        m_parsedFile.compositeDrawables.push_back(compDrawable);
    }

#undef P3D_TRACE

} // namespace Parser
} // namespace CTTR
