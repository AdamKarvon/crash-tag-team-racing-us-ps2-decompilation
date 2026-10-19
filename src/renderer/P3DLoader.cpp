#include "P3DLoader.h"
#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"
#include <fstream>
#include <iostream>
#include <iterator>
#include <algorithm>
#include <cctype>

namespace CTTR {
namespace Renderer {

    namespace {
        std::string TextureKey(const std::string& name) {
            size_t slash = name.find_last_of("/\\");
            std::string key = slash == std::string::npos ? name : name.substr(slash + 1);
            size_t dot = key.rfind('.');
            if (dot != std::string::npos) key.resize(dot);
            std::transform(key.begin(), key.end(), key.begin(),
                [](unsigned char c) { return static_cast<char>(std::tolower(c)); });
            return key;
        }

        constexpr uint32_t P3D_PRIMITIVE_TRIANGLE_LIST = 0;
        constexpr uint32_t P3D_PRIMITIVE_TRIANGLE_STRIP = 1;

        std::vector<uint32_t> ExpandStripBatches(
            const std::vector<uint32_t>& indices,
            const std::vector<uint32_t>& starts,
            const std::vector<Vertex>& vertices) {
            std::vector<uint32_t> triangles;
            const size_t vertexCount = indices.empty() ? vertices.size() : indices.size();
            if (vertexCount < 3) return triangles;

            std::vector<uint32_t> effectiveStarts = starts;
            if (effectiveStarts.empty()) {
                effectiveStarts.push_back(0);
            }

            std::vector<uint32_t> currentStrip;

            for (size_t sIdx = 0; sIdx < effectiveStarts.size(); ++sIdx) {
                const size_t begin = effectiveStarts[sIdx];
                const size_t end = (sIdx + 1 < effectiveStarts.size()) ? effectiveStarts[sIdx + 1] : vertexCount;
                if (begin >= end || end > vertexCount || (end - begin) < 3) continue;

                currentStrip.clear();

                for (size_t k = begin; k < end; ++k) {
                    uint32_t vIdx = indices.empty() ? static_cast<uint32_t>(k) : indices[k];
                    if (vIdx >= vertices.size()) continue;

                    currentStrip.push_back(vIdx);

                    if (currentStrip.size() >= 3) {
                        size_t n = currentStrip.size();
                        uint32_t a = currentStrip[n - 3];
                        uint32_t b = currentStrip[n - 2];
                        uint32_t c = currentStrip[n - 1];

                        if (a == b || b == c || a == c) {
                            continue;
                        }

                        const glm::vec3& pa = vertices[a].position;
                        const glm::vec3& pb = vertices[b].position;
                        const glm::vec3& pc = vertices[c].position;

                        float e1 = glm::distance(pa, pb);
                        float e2 = glm::distance(pb, pc);
                        float e3 = glm::distance(pc, pa);

                        if (e1 < 1e-4f || e2 < 1e-4f || e3 < 1e-4f) {
                            continue;
                        }

                        // Preserves proper winding based on parity within this continuous strip
                        if ((n & 1) != 0) { // odd: a, b, c
                            triangles.push_back(a);
                            triangles.push_back(b);
                            triangles.push_back(c);
                        } else { // even: b, a, c
                            triangles.push_back(b);
                            triangles.push_back(a);
                            triangles.push_back(c);
                        }
                    }
                }
            }

            return triangles;
        }
    }

    P3DLoader::P3DLoader() {}
    P3DLoader::~P3DLoader() {}

    bool P3DLoader::LoadModel(const std::string& filepath) {
        m_meshes.clear();
        m_skeleton.bones.clear();

        std::ifstream file(filepath, std::ios::binary);
        if (!file.is_open()) {
            std::cerr << "Failed to open .p3d file: " << filepath << std::endl;
            std::cerr << "Falling back to procedural verification mesh." << std::endl;
            BuildFallbackMesh();
            return false;
        }

        std::vector<uint8_t> bytes(
            (std::istreambuf_iterator<char>(file)),
            std::istreambuf_iterator<char>());

        std::cout << "Parsing Pure3D Archive: " << filepath << std::endl;
        if (!m_parser.Parse(bytes)) {
            std::cerr << "P3D parse failed. Falling back to procedural verification mesh." << std::endl;
            BuildFallbackMesh();
            return false;
        }

        BuildRuntimeMeshes();
        BuildRuntimeSkeleton();

        if (m_meshes.empty()) {
            std::cerr << "No renderable meshes decoded. Falling back to procedural verification mesh." << std::endl;
            BuildFallbackMesh();
            return false;
        }

        std::cout << "Runtime mesh conversion complete: " << m_meshes.size()
                  << " mesh(es), " << m_skeleton.bones.size() << " bone(s)." << std::endl;
        return true;
    }

