# Crash Tag Team Racing (PS2 PAL `SLES_534.39`) — Decompilation & Reverse-Engineering Reference

## 1. Executive Summary & Binary Provenance

| Property | Value |
| :--- | :--- |
| **Source ISO** | `Crash Tag Team Racing (Europe, Australia) (En,Fr,De,Es,It,Nl).iso` |
| **Executable Path** | `assets/raw_iso/SLES_534.39` (ISO9660 LBA `353`) |
| **File Size** | `4,968,060` bytes (`4.74 MB`) |
| **SHA-256** | `8c0816196d16c76470f4443a49f24148873cfb68becb27ba25f461af8280eec2` |
| **Architecture** | MIPS-III / Sony-Toshiba R5900 (Emotion Engine), EABI64, little-endian (`e_machine = 8`, `e_flags = 0x20924001`) |
| **Entry Point** | `0x00100008` |
| **Global Pointer (`$gp`)** | `0x005c2ef0` (`.reginfo` `gprmask = 0xd00000fc`) |
| **Detected Procedures** | `14,866` stack-framed procedures in `.text` |
| **Defined Strings** | `12,771` null-terminated ASCII strings in `.data` and `.rodata` |

### REA MCP Evidence Ledger (`run_id: b38eabbf-883a-46d1-9ca5-fc5363fa0188`)
- `ev_5dd9df47c2d5da1541f3fae4b1e317c88c11d1aee9488fc34646f7b34037f7a2`: Architecture verification (`elf32-little`, MIPS R5900).
- `ev_55bdd52b88c35848752096e4e2aa0f5a8953a980c29d970b6a1168738c591300`: ELF file headers (`start address 0x00100008`, `flags 0x00000102`).
- `ev_82707f2b6fe5cf5b6383e02bc016f05e7d6b302351a938fb731901a69f4c4d3a`: ELF section header table.
- `ev_2422afc0c2e4ffd3f5d1a5dce219ab0ec3e4221aed752b2743682edb64a56edc`: Symbol table confirmation (stripped retail ELF; full GCC 2.95.2 RTTI `__tf...` and script reflection tables intact in `.rodata`).

### ELF Section Memory Map (`ev_82707f2b6fe5cf5b6383e02bc016f05e7d6b302351a938fb731901a69f4c4d3a`)

| Index | Section Name | VMA Start | VMA End | Size (Bytes) | File Offset | Description |
| :---: | :--- | :--- | :--- | :--- | :--- | :--- |
| `[ 1]` | `.text` | `0x00100000` | `0x00506380` | `0x00406380` (`4,219,776`) | `0x00001000` | R5900 executable code (Game, Pure3D, RavenPhysics, RadScript) |
| `[ 2]` | `.vutext` | `0x00506380` | `0x0050eb60` | `0x000087e0` (`34,784`) | `0x00407380` | PS2 Vector Unit (VU0/VU1) microprograms (`Pure3D` skinning/clipping) |
| `[ 3]` | `.data` | `0x0050eb80` | `0x00539ab8` | `0x0002af38` (`175,928`) | `0x0040fb80` | Initialized globals, float tables (e.g. `EngineMotif` torque curve @ `0x0050ffc0`) |
| `[ 4]` | `.rodata` | `0x00539b00` | `0x005bae1c` | `0x0008131c` (`529,180`) | `0x0043ab00` | Read-only strings, C++ vtables, GCC RTTI (`__tf`), and script PTMF descriptors |
| `[ 5]` | `.gcc_except_table` | `0x005bae80` | `0x005baedc` | `0x0000005c` (`92`) | `0x004bbe80` | GCC exception handling tables |
| `[ 8]` | `.sbss` | `0x005baf00` | `0x005bb398` | `0x00000498` (`1,176`) | `0x004bbf00` | `$gp`-relative small uninitialized data |
| `[ 9]` | `.bss` | `0x005bb400` | `0x0062392c` | `0x0006852c` (`427,308`) | `0x004bbf68` | Uninitialized globals, singletons, and script method registries |
| `[10]` | `.reginfo` | `0x00000000` | `0x00000018` | `0x00000018` (`24`) | `0x004bbf68` | `gprmask = 0xd00000fc`, `gp_value = 0x005c2ef0` |

---

## 2. Phase A: Pure3D & RavenPhysics Chunk Parser Verification

