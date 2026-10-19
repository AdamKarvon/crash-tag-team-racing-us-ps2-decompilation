// =============================================================================
//  FrontendRenderer.cpp  —  2.5D Menu Renderer Implementation
//
//  Mirrors the shader compilation style from GLRenderer.cpp and uses
//  CTTR::Renderer::P3DMesh / P3DLoader directly — no new model abstractions.
// =============================================================================

#include "FrontendRenderer.h"

#include <fstream>
#include <iostream>
#include <cstring>
#include <algorithm>
#include <array>
#include <cctype>

#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>
#include <GLFW/glfw3.h>

#include <nlohmann/json.hpp>
using json = nlohmann::json;

// TGA loader — zero extra dependencies, defined at the bottom of this file.
static bool LoadTGA(const std::string& path,
                    int& outW, int& outH,
                    std::vector<uint8_t>& outRGBA);

namespace CTTR {
namespace Frontend {

namespace {
    using GlyphPattern = std::array<uint8_t, 7>;

    GlyphPattern FallbackGlyph(unsigned char ch)
    {
        switch (std::toupper(ch))
        {
            case 'A': return {0x0E,0x11,0x11,0x1F,0x11,0x11,0x11};
            case 'B': return {0x1E,0x11,0x11,0x1E,0x11,0x11,0x1E};
            case 'C': return {0x0F,0x10,0x10,0x10,0x10,0x10,0x0F};
            case 'D': return {0x1E,0x11,0x11,0x11,0x11,0x11,0x1E};
            case 'E': return {0x1F,0x10,0x10,0x1E,0x10,0x10,0x1F};
            case 'F': return {0x1F,0x10,0x10,0x1E,0x10,0x10,0x10};
            case 'G': return {0x0F,0x10,0x10,0x13,0x11,0x11,0x0F};
            case 'H': return {0x11,0x11,0x11,0x1F,0x11,0x11,0x11};
            case 'I': return {0x1F,0x04,0x04,0x04,0x04,0x04,0x1F};
            case 'J': return {0x01,0x01,0x01,0x01,0x11,0x11,0x0E};
            case 'K': return {0x11,0x12,0x14,0x18,0x14,0x12,0x11};
            case 'L': return {0x10,0x10,0x10,0x10,0x10,0x10,0x1F};
            case 'M': return {0x11,0x1B,0x15,0x15,0x11,0x11,0x11};
            case 'N': return {0x11,0x19,0x15,0x13,0x11,0x11,0x11};
            case 'O': return {0x0E,0x11,0x11,0x11,0x11,0x11,0x0E};
            case 'P': return {0x1E,0x11,0x11,0x1E,0x10,0x10,0x10};
            case 'Q': return {0x0E,0x11,0x11,0x11,0x15,0x12,0x0D};
            case 'R': return {0x1E,0x11,0x11,0x1E,0x14,0x12,0x11};
            case 'S': return {0x0F,0x10,0x10,0x0E,0x01,0x01,0x1E};
            case 'T': return {0x1F,0x04,0x04,0x04,0x04,0x04,0x04};
            case 'U': return {0x11,0x11,0x11,0x11,0x11,0x11,0x0E};
            case 'V': return {0x11,0x11,0x11,0x11,0x11,0x0A,0x04};
            case 'W': return {0x11,0x11,0x11,0x15,0x15,0x1B,0x11};
            case 'X': return {0x11,0x11,0x0A,0x04,0x0A,0x11,0x11};
            case 'Y': return {0x11,0x11,0x0A,0x04,0x04,0x04,0x04};
            case 'Z': return {0x1F,0x01,0x02,0x04,0x08,0x10,0x1F};
            case '0': return {0x0E,0x11,0x13,0x15,0x19,0x11,0x0E};
            case '1': return {0x04,0x0C,0x04,0x04,0x04,0x04,0x0E};
            case '2': return {0x0E,0x11,0x01,0x02,0x04,0x08,0x1F};
            case '3': return {0x1E,0x01,0x01,0x0E,0x01,0x01,0x1E};
            case '4': return {0x02,0x06,0x0A,0x12,0x1F,0x02,0x02};
            case '5': return {0x1F,0x10,0x10,0x1E,0x01,0x01,0x1E};
            case '6': return {0x0E,0x10,0x10,0x1E,0x11,0x11,0x0E};
            case '7': return {0x1F,0x01,0x02,0x04,0x08,0x08,0x08};
            case '8': return {0x0E,0x11,0x11,0x0E,0x11,0x11,0x0E};
            case '9': return {0x0E,0x11,0x11,0x0F,0x01,0x01,0x0E};
            case '>': return {0x10,0x08,0x04,0x02,0x04,0x08,0x10};
            case '<': return {0x01,0x02,0x04,0x08,0x04,0x02,0x01};
            case '/': return {0x01,0x01,0x02,0x04,0x08,0x10,0x10};
            case '-': return {0x00,0x00,0x00,0x1F,0x00,0x00,0x00};
            default:  return {0x1F,0x11,0x02,0x04,0x04,0x00,0x04};
        }
    }
}

// ---------------------------------------------------------------------------
//  GLSL  — style mirrors GLRenderer.cpp (const char* raw strings)
// ---------------------------------------------------------------------------

// Sprite / font shader  (orthographic, vertex-colour tint)
static const char* SPRITE_VERT = R"(
    #version 330 core
    layout(location = 0) in vec2 aPos;
    layout(location = 1) in vec2 aUV;
    layout(location = 2) in vec4 aColour;

