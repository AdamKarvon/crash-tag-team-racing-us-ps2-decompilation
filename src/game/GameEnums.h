#pragma once
#include <cstdint>

// =============================================================================
//  GameEnums.h — Canonical CTTR Enums (sourced from cttr-main / G9RE7D)
// =============================================================================

namespace CTTR {

enum EMiniGameType {
    EMiniGameType_TARGETS,
    EMiniGameType_SLINGSHOT,
    EMiniGameType_BOWLING,
};

enum ExplosionDirection {
    ExplosionDirection_Up,
    ExplosionDirection_Down,
    ExplosionDirection_AllDirections,
};

enum OnFootEffect {
    OnFootEffect_Collection,
    OnFootEffect_Breakable0,
    OnFootEffect_Impact0,
    OnFootEffect_Impact1,
    OnFootEffect_Impact2,
};

enum TypeValues {
    TypeRace,
    TypeBattle,
    TypeStunt,
    TypeMinigame,
    TypeOnFootMinigame,
    TypeAny,
};

enum DrivingValues {
    DrivingRace,
    DrivingKamikaze,
    DrivingRailShooter,
    DrivingRollingThunder,
    DrivingTimeTrial,
};

enum ThemeValues {
    ThemeAdventure,
    ThemeFairy,
    ThemeDino,
    ThemeEgypt,
    ThemeSolar,
    ThemeMidway,
};

enum SaveMode {
    SaveMode_Load,
    SaveMode_Save,
    SaveMode_Create,
};

enum ModeControlState {
    ModeControl_WaitingForStart,
    ModeControl_Intro,
    ModeControl_PreCountdown,
    ModeControl_CountdownToStart,
    ModeControl_Playing,
    ModeControl_WaitingForFinish,
    ModeControl_Finished,
};

enum ModeType {
    Mode_Race,
    Mode_Battle,
    Mode_Stunt,
    Mode_Platform,
};

enum Skill {
    Easiest,
    Easy,
    Medium,
    Hard,
    Multiplayer,
};

enum TurboIncreaseEvent {
    TurboIncrease_Powerslide_Stage_01,
    TurboIncrease_Powerslide_Stage_02,
    TurboIncrease_Powerslide_Stage_03,
    TurboIncrease_GoodLanding,
    TurboIncrease_Kill_Unclashed,
    TurboIncrease_Kill_Double,
    TurboIncrease_Kill_Rampage,
    TurboIncrease_Kill_Clashed,
    TurboIncrease_Shoot_Stateprop,
};

enum PresentationSound {
    PresentationSound_Back,
    PresentationSound_ControllerInput,
    PresentationSound_Invalid,
    PresentationSound_ScreenChange,
    PresentationSound_Select,
    PresentationSound_SelectVO,
    PresentationSound_SelectMusic,
    PresentationSound_Load,
    PresentationSound_Bell,
    PresentationSound_Crystal,
    PresentationSound_KO,
    PresentationSound_Vibration,
};

enum PresentationLanguage {
    PresentationLanguage_English,
    PresentationLanguage_French,
    PresentationLanguage_Italian,
    PresentationLanguage_German,
    PresentationLanguage_Spanish,
    PresentationLanguage_Dutch,
    PresentationLanguage_Japanese,
};

enum GameCollisionType : uint32_t {
    GameCollisionType_Unknown00000001           = 1,
    GameCollisionType_Ground                    = 2,
    GameCollisionType_CarChassis                = 4,
    GameCollisionType_CarUndercarriage          = 8,
    GameCollisionType_WeaponAndPowerUp          = 0x10,
    GameCollisionType_WeaponAndPowerUpContainer = 0x20,
    GameCollisionType_Fence                     = 0x40,
    GameCollisionType_SpeedBoost                = 0x80,
    GameCollisionType_Unknown00000100           = 0x100,
    GameCollisionType_Edge                      = 0x200,
    GameCollisionType_Warp                      = 0x400,
    GameCollisionType_StateProp                 = 0x800,
    GameCollisionType_Unknown00001000           = 0x1000,
    GameCollisionType_StickyCharge              = 0x2000,
    GameCollisionType_GuidedLandMissile         = 0x4000,
    GameCollisionType_ResetVolume               = 0x8000,
    GameCollisionType_TrackingMissile           = 0x10000,
    GameCollisionType_Unknown00020000           = 0x20000,
    GameCollisionType_Unknown00040000           = 0x40000,
    GameCollisionType_BowlingBallAmmo           = 0x80000,
    GameCollisionType_Ammo                      = 0x100000,
};

enum SurfaceTypeEnum {
    SURFACE_DEFAULT,
    SURFACE_ASPHALT,
    SURFACE_GRASS,
    SURFACE_WATER,
    SURFACE_WATER_SHOULDER,
    SURFACE_MUD,
    SURFACE_MUD_SHOULDER,
    SURFACE_SAND,
    SURFACE_SAND_SHOULDER,
    SURFACE_GRAVEL,
    SURFACE_GRAVEL_SHOULDER,
    SURFACE_DIRT,
    SURFACE_DIRT_SHOULDER,
    SURFACE_METALGRATE,
    SURFACE_METALGRATE_SHOULDER,
    SURFACE_WOOD,
    SURFACE_WOOD_SHOULDER,
    SURFACE_BRICK,
    SURFACE_BRICK_SHOULDER,
    SURFACE_GRASS_SHOULDER,
    SURFACE_ICE,
    SURFACE_ICE_SHOULDER,
    SURFACE_SNOW,
    SURFACE_SNOW_SHOULDER,
    SURFACE_INVALID,
};

} // namespace CTTR