### 2.1 `tP3DFileHandler` & `tSimpleChunkHandler` Architecture
- **`tP3DFileHandler::AddHandler(tChunkHandler*, uint32_t chunkId)`**: `0x003bef28` (53 registration call sites).
- **`tSimpleChunkHandler` Memory Layout**:
  - `+0x00` (`0`): `void** vtable`
  - `+0x04` (`4`): `int32_t mRefCount`
  - `+0x08` (`8`): `uint32_t mChunkId` (primary Pure3D chunk ID handled by this loader)
  - `+0x0c` (`12`): `uint32_t mStatus`
- **`tSimpleChunkHandler` Vtable Layout** (GCC 2.95.2 8-byte vtable entries: `{int16 delta, int16 index, void* func}`):
  - `vtable + 0x04`: `__tf` RTTI type-info function
  - `vtable + 0x0c`: Virtual destructor (`~Loader`)
  - `vtable + 0x14`: `Load(tChunkFile*, tEntityStore*)` (`0x003bf908` base dispatcher)
  - `vtable + 0x1c`: `CheckChunkID(uint32_t)` (`0x003bfa58`: compares `this->mChunkId == chunkId`)
  - `vtable + 0x24`: `GetChunkID()` (`0x003bfd90`: returns `this->mChunkId` at `+0x08`)
  - `vtable + 0x2c`: `LoadObject(tChunkFile*, tEntityStore*)` (overridden by each concrete Pure3D loader)

### 2.2 Complete Decompiled Chunk Handler Table (`0x0031ed20`, `0x0040bdd0`, `0x0027deb8`)