    uniform mat4 uProj;

    out vec2 vUV;
    out vec4 vColour;

    void main() {
        gl_Position = uProj * vec4(aPos, 0.0, 1.0);
        vUV     = aUV;
        vColour = aColour;
    }
)";

static const char* SPRITE_FRAG = R"(
    #version 330 core
    in  vec2 vUV;
    in  vec4 vColour;
    out vec4 FragColor;

    uniform sampler2D uTex;

    void main() {
        FragColor = texture(uTex, vUV) * vColour;
    }
)";

// Character preview shader  (simple diffuse — same Phong as GLRenderer's
// normal-vis shader but lit so the character reads clearly in the viewport)
static const char* MODEL_VERT = R"(
    #version 330 core
    layout(location = 0) in vec3 aPos;
    layout(location = 1) in vec3 aNormal;
    layout(location = 2) in vec2 aTexCoords;

    uniform mat4 model;
    uniform mat4 view;
    uniform mat4 projection;

    out vec3 Normal;
    out vec2 TexCoords;

    void main() {
        gl_Position = projection * view * model * vec4(aPos, 1.0);
        Normal      = mat3(transpose(inverse(model))) * aNormal;
        TexCoords   = aTexCoords;
    }
)";

// Visualise normals for character model, consistent with GLRenderer.cpp's
// verification shader, but blended with a rim-light so it reads nicely in menu.
static const char* MODEL_FRAG = R"(
    #version 330 core
    in  vec3 Normal;
    in  vec2 TexCoords;
    out vec4 FragColor;

    uniform vec3 uLightDir;

    void main() {
        vec3  N      = normalize(Normal);
        float diff   = max(dot(N, uLightDir), 0.0);
        vec3  colour = N * 0.5 + 0.5;           // normal-vis base (matches verification shader)
        FragColor    = vec4(colour * (0.35 + 0.65 * diff), 1.0);
    }
)";

// ---------------------------------------------------------------------------
//  Shader helper  —  mirrors CompileShader in GLRenderer.cpp exactly
// ---------------------------------------------------------------------------

