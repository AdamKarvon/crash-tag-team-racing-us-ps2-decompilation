#include "DebugDraw.h"
#include <glad/glad.h>
#include <iostream>
#include <cmath>

namespace CTTR {
namespace Renderer {

    // Simple Unlit Vertex Color Shader for Debug Geometry
    const char* DEBUG_VERT_SRC = R"(
        #version 330 core
        layout (location = 0) in vec3 aPos;
        layout (location = 1) in vec3 aColor;

        uniform mat4 view;
        uniform mat4 projection;

        out vec3 vertexColor;

        void main() {
            gl_Position = projection * view * vec4(aPos, 1.0);
            vertexColor = aColor;
        }
    )";

    const char* DEBUG_FRAG_SRC = R"(
        #version 330 core
        in vec3 vertexColor;
        out vec4 FragColor;

        void main() {
            FragColor = vec4(vertexColor, 1.0);
        }
    )";

    DebugDraw::DebugDraw() : m_vao(0), m_vbo(0), m_shaderProgram(0) {}
    DebugDraw::~DebugDraw() { Shutdown(); }

    void DebugDraw::Initialize() {
        std::cout << "DebugDraw: Initializing Independent Subsystem..." << std::endl;
        
        glGenVertexArrays(1, &m_vao);
        glGenBuffers(1, &m_vbo);
        
        glBindVertexArray(m_vao);
        glBindBuffer(GL_ARRAY_BUFFER, m_vbo);
        
        // Define attribute pointers
        // Position
        glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, sizeof(DebugVertex), (void*)offsetof(DebugVertex, position));
        glEnableVertexAttribArray(0);
        // Color
        glVertexAttribPointer(1, 3, GL_FLOAT, GL_FALSE, sizeof(DebugVertex), (void*)offsetof(DebugVertex, color));
        glEnableVertexAttribArray(1);
        
        glBindVertexArray(0);

        // Compile Shader
        uint32_t vertexShader = glCreateShader(GL_VERTEX_SHADER);
        glShaderSource(vertexShader, 1, &DEBUG_VERT_SRC, NULL);
        glCompileShader(vertexShader);
        
        int success;
        char infoLog[512];
        glGetShaderiv(vertexShader, GL_COMPILE_STATUS, &success);
        if (!success) {
            glGetShaderInfoLog(vertexShader, 512, NULL, infoLog);
            std::cerr << "DebugDraw ERROR::SHADER::VERTEX::COMPILATION_FAILED\n" << infoLog << std::endl;
        }
        
        uint32_t fragmentShader = glCreateShader(GL_FRAGMENT_SHADER);
        glShaderSource(fragmentShader, 1, &DEBUG_FRAG_SRC, NULL);
        glCompileShader(fragmentShader);
        
        glGetShaderiv(fragmentShader, GL_COMPILE_STATUS, &success);
        if (!success) {
            glGetShaderInfoLog(fragmentShader, 512, NULL, infoLog);
            std::cerr << "DebugDraw ERROR::SHADER::FRAGMENT::COMPILATION_FAILED\n" << infoLog << std::endl;
        }
        
        m_shaderProgram = glCreateProgram();
        glAttachShader(m_shaderProgram, vertexShader);
        glAttachShader(m_shaderProgram, fragmentShader);
        glLinkProgram(m_shaderProgram);
        
        glGetProgramiv(m_shaderProgram, GL_LINK_STATUS, &success);
        if (!success) {
            glGetProgramInfoLog(m_shaderProgram, 512, NULL, infoLog);
            std::cerr << "DebugDraw ERROR::SHADER::PROGRAM::LINKING_FAILED\n" << infoLog << std::endl;
        }
        
        glDeleteShader(vertexShader);
        glDeleteShader(fragmentShader);
    }

    void DebugDraw::Shutdown() {
        if (m_vao) glDeleteVertexArrays(1, &m_vao);
        if (m_vbo) glDeleteBuffers(1, &m_vbo);
        if (m_shaderProgram) glDeleteProgram(m_shaderProgram);
        m_lineVertices.clear();
    }

    void DebugDraw::BeginFrame() {
        m_lineVertices.clear();
    }

    void DebugDraw::DrawLine(const glm::vec3& start, const glm::vec3& end, const glm::vec3& color) {
        m_lineVertices.push_back({start, color});
        m_lineVertices.push_back({end, color});
    }

    void DebugDraw::DrawAxis(const glm::mat4& transform, float scale) {
        // Extract translation
        glm::vec3 pos = glm::vec3(transform[3]);
        
        // Extract basis vectors
        glm::vec3 right   = glm::vec3(transform[0]) * scale;
        glm::vec3 up      = glm::vec3(transform[1]) * scale;
        glm::vec3 forward = glm::vec3(transform[2]) * scale;

        // X = Red, Y = Green, Z = Blue (RGB)
        DrawLine(pos, pos + right,   glm::vec3(1.0f, 0.0f, 0.0f)); 
        DrawLine(pos, pos + up,      glm::vec3(0.0f, 1.0f, 0.0f)); 
        DrawLine(pos, pos + forward, glm::vec3(0.0f, 0.0f, 1.0f)); 
    }

    void DebugDraw::DrawSphere(const glm::vec3& center, float radius, const glm::vec3& color) {
        // Draw 3 interlocking circles (XY, XZ, YZ planes) to represent a sphere
        const int SEGMENTS = 16;
        const float STEP = (2.0f * 3.14159265f) / SEGMENTS;

        for (int i = 0; i < SEGMENTS; ++i) {
            float theta1 = i * STEP;
            float theta2 = (i + 1) * STEP;

            // XY Plane
            glm::vec3 xy1 = center + glm::vec3(cos(theta1) * radius, sin(theta1) * radius, 0.0f);
            glm::vec3 xy2 = center + glm::vec3(cos(theta2) * radius, sin(theta2) * radius, 0.0f);
            DrawLine(xy1, xy2, color);

            // XZ Plane
            glm::vec3 xz1 = center + glm::vec3(cos(theta1) * radius, 0.0f, sin(theta1) * radius);
            glm::vec3 xz2 = center + glm::vec3(cos(theta2) * radius, 0.0f, sin(theta2) * radius);
            DrawLine(xz1, xz2, color);

            // YZ Plane
            glm::vec3 yz1 = center + glm::vec3(0.0f, cos(theta1) * radius, sin(theta1) * radius);
            glm::vec3 yz2 = center + glm::vec3(0.0f, cos(theta2) * radius, sin(theta2) * radius);
            DrawLine(yz1, yz2, color);
        }
    }

    void DebugDraw::DrawAABB(const glm::vec3& minBounds, const glm::vec3& maxBounds, const glm::vec3& color) {
        // Draw the 12 lines of an Axis-Aligned Bounding Box
        glm::vec3 p0 = minBounds;
        glm::vec3 p1 = glm::vec3(maxBounds.x, minBounds.y, minBounds.z);
        glm::vec3 p2 = glm::vec3(maxBounds.x, maxBounds.y, minBounds.z);
        glm::vec3 p3 = glm::vec3(minBounds.x, maxBounds.y, minBounds.z);
        glm::vec3 p4 = glm::vec3(minBounds.x, minBounds.y, maxBounds.z);
        glm::vec3 p5 = glm::vec3(maxBounds.x, minBounds.y, maxBounds.z);
        glm::vec3 p6 = maxBounds;
        glm::vec3 p7 = glm::vec3(minBounds.x, maxBounds.y, maxBounds.z);

        // Bottom face
        DrawLine(p0, p1, color); DrawLine(p1, p2, color);
        DrawLine(p2, p3, color); DrawLine(p3, p0, color);
        // Top face
        DrawLine(p4, p5, color); DrawLine(p5, p6, color);
        DrawLine(p6, p7, color); DrawLine(p7, p4, color);
        // Pillars
        DrawLine(p0, p4, color); DrawLine(p1, p5, color);
        DrawLine(p2, p6, color); DrawLine(p3, p7, color);
    }

    void DebugDraw::Render(const glm::mat4& viewMatrix, const glm::mat4& projMatrix) {
        if (m_lineVertices.empty()) return;

        glBindVertexArray(m_vao);
        glBindBuffer(GL_ARRAY_BUFFER, m_vbo);
        glBufferData(GL_ARRAY_BUFFER, m_lineVertices.size() * sizeof(DebugVertex), m_lineVertices.data(), GL_DYNAMIC_DRAW);
        
        glUseProgram(m_shaderProgram);
        
        int viewLoc = glGetUniformLocation(m_shaderProgram, "view");
        int projLoc = glGetUniformLocation(m_shaderProgram, "projection");
        glUniformMatrix4fv(viewLoc, 1, GL_FALSE, &viewMatrix[0][0]);
        glUniformMatrix4fv(projLoc, 1, GL_FALSE, &projMatrix[0][0]);
        
        glDrawArrays(GL_LINES, 0, static_cast<int>(m_lineVertices.size()));
        glBindVertexArray(0);
    }

} // namespace Renderer
} // namespace CTTR
