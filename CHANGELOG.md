# Changelog

All notable changes to the CTTR Online project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-05-19
### Added
- **Core Engine Initialization**: Basic OpenGL renderer and `DebugDraw` implemented for visualizations.
- **P3D Parsing Engine**: Custom `P3DParser` capable of decoding Radical Entertainment's proprietary `.p3d` chunks.
  - Implemented `ParseMeshChunk`, `ParsePrimitiveGroupChunk` (supporting legacy and current formats).
  - Implemented Vertex data lists: `ParsePositionListChunk`, `ParseNormalListChunk`, `ParseUVListChunk`, `ParseIndexListChunk`.
  - Added support for proprietary `CHUNK_SKELETON_2` and `CHUNK_SKELETON_JOINT_2` decoding (including 4x4 Rest Pose Matrix extraction).
  - Added support for `CHUNK_LOCATOR` specifically for "clash" mounts and "weapon" mounts.
- **Networking Foundation**: Basic `LanServer` with 60Hz tick rate and simulated server logic.
- **Tools / Artifacts**: Tools for extracting P3D data and verifying pipeline correctness (`p3d_inspect.exe`, `test_parse.exe`).
- **Asset Pipeline**: Formatted the file system by renaming the extracted ISO folder to `game_data` and implemented command-line arguments in the main server logic for dynamic asset pathing.

### Changed
- Refined the vertex-to-index mapping for `CHUNK_PRIM_GROUP` to resolve VIF mesh topology extraction failures.
- Transitioned towards a deterministic C++ parser aligned with NetP3DLib's specifications.
- Updated the main engine loop to depend on the window lifecycle (`renderer.ShouldClose()`) rather than a hardcoded simulation tick limit.
- Overhauled the `P3DParser` exception handling by introducing a custom `P3DParseException` that provides specific chunk IDs and byte offsets for easier reverse-engineering debugging.
- Un-mocked the OpenGL `GLRenderer` to implement a real GLFW/GLAD window context, including dynamic GPU buffer generation (VAO, VBO, EBO) and real draw calls.

### Fixed
- Addressed zero index extraction bugs during VIF mesh parsing by properly handling index lists in `WalkPrimitiveGroupChildren`.