static GLuint CompileShaderPair(const char* vertSrc, const char* fragSrc)
{
    auto compileStage = [](GLenum type, const char* src) -> GLuint
    {
        GLuint id = glCreateShader(type);
        glShaderSource(id, 1, &src, nullptr);
        glCompileShader(id);

        int ok = 0;
        glGetShaderiv(id, GL_COMPILE_STATUS, &ok);
        if (!ok)
        {
            char log[512];
            glGetShaderInfoLog(id, 512, nullptr, log);
            std::cerr << "FrontendRenderer::Shader compile error:\n" << log << std::endl;
            glDeleteShader(id);
            return 0;
        }
        return id;
    };

    GLuint v = compileStage(GL_VERTEX_SHADER,   vertSrc);
    GLuint f = compileStage(GL_FRAGMENT_SHADER, fragSrc);
    if (!v || !f) { glDeleteShader(v); glDeleteShader(f); return 0; }

    GLuint prog = glCreateProgram();
    glAttachShader(prog, v);
    glAttachShader(prog, f);
    glLinkProgram(prog);

    int ok = 0;
    glGetProgramiv(prog, GL_LINK_STATUS, &ok);
    if (!ok)
    {
        char log[512];
        glGetProgramInfoLog(prog, 512, nullptr, log);
        std::cerr << "FrontendRenderer::Shader link error:\n" << log << std::endl;
        glDeleteProgram(prog);
        prog = 0;
    }

    glDeleteShader(v);
    glDeleteShader(f);
    return prog;
}

// ---------------------------------------------------------------------------
//  Init / Shutdown
// ---------------------------------------------------------------------------

bool FrontendRenderer::Init(const std::string& assetDir)
{
    std::cout << "FrontendRenderer: Initialising from " << assetDir << std::endl;

    m_spriteShader = CompileShaderPair(SPRITE_VERT, SPRITE_FRAG);
    m_modelShader  = CompileShaderPair(MODEL_VERT,  MODEL_FRAG);

    if (!m_spriteShader || !m_modelShader)
    {
        std::cerr << "FrontendRenderer: Shader compilation failed." << std::endl;
        return false;
    }

    // Orthographic projection — (left, right, bottom, top, near, far)
    // Y axis points down to match screen-space convention (top-left origin).
    m_orthoProj = glm::ortho(0.0f, kTargetW, kTargetH, 0.0f, -1.0f, 1.0f);

    glUseProgram(m_spriteShader);
    glUniformMatrix4fv(glGetUniformLocation(m_spriteShader, "uProj"),
                       1, GL_FALSE, glm::value_ptr(m_orthoProj));
    glUniform1i(glGetUniformLocation(m_spriteShader, "uTex"), 0);
    glUseProgram(0);

    // --- VAO / VBO  (mirrors GLRenderer::BufferMesh layout) ---
    glGenVertexArrays(1, &m_vao);
    glGenBuffers(1, &m_vbo);

    glBindVertexArray(m_vao);
    glBindBuffer(GL_ARRAY_BUFFER, m_vbo);
    glBufferData(GL_ARRAY_BUFFER,
                 kMaxBatchVerts * sizeof(SpriteVertex),
                 nullptr, GL_DYNAMIC_DRAW);

    // aPos    loc 0  — xy
    glVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, sizeof(SpriteVertex),
                          reinterpret_cast<void*>(offsetof(SpriteVertex, x)));
    glEnableVertexAttribArray(0);
    // aUV     loc 1  — uv
    glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, sizeof(SpriteVertex),
                          reinterpret_cast<void*>(offsetof(SpriteVertex, u)));
    glEnableVertexAttribArray(1);
    // aColour loc 2  — rgba
    glVertexAttribPointer(2, 4, GL_FLOAT, GL_FALSE, sizeof(SpriteVertex),
                          reinterpret_cast<void*>(offsetof(SpriteVertex, r)));
    glEnableVertexAttribArray(2);

    glBindVertexArray(0);
    glBindBuffer(GL_ARRAY_BUFFER, 0);

    m_batchVerts.reserve(kMaxBatchVerts);

    const uint8_t whitePixel[4] = { 255, 255, 255, 255 };
    glGenTextures(1, &m_whiteTex);
    glBindTexture(GL_TEXTURE_2D, m_whiteTex);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, 1, 1, 0,
                 GL_RGBA, GL_UNSIGNED_BYTE, whitePixel);
    glBindTexture(GL_TEXTURE_2D, 0);

    // --- JSON asset data ---
    if (!LoadLayoutJSON(assetDir + "/ui_layout.json")) return false;
    if (!LoadFontsJSON (assetDir + "/ui_fonts.json"))  return false;

    // --- Textures ---
    const std::string texDir = assetDir + "/textures";
    for (auto& s : m_sprites)
        if (m_textures.find(s.texture) == m_textures.end() &&
            m_failedTextures.find(s.texture) == m_failedTextures.end())
            LoadTexture(texDir, s.texture);

    for (auto& fd : m_fonts)
        for (auto& g : fd.glyphs)
            if (m_textures.find(g.texture) == m_textures.end() &&
                m_failedTextures.find(g.texture) == m_failedTextures.end())
                LoadTexture(texDir, g.texture);

    std::cout << "FrontendRenderer: Ready.  "
              << m_sprites.size()   << " sprites, "
              << m_fonts.size()     << " font(s), "
              << m_textures.size()  << " texture(s)." << std::endl;

    m_ready = true;
    return true;
}

