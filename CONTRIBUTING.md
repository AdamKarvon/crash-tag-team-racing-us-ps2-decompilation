# Contributing

Thanks for helping with CTTR Online. Keep changes focused, describe how they
were verified, and avoid committing generated files or game data.

## Project layout

| Path | Purpose |
| --- | --- |
| `src/` | Runtime code, grouped by asset, frontend, game, networking, parsing, physics, and rendering. |
| `tools/` | Asset extraction, inspection, and UI export utilities. |
| `verification/` | Standalone parser, asset-pipeline, and determinism checks. |
| `docs/` | Architecture and workflow guides. |
| `external/` | Dependencies directly used by the C++ build. |
| `assets/` | Local RCF extraction inputs, outputs, and registry. |
| `game_data/` | Local game files used to run and verify the project. |
| `scratch/` | Local experiments and out-of-tree CMake build directories. |
| `NetP3DLib-main/` | Bundled third-party Pure3D library used by the UI exporter; see its license and [third-party notices](THIRD_PARTY_NOTICES.md). |

Local game data, extracted assets, build output, and investigation notes are
intentionally excluded from the repository.

## Build and verify

Requirements: CMake 3.20+, a C++17 compiler, and Git. Python 3.8+ and
`pycdlib` are only needed for ISO extraction tools.

Configure and build with the Ninja preset:

```powershell
cmake --preset ninja
cmake --build --preset ninja
```

The preset writes build output to `scratch/cttr_build_ninja`. Run the focused
verification executables from the repository root:

```powershell
.\scratch\cttr_build_ninja\test_asset_pipeline.exe
.\scratch\cttr_build_ninja\test_determinism.exe
```

The asset-pipeline checks require the relevant files under `game_data/`; the
determinism check does not. If a MinGW-built executable cannot find its runtime
DLLs, add the matching MinGW `bin` directory to `PATH` before running it.

To use the runtime or asset tools, provide your own legally obtained game data
locally. Do not commit disc images, extracted retail assets, generated exports,
or local build output. The root `.gitignore` excludes the standard local data
and build paths.

## Changes and pull requests

- Keep parser, renderer, physics, and networking behavior changes covered by
  the relevant verification executable where practical.
- For asset-decoding or rendering changes, include reproducible input details
  and verification evidence; do not add copyrighted asset files.
- Update the relevant guide when changing a documented workflow or format.
- In pull requests, summarize the change, list validation commands and
  outcomes, and call out any known limitations.
