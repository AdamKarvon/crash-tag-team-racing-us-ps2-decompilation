#include "GLRenderer.h"
#include <iostream>
#include <fstream>
#include <iomanip>
#include <algorithm>
#include <limits>
#include <cmath>

namespace CTTR {
namespace Renderer {

    // A basic shader to verify geometry and vertex colors/normals
    const char* VERTEX_SHADER = R"(
        #version 330 core
        layout (location = 0) in vec3 aPos;
        layout (location = 1) in vec3 aNormal;
        layout (location = 2) in vec2 aTexCoords;

        uniform mat4 model;
        uniform mat4 view;
        uniform mat4 projection;
        uniform vec2 uvOffset;

        out vec3 Normal;
        out vec2 TexCoords;

        void main() {
            gl_Position = projection * view * model * vec4(aPos, 1.0);
            Normal = mat3(transpose(inverse(model))) * aNormal;
            TexCoords = aTexCoords + uvOffset;
        }
    )";

    const char* FRAGMENT_SHADER = R"(
        #version 330 core
        out vec4 FragColor;
        
        in vec3 Normal;
        in vec2 TexCoords;

        uniform sampler2D textureMap;
        uniform bool useTexture;
        uniform int blendMode; // 0: Opaque, 1: AlphaTest, 2: AlphaBlend, 3: Additive