void FrontendRenderer::Shutdown()
{
    FlushBatch();
    for (auto& [name, id] : m_textures) glDeleteTextures(1, &id);
    m_textures.clear();
    m_failedTextures.clear();
    if (m_whiteTex)     { glDeleteTextures(1, &m_whiteTex);  m_whiteTex = 0; }
    if (m_vbo)          { glDeleteBuffers(1,      &m_vbo);          m_vbo = 0; }
    if (m_vao)          { glDeleteVertexArrays(1, &m_vao);          m_vao = 0; }
    if (m_spriteShader) { glDeleteProgram(m_spriteShader);  m_spriteShader = 0; }
    if (m_modelShader)  { glDeleteProgram(m_modelShader);   m_modelShader  = 0; }
    m_ready = false;
}

// ---------------------------------------------------------------------------
//  Per-frame
// ---------------------------------------------------------------------------

void FrontendRenderer::BeginFrame()
{
    int fbW = static_cast<int>(kTargetW);
    int fbH = static_cast<int>(kTargetH);
    if (GLFWwindow* window = glfwGetCurrentContext())
    {
        glfwGetFramebufferSize(window, &fbW, &fbH);
    }

    glDisable(GL_SCISSOR_TEST);
    glViewport(0, 0, fbW, fbH);
    glDisable(GL_DEPTH_TEST);
    glEnable(GL_BLEND);
    glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

    glUseProgram(m_spriteShader);
    glBindVertexArray(m_vao);

    m_batchVerts.clear();
    m_currentTex = 0;
}

void FrontendRenderer::DrawSprites(const std::string& filter)
{
    for (const auto& s : m_sprites)
    {
        if (!filter.empty() && s.name.find(filter) == std::string::npos)
            continue;

        auto it = m_textures.find(s.texture);
        if (it == m_textures.end()) continue;

        float r, g, b, a;
        UnpackColour(s.colour, r, g, b, a);

        PushQuad(MapX(s.x), MapY(s.y), MapW(s.w), MapH(s.h),
                 s.u0, s.v0, s.u1, s.v1,
                 r, g, b, a, it->second);
    }
}

void FrontendRenderer::DrawSprite(const std::string& name)
{
    const SpriteEntry* s = FindSprite(name);
    if (!s) return;

    auto it = m_textures.find(s->texture);
    if (it == m_textures.end()) return;

    float r, g, b, a;
    UnpackColour(s->colour, r, g, b, a);

    PushQuad(MapX(s->x), MapY(s->y), MapW(s->w), MapH(s->h),
             s->u0, s->v0, s->u1, s->v1,
             r, g, b, a, it->second);
}

void FrontendRenderer::DrawSpriteAt(const std::string& name,
                                    float nx, float ny, float nw, float nh,
                                    uint32_t colour)
{
    const SpriteEntry* s = FindSprite(name);
    if (!s) return;

    auto it = m_textures.find(s->texture);
    if (it == m_textures.end()) return;

    float r, g, b, a;
    UnpackColour(colour, r, g, b, a);

    PushQuad(MapX(nx), MapY(ny), MapW(nw), MapH(nh),
             s->u0, s->v0, s->u1, s->v1,
             r, g, b, a, it->second);
}