| Chunk ID | `P3DParser.h` Constant | RTTI Class Name | Reg. Call VMA | Ctor VMA | Vtable VMA | `LoadObject` VMA | Size |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :---: |
| `0x00010000` | `CHUNK_MESH` | `pure3d::GeometryLoader` | `0x0031ee10` | `0x00314e40` | `0x0057fbd0` | `0x00314008` | `44` |
| `0x00010001` | `CHUNK_SKIN` | `pure3d::PolySkinLoader` | `0x0031ee4c` | `0x0032fca0` | `0x005820a0` | `0x0032f1c8` | `32` |
| `0x00019000` | `CHUNK_TEXTURE` | `pure3d::TextureLoader` | `0x0031ed68` | `0x00372a40` | `0x00587200` | `0x003720e8` | `28` |
| `0x00019001` | `CHUNK_IMAGE` | `pure3d::ImageLoader` | `0x0031ed48` | `0x00316678` | `0x0057fe58` | `0x00315df0` | `28` |
| `0x00019005` | `CHUNK_SPRITE` | `pure3d::SpriteLoader` | `0x0031f2f0` | `0x0036e648` | `0x00586d60` | `0x0036de00` | `40` |
| `0x00011000` | `CHUNK_SHADER` | `pure3d::ShaderLoader` | `0x0031edd0` | `0x003641c0` | `0x00585bd0` | `0x00363918` | `20` |
| `0x00004500` | `CHUNK_SKELETON` | `pure3d::SkeletonLoader` (v1) | `0x0031ee6c` | `0x0036b9e8` | `0x00586860` | `0x0036b198` | `20` |
| `0x00023000` | `CHUNK_SKELETON_2` | `pure3d::SkeletonLoader` (v2) | `0x0031ee8c` | `0x0036b9b0` | `0x00586860` | `0x0036b550` | `24` |
| `0x00014000` | `CHUNK_OLD_LOCATOR` | `pure3d::LocatorLoader` | `0x0031f250` | `0x0031f750` | `0x00580c20` | `0x0031f580` | `20` |
| `0x03000005` | `CHUNK_LOCATOR` | `LocatorChunkLoader` | `0x0027df34` | `0x001f4ee0` | `0x00559c60` | `0x001f4d30` | `16` |
| `0x00123000` | `CHUNK_COMPOSITE_DRAWABLE` | `pure3d::CompositeDrawableLoader` | `0x0031eeac` | `0x003001c8` | `0x0057d130` | `0x002ff0e8` | `16` |
| `0x00120100` | `CHUNK_SCENEGRAPH` | `pure3d::scenegraph::ScenegraphLoader` | `0x0031f108` | `0x003629f8` | `0x005858d0` | `0x0035f680` | `32` |
| `0x00121000` | `CHUNK_ANIMATION` | `pure3d::AnimationLoader` | `0x0031ef4c` | `0x002ece48` | `0x0057bb88` | `0x002eba78` | `20` |
| `0x00121100` | `CHUNK_CHANNEL` | `pure3d::ChannelLoader` | `0x0031ef6c` | `0x002fb3f8` | `0x0057cb60` | `0x002f9d98` | `12` |
| `0x00121201` | `CHUNK_FRAME_CONTROLLER` | `pure3d::FrameControllerLoader` | `0x0031f00c` | `0x003097d8` | `0x0057df08` | `0x00308ea8` | `16` |
| `0x00121301` | `CHUNK_ANIMATED_OBJECT_FACTORY` | `pure3d::AnimatedObjectFactoryLoader` | `0x0031efac` | `0x002eaec8` | `0x0057b7f8` | `0x002ea650` | `16` |
| `0x00121302` | `CHUNK_ANIMATED_OBJECT` | `pure3d::AnimatedObjectLoader` | `0x0031efcc` | `0x002eb218` | `0x0057b7b8` | `0x002eaaf0` | `16` |
| `0x00018000` | `CHUNK_FRONTEND_PROJECT` | `pure3d::frontend::ProjectLoader` | `0x0031f330` | `0x0030b948` | `0x0057e3f8` | `0x00309858` | `84` |
| `0x00018100` | `CHUNK_TEXT_BIBLE` | `pure3d::TextBibleLoader` | `0x0031f350` | `0x003708e0` | `0x00586fa0` | `0x00370410` | `24` |
| `0x00022000` | `CHUNK_TEXTURE_FONT` | `pure3d::TextureFontLoader` | `0x0031ed88` | `0x00371cb0` | `0x00587118` | `0x003711a8` | `16` |
| `0x00022001` | `CHUNK_IMAGE_FONT` | `pure3d::ImageFontLoader` | `0x0031eda8` | `0x00318238` | `0x00580138` | `0x00317978` | `16` |
| `0x00013000` | `CHUNK_LIGHT` | `pure3d::LightLoader` | `0x0031f210` | `0x0031e190` | `0x00580960` | `0x0031db28` | `16` |
| `0x00013006` | `CHUNK_LIGHT_GROUP` | `pure3d::LightGroupLoader` | `0x0031f230` | `0x0031ea08` | `0x005809e8` | `0x0031e7b8` | `16` |
| `0x00002200` | `CHUNK_CAMERA` | `pure3d::CameraLoader` | `0x0031f270` | `0x002f6f00` | `0x0057c4b8` | `0x002f6ab0` | `16` |
| `0x07010000` | `CHUNK_COLLISION_OBJECT` | `ravenphysics::CollisionObjectLoader` | `0x0040be30` | `0x0040fa00` | `0x005974f8` | `0x0040e7f8` | `16` |
| `0x07010025` | `CHUNK_WORLD_COLLISION_OBJECT` | `ravenphysics::WorldCollisionObjectLoader` | `0x0040be50` | `0x0040fa28` | `0x005974b8` | `0x0040f158` | `16` |
| `0x07011000` | `CHUNK_RIGID_BODY` | `ravenphysics::RigidBodyLoader` | `0x0040be10` | `0x0040aaa8` | `0x00597108` | `0x00409ab0` | `16` |
| `0x09000001` | `CHUNK_ROAD_NETWORK` | `RoadNetworkChunkLoader` | `0x0027df18` | `0x00257ef0` | `0x00567ef8` | `0x00257240` | `16` |
| `0x02680019` | `CHUNK_SOUND_DATA` | `SoundDataChunkLoader` | `0x002869a4` | `0x00286c60` | `0x0056ea80` | `0x00286a00` | `16` |

---

## 3. Phase B: Vehicle Physics & Driveline Decompilation

### 3.1 `CarPhysicsMotif` (`0x00173468` / `0x00173650` / `0x00174618..0x00174a70`)