        void main() {
            vec3 norm = normalize(Normal);
            vec3 lightDir = normalize(vec3(0.5, 1.0, 0.7));
            float diff = max(dot(norm, lightDir), 0.35);

            vec4 texColor = vec4(0.65, 0.65, 0.65, 1.0);
            if (useTexture) {
                texColor = texture(textureMap, TexCoords);
            }

            if (texColor.a < 0.05) {
                discard;
            }

            if (blendMode == 3) {
                // Additive caustics: delicate shimmering light webs on water floor
                vec3 causticLight = max(texColor.rgb - 0.08, vec3(0.0)) * 0.4;
                FragColor = vec4(causticLight, 1.0);
            } else if (blendMode == 2) {
                // Translucent water surface plane
                FragColor = vec4(texColor.rgb * diff * 0.8, 0.45);
            } else {
                FragColor = vec4(texColor.rgb * diff, texColor.a);
            }
        }
    )";

    GLRenderer::GLRenderer() : m_isInitialized(false), m_shaderProgram(0), m_window(nullptr) {}
    GLRenderer::~GLRenderer() { Shutdown(); }

    bool GLRenderer::Initialize(int width, int height, const std::string& title) {
        std::cout << "Initializing OpenGL Renderer: " << title << " (" << width << "x" << height << ")" << std::endl;
        
        if (!glfwInit()) {
            std::cerr << "Failed to initialize GLFW." << std::endl;
            return false;
        }

        glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
        glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3);
        glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);

        m_window = glfwCreateWindow(width, height, title.c_str(), NULL, NULL);
        if (!m_window) {
            std::cerr << "Failed to create GLFW window." << std::endl;
            glfwTerminate();
            return false;
        }

        glfwMakeContextCurrent(m_window);

        if (!gladLoadGLLoader((GLADloadproc)glfwGetProcAddress)) {
            std::cerr << "Failed to initialize GLAD." << std::endl;
            return false;
        }

        glfwSwapInterval(1);

        m_shaderProgram = CompileShader(VERTEX_SHADER, FRAGMENT_SHADER);
        m_isInitialized = true;
        
        m_locs.model = glGetUniformLocation(m_shaderProgram, "model");
        m_locs.view = glGetUniformLocation(m_shaderProgram, "view");
        m_locs.projection = glGetUniformLocation(m_shaderProgram, "projection");
        m_locs.uvOffset = glGetUniformLocation(m_shaderProgram, "uvOffset");
        m_locs.useTexture = glGetUniformLocation(m_shaderProgram, "useTexture");
        m_locs.textureMap = glGetUniformLocation(m_shaderProgram, "textureMap");
        m_locs.blendMode = glGetUniformLocation(m_shaderProgram, "blendMode");

        glEnable(GL_DEPTH_TEST);
        glDepthFunc(GL_LEQUAL);
        // Pure3D foliage and cards are authored as double-sided alpha-tested
        // geometry; culling one side removes half of the visible tree cards.
        glDisable(GL_CULL_FACE);
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        glEnable(GL_SAMPLE_ALPHA_TO_COVERAGE);

        return true;
    }

    void GLRenderer::Shutdown() {
        if (m_isInitialized) {
            std::cout << "Shutting down OpenGL Renderer." << std::endl;
            if (m_window) {
                glfwDestroyWindow(m_window);
            }
            glfwTerminate();
            m_isInitialized = false;
        }
    }

    bool GLRenderer::ShouldClose() const {
        if (!m_window) return false;
        return glfwWindowShouldClose(m_window);
    }

    void GLRenderer::SwapBuffers() {
        if (m_window) {
            glfwSwapBuffers(m_window);
            glfwPollEvents();
        }
    }

    void GLRenderer::Clear() {
        glClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    }

    void GLRenderer::BufferMesh(P3DMesh& mesh) {
        if (!m_isInitialized) return;

        glGenVertexArrays(1, &mesh.vao);
        glGenBuffers(1, &mesh.vbo);
        glGenBuffers(1, &mesh.ebo);
        
        glBindVertexArray(mesh.vao);
        
        glBindBuffer(GL_ARRAY_BUFFER, mesh.vbo);
        glBufferData(GL_ARRAY_BUFFER, mesh.vertices.size() * sizeof(Vertex), mesh.vertices.data(), GL_STATIC_DRAW);
        
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, mesh.ebo);
        glBufferData(GL_ELEMENT_ARRAY_BUFFER, mesh.indices.size() * sizeof(uint32_t), mesh.indices.data(), GL_STATIC_DRAW);
        
        // Vertex Attribute Pointers
        // Position
        glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, sizeof(Vertex), (void*)offsetof(Vertex, position));
        glEnableVertexAttribArray(0);
        // Normal
        glVertexAttribPointer(1, 3, GL_FLOAT, GL_FALSE, sizeof(Vertex), (void*)offsetof(Vertex, normal));
        glEnableVertexAttribArray(1);
        // TexCoords
        glVertexAttribPointer(2, 2, GL_FLOAT, GL_FALSE, sizeof(Vertex), (void*)offsetof(Vertex, texCoords));
        glEnableVertexAttribArray(2);
        
        glBindVertexArray(0);

        // Upload Texture to GPU if mesh has decoded texture data
        if (mesh.hasTexture && !mesh.textureData.empty() && 
            mesh.textureWidth > 0 && mesh.textureHeight > 0 && 
            mesh.textureData.size() >= static_cast<size_t>(mesh.textureWidth * mesh.textureHeight * 4) && 
            mesh.textureId == 0) {
            glGenTextures(1, &mesh.textureId);
            glBindTexture(GL_TEXTURE_2D, mesh.textureId);
            glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, mesh.textureWidth, mesh.textureHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, mesh.textureData.data());
            glGenerateMipmap(GL_TEXTURE_2D);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
            glBindTexture(GL_TEXTURE_2D, 0);
            std::cout << "[GLRenderer] Uploaded OpenGL texture for mesh '" << mesh.name 
                      << "' (" << mesh.textureWidth << "x" << mesh.textureHeight << ") ID=" << mesh.textureId << std::endl;
        }
    }

    void GLRenderer::RenderFrame(const std::vector<P3DMesh>& meshes,
                                 const glm::mat4& viewMatrix,
                                 const glm::mat4& projMatrix,
                                 const glm::mat4& modelMatrix,
                                 float currentTime) {
        if (!m_isInitialized || meshes.empty()) return;

        glUseProgram(m_shaderProgram);
        glUniformMatrix4fv(m_locs.view, 1, GL_FALSE, &viewMatrix[0][0]);
        glUniformMatrix4fv(m_locs.projection, 1, GL_FALSE, &projMatrix[0][0]);
        glUniformMatrix4fv(m_locs.model, 1, GL_FALSE, &modelMatrix[0][0]);

        // Bucket meshes by blend mode
        std::vector<const P3DMesh*> opaqueMeshes;
        std::vector<const P3DMesh*> alphaBlendMeshes;
        std::vector<const P3DMesh*> additiveMeshes;

        for (const auto& mesh : meshes) {
            if (mesh.blendMode == BlendMode::Additive) {
                additiveMeshes.push_back(&mesh);
            } else if (mesh.blendMode == BlendMode::AlphaBlend) {
                alphaBlendMeshes.push_back(&mesh);
            } else {
                opaqueMeshes.push_back(&mesh);
            }
        }

        auto drawMeshList = [&](const std::vector<const P3DMesh*>& list) {
            for (const auto* meshPtr : list) {
                const auto& mesh = *meshPtr;
                glm::vec2 offset = mesh.uvScroll * currentTime;
                glUniform2f(m_locs.uvOffset, offset.x, offset.y);
                glUniform1i(m_locs.blendMode, static_cast<int>(mesh.blendMode));

                if (mesh.hasTexture && mesh.textureId != 0) {
                    glActiveTexture(GL_TEXTURE0);
                    glBindTexture(GL_TEXTURE_2D, mesh.textureId);
                    glUniform1i(m_locs.useTexture, 1);
                    glUniform1i(m_locs.textureMap, 0);
                } else {
                    glUniform1i(m_locs.useTexture, 0);
                }

                glBindVertexArray(mesh.vao);
                glDrawElements(GL_TRIANGLES, static_cast<int>(mesh.indices.size()), GL_UNSIGNED_INT, 0);
                glBindVertexArray(0);
            }
        };

        // Pass 1: Opaque & Cutout
        glDisable(GL_BLEND);
        glDepthMask(GL_TRUE);
        drawMeshList(opaqueMeshes);

        // Pass 2: Alpha-blended (e.g. water surface plane, glass)
        if (!alphaBlendMeshes.empty()) {
            glEnable(GL_BLEND);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
            glDepthMask(GL_FALSE);
            drawMeshList(alphaBlendMeshes);
        }

        // Pass 3: Additive caustics ripples (GL_ONE, GL_ONE, depth write disabled)
        if (!additiveMeshes.empty()) {
            glEnable(GL_BLEND);
            glBlendFunc(GL_ONE, GL_ONE);
            glDepthMask(GL_FALSE);
            drawMeshList(additiveMeshes);
        }

        // Restore default state
        glDepthMask(GL_TRUE);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    }

    uint32_t GLRenderer::CompileShader(const char* vertexSrc, const char* fragSrc) {
        std::cout << "Compiling Verification Shaders..." << std::endl;
        
        uint32_t vertexShader = glCreateShader(GL_VERTEX_SHADER);
        glShaderSource(vertexShader, 1, &vertexSrc, NULL);
        glCompileShader(vertexShader);
        
        int success;
        char infoLog[512];
        glGetShaderiv(vertexShader, GL_COMPILE_STATUS, &success);
        if (!success) {
            glGetShaderInfoLog(vertexShader, 512, NULL, infoLog);
            std::cerr << "ERROR::SHADER::VERTEX::COMPILATION_FAILED\n" << infoLog << std::endl;
        }
        
        uint32_t fragmentShader = glCreateShader(GL_FRAGMENT_SHADER);
        glShaderSource(fragmentShader, 1, &fragSrc, NULL);
        glCompileShader(fragmentShader);
        
        glGetShaderiv(fragmentShader, GL_COMPILE_STATUS, &success);
        if (!success) {
            glGetShaderInfoLog(fragmentShader, 512, NULL, infoLog);
            std::cerr << "ERROR::SHADER::FRAGMENT::COMPILATION_FAILED\n" << infoLog << std::endl;
        }
        
        uint32_t shaderProgram = glCreateProgram();
        glAttachShader(shaderProgram, vertexShader);
        glAttachShader(shaderProgram, fragmentShader);
        glLinkProgram(shaderProgram);
        
        glGetProgramiv(shaderProgram, GL_LINK_STATUS, &success);
        if (!success) {
            glGetProgramInfoLog(shaderProgram, 512, NULL, infoLog);
            std::cerr << "ERROR::SHADER::PROGRAM::LINKING_FAILED\n" << infoLog << std::endl;
        }
        
        glDeleteShader(vertexShader);
        glDeleteShader(fragmentShader);
        
        return shaderProgram;
    }

    bool GLRenderer::ExportVerificationArtifacts(const std::vector<P3DMesh>& meshes, const std::filesystem::path& outputDir) const {
        std::error_code ec;
        std::filesystem::create_directories(outputDir, ec);
        if (ec) {
            std::cerr << "Failed to create verification directory: " << outputDir << " (" << ec.message() << ")" << std::endl;
            return false;
        }

        const auto objPath = outputDir / "cttr_vehicle_proxy.obj";
        const auto reportPath = outputDir / "cttr_vehicle_report.txt";
        const auto ppmPath = outputDir / "cttr_vehicle_preview.ppm";

        const bool ok = ExportObj(meshes, objPath) &&
                        ExportReport(meshes, reportPath) &&
                        ExportPreviewPPM(meshes, ppmPath);

        if (ok) {
            std::cout << "Verification artifacts written:" << std::endl;
            std::cout << "  OBJ:    " << objPath.string() << std::endl;
            std::cout << "  Report: " << reportPath.string() << std::endl;
            std::cout << "  Preview:" << ppmPath.string() << std::endl;
        }

        return ok;
    }

    bool GLRenderer::ExportObj(const std::vector<P3DMesh>& meshes, const std::filesystem::path& objPath) const {
        std::ofstream out(objPath);
        if (!out.is_open()) {
            std::cerr << "Failed to open OBJ for writing: " << objPath << std::endl;
            return false;
        }

        out << "# CTTR Online verification geometry\n";
        out << "# Asset-derived proxy meshes from Pure3D parse\n";

        uint32_t vertexBase = 1;
        for (const auto& mesh : meshes) {
            out << "o " << mesh.name << "\n";
            for (const auto& vertex : mesh.vertices) {
                out << "v " << vertex.position.x << " " << vertex.position.y << " " << vertex.position.z << "\n";
            }
            for (const auto& vertex : mesh.vertices) {
                out << "vn " << vertex.normal.x << " " << vertex.normal.y << " " << vertex.normal.z << "\n";
            }
            for (size_t i = 0; i + 2 < mesh.indices.size(); i += 3) {
                const uint32_t a = vertexBase + mesh.indices[i];
                const uint32_t b = vertexBase + mesh.indices[i + 1];
                const uint32_t c = vertexBase + mesh.indices[i + 2];
                out << "f " << a << "//" << a << " " << b << "//" << b << " " << c << "//" << c << "\n";
            }
            vertexBase += static_cast<uint32_t>(mesh.vertices.size());
        }

        return true;
    }

    bool GLRenderer::ExportReport(const std::vector<P3DMesh>& meshes, const std::filesystem::path& reportPath) const {
        std::ofstream out(reportPath);
        if (!out.is_open()) {
            std::cerr << "Failed to open report for writing: " << reportPath << std::endl;
            return false;
        }

        size_t totalVertices = 0;
        size_t totalTriangles = 0;
        glm::vec3 minBounds(std::numeric_limits<float>::max());
        glm::vec3 maxBounds(std::numeric_limits<float>::lowest());

        for (const auto& mesh : meshes) {
            totalVertices += mesh.vertices.size();
            totalTriangles += mesh.indices.size() / 3;
            for (const auto& vertex : mesh.vertices) {
                minBounds = glm::min(minBounds, vertex.position);
                maxBounds = glm::max(maxBounds, vertex.position);
            }
        }

        out << "CTTR Online Verification Report\n";
        out << "================================\n";
        out << "Meshes: " << meshes.size() << "\n";
        out << "Vertices: " << totalVertices << "\n";
        out << "Triangles: " << totalTriangles << "\n";
        out << std::fixed << std::setprecision(4);
        out << "Bounds Min: " << minBounds.x << ", " << minBounds.y << ", " << minBounds.z << "\n";
        out << "Bounds Max: " << maxBounds.x << ", " << maxBounds.y << ", " << maxBounds.z << "\n\n";

        for (const auto& mesh : meshes) {
            out << mesh.name << "\n";
            out << "  vertices=" << mesh.vertices.size() << " triangles=" << (mesh.indices.size() / 3) << "\n";
        }

        return true;
    }

    bool GLRenderer::ExportPreviewPPM(const std::vector<P3DMesh>& meshes, const std::filesystem::path& ppmPath) const {
        constexpr int width = 960;
        constexpr int height = 540;
        std::vector<unsigned char> pixels(width * height * 3, 245);

        glm::vec3 minBounds(std::numeric_limits<float>::max());
        glm::vec3 maxBounds(std::numeric_limits<float>::lowest());
        for (const auto& mesh : meshes) {
            for (const auto& vertex : mesh.vertices) {
                minBounds = glm::min(minBounds, vertex.position);
                maxBounds = glm::max(maxBounds, vertex.position);
            }
        }

        const float spanX = std::max(0.001f, maxBounds.x - minBounds.x);
        const float spanY = std::max(0.001f, maxBounds.y - minBounds.y);
        const float scale = 0.82f * std::min(width / spanX, height / spanY);
        const glm::vec2 center((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f);

        auto project = [&](const glm::vec3& p) {
            const int x = static_cast<int>((p.x - center.x) * scale + width * 0.5f);
            const int y = static_cast<int>(height * 0.5f - (p.y - center.y) * scale);
            return glm::ivec2(x, y);
        };

        auto putPixel = [&](int x, int y, const glm::vec3& color) {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            const size_t idx = static_cast<size_t>((y * width + x) * 3);
            pixels[idx + 0] = static_cast<unsigned char>(std::clamp(color.r, 0.0f, 1.0f) * 255.0f);
            pixels[idx + 1] = static_cast<unsigned char>(std::clamp(color.g, 0.0f, 1.0f) * 255.0f);
            pixels[idx + 2] = static_cast<unsigned char>(std::clamp(color.b, 0.0f, 1.0f) * 255.0f);
        };

        auto drawLine = [&](glm::ivec2 a, glm::ivec2 b, const glm::vec3& color) {
            int x0 = a.x;
            int y0 = a.y;
            const int x1 = b.x;
            const int y1 = b.y;
            const int dx = std::abs(x1 - x0);
            const int sx = x0 < x1 ? 1 : -1;
            const int dy = -std::abs(y1 - y0);
            const int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true) {
                putPixel(x0, y0, color);
                if (x0 == x1 && y0 == y1) break;
                const int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        };

        for (const auto& mesh : meshes) {
            if (mesh.vertices.empty() || mesh.indices.empty()) continue;
            const size_t vertCount = mesh.vertices.size();
            for (size_t i = 0; i + 2 < mesh.indices.size(); i += 3) {
                const uint32_t ia = mesh.indices[i];
                const uint32_t ib = mesh.indices[i + 1];
                const uint32_t ic = mesh.indices[i + 2];

                if (ia >= vertCount || ib >= vertCount || ic >= vertCount) {
                    continue; // Skip invalid out-of-bounds indices safely
                }

                const auto& va = mesh.vertices[ia];
                const auto& vb = mesh.vertices[ib];
                const auto& vc = mesh.vertices[ic];
                glm::vec3 color = glm::normalize(va.normal + vb.normal + vc.normal);
                color = color * 0.5f + glm::vec3(0.5f);
                drawLine(project(va.position), project(vb.position), color);
                drawLine(project(vb.position), project(vc.position), color);
                drawLine(project(vc.position), project(va.position), color);
            }
        }

        std::ofstream out(ppmPath, std::ios::binary);
        if (!out.is_open()) {
            std::cerr << "Failed to open preview for writing: " << ppmPath << std::endl;
            return false;
        }
        out << "P6\n" << width << " " << height << "\n255\n";
        out.write(reinterpret_cast<const char*>(pixels.data()), static_cast<std::streamsize>(pixels.size()));
        return true;
    }

} // namespace Renderer
} // namespace CTTR