void FrontendRenderer::DrawSolidRect(float nx, float ny, float nw, float nh,
                                     uint32_t colour)
{
    if (!m_whiteTex) return;

    float r, g, b, a;
    UnpackColour(colour, r, g, b, a);

    PushQuad(MapX(nx), MapY(ny), MapW(nw), MapH(nh),
             0.0f, 0.0f, 1.0f, 1.0f,
             r, g, b, a, m_whiteTex);
}

void FrontendRenderer::DrawText(const std::string& fontName,
                                const std::string& text,
                                float nx, float ny,
                                float scale, uint32_t colour)
{
    (void)fontName;
    if (!m_whiteTex) return;

    float r, g, b, a;
    UnpackColour(colour, r, g, b, a);

    float cursorX = MapX(nx);
    float baseY   = MapY(ny);
    const float cell = 4.0f * scale;
    const float pixel = std::max(1.0f, cell * 0.88f);
    const float gap = 2.0f * scale;

    for (unsigned char c : text)
    {
        if (c == ' ')
        {
            cursorX += cell * 4.0f;
            continue;
        }

        const GlyphPattern glyph = FallbackGlyph(c);
        for (int row = 0; row < 7; ++row)
        {
            for (int col = 0; col < 5; ++col)
            {
                if ((glyph[row] & (1 << (4 - col))) == 0) continue;
                PushQuad(cursorX + col * cell,
                         baseY + row * cell,
                         pixel, pixel,
                         0.0f, 0.0f, 1.0f, 1.0f,
                         r, g, b, a, m_whiteTex);
            }
        }

        cursorX += cell * 6.0f + gap;
    }
}

void FrontendRenderer::FlushBatch()
{
    if (m_batchVerts.empty()) return;

    glBindBuffer(GL_ARRAY_BUFFER, m_vbo);
    glBufferSubData(GL_ARRAY_BUFFER, 0,
                    m_batchVerts.size() * sizeof(SpriteVertex),
                    m_batchVerts.data());

    glActiveTexture(GL_TEXTURE0);
    glBindTexture(GL_TEXTURE_2D, m_currentTex);

    glDrawArrays(GL_TRIANGLES, 0, static_cast<GLsizei>(m_batchVerts.size()));

    m_batchVerts.clear();
    m_currentTex = 0;
}

