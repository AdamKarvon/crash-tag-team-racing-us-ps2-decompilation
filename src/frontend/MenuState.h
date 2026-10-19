#pragma once
// =============================================================================
//  MenuState.h  —  Frontend Selection Flow State Machine
//
//  Screen flow:   StartScreen → ModeSelection → CharacterSelection
//
//  Model ownership:
//    Each character has its own P3DLoader instance (lazy-loaded on first
//    highlight).  The loader must have its meshes buffered to the GPU via
//    GLRenderer::BufferMesh before being handed to FrontendRenderer for
//    DrawCharacterViewport — see MenuState.cpp EnsureCharacterModel().
//
//  Input contract:
//    main.cpp's GLFW KeyCallback calls MenuState::OnKeyInput(key, action).
//    MenuState never includes GLFW headers directly; it uses the MenuKey /
//    MenuAction constants below (equal to GLFW values).
// =============================================================================

#include <string>
#include <vector>
#include <memory>
#include <functional>

#include "../renderer/P3DLoader.h"  // CTTR::Renderer::P3DLoader

// Forward declaration — avoids pulling the full FrontendRenderer header into
// every translation unit that includes MenuState.h.
namespace CTTR { namespace Frontend { class FrontendRenderer; } }
namespace CTTR { namespace Renderer { class GLRenderer; } }

// GLFW key / action values re-exported as named constants so MenuState.cpp
// never needs to include GLFW.
namespace MenuKey    { constexpr int Space = 32;  constexpr int Enter = 257;
                       constexpr int Left  = 263; constexpr int Right = 262;
                       constexpr int Up    = 265; constexpr int Down  = 264;
                       constexpr int Escape = 256; }
namespace MenuAction { constexpr int Press = 1;   constexpr int Repeat = 2;
                       constexpr int Release = 0; }

namespace CTTR {
namespace Frontend {

// ---------------------------------------------------------------------------
//  Screen enum
// ---------------------------------------------------------------------------
enum class Screen
{
    StartScreen,
    ModeSelection,
    CharacterSelection,
};

enum class GameMode
{
    LocalRace,
    OnlineLobby,
    Options,
    Count
};

// ---------------------------------------------------------------------------
//  Character entry
// ---------------------------------------------------------------------------
struct CharacterEntry
{
    std::string name;           ///< Display name
    std::string modelPath;      ///< Absolute or data-root-relative path to .p3d
    std::string portraitSprite; ///< Sprite name in ui_layout.json
};

// ---------------------------------------------------------------------------
//  MenuState
// ---------------------------------------------------------------------------
class MenuState
{
public:
    /// GLRenderer reference is needed to call BufferMesh on newly loaded
    /// character models before they can be drawn.
    /// @param renderer      Initialised FrontendRenderer.
    /// @param glRenderer    Initialised GLRenderer (for BufferMesh calls).
    /// @param gameDataRoot  Base path prepended to CharacterEntry::modelPath.
    MenuState(FrontendRenderer&          renderer,
              Renderer::GLRenderer&      glRenderer,
              const std::string&         gameDataRoot);

    ~MenuState() = default;

    MenuState(const MenuState&)            = delete;
    MenuState& operator=(const MenuState&) = delete;

    // -----------------------------------------------------------------------
    //  Game loop
    // -----------------------------------------------------------------------
    bool IsActive() const { return m_active; }

    void Update(float dt);  ///< Advance timers, model rotation, blink.
    void Render();           ///< Issue all FrontendRenderer draw calls.

    // -----------------------------------------------------------------------
    //  Input  — called from GLFW KeyCallback in main.cpp
    // -----------------------------------------------------------------------
    void OnKeyInput(int key, int action);

    // -----------------------------------------------------------------------
    //  Accessors
    // -----------------------------------------------------------------------
    Screen   GetCurrentScreen()  const { return m_screen; }
    GameMode GetSelectedMode()   const { return static_cast<GameMode>(m_modeIndex); }
    int      GetCharacterIndex() const { return m_charIndex; }
    int      GetConfirmedCharacterIndex() const { return m_confirmedCharacterIndex; }
    const CharacterEntry* GetConfirmedCharacter() const;

    void Deactivate() { m_active = false; }

private:
    void UpdateStartScreen    (float dt);
    void UpdateModeSelection  (float dt);
    void UpdateCharSelection  (float dt);

    void RenderStartScreen    ();
    void RenderModeSelection  ();
    void RenderCharSelection  ();

    void TransitionTo(Screen next);
    void EnsureCharacterModel(int index);

    // -----------------------------------------------------------------------
    //  Members
    // -----------------------------------------------------------------------
    FrontendRenderer&      m_renderer;
    Renderer::GLRenderer&  m_glRenderer;
    std::string            m_gameDataRoot;

    Screen  m_screen = Screen::StartScreen;
    bool    m_active = true;

    // StartScreen
    float m_blinkTimer   = 0.0f;
    bool  m_blinkVisible = true;
    static constexpr float kBlinkPeriod = 0.5f;

    // ModeSelection
    int m_modeIndex = 0;
    static constexpr int kModeCount = static_cast<int>(GameMode::Count);

    // CharacterSelection
    int   m_charIndex = 0;
    int   m_confirmedCharacterIndex = -1;
    float m_modelRotY = 0.0f;
    static constexpr float kModelRotSpeed = 0.8f; // rad/s

    std::vector<CharacterEntry>                            m_characters;
    // One P3DLoader per character — nullptr until first highlighted.
    std::vector<std::unique_ptr<Renderer::P3DLoader>>      m_loaderCache;

    static constexpr const char* kCharViewportName = "character_preview";

    static const std::vector<CharacterEntry> kDefaultRoster;
};

} // namespace Frontend
} // namespace CTTR
