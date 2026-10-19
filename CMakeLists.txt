cmake_minimum_required(VERSION 3.20)

project(CTTROnline LANGUAGES C CXX)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

include(FetchContent)

set(GLFW_BUILD_EXAMPLES OFF CACHE BOOL "Build GLFW examples" FORCE)
set(GLFW_BUILD_TESTS OFF CACHE BOOL "Build GLFW tests" FORCE)
set(GLFW_BUILD_DOCS OFF CACHE BOOL "Build GLFW documentation" FORCE)
set(GLFW_INSTALL OFF CACHE BOOL "Install GLFW" FORCE)

FetchContent_Declare(
    glfw
    GIT_REPOSITORY https://github.com/glfw/glfw.git
    GIT_TAG 3.3.10
)
FetchContent_MakeAvailable(glfw)

add_executable(cttr_online
    src/main.cpp
    src/network/LanGameSync.cpp
    src/parser/P3DParser.cpp
    src/physics/Vehicle.cpp
    src/renderer/DebugDraw.cpp
    src/renderer/GLRenderer.cpp
    src/renderer/FrontendRenderer.cpp
    src/renderer/P3DLoader.cpp
    src/game/MenuState.cpp
    src/game/LevelData.cpp
    src/game/World.cpp
    src/game/TrackSpline.cpp
    src/game/RaceController.cpp
    src/ai/AIDriver.cpp
    src/asset/god_parser.cpp
    src/asset/object_registry.cpp
    src/asset/level_loader.cpp
    src/frontend/FrontendRenderer.cpp
    src/frontend/MenuState.cpp
    external/glad/src/glad.c
)

target_include_directories(cttr_online PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
    ${CMAKE_CURRENT_SOURCE_DIR}/external/glad/include
)

FetchContent_Declare(
    nlohmann_json
    GIT_REPOSITORY https://github.com/nlohmann/json.git
    GIT_TAG        v3.11.3
)
FetchContent_MakeAvailable(nlohmann_json)

target_link_libraries(cttr_online PRIVATE glfw nlohmann_json::nlohmann_json)

find_package(glm CONFIG QUIET)
if(glm_FOUND)
    target_link_libraries(cttr_online PRIVATE glm::glm)
else()
    find_path(GLM_INCLUDE_DIR glm/glm.hpp)
    if(GLM_INCLUDE_DIR)
        target_include_directories(cttr_online PRIVATE ${GLM_INCLUDE_DIR})
    else()
        message(WARNING "glm was not found. Install glm or provide GLM_INCLUDE_DIR before building.")
    endif()
endif()



add_executable(test_asset_pipeline
    verification/test_asset_pipeline.cpp
    src/parser/P3DParser.cpp
    src/asset/god_parser.cpp
    src/asset/object_registry.cpp
    src/asset/level_loader.cpp
)

target_include_directories(test_asset_pipeline PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
)

if(glm_FOUND)
    target_link_libraries(test_asset_pipeline PRIVATE glm::glm)
else()
    if(GLM_INCLUDE_DIR)
        target_include_directories(test_asset_pipeline PRIVATE ${GLM_INCLUDE_DIR})
    endif()
endif()

add_executable(test_determinism
    verification/test_determinism.cpp
    src/physics/Vehicle.cpp
)

target_include_directories(test_determinism PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
)

if(glm_FOUND)
    target_link_libraries(test_determinism PRIVATE glm::glm)
else()
    if(GLM_INCLUDE_DIR)
        target_include_directories(test_determinism PRIVATE ${GLM_INCLUDE_DIR})
    endif()
endif()

add_executable(test_track_spline
    verification/test_track_spline.cpp
    src/game/TrackSpline.cpp
    src/game/LevelData.cpp
    src/asset/god_parser.cpp
)

target_include_directories(test_track_spline PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
)

if(glm_FOUND)
    target_link_libraries(test_track_spline PRIVATE glm::glm)
else()
    if(GLM_INCLUDE_DIR)
        target_include_directories(test_track_spline PRIVATE ${GLM_INCLUDE_DIR})
    endif()
endif()

add_executable(test_race_controller
    verification/test_race_controller.cpp
    src/game/RaceController.cpp
    src/game/TrackSpline.cpp
    src/game/LevelData.cpp
    src/asset/god_parser.cpp
)

target_include_directories(test_race_controller PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
)

if(glm_FOUND)
    target_link_libraries(test_race_controller PRIVATE glm::glm)
else()
    if(GLM_INCLUDE_DIR)
        target_include_directories(test_race_controller PRIVATE ${GLM_INCLUDE_DIR})
    endif()
endif()

add_executable(test_ai_opponents
    verification/test_ai_opponents.cpp
    src/ai/AIDriver.cpp
    src/physics/Vehicle.cpp
    src/game/TrackSpline.cpp
    src/game/RaceController.cpp
    src/game/LevelData.cpp
    src/asset/god_parser.cpp
)

target_include_directories(test_ai_opponents PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
)

if(glm_FOUND)
    target_link_libraries(test_ai_opponents PRIVATE glm::glm)
else()
    if(GLM_INCLUDE_DIR)
        target_include_directories(test_ai_opponents PRIVATE ${GLM_INCLUDE_DIR})
    endif()
endif()

add_executable(inspect_underground_meshes
    verification/inspect_underground_meshes.cpp
    src/renderer/P3DLoader.cpp
    src/parser/P3DParser.cpp
    src/game/LevelData.cpp
    src/asset/god_parser.cpp
)

target_include_directories(inspect_underground_meshes PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
    ${CMAKE_CURRENT_SOURCE_DIR}/external/glad/include
)

if(glm_FOUND)
    target_link_libraries(inspect_underground_meshes PRIVATE glm::glm)
else()
    if(GLM_INCLUDE_DIR)
        target_include_directories(inspect_underground_meshes PRIVATE ${GLM_INCLUDE_DIR})
    endif()
endif()