void FrontendRenderer::DrawCharacterViewport(const std::string&         viewportName,
                                             const Renderer::P3DLoader& loader,
                                             float                      rotationY)
{
    // Flush 2-D batch before switching shader and GL state.
    FlushBatch();

    auto vi = m_viewportIndex.find(viewportName);
    if (vi == m_viewportIndex.end())
    {
        std::cerr << "FrontendRenderer: Unknown viewport '" << viewportName << "'" << std::endl;
        return;
    }
    const Pure3DViewport& vp = m_viewports[vi->second];

    // Map normalised rect → integer pixel rect.
    // Note: OpenGL scissor/viewport Y is measured from the BOTTOM of the window,
    // so we flip: gl_y = windowH - (screen_y + screen_h).
    int vpX = static_cast<int>(MapX(vp.x));
    int vpY = static_cast<int>(MapY(vp.y));
    int vpW = static_cast<int>(MapW(vp.w));
    int vpH = static_cast<int>(MapH(vp.h));
    int glY = static_cast<int>(kTargetH) - vpY - vpH;

    glEnable(GL_SCISSOR_TEST);
    glScissor(vpX, glY, vpW, vpH);
    glViewport(vpX, glY, vpW, vpH);

    // --- Depth partition ---
    // Clear depth ONLY inside this sub-viewport so the 3-D model composites
    // over the 2-D sprites already drawn here without z-fighting.
    glEnable(GL_DEPTH_TEST);
    glDepthMask(GL_TRUE);
    glClear(GL_DEPTH_BUFFER_BIT);

    // --- Matrices ---
    float aspect = (vpH > 0) ? static_cast<float>(vpW) / static_cast<float>(vpH) : 1.0f;

    // Camera matches the project convention in main.cpp:
    //   view = glm::translate(mat4(1), vec3(0, -2, -10))  (pan back/up)
    // For the character portrait we want a tighter framing.
    glm::mat4 proj = glm::perspective(glm::radians(45.0f), aspect, 0.1f, 100.0f);
    glm::mat4 view = glm::lookAt(glm::vec3(0.0f,  1.5f, 3.5f),
                                 glm::vec3(0.0f,  0.9f, 0.0f),
                                 glm::vec3(0.0f,  1.0f, 0.0f));
    glm::mat4 mdl  = glm::rotate(glm::mat4(1.0f), rotationY, glm::vec3(0, 1, 0));

    // --- Render all meshes from the loader ---
    // P3DLoader::GetMeshes() returns const std::vector<P3DMesh>&.
    // Each P3DMesh already has .vao / .ebo / .indices buffered to the GPU
    // by GLRenderer::BufferMesh — we just bind and draw.
    glUseProgram(m_modelShader);

    glUniformMatrix4fv(glGetUniformLocation(m_modelShader, "model"),      1, GL_FALSE, glm::value_ptr(mdl));
    glUniformMatrix4fv(glGetUniformLocation(m_modelShader, "view"),       1, GL_FALSE, glm::value_ptr(view));
    glUniformMatrix4fv(glGetUniformLocation(m_modelShader, "projection"), 1, GL_FALSE, glm::value_ptr(proj));

    glm::vec3 lightDir = glm::normalize(glm::vec3(0.5f, 1.0f, 0.8f));
    glUniform3fv(glGetUniformLocation(m_modelShader, "uLightDir"), 1, glm::value_ptr(lightDir));

    for (const auto& mesh : loader.GetMeshes())
    {
        if (mesh.vao == 0 || mesh.indices.empty()) continue;
        glBindVertexArray(mesh.vao);
        glDrawElements(GL_TRIANGLES,
                       static_cast<int>(mesh.indices.size()),
                       GL_UNSIGNED_INT, nullptr);
        glBindVertexArray(0);
    }

    // --- Restore full-screen state ---
    glDisable(GL_SCISSOR_TEST);
    glDisable(GL_DEPTH_TEST);
    int fbW = static_cast<int>(kTargetW);
    int fbH = static_cast<int>(kTargetH);
    if (GLFWwindow* window = glfwGetCurrentContext())
    {
        glfwGetFramebufferSize(window, &fbW, &fbH);
    }
    glViewport(0, 0, fbW, fbH);

    // Restore sprite shader + VAO for any subsequent 2-D draws.
    glUseProgram(m_spriteShader);
    glBindVertexArray(m_vao);
    glEnable(GL_BLEND);
    glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
}

void FrontendRenderer::EndFrame()
{
    FlushBatch();
    glBindVertexArray(0);
    glUseProgram(0);
    glDisable(GL_BLEND);
}

// ---------------------------------------------------------------------------
//  Accessors
// ---------------------------------------------------------------------------

const SpriteEntry* FrontendRenderer::FindSprite(const std::string& name) const
{
    auto it = m_spriteIndex.find(name);
    return (it != m_spriteIndex.end()) ? &m_sprites[it->second] : nullptr;
}

const Pure3DViewport* FrontendRenderer::FindViewport(const std::string& name) const
{
    auto it = m_viewportIndex.find(name);
    return (it != m_viewportIndex.end()) ? &m_viewports[it->second] : nullptr;
}

// ---------------------------------------------------------------------------
//  JSON loading
// ---------------------------------------------------------------------------