    void P3DLoader::BuildRuntimeMeshes() {
        const auto& parsed = m_parser.GetParsedFile();
        m_meshes.reserve(parsed.meshes.size());

        // 1. Precalculate world transform matrices for all skeletons in file
        std::map<std::string, std::vector<glm::mat4>> skelWorldMatrices;
        for (const auto& skel : parsed.skeletons) {
            std::string sname = skel.name;
            while (!sname.empty() && (sname.back() == ' ' || sname.back() == '\0')) sname.pop_back();
            std::vector<glm::mat4> worldMats(skel.joints.size(), glm::mat4(1.0f));
            for (size_t i = 0; i < skel.joints.size(); ++i) {
                int32_t p = skel.joints[i].parentIndex;
                if (p == (int32_t)i || p < 0 || p >= (int32_t)skel.joints.size()) {
                    worldMats[i] = skel.joints[i].localTransform;
                } else {
                    worldMats[i] = worldMats[p] * skel.joints[i].localTransform;
                }
            }
            skelWorldMatrices[sname] = worldMats;
        }

        // 2. Build composite prop transform lookup: propName -> joint world transform
        std::vector<std::pair<std::string, glm::mat4>> propTransforms;
        for (const auto& cd : parsed.compositeDrawables) {
            auto it = skelWorldMatrices.find(cd.skeletonName);
            if (it != skelWorldMatrices.end()) {
                const auto& worldMats = it->second;
                for (const auto& prop : cd.props) {
                    if (prop.jointIndex < worldMats.size()) {
                        propTransforms.push_back({prop.name, worldMats[prop.jointIndex]});
                    }
                }
            }
        }

        for (const auto& parsedMesh : parsed.meshes) {
            if (parsedMesh.vertices.empty()) {
                continue;
            }

            P3DMesh runtimeMesh;
            runtimeMesh.name = parsedMesh.name;
            runtimeMesh.shaderName = parsedMesh.shaderName;
            runtimeMesh.textureName = parsedMesh.textureName;
            runtimeMesh.vao = runtimeMesh.vbo = runtimeMesh.ebo = 0;
            runtimeMesh.textureId = 0;

            // Use only the texture decoded from this P3D file. External basename
            // caches and fuzzy matching can select a different asset.
            if (!parsedMesh.textureName.empty()) {
                bool found = false;

                for (const auto& tex : parsed.textures) {
                    if (tex.name == parsedMesh.textureName) {
                        runtimeMesh.hasTexture = true;
                        runtimeMesh.textureWidth = tex.width;
                        runtimeMesh.textureHeight = tex.height;
                        runtimeMesh.textureData = tex.rgbaData;
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    const std::string key = TextureKey(parsedMesh.textureName);
                    const Parser::ParsedTexture* candidate = nullptr;
                    bool ambiguous = false;
                    for (const auto& tex : parsed.textures) {
                        if (TextureKey(tex.name) == key) {
                            if (candidate != nullptr) {
                                ambiguous = true;
                                break;
                            }
                            candidate = &tex;
                        }
                    }
                    if (candidate != nullptr && !ambiguous) {
                        runtimeMesh.hasTexture = true;
                        runtimeMesh.textureWidth = candidate->width;
                        runtimeMesh.textureHeight = candidate->height;
                        runtimeMesh.textureData = candidate->rgbaData;
                        found = true;
                    }
                }
                if (!found) {
                    std::string rawName = parsedMesh.textureName;
                    size_t slash = rawName.find_last_of("/\\");
                    std::string base = (slash == std::string::npos) ? rawName : rawName.substr(slash + 1);
                    size_t dot = base.rfind('.');
                    if (dot != std::string::npos) base.resize(dot);

                    std::vector<std::string> searchDirs = {
                        "game_data/textures_decoded/",
                        "C:/Users/Adam/Documents/Project/CTTR_Online/game_data/textures_decoded/"
                    };

                    for (const auto& dir : searchDirs) {
                        std::string pngPath = dir + base + ".png";
                        int w = 0, h = 0, comp = 0;
                        unsigned char* pixels = stbi_load(pngPath.c_str(), &w, &h, &comp, 4);
                        if (pixels) {
                            runtimeMesh.hasTexture = true;
                            runtimeMesh.textureWidth = static_cast<uint32_t>(w);
                            runtimeMesh.textureHeight = static_cast<uint32_t>(h);
                            runtimeMesh.textureData.assign(pixels, pixels + (w * h * 4));
                            stbi_image_free(pixels);
                            found = true;
                            std::cout << "[P3DLoader] Loaded fallback texture from disk: " << pngPath 
                                      << " (" << w << "x" << h << ") for " << parsedMesh.name << std::endl;
                            break;
                        }
                    }
                }
                if (!found) {
                    std::cout << "[P3DLoader] Texture NOT FOUND for mesh: " << parsedMesh.name 
                              << " expected texture: " << parsedMesh.textureName << std::endl;
                }
            }

            auto containsCI = [](const std::string& str, const std::string& sub) {
                auto it = std::search(
                    str.begin(), str.end(),
                    sub.begin(), sub.end(),
                    [](char ch1, char ch2) { return std::tolower(static_cast<unsigned char>(ch1)) == std::tolower(static_cast<unsigned char>(ch2)); }
                );
                return it != str.end();
            };

            if (!runtimeMesh.hasTexture) {
                if (containsCI(runtimeMesh.shaderName, "water") || containsCI(runtimeMesh.name, "water")) {
                    runtimeMesh.hasTexture = true;
                    runtimeMesh.textureWidth = 4;
                    runtimeMesh.textureHeight = 4;
                    runtimeMesh.textureData.resize(4 * 4 * 4);
                    for (size_t i = 0; i < 16; ++i) {
                        runtimeMesh.textureData[i * 4 + 0] = 60;
                        runtimeMesh.textureData[i * 4 + 1] = 140;
                        runtimeMesh.textureData[i * 4 + 2] = 220;
                        runtimeMesh.textureData[i * 4 + 3] = 150; // translucent water
                    }
                } else if (containsCI(runtimeMesh.shaderName, "refract") || containsCI(runtimeMesh.shaderName, "shoe") ||
                           containsCI(runtimeMesh.name, "shoe") || containsCI(runtimeMesh.shaderName, "glass")) {
                    runtimeMesh.hasTexture = true;
                    runtimeMesh.textureWidth = 4;
                    runtimeMesh.textureHeight = 4;
                    runtimeMesh.textureData.resize(4 * 4 * 4);
                    for (size_t i = 0; i < 16; ++i) {
                        runtimeMesh.textureData[i * 4 + 0] = 210;
                        runtimeMesh.textureData[i * 4 + 1] = 230;
                        runtimeMesh.textureData[i * 4 + 2] = 255;
                        runtimeMesh.textureData[i * 4 + 3] = 90; // translucent glass
                    }
                }
            }

            // Material & Blend Mode classification
            if (containsCI(runtimeMesh.shaderName, "caustic") || containsCI(runtimeMesh.name, "caustic")) {
                runtimeMesh.blendMode = BlendMode::Additive;
                // Layer 2 counter-scrolls against Layer 1 to create authentic shimmering caustics
                if (containsCI(runtimeMesh.name, "Shape2") || containsCI(runtimeMesh.name, "pg263") || containsCI(runtimeMesh.name, "pg77")) {
                    runtimeMesh.uvScroll = glm::vec2(-0.04f, 0.035f);
                } else {
                    runtimeMesh.uvScroll = glm::vec2(0.05f, 0.03f);
                }
            } else if (containsCI(runtimeMesh.shaderName, "water") || containsCI(runtimeMesh.name, "water")) {
                runtimeMesh.blendMode = BlendMode::AlphaBlend;
                runtimeMesh.uvScroll = glm::vec2(0.02f, 0.015f);
            } else if (containsCI(runtimeMesh.shaderName, "refract") || containsCI(runtimeMesh.shaderName, "shoe") ||
                       containsCI(runtimeMesh.name, "shoe") || containsCI(runtimeMesh.shaderName, "glass")) {
                runtimeMesh.blendMode = BlendMode::AlphaBlend;
            } else {
                runtimeMesh.blendMode = BlendMode::Opaque;
            }

            // Check if mesh belongs to a composite drawable prop
            glm::mat4 meshTransform(1.0f);
            bool isCompositeProp = false;
            for (const auto& pt : propTransforms) {
                if (parsedMesh.name.find(pt.first) != std::string::npos) {
                    meshTransform = pt.second;
                    isCompositeProp = true;
                    break;
                }
            }

            runtimeMesh.vertices.reserve(parsedMesh.vertices.size());
            glm::vec3 minB(std::numeric_limits<float>::max());
            glm::vec3 maxB(std::numeric_limits<float>::lowest());

            for (const auto& parsedVertex : parsedMesh.vertices) {
                Vertex vertex{};
                glm::vec3 pos = parsedVertex.position;
                glm::vec3 norm = parsedVertex.normal;
                if (isCompositeProp) {
                    pos = glm::vec3(meshTransform * glm::vec4(pos, 1.0f));
                    norm = glm::mat3(meshTransform) * norm;
                    float len = glm::length(norm);
                    if (len > 1e-4f) norm /= len;
                }
                vertex.position = ConvertPS2ToGL(pos);
                vertex.normal = ConvertNormalPS2ToGL(norm);
                vertex.texCoords = parsedVertex.uv;
                vertex.boneWeights = parsedVertex.boneWeights;
                vertex.boneIndices = parsedVertex.boneIndices;
                runtimeMesh.vertices.push_back(vertex);

                minB = glm::min(minB, vertex.position);
                maxB = glm::max(maxB, vertex.position);
            }

            if (!runtimeMesh.vertices.empty()) {
                runtimeMesh.boundsCenter = (minB + maxB) * 0.5f;
            }

            if (parsedMesh.primitiveType == P3D_PRIMITIVE_TRIANGLE_STRIP) {
                runtimeMesh.indices = ExpandStripBatches(
                    parsedMesh.indices,
                    parsedMesh.stripStarts,
                    runtimeMesh.vertices);
            } else {
                runtimeMesh.indices = parsedMesh.indices;
                // Only fall back to sequential index generation if it was declared as an unindexed TRIANGLE_LIST
                if (runtimeMesh.indices.empty() && runtimeMesh.vertices.size() >= 3) {
                    const size_t usableVertexCount = runtimeMesh.vertices.size() - (runtimeMesh.vertices.size() % 3);
                    for (uint32_t i = 0; i < usableVertexCount; ++i) {
                        runtimeMesh.indices.push_back(i);
                    }
                }
            }

            // Filter invalid out-of-bounds indices to prevent GPU & preview crashes
            if (!runtimeMesh.vertices.empty() && !runtimeMesh.indices.empty()) {
                const uint32_t maxIdx = static_cast<uint32_t>(runtimeMesh.vertices.size());
                std::vector<uint32_t> validIndices;
                validIndices.reserve(runtimeMesh.indices.size());
                for (size_t i = 0; i + 2 < runtimeMesh.indices.size(); i += 3) {
                    if (runtimeMesh.indices[i] < maxIdx && 
                        runtimeMesh.indices[i+1] < maxIdx && 
                        runtimeMesh.indices[i+2] < maxIdx) {
                        validIndices.push_back(runtimeMesh.indices[i]);
                        validIndices.push_back(runtimeMesh.indices[i+1]);
                        validIndices.push_back(runtimeMesh.indices[i+2]);
                    }
                }
                runtimeMesh.indices = std::move(validIndices);
            }

            if (!runtimeMesh.vertices.empty()) {
                m_meshes.push_back(runtimeMesh);
            }
        }
    }

    void P3DLoader::BuildRuntimeSkeleton() {
        const auto& parsed = m_parser.GetParsedFile();
        m_skeleton.bones.reserve(parsed.skeleton.size());

        for (const auto& parsedBone : parsed.skeleton) {
            P3DSkeleton::Bone bone;
            bone.name = parsedBone.name;
            bone.parentIndex = parsedBone.parentIndex;
            bone.localBindTransform = parsedBone.localTransform;
            bone.inverseBindTransform = glm::mat4(1.0f);
            m_skeleton.bones.push_back(bone);
        }
    }

    void P3DLoader::BuildFallbackMesh() {
        P3DMesh stubKart;
        stubKart.name = "Fallback_Kart_Verification_Quad";
        stubKart.vao = stubKart.vbo = stubKart.ebo = 0;

        stubKart.vertices = {
            { ConvertPS2ToGL({-1.0f, -1.0f, -2.0f}), ConvertNormalPS2ToGL({0, 0, -1}), {0.0f, 0.0f}, {1,0,0,0}, {0,0,0,0} },
            { ConvertPS2ToGL({ 1.0f, -1.0f, -2.0f}), ConvertNormalPS2ToGL({0, 0, -1}), {1.0f, 0.0f}, {1,0,0,0}, {0,0,0,0} },
            { ConvertPS2ToGL({ 1.0f,  1.0f, -2.0f}), ConvertNormalPS2ToGL({0, 0, -1}), {1.0f, 1.0f}, {1,0,0,0}, {0,0,0,0} },
            { ConvertPS2ToGL({-1.0f,  1.0f, -2.0f}), ConvertNormalPS2ToGL({0, 0, -1}), {0.0f, 1.0f}, {1,0,0,0}, {0,0,0,0} }
        };
        stubKart.indices = { 0, 2, 1, 2, 0, 3 };
        m_meshes.push_back(stubKart);
    }

    glm::vec3 P3DLoader::ConvertPS2ToGL(const glm::vec3& ps2Pos) {
        const float SCALE = 1.0f;
        return glm::vec3(ps2Pos.x * SCALE, ps2Pos.y * SCALE, -ps2Pos.z * SCALE);
    }

    glm::vec3 P3DLoader::ConvertNormalPS2ToGL(const glm::vec3& ps2Normal) {
        return glm::vec3(ps2Normal.x, ps2Normal.y, -ps2Normal.z);
    }

} // namespace Renderer
} // namespace CTTR
