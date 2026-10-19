#pragma once
// =============================================================================
//  FrontendRenderer.h  —  2.5D Menu Renderer
//
//  Depends on:
//    renderer/P3DLoader.h  (CTTR::Renderer::P3DLoader, P3DMesh, Vertex)
//    glad/glad.h           (already used by GLRenderer)
//    glm/glm.hpp           (already a project dependency)
//    nlohmann/json         (provided by CMake FetchContent)
//
//  Coordinate convention:
//    UIExporter outputs normalised [0,1]×[0,1] positions authored on a
//    640×480 PS2 virtual canvas.  This renderer maps them onto 1280×720
//    with a pillarbox correction (960 px wide art centred on a 1280 canvas).
// =============================================================================

#include <string>
#include <vector>
#include <unordered_map>
#include <unordered_set>

#include <glad/glad.h>
#include <glm/glm.hpp>

// Use the project's own loader/mesh types — no new abstractions needed.
#include "../renderer/P3DLoader.h"   // CTTR::Renderer::P3DLoader, P3DMesh

namespace CTTR {
namespace Frontend {

// ---------------------------------------------------------------------------
//  JSON data mirrors  (populated by FrontendRenderer::Init from the JSON files)
// ---------------------------------------------------------------------------

struct SpriteEntry
{
    std::string name;
    std::string texture;   ///< TGA filename inside textures/ dir
    float u0, v0, u1, v1; ///< UV rect on the source texture
    float x, y, w, h;     ///< Normalised screen rect [0,1]
    uint32_t colour;       ///< RGBA packed (0xRRGGBBAA)
};

struct Pure3DViewport
{
    std::string name;
    float x, y, w, h;     ///< Normalised screen rect [0,1]
};

struct GlyphEntry
{
    int   codepoint;
    std::string texture;
    float u0, v0, u1, v1;
    float advance;         ///< Normalised advance width
    float w, h;            ///< Normalised glyph dimensions
};

struct FontData
{
    std::string             name;
    float                   lineHeight; ///< Normalised
    std::vector<GlyphEntry> glyphs;
    std::unordered_map<int, size_t> glyphIndex; ///< codepoint → glyphs[] index
};

// ---------------------------------------------------------------------------
//  Sprite batch vertex  (pos xy, uv, rgba  — matches the VAO layout below)
// ---------------------------------------------------------------------------

struct SpriteVertex
{
    float x, y;
    float u, v;
    float r, g, b, a;
};

// ---------------------------------------------------------------------------
//  FrontendRenderer
// ---------------------------------------------------------------------------

class FrontendRenderer
{
public:
    static constexpr float kTargetW  = 1280.0f;
    static constexpr float kTargetH  =  720.0f;
    static constexpr float kArtW     =  960.0f; ///< 4:3 art pillarboxed on 16:9 canvas
    static constexpr float kArtOffX  = (kTargetW - kArtW) * 0.5f; ///< 160 px

    FrontendRenderer()  = default;
    ~FrontendRenderer() { Shutdown(); }

    FrontendRenderer(const FrontendRenderer&)            = delete;
    FrontendRenderer& operator=(const FrontendRenderer&) = delete;

    // -----------------------------------------------------------------------
    //  Lifecycle
    // -----------------------------------------------------------------------

    /// @param assetDir  Directory containing ui_layout.json, ui_fonts.json,
    ///                  and textures/ sub-folder (output of UIExporter).
    bool Init(const std::string& assetDir);
    void Shutdown();
    bool IsReady() const { return m_ready; }

    // -----------------------------------------------------------------------
    //  Per-frame  (call in order: Begin → Draw* → End)
    // -----------------------------------------------------------------------

    void BeginFrame();

    /// Draw all sprites whose name contains @p filter (pass "" for all).
    void DrawSprites(const std::string& filter = "");

    /// Draw a single sprite by exact name using its exported coordinates.
    void DrawSprite(const std::string& name);

    /// Draw a single sprite by exact name at caller-supplied normalised coords.
    void DrawSpriteAt(const std::string& name,
                      float nx, float ny, float nw, float nh,
                      uint32_t colour = 0xFFFFFFFFu);

    /// Draw an untextured screen-space rectangle in normalised coordinates.
    void DrawSolidRect(float nx, float ny, float nw, float nh,
                       uint32_t colour = 0xFFFFFFFFu);

    /// Draw a UTF-8 string with a named bitmap font at a normalised position.
    void DrawText(const std::string& fontName,
                  const std::string& text,
                  float nx, float ny,
                  float scale    = 1.0f,
                  uint32_t colour = 0xFFFFFFFFu);

    /// Flush accumulated 2-D quads to the GPU.
    void FlushBatch();

    /// Composite a 3-D character model inside a named Pure3D viewport.
    /// Depth is cleared within the viewport region only, so the model always
    /// appears over any 2-D sprites already drawn there.
    ///
    /// @param viewportName  Must match a name in ui_layout.json viewports[].
    /// @param loader        P3DLoader that already has the character model loaded.
    /// @param rotationY     Current Y-axis idle rotation in radians.
    void DrawCharacterViewport(const std::string&            viewportName,
                               const Renderer::P3DLoader&    loader,
                               float                         rotationY);

    void EndFrame();

    // -----------------------------------------------------------------------
    //  Accessors
    // -----------------------------------------------------------------------
    const SpriteEntry*    FindSprite  (const std::string& name) const;
    const Pure3DViewport* FindViewport(const std::string& name) const;

private:
    // --- JSON loaders ---
    bool LoadLayoutJSON(const std::string& path);
    bool LoadFontsJSON (const std::string& path);
    bool LoadTexture   (const std::string& texDir, const std::string& filename);

    // --- Batch helper ---
    void PushQuad(float px, float py, float pw, float ph,
                  float u0, float v0, float u1, float v1,
                  float r,  float g,  float b,  float a,
                  GLuint texId);

    // --- Coordinate mapping (normalised → pixel-space) ---
    float MapX(float nx) const { return nx * kTargetW;           }
    float MapY(float ny) const { return ny * kTargetH;           }
    float MapW(float nw) const { return nw * kTargetW;           }
    float MapH(float nh) const { return nh * kTargetH;           }

    void UnpackColour(uint32_t packed,
                      float& r, float& g, float& b, float& a) const;

    // --- GL objects ---
    GLuint m_spriteShader = 0;
    GLuint m_modelShader  = 0;
    GLuint m_vao          = 0;
    GLuint m_vbo          = 0;
    GLuint m_whiteTex     = 0;

    // --- Sprite batch ---
    static constexpr size_t kMaxBatchVerts = 65536;
    std::vector<SpriteVertex> m_batchVerts;
    GLuint                    m_currentTex = 0;

    // --- Asset data ---
    std::vector<SpriteEntry>    m_sprites;
    std::vector<Pure3DViewport> m_viewports;
    std::vector<FontData>       m_fonts;

    std::unordered_map<std::string, size_t>  m_spriteIndex;
    std::unordered_map<std::string, size_t>  m_viewportIndex;
    std::unordered_map<std::string, size_t>  m_fontIndex;
    std::unordered_map<std::string, GLuint>  m_textures;
    std::unordered_set<std::string>          m_failedTextures;

    glm::mat4 m_orthoProj = glm::mat4(1.0f);
    bool      m_ready     = false;
};

} // namespace Frontend
} // namespace CTTR