bool FrontendRenderer::LoadLayoutJSON(const std::string& path)
{
    std::ifstream f(path);
    if (!f.is_open())
    {
        std::cerr << "FrontendRenderer: Cannot open " << path << std::endl;
        return false;
    }

    json doc;
    try { f >> doc; }
    catch (const json::parse_error& e)
    {
        std::cerr << "FrontendRenderer: JSON parse error in " << path
                  << ": " << e.what() << std::endl;
        return false;
    }

    for (const auto& s : doc.value("sprites", json::array()))
    {
        SpriteEntry e{};
        e.name    = s.value("name",    "");
        e.texture = s.value("texture", "");
        e.u0      = s.value("u0", 0.0f);  e.v0 = s.value("v0", 0.0f);
        e.u1      = s.value("u1", 1.0f);  e.v1 = s.value("v1", 1.0f);
        e.x       = s.value("x",  0.0f);  e.y  = s.value("y",  0.0f);
        e.w       = s.value("w",  1.0f);  e.h  = s.value("h",  1.0f);
        e.colour  = s.value("colour", 0xFFFFFFFFu);
        if (m_spriteIndex.find(e.name) == m_spriteIndex.end())
            m_spriteIndex[e.name] = m_sprites.size();
        m_sprites.push_back(std::move(e));
    }

    for (const auto& v : doc.value("viewports", json::array()))
    {
        Pure3DViewport vp{};
        vp.name = v.value("name", "");
        vp.x    = v.value("x", 0.0f); vp.y = v.value("y", 0.0f);
        vp.w    = v.value("w", 1.0f); vp.h = v.value("h", 1.0f);
        if (m_viewportIndex.find(vp.name) == m_viewportIndex.end())
            m_viewportIndex[vp.name] = m_viewports.size();
        m_viewports.push_back(std::move(vp));
    }

    if (m_viewportIndex.find("character_preview") == m_viewportIndex.end())
    {
        const char* aliases[] = { "character10", "character20", "character40" };
        for (const char* alias : aliases)
        {
            auto it = m_viewportIndex.find(alias);
            if (it == m_viewportIndex.end()) continue;
            Pure3DViewport vp = m_viewports[it->second];
            vp.name = "character_preview";
            m_viewportIndex[vp.name] = m_viewports.size();
            m_viewports.push_back(std::move(vp));
            break;
        }
    }

    std::cout << "FrontendRenderer: Loaded " << m_sprites.size()
              << " sprites, " << m_viewports.size()
              << " viewports from " << path << std::endl;
    return true;
}

bool FrontendRenderer::LoadFontsJSON(const std::string& path)
{
    std::ifstream f(path);
    if (!f.is_open())
    {
        std::cerr << "FrontendRenderer: Cannot open " << path << std::endl;
        return false;
    }

    json doc;
    try { f >> doc; }
    catch (const json::parse_error& e)
    {
        std::cerr << "FrontendRenderer: JSON parse error in " << path
                  << ": " << e.what() << std::endl;
        return false;
    }

    // Support both direct array and object with "Fonts" array
    json fontsArray = doc.is_array() ? doc : doc.value("Fonts", json::array());

    for (const auto& fd : fontsArray)
    {
        FontData font{};
        font.name       = fd.value("name",        "");
        font.lineHeight = fd.value("line_height",  1.0f);

        for (const auto& g : fd.value("glyphs", json::array()))
        {
            GlyphEntry ge{};
            ge.codepoint = g.value("codepoint", 0);
            ge.texture   = g.value("texture",   "");
            ge.u0 = g.value("u0", 0.0f); ge.v0 = g.value("v0", 0.0f);
            ge.u1 = g.value("u1", 1.0f); ge.v1 = g.value("v1", 1.0f);
            ge.advance   = g.value("advance", 0.0f);
            ge.w         = g.value("w", 0.0f);
            ge.h         = g.value("h", 0.0f);
            font.glyphIndex[ge.codepoint] = font.glyphs.size();
            font.glyphs.push_back(std::move(ge));
        }

        m_fontIndex[font.name] = m_fonts.size();
        m_fonts.push_back(std::move(font));
    }

    std::cout << "FrontendRenderer: Loaded " << m_fonts.size()
              << " font(s) from " << path << std::endl;
    return true;
}

bool FrontendRenderer::LoadTexture(const std::string& texDir,
                                   const std::string& filename)
{
    std::string fullPath = texDir + "/" + filename;
    int w = 0, h = 0;
    std::vector<uint8_t> rgba;

    if (!LoadTGA(fullPath, w, h, rgba))
    {
        std::cerr << "FrontendRenderer: Failed to load TGA: " << fullPath << std::endl;
        m_failedTextures.insert(filename);
        return false;
    }

    GLuint tid;
    glGenTextures(1, &tid);
    glBindTexture(GL_TEXTURE_2D, tid);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, w, h, 0,
                 GL_RGBA, GL_UNSIGNED_BYTE, rgba.data());
    glBindTexture(GL_TEXTURE_2D, 0);

    m_textures[filename] = tid;
    return true;
}