| Offset | Type | Field Name | Getter / Setter VMA | PTMF VMA | Notes |
| :---: | :--- | :--- | :--- | :--- | :--- |
| `+0x00` | `void*` | `vtable` | — | — | `0x005495b0` |
| `+0x08` | `float` | `mCmYOffset` | `0x00174938` (`SetCmYOffset`) | `0x00549bb0` | Added to RigidBody CM `.y` in `Initialize` (`0x0017352c`) |
| `+0x0c` | `float` | `mCmZOffset` | `0x00174940` (`SetCmZOffset`) | `0x00549be8` | Added to RigidBody CM `.z` in `Initialize` (`0x00173530`) |
| `+0x10` | `Vector3` | `mBaseCenterOfMass` | `0x001734d4` | — | Cached from `RigidBody + 188` |
| `+0x1c` | `float` | `mDragCoeff` | `0x001735c8` | — | Precomputed in `Initialize`: $F_{\text{max}} / (v_{\max}(v_{\max} + 30))$ |
| `+0x20` | `float` | `mDragCoeffWithTurbo` | `0x00173638` | — | Precomputed in `Initialize`: $F_{\text{max,turbo}} / (v_{\text{turbo}}(v_{\text{turbo}} + 30))$ |
| `+0x2c` | `float` | `mMass` | `0x00174948` (`SetMass`) | `0x00549c10` | Vehicle mass in kg (`1000.0f` kart, `1500.0f` clashed) |
| `+0x34` | `CollisionObject*` | `mChassisCollisionObject` | `0x00173368` | `0x00549fa8` | Fetched via `FetchChassisCollisionObject` |
| `+0x3c` | `float` | `mPowerSlideBoost` | `0x00174950` (`SetPowerSlideBoost`) | `0x00549c48` | Powerslide boost factor |
| `+0x40` | `float` | `mPowerSlideKickout` | `0x00174958` (`SetPowerSlideKickout`) | `0x00549c80` | Powerslide yaw kickout impulse |
| `+0x44` | `float` | `mScootchImpulseMagnitude` | `0x00174960` (`SetScootchImpulseMagnitude`) | `0x00549cc0` | Scootch jump/shunt impulse magnitude |
| `+0x48` | `float` | `mScootchImpulseTime` | `0x00174968` (`SetScootchImpulseTime`) | `0x00549ce0` | Scootch impulse duration (s) |
| `+0x4c` | `float` | `mScootchMinTimeBetween` | `0x00174970` (`SetScootchMinTimeBetween`) | `0x00549d08` | Cooldown between scootches (s) |
| `+0x50` | `float` | `mScootchTurboUsage` | `0x00174978` (`SetScootchTurboUsage`) | `0x00549d28` | Turbo drained per scootch |
| `+0x54` | `Locator*[6]` | `mSuspensionLocators` | `0x00174a10` / `0x00174a20` | `0x00549790` | Wheel locator array |
| `+0x6c` | `float` | `mTopSpeedKph` | `0x00174980` (`SetTopSpeedKph`) | `0x00549d60` | Unboosted top speed in km/h |
| `+0x70` | `float` | `mTopSpeedKphWithTurbo` | `0x00174988` (`SetTopSpeedKphWithTurbo`) | `0x00549d80` | Turbo top speed in km/h |
| `+0x78` | `float` | `mTurboMeterCapacity` | `0x00174620` (`SetTurboMeterCapacity`) | `0x00549dc0` | Turbo capacity (`100.0f`) |
| `+0x7c` | `float` | `mTurboMeterMinimumLevelForUsage` | `0x00174628` | `0x00549e08` | Min turbo to activate (`50.0f`) |
| `+0x80` | `float` | `mTurboMeterRechargePerSecond` | `0x00174630` | `0x00549e50` | Passive turbo recharge rate |
| `+0x84` | `float` | `mTurboMeterUsagePerSecond` | `0x00174638` | `0x00549e90` | Turbo drain rate (`14.0f`/s) |
| `+0x8c` | `int32_t` | `mWheelCount` | `0x00174618` / `0x00174998` | `0x00549ec0` | Number of wheels (`5`: 4 physical + 1 virtual) |
| `+0x90` | `float` | `mWheelGripLossDueToGas` | `0x001749a0` (`SetWheelGripLossDueToGas`) | `0x00549f08` | Lateral grip reduction under throttle |
| `+0x98` | `float` | `mWheelSlipTendency` | `0x001749a8` (`SetWheelSlipTendency`) | `0x00549f48` | Slip threshold scaling during powerslide |
| `+0x9c` | `float` | `mWeebleAmount` | `0x00174990` (`SetWeebleAmount`) | `0x00549f60` | Roll righting torque scale |
| `+0xa4` | `BrakeMotif*` | `mBrakeMotif` | `0x001749b0` / `0x001749b8` | `0x005498b0` | Brake motif pointer |
| `+0xa8` | `DifferentialMotif*[2]` | `mDifferentialMotifs` | `0x001749c0` / `0x001749d0` | `0x00549910` | Differential motif pointers |
| `+0xac` | `EngineMotif*` | `mEngineMotif` | `0x001749e0` / `0x001749e8` | `0x00549950` | Engine motif pointer |
| `+0xb0` | `InAirControlMotif*` | `mInAirControlMotif` | `0x001749f0` / `0x001749f8` | `0x005499a0` | In-air control motif pointer |
| `+0xb4` | `SteeringWheelMotif*` | `mSteeringWheelMotif` | `0x00174a00` / `0x00174a08` | `0x005499f8` | Steering motif pointer |
| `+0xb8` | `SuspensionMotif*[6]` | `mSuspensionMotifs` | `0x00174a30` / `0x00174a40` | `0x00549a68` | Per-wheel suspension motif pointers |
| `+0xd0` | `TransmissionMotif*` | `mTransmissionMotif` | `0x00174a50` / `0x00174a58` | `0x00549ab8` | Transmission motif pointer |
| `+0xd4` | `WheelMotif*[6]` | `mWheelMotifs` | `0x00174a60` / `0x00174a70` | `0x00549b50` | Per-wheel motif pointers |

