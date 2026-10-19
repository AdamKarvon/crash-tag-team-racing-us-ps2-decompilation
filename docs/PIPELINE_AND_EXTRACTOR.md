# RCF Extractor & Asset Pipeline Guide

This document outlines the asset pipeline of **CTTR Online**, tracing how raw commercial media is extracted, cataloged, and decompressed from the original PlayStation 2 *Crash Tag Team Racing* ISO into deterministic assets for the C++ engine.

---

## 🗺️ Asset Pipeline Overview

The asset pipeline extracts compiled console archives and maps their layout. The process flows through three phases:

```
+------------------+       pycdlib       +-------------------------+
|                  | ------------------> |                         |
|   Original ISO   |                     |  Raw RCF Archives (.RCF) |
|                  | <------------------ |                         |
+------------------+   ISO Filesystem    +-------------------------+
                                                      |
                                                      | rcf_extractor.py
                                                      v
+-------------------------+  JSON Compilation  +-------------------------+
|                         | <----------------- |                         |
|   Extracted Directory   |                    |    Unified Registry     |
|   (.p3d, .god, .txt)    | -----------------> |  (rcf_registry.json)    |
|                         |                    |                         |
+-------------------------+                    +-------------------------+
```

---

## 📀 Phase 1: ISO9660 Direct Extraction (`pipeline.py`)

To ensure clean, bit-perfect binary access, the script parses the ISO9660 filesystem directly using the `pycdlib` package. 
- **Target**: Radical Entertainment container archives (`.RCF`).
- **ISO9660 Naming**: ISO filesystems append a version suffix (e.g. `DINO1.RCF;1`). The extractor automatically parses this, strips the version suffix, and extracts the clean file payload to `assets/raw_iso/`.

---

## 📦 Phase 2: Radical Container Format (.RCF) Binary Layout

An RCF file is a high-speed, uncompressed virtual file system archive containing thousands of game assets. The parser in `tools/rcf_extractor.py` reads the binary structures using the following specifications:

```
RCF Archive File Layout:
+--------------------------------------------------------------+
| Magic Signature: "ATG CORE CEMENT LIBRARY" (32 Bytes)        |
+--------------------------------------------------------------+
| ... [Header Parameters]                                      |
+--------------------------------------------------------------+
| Offset 0x2C: stringTableOffset (4-Byte uint32)               |
+--------------------------------------------------------------+
| Offset 0x38: numFiles (4-Byte uint32)                        |
+--------------------------------------------------------------+
| Offset 0x3C: File Allocation Table (FAT)                     |
|              - numFiles entries                              |
|              - 12 Bytes per entry: [hash, offset, size]      |
+--------------------------------------------------------------+
| ... [Raw File Data Payloads]                                 |
+--------------------------------------------------------------+
| Offset stringTableOffset: String Table Directory             |
|              - numFiles entries                              |
|              - [12B header, 4B length, characters, padding]  |
+--------------------------------------------------------------+
```

### 1. The RCF Header

| Offset | Data Type | Name | Purpose / Value |
| :--- | :---: | :--- | :--- |
| **`0x00`** | `char[32]` | `magic` | Identifies archive: `"ATG CORE CEMENT LIBRARY\0\0\0\0\0\0\0\0"` |
| **`0x2C`** | `uint32_t` | `stringTableOffset` | Absolute byte offset to the filename directory in the archive. |
| **`0x38`** | `uint32_t` | `numFiles` | Total count of files packed inside this archive. |
| **`0x3C`** | `Struct[]` | `fatTable` | The FAT array begins immediately here. |

### 2. The File Allocation Table (FAT)
The FAT begins at offset `0x3C` and consists of `numFiles` consecutive **12-byte entries** matching this layout:

```c++
struct RcFATEntry {
    uint32_t hashVal; // 4-byte Radical proprietary name hash
    uint32_t offset;  // 4-byte absolute file offset within this RCF archive
    uint32_t size;    // 4-byte size of the file payload in bytes
};
```

### 3. The String Table Directory
Located at `stringTableOffset`. This block translates the numeric hashes in the FAT back to human-readable file paths (e.g. `art\cars\CrashStang\CrashStang.p3d`). Each string entry consists of:
- **`12 bytes`**: Timestamps/metadata flags.
- **`uint32_t nameLength`**: 4-byte integer length of the filename string.
- **`char name[nameLength]`**: Path characters.
- **`padding`**: Variable padding to align the next string entry to a 4-byte boundary.

---

## 🗄️ Phase 3: The Unified Registry Database (`rcf_registry.json`)

Radical Entertainment utilized a spanned-archive extraction method for *Crash Tag Team Racing*. Some file offsets recorded inside the individual `.RCF` archives represent **absolute offsets across spanned volumes** or pointers into separate mounted game discs.

### 🛡️ Spanned Offset Resolution Heuristic
To bypass bounds-checking exceptions, `rcf_extractor.py` performs a multi-archive compilation step:

1. **Compilation**: Scans all available `.RCF` archives in the RAW directory.
2. **Unified Registry**: Compiles a single registry (`assets/rcf_registry.json`). Every entry indexes:
   ```json
   "art\\cars\\CrashStang\\CrashStang.p3d": {
       "archive": "DINO1.RCF",
       "hash": "0xa6f8d21b",
       "offset": 1048576,
       "size": 524288,
       "extracted_path": "assets\\extracted_rcf\\art\\cars\\CrashStang\\CrashStang.p3d"
   }
   ```
3. **Smart Extraction**: During extraction, if a file offset falls outside the bounds of the scanned archive file, the extractor uses the unified metadata registry to trace and pull the asset from secondary virtual mounts (e.g. fallback directories `D:\DINO\` or `D:\COMMON\`).

---

## 🏃 Running the Extraction Pipeline

### 1. Install Prerequisites
Direct ISO parsing requires the `pycdlib` library:
```powershell
pip install pycdlib
```

### 2. Configure Paths
Verify that your target ISO path is properly set in `tools/pipeline.py` or place your raw `.RCF` files directly into `assets/raw_iso/`.

### 3. Execute Extractor
Run the pipeline to scan the archives, compile the database, and unpack all assets:
```powershell
python tools/rcf_extractor.py
```

Upon completion, a **File Census** report will summarize the extracted models:
```text
--- Phase 4: File Census ---
P3D Models: 84
GOD Configs: 41
Ready for bulk structural validation.
```