// ---------------------------------------------------------------------------
//  Batch quad helper
// ---------------------------------------------------------------------------

void FrontendRenderer::PushQuad(float px, float py, float pw, float ph,
                                float u0, float v0, float u1, float v1,
                                float r,  float g,  float b,  float a,
                                GLuint texId)
{
    if (m_currentTex != 0 && texId != m_currentTex) FlushBatch();
    if (m_batchVerts.size() + 6 > kMaxBatchVerts)   FlushBatch();

    m_currentTex = texId;

    float x0 = px,      y0 = py;
    float x1 = px + pw, y1 = py + ph;

    // Two CCW triangles: TL-BL-BR, TL-BR-TR
    SpriteVertex verts[6] = {
        { x0, y0,  u0, v0,  r, g, b, a },
        { x0, y1,  u0, v1,  r, g, b, a },
        { x1, y1,  u1, v1,  r, g, b, a },
        { x0, y0,  u0, v0,  r, g, b, a },
        { x1, y1,  u1, v1,  r, g, b, a },
        { x1, y0,  u1, v0,  r, g, b, a },
    };
    m_batchVerts.insert(m_batchVerts.end(), std::begin(verts), std::end(verts));
}

void FrontendRenderer::UnpackColour(uint32_t packed,
                                    float& r, float& g, float& b, float& a) const
{
    r = ((packed >> 24) & 0xFF) / 255.0f;
    g = ((packed >> 16) & 0xFF) / 255.0f;
    b = ((packed >>  8) & 0xFF) / 255.0f;
    a = ((packed >>  0) & 0xFF) / 255.0f;
}

} // namespace Frontend
} // namespace CTTR

// ===========================================================================
//  Minimal TGA loader — uncompressed 24/32-bit, handles both origin flavours
// ===========================================================================

static bool LoadTGA(const std::string& path,
                    int& outW, int& outH,
                    std::vector<uint8_t>& outRGBA)
{
    std::ifstream f(path, std::ios::binary);
    if (!f.is_open()) return false;

    uint8_t hdr[18];
    if (!f.read(reinterpret_cast<char*>(hdr), 18)) return false;

    uint8_t idLen   = hdr[0];
    uint8_t imgType = hdr[2];
    int     w       = hdr[12] | (hdr[13] << 8);
    int     h       = hdr[14] | (hdr[15] << 8);
    uint8_t bpp     = hdr[16];
    uint8_t imgDesc = hdr[17];

    if (imgType != 2)
    {
        std::cerr << "LoadTGA: unsupported type " << (int)imgType
                  << " in " << path << std::endl;
        return false;
    }
    if (bpp != 24 && bpp != 32)
    {
        std::cerr << "LoadTGA: unsupported bpp " << (int)bpp
                  << " in " << path << std::endl;
        return false;
    }

    f.seekg(idLen, std::ios::cur);

    int    channels  = bpp / 8;
    size_t dataSize  = static_cast<size_t>(w) * h * channels;
    std::vector<uint8_t> raw(dataSize);
    if (!f.read(reinterpret_cast<char*>(raw.data()), dataSize)) return false;

    outW = w;
    outH = h;
    outRGBA.resize(w * h * 4);

    bool topLeft = (imgDesc & 0x20) != 0;

    for (int row = 0; row < h; ++row)
    {
        int srcRow = topLeft ? row : (h - 1 - row);
        for (int col = 0; col < w; ++col)
        {
            int src = (srcRow * w + col) * channels;
            int dst = (row    * w + col) * 4;

            // TGA stores BGR(A) on disk
            outRGBA[dst + 0] = raw[src + 2]; // R
            outRGBA[dst + 1] = raw[src + 1]; // G
            outRGBA[dst + 2] = raw[src + 0]; // B
            outRGBA[dst + 3] = (channels == 4) ? raw[src + 3] : 255;
        }
    }

    return true;
}