#### `CarPhysicsMotif::Initialize` (`0x00173468`) — Exact Top-Speed Drag Formula
In `0x00173558..0x00173638`, `CarPhysicsMotif::Initialize` derives the chassis aerodynamic drag coefficient directly from the driveline's top-gear wheel force and the scripted top speed:
$$v_{\max} = \text{mTopSpeedKph} \times 0.2777778 \quad (\text{constant } \mathtt{0x3e8e38e4} = \tfrac{1}{3.6})$$
$$F_{\text{drive},\max} = \frac{\tau_{\text{engine},\max} \cdot G_{\text{top}} \cdot G_{\text{crown}} \cdot \eta_{\text{trans}}}{r_{\text{wheel}}}$$
$$C_{\text{drag}} = \frac{F_{\text{drive},\max}}{v_{\max} \cdot (v_{\max} + 30.0)} \quad (\text{constant } \mathtt{0x41f00000} = 30.0)$$

---

### 3.2 `DrivelineComponent`, `Engine`, `Transmission`, and `Differential`

#### `DrivelineComponent` Base Layout (`0x001a5528`)
- `+0x00` (`0`): `float mRPM` (angular speed / RPM)
- `+0x04` (`4`): `float mTorque` (output torque in N·m)
- `+0x08` (`8`): `void* vtable`

#### `EngineMotif` (`0x001c31f0`, size `76` bytes) & `Engine` (`0x001c2be0`)
- **`EngineMotif` Fields & Defaults**:
  - `+0x0c` (`12`): `float mMaxRPM = 7500.0f` (`0x45ea6000`, `SetMaxRPM` @ `0x001c3320`)
  - `+0x10` (`16`): `float mMaxRPMRateOfChange = 15000.0f` (`0x466a6000`)
  - `+0x14` (`20`): `float mMaxTorque = 1000.0f` (`0x447a0000`, `SetMaxTorque` @ `0x001c3338`)
  - `+0x18` (`24`): `float mMaxTorqueWithTurbo = 1500.0f` (`0x44bb8000`, `SetMaxTorqueWithTurbo` @ `0x001c3340`)
  - `+0x1c` (`28`): `float mMinRPM = 1000.0f` (`0x447a0000`)
  - `+0x20` (`32`): `float mRPMRange = 6500.0f` (`mMaxRPM - mMinRPM`, updated in `SetMaxRPM` @ `0x001c3328`)
  - `+0x24..+0x48` (`36..72`): `float mTorqueCurve[10]` initialized from `.data` @ **`0x0050ffc0`**:
    `{ 0.30f, 0.51f, 0.67f, 0.72f, 0.93f, 0.99f, 1.00f, 0.94f, 0.89f, 0.81f }`
- **`EngineMotif::EvaluateTorqueCurve(float normRPM)` (`0x001c32d0`)**:
  ```cpp
  int i0 = static_cast<int>(normRPM * 10.0f);
  int i1 = (i0 + 1 < 9) ? (i0 + 1) : 9;
  return mTorqueCurve[i0] + normRPM * (mTorqueCurve[i1] - mTorqueCurve[i0]);
  ```
