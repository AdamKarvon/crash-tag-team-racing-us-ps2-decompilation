#include "level_loader.h"
#include <iostream>
#include <sstream>
#include <algorithm>

namespace CTTR {
namespace Asset {

    void LevelLoader::LogStep(int stepNum, const std::string& desc, const std::string& loadSet, const std::string& asset, bool success) {
        m_executionLog.push_back({stepNum, desc, loadSet, asset, success});
        std::cout << "[Step " << stepNum << "/37] " << desc;
        if (!loadSet.empty()) std::cout << " -> LoadSet: " << loadSet;
        if (!asset.empty()) std::cout << " (" << asset << ")";
        std::cout << (success ? " [OK]" : " [FAILED]") << std::endl;
    }

    bool LevelLoader::InitializeLevel(const LevelLoadConfig& config) {
        m_config = config;
        m_currentLevel = config.levelName;
        m_executionLog.clear();

        auto& reg = ObjectRegistry::Get();

        // 1. Issue memory monitor flag "driving"
        LogStep(1, "Issue memory monitor flag 'driving'");

        // 2. Silence intro audio (SoundManager.DuckIntro(0.0))
        LogStep(2, "SoundManager.DuckIntro(0.0)");

        // 3. Extract theme from level name: theme = level.gsub("%d+", "")
        m_currentTheme = reg.ExtractTheme(config.levelName);
        LogStep(3, "Extract theme from level name: '" + config.levelName + "' -> '" + m_currentTheme + "'");

        // 4. LoadCommon and LoadDriving
        reg.LoadObjects("design/startup.god", "PermanentCommon", "Script");
        reg.LoadObjects("design/levels/common/common.god", "CommonObjects", "Script");
        LogStep(4, "Load permanent and driving shared sets (LoadCommon, LoadDriving)");

        // 5. Load HUD mini-map: art/frontend/resources/maps/[level].p3d -> "hudmap"
        std::string mapPath = "art/frontend/resources/maps/" + config.levelName + ".p3d";
        reg.LoadObjects(mapPath, "hudmap", "Frontend");
        LogStep(5, "Load HUD mini-map", "hudmap", mapPath);

        // 6. Load HUD 3D objects: art/frontend/resources/3Dobjects/3Dobjects.p3d -> "hud3Dobjects"
        std::string hud3DPath = "art/frontend/resources/3Dobjects/3Dobjects.p3d";
        reg.LoadObjects(hud3DPath, "hud3Dobjects", "Frontend");
        LogStep(6, "Load HUD 3D objects", "hud3Dobjects", hud3DPath);

        // 7. Load Vehicle Common: art/cars/VehicleCommon.p3d -> "VehicleCommonLoadSet"
        std::string vehCommonPath = "art/cars/VehicleCommon.p3d";
        reg.LoadObjects(vehCommonPath, "VehicleCommonLoadSet", "Vehicles");
        LogStep(7, "Load Vehicle Common", "VehicleCommonLoadSet", vehCommonPath);

        // 8. Load region-aware frontend art -> "IngameProject"
        std::string feArt = "art/frontend/driving.p3d";
        if (config.isJapanese) feArt = "art/frontend/driving_japan.p3d";
        else if (config.isPAL) feArt = "art/frontend/driving_pal.p3d";
        reg.LoadObjects(feArt, "IngameProject", "Frontend");
        LogStep(8, "Load region-aware frontend art", "IngameProject", feArt);

        // 9. Load PAL countdown sprites if applicable
        if (config.isPAL) {
            std::string cdPath = "art/frontend/resources/countdown_pal_" + config.language + ".p3d";
            reg.LoadObjects(cdPath, "countdown", "Frontend");
            LogStep(9, "Load PAL countdown sprites", "countdown", cdPath);
        } else {
            LogStep(9, "PAL countdown skipped (NTSC mode)");
        }

        // 10. Load AI tunables and funparams by skill level
        std::string skillLower = config.skillLevel;
        std::transform(skillLower.begin(), skillLower.end(), skillLower.begin(), ::tolower);
        std::string aiTunables = "design/levels/common/aitunables/aitunables_" + skillLower + ".god";
        std::string funParams = "design/levels/common/funcontrol/funparams_" + skillLower + ".god";
        reg.LoadObjects(aiTunables, "AITunables", "Ai");
        reg.LoadObjects(funParams, "FunParams", "Ai");
        LogStep(10, "Load AI tunables and FunControl motifs (" + config.skillLevel + ")", "AITunables", aiTunables);

        // 11. Load car lights: art/cars/lights/car_lights.p3d -> "Lights"
        std::string lightsPath = "art/cars/lights/car_lights.p3d";
        reg.LoadObjects(lightsPath, "Lights", "Vehicles");
        LogStep(11, "Load vehicle lights and set LightGroup", "Lights", lightsPath);

        // 12. Load common objects: design/levels/common/driving_objects.god -> "CommonObjects"
        std::string commonObjPath = "design/levels/common/driving_objects.god";
        reg.LoadObjects(commonObjPath, "CommonObjects", "Script");
        LogStep(12, "Load driving shared object pool", "CommonObjects", commonObjPath);

        // 13. Load common animations: art/animation/fighttrees/turret.fig -> "CommonAnimations"
        std::string animPath = "art/animation/fighttrees/turret.fig";
        reg.LoadObjects(animPath, "CommonAnimations", "Fight");
        LogStep(13, "Load turret fight animations", "CommonAnimations", animPath);

        // 14. Set parade lap cameras: ModeManager.GetActiveControl().SetParadeLapCameras("Cameras/ParadeLapCameraArray")
        LogStep(14, "Configure parade lap cameras ('Cameras/ParadeLapCameraArray')");

        // 15. Load music: sound/scripts/music/ingame/[level].p3d -> "Music"
        std::string musicPath = "sound/scripts/music/ingame/" + config.levelName + ".p3d";
        reg.LoadObjects(musicPath, "Music", "Sounds");
        LogStep(15, "Load level music script", "Music", musicPath);

        // 16. Load level SFX if exists: sound/scripts/sfx/racing/[level].p3d -> "LevelSFX"
        std::string sfxPath = "sound/scripts/sfx/racing/" + config.levelName + ".p3d";
        reg.LoadObjects(sfxPath, "LevelSFX", "Sounds");
        LogStep(16, "Load level SFX script", "LevelSFX", sfxPath);

        // 17. Determine track variant and load track: art/levels/[level]/track_[lang].p3d -> "Track"
        std::string trackPath = "art/levels/" + config.levelName + "/track_" + config.language + ".p3d";
        reg.LoadObjects(trackPath, "Track", "World");
        LogStep(17, "Load track geometry", "Track", trackPath);

        // 18. Load props: art/levels/[level]/props.p3d -> "PropArt"
        std::string propsArtPath = "art/levels/" + config.levelName + "/props.p3d";
        reg.LoadObjects(propsArtPath, "PropArt", "Props");
        LogStep(18, "Load props model library", "PropArt", propsArtPath);

        // 19. Load sounds: design/levels/[level]/sounds.god -> "Sounds"
        std::string soundsPath = "design/levels/" + config.levelName + "/sounds.god";
        reg.LoadObjects(soundsPath, "Sounds", "Sounds");
        LogStep(19, "Load level sounds configuration", "Sounds", soundsPath);

        // 20. Load fences: design/levels/[level]/fences.p3d -> "Fences"
        std::string fencesPath = "design/levels/" + config.levelName + "/fences.p3d";
        reg.LoadObjects(fencesPath, "Fences", "World");
        LogStep(20, "Load track fences and boundaries", "Fences", fencesPath);

        // 21. Load weapons: design/levels/[level]/weapons.god -> "WeaponMotif"
        std::string weaponsPath = "design/levels/" + config.levelName + "/weapons.god";
        reg.LoadObjects(weaponsPath, "WeaponMotif", "Weapons");
        LogStep(21, "Load weapons configuration", "WeaponMotif", weaponsPath);

        // 22. Load props common: design/levels/[level]/props_common.god -> "CommonProps"
        std::string propsCommonPath = "design/levels/" + config.levelName + "/props_common.god";
        reg.LoadObjects(propsCommonPath, "CommonProps", "Props");
        LogStep(22, "Load common props placement", "CommonProps", propsCommonPath);

        // 23. Load props: design/levels/[level]/props.god -> "Props"
        std::string propsPath = "design/levels/" + config.levelName + "/props.god";
        reg.LoadObjects(propsPath, "Props", "Props");
        LogStep(23, "Load level props placement", "Props", propsPath);

        // 24. Load objects: design/levels/[level]/objects.god -> "Design"
        std::string objectsPath = "design/levels/" + config.levelName + "/objects.god";
        reg.LoadObjects(objectsPath, "Design", "Script");
        LogStep(24, "Load level objects & grid spawns", "Design", objectsPath);

        // 25. Load roads: design/levels/[level]/roads.mid -> "Roads"
        std::string roadsPath = "design/levels/" + config.levelName + "/roads.mid";
        reg.LoadObjects(roadsPath, "Roads", "Roads");
        LogStep(25, "Load roads network spline (.mid)", "Roads", roadsPath);

        // 26. Load animated cameras if available
        std::string animCams = "art/levels/" + config.levelName + "/animatedcams.p3d";
        reg.LoadObjects(animCams, "AnimatedCameras", "Animation");
        LogStep(26, "Load animated cameras", "AnimatedCameras", animCams);

        // 27. Load clash cameras: art/animation/clashcameraA.p3d -> "ClashCameras"
        std::string clashCamPath = "art/animation/clashcameraA.p3d";
        reg.LoadObjects(clashCamPath, "ClashCameras", "Animation");
        LogStep(27, "Load clash camera system", "ClashCameras", clashCamPath);

        // 28. AvatarManager.CreateAvatars()
        LogStep(28, "AvatarManager.CreateAvatars()");

        // 29. Load 3 Uber vehicle tiers (uber1..3 base and middle)
        for (int tier = 1; tier <= 3; ++tier) {
            std::string uName = "uber" + std::to_string(tier);
            std::string uBase = "art/cars/" + uName + "/" + uName + "_base.p3d";
            std::string uMid = "art/cars/" + uName + "/" + uName + "_middle.p3d";
            reg.LoadObjects(uBase, uName + "_base", "Vehicles");
            reg.LoadObjects(uMid, uName + "_middle", "Vehicles");
        }
        LogStep(29, "Load 3 Uber vehicle tiers (uber1..3 base and middle)");

        // 30. Load player vehicles and characters
        std::string pVeh = "art/cars/" + config.playerVehicle + "/" + config.playerVehicle + ".p3d";
        std::string pChar = "art/characters/" + config.playerCharacter + "/" + config.playerCharacter + ".p3d";
        reg.LoadObjects(pVeh, config.playerVehicle, "Vehicles");
        reg.LoadObjects(pChar, config.playerCharacter, "Characters");
        LogStep(30, "Load player vehicle & character (" + config.playerVehicle + ", " + config.playerCharacter + ")");

        // 31. Load AI characters and vehicles
        LogStep(31, "Load AI characters & vehicles (numAI=" + std::to_string(config.numAI) + ", theme=" + m_currentTheme + ")");

        // 32. Presentation sound & vehicle settings
        LogStep(32, "Configure PresentationManager and vehicle animation load sets");

        // 33. Collision manager and race control setup
        LogStep(33, "Setup GameCollisionManager and RaceControl with FunControlMotif");

        // 34. Assign grid spawn locations from Design/CarLocation0..7
        int foundLocations = 0;
        for (int i = 0; i < 8; ++i) {
            std::string locName = "CarLocation" + std::to_string(i);
            if (reg.ObjectExists(locName)) {
                foundLocations++;
            }
        }
        LogStep(34, "Assign grid spawn locations (Found " + std::to_string(foundLocations) + "/8 locations in registry)");

        // 35. Initialize avatars for driving
        LogStep(35, "Initialize avatars for driving (InitializeForDriving(), AddTrackedAvatar)");

        // 36. Apply per-level render settings (fog, culling, lighting)
        LogStep(36, "Apply per-level render & lighting settings");

        // 37. Fade in screen (PresentationManager.FadeScreenToColour)
        LogStep(37, "PresentationManager.FadeScreenToColour(0,0,0,255, 0,0, 1.0) - Fade in from black");

        m_isLoaded = true;
        return true;
    }

    bool LevelLoader::TerminateLevel() {
        if (!m_isLoaded) return false;

        auto& reg = ObjectRegistry::Get();
        // Unload load sets in reverse order
        std::vector<std::string> loadSets = {
            "ClashCameras", "AnimatedCameras", "LevelSFX", "countdown", "Music", 
            "Track", "PropArt", "Sounds", "Fences", "WeaponMotif", "Props", 
            "CommonProps", "Roads", "Design", "CommonObjects", "Lights", 
            "VehicleCommonLoadSet", "hud3Dobjects", "hudmap", "IngameProject", 
            "CommonAnimations", "AITunables", "FunParams"
        };

        for (const auto& set : loadSets) {
            reg.UnloadObjects(set);
        }

        m_isLoaded = false;
        return true;
    }

    bool LevelLoader::ResetLevel() {
        if (!m_isLoaded) return false;
        std::cout << "[LevelLoader] Resetting Level: " << m_currentLevel << std::endl;
        return true;
    }

} // namespace Asset
} // namespace CTTR
