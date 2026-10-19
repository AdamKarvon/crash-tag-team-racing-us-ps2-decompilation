// =============================================================================
//  MenuState.cpp  —  Frontend Selection Flow State Machine Implementation
// =============================================================================

#include "MenuState.h"
#include "FrontendRenderer.h"

#include "../renderer/GLRenderer.h"   // CTTR::Renderer::GLRenderer::BufferMesh

#include <iostream>

namespace CTTR {
namespace Frontend {

// ---------------------------------------------------------------------------
//  Character roster
//  modelPath is relative to gameDataRoot (the same path prefix used by
//  main.cpp: gameDataPath + "/adefault/default/art/cars/...")
// ---------------------------------------------------------------------------

/*static*/ const std::vector<CharacterEntry> MenuState::kDefaultRoster =
{
    { "Crash",  "common/frontend/art/characters/crash_onfoot_model.p3d",      "character0" },
    { "Coco",   "common/frontend/art/characters/coco_onfoot_model.p3d",       "character4" },
    { "Cortex", "common/frontend/art/characters/neocortex_onfoot_model.p3d",  "character1" },
    { "Nina",   "common/frontend/art/characters/nina_onfoot_model.p3d",       "character5" },
};

static const char* kModeLabels[] =
{
    "LOCAL MODE",
    "ONLINE LOBBY",
    "OPTIONS",
};
static_assert(sizeof(kModeLabels) / sizeof(kModeLabels[0])
              == static_cast<size_t>(GameMode::Count),
              "kModeLabels must have one entry per GameMode");

// ---------------------------------------------------------------------------
//  Constructor
// ---------------------------------------------------------------------------

MenuState::MenuState(FrontendRenderer&     renderer,
                     Renderer::GLRenderer& glRenderer,
                     const std::string&    gameDataRoot)
    : m_renderer    (renderer)
    , m_glRenderer  (glRenderer)
    , m_gameDataRoot(gameDataRoot)
{
    m_characters = kDefaultRoster;
    m_loaderCache.resize(m_characters.size());
}

// ---------------------------------------------------------------------------
//  Update
// ---------------------------------------------------------------------------

void MenuState::Update(float dt)
{
    if (!m_active) return;

    switch (m_screen)
    {
        case Screen::StartScreen:        UpdateStartScreen(dt);   break;
        case Screen::ModeSelection:      UpdateModeSelection(dt); break;
        case Screen::CharacterSelection: UpdateCharSelection(dt); break;
    }
}

void MenuState::UpdateStartScreen(float dt)
{
    m_blinkTimer += dt;
    if (m_blinkTimer >= kBlinkPeriod)
    {
        m_blinkTimer  -= kBlinkPeriod;
        m_blinkVisible = !m_blinkVisible;
    }
}

void MenuState::UpdateModeSelection(float /*dt*/) {}

void MenuState::UpdateCharSelection(float dt)
{
    m_modelRotY += kModelRotSpeed * dt;
    if (m_modelRotY > 6.2831853f) m_modelRotY -= 6.2831853f; // wrap at 2π
}

// ---------------------------------------------------------------------------
//  Render
// ---------------------------------------------------------------------------

void MenuState::Render()
{
    if (!m_active) return;

    switch (m_screen)
    {
        case Screen::StartScreen:        RenderStartScreen();   break;
        case Screen::ModeSelection:      RenderModeSelection(); break;
        case Screen::CharacterSelection: RenderCharSelection(); break;
    }
}

void MenuState::RenderStartScreen()
{
    m_renderer.DrawSolidRect(0.0f, 0.0f, 1.0f, 1.0f, 0x111820FFu);
    m_renderer.DrawSolidRect(0.14f, 0.18f, 0.72f, 0.22f, 0x243142E0u);

    m_renderer.DrawText(
        "myriad_fe",
        "CTTR ONLINE",
        0.31f, 0.25f,
        1.45f, 0xFFFFFFFFu);

    if (m_blinkVisible)
    {
        m_renderer.DrawText(
            "myriad_fe",
            "PRESS START / ENTER",
            0.31f, 0.72f,
            1.0f, 0xFFD700FFu);
    }
}

void MenuState::RenderModeSelection()
{
    m_renderer.DrawSolidRect(0.0f, 0.0f, 1.0f, 1.0f, 0x10151EFFu);
    m_renderer.DrawSolidRect(0.20f, 0.30f, 0.42f, 0.34f, 0x1B2635E8u);
    m_renderer.DrawSolidRect(0.215f, 0.325f + m_modeIndex * 0.09f,
                             0.39f, 0.065f, 0x445D7AFFu);

    const float kStartY   = 0.38f;
    const float kItemStep = 0.09f;

    for (int i = 0; i < kModeCount; ++i)
    {
        bool     sel    = (i == m_modeIndex);
        uint32_t colour = sel ? 0xFFD700FFu : 0xC8C8C8FFu;
        float    scale  = sel ? 1.15f : 1.0f;

        m_renderer.DrawText(
            "myriad_fe",
            kModeLabels[i],
            0.28f, kStartY + i * kItemStep,
            scale, colour);
    }

    // Cursor
    m_renderer.DrawText(
        "myriad_fe", ">",
        0.22f, kStartY + m_modeIndex * kItemStep,
        1.15f, 0xFFD700FFu);
}

void MenuState::RenderCharSelection()
{
    m_renderer.DrawSolidRect(0.0f, 0.0f, 1.0f, 1.0f, 0x10151EFFu);
    m_renderer.DrawSolidRect(0.10f, 0.57f, 0.80f, 0.21f, 0x1B2635E8u);
    m_renderer.DrawSolidRect(0.18f, 0.17f, 0.42f, 0.38f, 0x0B0F16FFu);

    const int count = static_cast<int>(m_characters.size());

    if (m_charIndex >= 0 && m_charIndex < count)
    {
        // Character name
        m_renderer.DrawText(
            "myriad_fe",
            m_characters[m_charIndex].name,
            0.35f, 0.12f,
            1.2f, 0xFFD700FFu);

        // Character grid. The exported PS2 image decode is still being
        // refined, so this GL-native shell keeps the menu usable.
        for (int i = 0; i < count; ++i)
        {
            const float x = 0.16f + i * 0.18f;
            const float y = 0.64f;
            m_renderer.DrawSolidRect(x, y, 0.14f, 0.09f,
                                     i == m_charIndex ? 0xEF7114FFu : 0x263448FFu);
            m_renderer.DrawSolidRect(x + 0.01f, y + 0.01f, 0.12f, 0.07f,
                                     0x10151EFFu);
            m_renderer.DrawText("myriad_fe", m_characters[i].name,
                                x + 0.018f, y + 0.035f,
                                0.62f, 0xFFFFFFFFu);
        }

        // 3-D model — only when the loader has been initialised and has
        // valid meshes with GPU buffers (VAO != 0).
        Renderer::P3DLoader* loader = m_loaderCache[m_charIndex].get();
        if (loader && !loader->GetMeshes().empty()
            && loader->GetMeshes()[0].vao != 0)
        {
            m_renderer.DrawCharacterViewport(
                kCharViewportName,
                *loader,
                m_modelRotY);
        }
    }

    m_renderer.DrawText("myriad_fe", "< >",    0.44f, 0.88f, 0.85f, 0xAAAAAAAAu);
    m_renderer.DrawText("myriad_fe", "ENTER",  0.44f, 0.93f, 0.85f, 0xAAAAAAAAu);
}

// ---------------------------------------------------------------------------
//  Input
// ---------------------------------------------------------------------------

void MenuState::OnKeyInput(int key, int action)
{
    if (action == MenuAction::Release) return;

    switch (m_screen)
    {
        // ------------------------------------------------------------------
        case Screen::StartScreen:
            if (key == MenuKey::Enter || key == MenuKey::Space)
                TransitionTo(Screen::ModeSelection);
            break;

        // ------------------------------------------------------------------
        case Screen::ModeSelection:
            if (key == MenuKey::Up)
                m_modeIndex = (m_modeIndex - 1 + kModeCount) % kModeCount;
            else if (key == MenuKey::Down)
                m_modeIndex = (m_modeIndex + 1) % kModeCount;
            else if (key == MenuKey::Enter && GetSelectedMode() == GameMode::OnlineLobby)
                TransitionTo(Screen::CharacterSelection);
            else if (key == MenuKey::Escape)
                TransitionTo(Screen::StartScreen);
            break;

        // ------------------------------------------------------------------
        case Screen::CharacterSelection:
        {
            const int count = static_cast<int>(m_characters.size());

            if (key == MenuKey::Up || key == MenuKey::Left ||
                key == MenuKey::Down || key == MenuKey::Right)
            {
                int delta   = (key == MenuKey::Down || key == MenuKey::Right) ? 1 : -1;
                m_charIndex = (m_charIndex + delta + count) % count;
                m_modelRotY = 0.0f;
                EnsureCharacterModel(m_charIndex);
            }
            else if (key == MenuKey::Enter)
            {
                m_confirmedCharacterIndex = m_charIndex;
                std::cout << "[MenuState] Selected character: "
                          << m_characters[m_charIndex].name
                          << "  Mode: " << kModeLabels[m_modeIndex] << std::endl;
                Deactivate();
            }
            else if (key == MenuKey::Escape)
            {
                TransitionTo(Screen::ModeSelection);
            }
            break;
        }
    }
}

// ---------------------------------------------------------------------------
//  Transitions
// ---------------------------------------------------------------------------

void MenuState::TransitionTo(Screen next)
{
    std::cout << "[MenuState] Screen transition: "
              << static_cast<int>(m_screen) << " -> "
              << static_cast<int>(next) << std::endl;

    m_screen = next;

    switch (next)
    {
        case Screen::StartScreen:
            m_blinkTimer   = 0.0f;
            m_blinkVisible = true;
            break;
        case Screen::ModeSelection:
            m_modeIndex = 0;
            break;
        case Screen::CharacterSelection:
            m_charIndex = 0;
            m_modelRotY = 0.0f;
            EnsureCharacterModel(0);
            break;
    }
}

// ---------------------------------------------------------------------------
//  Model loading
//
//  Uses CTTR::Renderer::P3DLoader exactly as main.cpp does — LoadModel() then
//  GLRenderer::BufferMesh() for each returned mesh so the VAOs are GPU-resident
//  before DrawCharacterViewport tries to bind them.
// ---------------------------------------------------------------------------

void MenuState::EnsureCharacterModel(int index)
{
    if (index < 0 || index >= static_cast<int>(m_loaderCache.size())) return;
    if (m_loaderCache[index]) return; // already loaded

    const std::string fullPath =
        m_gameDataRoot + "/" + m_characters[index].modelPath;

    std::cout << "[MenuState] Loading character model: " << fullPath << std::endl;

    auto loader = std::make_unique<Renderer::P3DLoader>();
    loader->LoadModel(fullPath); // falls back to procedural mesh on failure

    // Buffer every mesh to the GPU — same pattern as main.cpp.
    for (auto& mesh : loader->GetMeshes())
    {
        m_glRenderer.BufferMesh(mesh);
    }

    m_loaderCache[index] = std::move(loader);
}

const CharacterEntry* MenuState::GetConfirmedCharacter() const
{
    if (m_confirmedCharacterIndex < 0 ||
        m_confirmedCharacterIndex >= static_cast<int>(m_characters.size()))
    {
        return nullptr;
    }
    return &m_characters[m_confirmedCharacterIndex];
}

} // namespace Frontend
} // namespace CTTR