- **`Engine::Update(float dt)` (`0x001c2be0`)**:
  - Tracks `fabsf(mOutput->mRPM)` with rate limit `mMotif->mMaxRPMRateOfChange * dt`, clamped to `[mMinRPM, mMaxRPM - 1.0f]`.
  - Computes `mTorque = curveFactor * (mIsTurbo ? mMotif->mMaxTorqueWithTurbo : mMotif->mMaxTorque) * mGas`.

#### `TransmissionMotif` (`0x002c3138`, size `68` bytes) & `Transmission` (`0x002c2920`)
- **`TransmissionMotif` Fields & Defaults**:
  - `+0x0c` (`12`): `float mDownShiftRPM = 2500.0f` (`0x451c4000`, `SetDownShiftRPM` @ `0x002c3220`)
  - `+0x10` (`16`): `float mEfficiency = 1.0f` (`0x3f800000`, `SetEfficiency` @ `0x002c3228`)
  - `+0x14..+0x30` (`20..48`): `float mGearRatios[8]` (`R, N, 1, 2, 3, 4, 5, 6`) initialized from `.rodata` @ **`0x005766b8`**:
    `{ -1.30f, 0.00f, 2.66f, 1.78f, 1.30f, 1.00f, 0.74f, 0.50f }`
  - `+0x34` (`52`): `bool mAutomatic = true` (`SetAutomatic` @ `0x002c3218`)
  - `+0x38` (`56`): `float mMinTimeBetweenShifts = 1.0f` (`0x3f800000`, `GetMinTimeBetweenShifts` @ `0x002c3208`)
  - `+0x3c` (`60`): `int32_t mTopGear = 4` (`SetTopGear` @ `0x002c3270`)
  - `+0x40` (`64`): `float mUpShiftRPM = 5000.0f` (`0x459c4000`, `SetUpShiftRPM` @ `0x002c3278`)
- **`Transmission::Update(float dt, bool canShift)` (`0x002c2920`)**:
  - Increments `mTimeSinceLastShift += dt`.
  - If `mMotif->mAutomatic && canShift && mCurrentGear != -1`:
    - If `mInput->mRPM >= mMotif->mUpShiftRPM`, calls `ShiftUp()` (`0x002c28c0`).
    - Else if `mInput->mRPM <= mMotif->mDownShiftRPM`, calls `ShiftDown()` (`0x002c2868`).
  - Sets `this->mRPM = mOutput->mRPM * gearRatio` and `this->mTorque = mInput->mTorque * gearRatio * mMotif->mEfficiency`.

#### `DifferentialMotif` (`0x001a47e8`, size `20` bytes) & `Differential` (`0x001a4660`)
- **`DifferentialMotif` Defaults**:
  - `+0x0c` (`12`): `float mLock = 0.5f` (`0x3f000000`)
  - `+0x10` (`16`): `float mCrownGearRatio = 3.6f` (`0x40666666`)
- **`Differential::Update(float dt)` (`0x001a4660`)**:
  ```cpp
  this->mRPM    = 0.5f * (mOutput[0]->mRPM + mOutput[1]->mRPM) * mMotif->mCrownGearRatio;
  this->mTorque = 0.5f * mInput->mTorque * mMotif->mCrownGearRatio;
  ```

---

### 3.3 `SuspensionMotif` (`0x002a5f00` / `0x002a5f70`) & `Wheel` (`0x002e65f8..0x002e8ca0`)

#### `SuspensionMotif::Initialize(float vehicleMass)` (`0x002a5f70`)
- Computes spring stiffness and critical quarter-car damping from world gravity $g_y$ (`0x002e95d8`):
  $$k_{\text{spring}} = \frac{-m_{\text{vehicle}} \cdot g_y}{0.5 \cdot L_{\text{maxExtension}}} \quad (\text{stored at } \mathtt{+0x10})$$
  $$c_{\text{crit}} = 2 \sqrt{k_{\text{spring}} \cdot m_{\text{vehicle}}} \times 0.25 \quad (\text{stored at } \mathtt{+0x0c})$$
