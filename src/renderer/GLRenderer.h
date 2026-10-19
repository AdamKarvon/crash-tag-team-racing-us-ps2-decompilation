#pragma once
#include <string>
#include "P3DLoader.h"
#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glad/glad.h>
#include <GLFW/glfw3.h>
#include <filesystem>

namespace CTTR {
namespace Renderer {

    class GLRenderer {
    public:
        GLRenderer();
        ~GLRenderer();

        bool Initialize(int width, int height, const std::string& title);
        void Shutdown();
        bool ShouldClose() const;
        void SwapBuffers();
        void Clear();

        // Uploads the P3D mesh data to the GPU (VBO, VAO, EBO)
        void BufferMesh(P3DMesh& mesh);

        GLFWwindow* GetWindow() const { return m_window; }

        // Main render loop function
        void RenderFrame(const std::vector<P3DMesh>& meshes,
                         const glm::mat4& viewMatrix,
                         const glm::mat4& projMatrix,
                         const glm::mat4& modelMatrix = glm::mat4(1.0f),
                         float currentTime = 0.0f);

        // Headless verification artifacts for preservation/reverse-engineering builds.
        bool ExportVerificationArtifacts(const std::vector<P3DMesh>& meshes, const std::filesystem::path& outputDir) const;

    private:
        uint32_t CompileShader(const char* vertexSrc, const char* fragSrc);
        bool ExportObj(const std::vector<P3DMesh>& meshes, const std::filesystem::path& objPath) const;
        bool ExportReport(const std::vector<P3DMesh>& meshes, const std::filesystem::path& reportPath) const;
        bool ExportPreviewPPM(const std::vector<P3DMesh>& meshes, const std::filesystem::path& ppmPath) const;
        
        struct UniformLocs {
            int model = -1;
            int view = -1;
            int projection = -1;
            int uvOffset = -1;
            int useTexture = -1;
            int textureMap = -1;
            int blendMode = -1;
        };

        UniformLocs m_locs;
        uint32_t m_shaderProgram;
        bool m_isInitialized;
        GLFWwindow* m_window;
    };

} // namespace Renderer
} // namespace CTTR
