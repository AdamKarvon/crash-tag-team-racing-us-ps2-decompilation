#pragma once
#include <vector>
#include <glm/glm.hpp>

namespace CTTR {
namespace Renderer {

    // Immediate-mode style Debug Drawing subsystem
    // Completely decoupled from the production MeshRenderer
    class DebugDraw {
    public:
        DebugDraw();
        ~DebugDraw();

        void Initialize();
        void Shutdown();

        // Must be called at the start of each frame to clear the draw buffer
        void BeginFrame();

        // Core Drawing Primitives
        void DrawLine(const glm::vec3& start, const glm::vec3& end, const glm::vec3& color);
        void DrawSphere(const glm::vec3& center, float radius, const glm::vec3& color);
        void DrawAABB(const glm::vec3& minBounds, const glm::vec3& maxBounds, const glm::vec3& color);
        
        // Critical for reverse engineering: Visualizes the RGB Coordinate Frame
        // X = Red, Y = Green, Z = Blue
        void DrawAxis(const glm::mat4& transform, float scale = 1.0f);

        // Flushes the accumulated debug geometry to the GPU
        void Render(const glm::mat4& viewMatrix, const glm::mat4& projMatrix);

    private:
        struct DebugVertex {
            glm::vec3 position;
            glm::vec3 color;
        };

        std::vector<DebugVertex> m_lineVertices;
        
        // OpenGL IDs
        uint32_t m_vao, m_vbo;
        uint32_t m_shaderProgram;
    };

} // namespace Renderer
} // namespace CTTR