- In `Wheel::UpdateSuspension` (`0x002e6cd0`):
  - Effective spring constant: $k = k_{\text{spring}} \cdot \text{mSpringTune}$ (`+0x20`)
  - Effective damper constant: $c = c_{\text{crit}} \cdot \text{mDamperTune}$ (`+0x14`)
  - Damper force magnitude is clamped to **`50000.0f` N** (`0x47435000` at `0x002e6e18` & `0x002e6e70`).

#### `WheelMotif` (`0x002e81d8` / `0x002e89a8..0x002e8ca0`) — All Unknown Fields Resolved
| Offset | Type | Field Name | Setter / Getter VMA | Decompiled Meaning |
| :---: | :--- | :--- | :--- | :--- |
| `+0x0c` | `float` | `mDefaultRadius` (`0.4f`) | `0x002e88c0` | Base radius parameter |
| `+0x10` | `float` | `mBrakeBalance` | `0x002e89a8` (`SetBrakeBalance`) | Front/rear brake distribution `[0, 1]` |
| `+0x14` | `float` | `mCamber` (`0.0f`) | `0x002e88d0` | Reserved camber angle |
| `+0x18` | `float` | `mSlipAngleInDegrees` | `0x002e8a20` (`SetSlipAngleInDegrees`) | Full-slip threshold angle (degrees) |
| `+0x1c` | `float` | `mSlipTransition` | `0x002e8b58` (`SetSlipTransition`) | Transition fraction `[0, 1]` |
| `+0x20` | `float` | `mCosSlipStart` | `0x002e8af8` / `0x002e8940` | $\cos(\text{mSlipAngleInDegrees} \cdot \text{mSlipTransition} \cdot \frac{\pi}{180})$ |
| `+0x24` | `float` | `mCosSlipEnd` | `0x002e8b4c` / `0x002e8938` | $\cos(\text{mSlipAngleInDegrees} \cdot \frac{\pi}{180})$ |
| `+0x28` | `float` | `mRotationalInertia` | `0x002e89e0` (`SetMass`) | $\frac{1}{2} \cdot \text{mMass} \cdot \text{mRadius}^2$ |
| `+0x2c` | `int32_t` | `mDriving` | `0x002e89b8` (`SetDriving`) | `1` if driven by differential |
| `+0x30` | `int32_t` | `mPowerslideEffectVisibleOnHiddenWheel` | `0x002e8a08` | Visual effect flag |
| `+0x34` | `int32_t` | `mPhysical` | `0x002e8a10` (`SetPhysical`) | `1` for physical wheels `0..3`, `0` for virtual 5th wheel |
| `+0x38` | `int32_t` | `mSteering` | `0x002e8c90` (`SetSteering`) | `1` if steered |
| `+0x3c` | `int32_t` | `mSteeringVisual` | `0x002e8c98` (`SetSteeringVisual`) | Visual steering flag |
| `+0x40` | `int32_t` | `mVisible` | `0x002e8ca0` (`SetVisible`) | Render visibility flag |
| `+0x44` | `int32_t` | `mVisualInverseSteering` | `0x002e89c0` | Rear-steer visual inversion flag |
| `+0x48` | `float` | `mLateralGripInSlip` | `0x002e89c8` (`SetLateralGripInSlip`) | Lateral grip multiplier in full slip |
| `+0x4c` | `float` | `mLateralGripNoSlip` | `0x002e89d0` (`SetLateralGripNoSlip`) | Lateral grip multiplier before slip |
| `+0x50` | `float` | `mLongtitudinalGrip` | `0x002e89d8` (`SetLongtitudinalGrip`) | Longitudinal traction multiplier |
| `+0x54` | `float` | `mMass` | `0x002e89e0` (`SetMass`) | Wheel mass in kg (updates `mRotationalInertia` at `+0x28`) |
| `+0x58` | `float` | `mRadius` (`0.4f`) | `0x002e8960` (`GetRadius`) | Physical wheel radius in metres |

#### `Wheel::Update` (`0x002e7000`), `UpdateLongitudinalForce` (`0x002e6a18`), & `UpdateLateralForce` (`0x002e65f8`)
1. **Static Quarter-Car Normal Load (`0x002e7064..0x002e7088`)**:
   - `this->mNormalForce = -(vehicleMass * gravityY) * 0.25f` (`+0x3c`).
2. **Cosine-Domain Slip Blending & Hysteresis (`0x002e678c..0x002e6894`)**:
   - Computes $d = 1.0 - |\hat{v}_{\text{wheel}} \cdot \hat{f}_{\text{wheel}}| = 1.0 - |\cos\alpha|$.
   - Compares $d$ against $d_{\text{start}} = 1.0 - \text{mCosSlipStart}$ and $d_{\text{end}} = 1.0 - \text{mCosSlipEnd}$.
   - Once `mIsSlipping` (`+0x48`) is latched to `1`, it remains in slip until $d \le 1.0 - 0.9961947$ (**`0x0051c570` = $\cos(5^\circ)$**).
3. **Turbo Longitudinal Force Multiplier (`0x002e6c6c..0x002e6ca4`)**:
   - When `ctx->mIsTurbo == 1`, longitudinal wheel force (`+0x20..+0x28`) is multiplied by **`1.2f`** (`0x0051c57c`).

---

## 4. Phase C: Game Script & Mode Integration (`driving.god`)

### 4.1 Global RadScript / God-Script Native Registration (`0x0026c418`)
- **`RegisterScriptType`**: `0x0026e320`
- **`RegisterScriptFunction`**: `0x0026e410`
- **`RegisterScriptMethod`**: `0x002712c8`
- **Core Global Script Callbacks Registered in `0x0026c418`**:
  - `CreateObject(const char* className, const char* objectName)` (`0x0056a870`)
  - `FindObject(const char* className, const char* objectName)` (`0x0056a8d0`)
  - `LoadObjects(LoadSet* pLoadSet)` (`0x0056aa48`)
  - `LoadObjectsToHeap(LoadSet* pLoadSet, const char* heapName)` (`0x0056aaa8`)
  - `LoadObjectsUsing(LoadSet* pLoadSet, const char* loaderName)` (`0x0056ab28`)
  - `UnloadObjects(LoadSet* pLoadSet)` (`0x0056abb0`)
  - `AreObjectsLoaded(LoadSet* pLoadSet)` (`0x0056abe8`)
  - `FileExists(const char* fileName)` (`0x0056ac28`)

### 4.2 Driving Mode Singletons & Class Registrations (`driving.god`)

| Script Class / Singleton | Reg. Procedure VMA | Key Methods & Native Function VMAs |
| :--- | :--- | :--- |
| **`RaceControl`** | `0x0024ffb0` | `SetMotif` (`0x002505b0`), `PrepareRaceData` (`0x0024eed0`), `ResetRaceData` (`0x0024f180`), `RemoveRaceData` (`0x00250638`), `GetRacePosition` (`0x00250630`), `AreHumanPlayersFinished` (`0x00250718`), `SetRaceMusic` (`0x00250720`), `SetRaceMusicParameters` (`0x00250758`), `SetFunControlMotif` (`0x00250600`), `SetFinishLineCamMotif` (`0x00250628`) |
| **`RaceControlMotif`** | `0x00250a88` | `IsGrandPrix`, `GetNextGrandPrixLevel`, `SetClashedRace`, `GetNumberOfLaps` |
| **`AvatarManager`** | `0x0014b588` | `AvatarManager_Get` (`0x00544910`), `DestroyAvatars` (`0x0014bc08`), `SetNumberOfAIAvatars` (`0x0014bcb0`, offset `+404`) |
| **`VehicleManager`** | `0x002d73d8` | `VehicleManager_Get` (`0x00578f40`), `VehicleManager_SetCarAnimationLoadSet` (`0x00578f78`) |
| **`ClashManager`** | `0x0018a620` | `ClashManager` (`0x0054d048`, RTTI `12ClashManager` @ `0x0054d090`) |
| **`GameCollisionManager`** | `0x001da7d0` | `CollisionFetchFromInventory`, `EmptyCollisionManager` (`0x00555518`) |
| **`CarPhysics`** | `0x00172330` | `GetMotif` (`0x001729a0`, `+96`), `GetDifferential` (`0x001726a8`, `+108`), `GetEngine` (`0x00172910`, `+112`), `GetInAirControl` (`0x00172988`, `+120`), `GetSteeringWheel` (`0x001729d0`, `+144`), `GetTransmission` (`0x001729d8`, `+272`), `GetWheelFromScript` (`0x001729f0`, `+288`) |
| **`CarPhysicsMotif`** | `0x00173650` | `Initialize` (`0x00173468`), `FetchChassisCollisionObject` (`0x00173368`), plus 34 motif getters/setters (`0x00174618..0x00174a70`) |
