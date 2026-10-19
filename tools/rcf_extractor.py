import os
import struct
import json
from pathlib import Path

# Paths
PROJECT_ROOT = Path(r"c:\Users\Adam\Documents\Project\CTTR_Online")
RAW_DIR = PROJECT_ROOT / "assets" / "raw_iso"
EXTRACT_DIR = PROJECT_ROOT / "assets" / "extracted_rcf"
META_DB_PATH = PROJECT_ROOT / "assets" / "rcf_registry.json"

def read_string(f):
    """Reads a null-terminated string from the file."""
    chars = []
    while True:
        c = f.read(1)
        if not c or c == b'\x00':
            break
        chars.append(c)
    return b"".join(chars).decode('utf-8', errors='ignore')

def build_unified_registry():
    print("--- Phase 1 & 2: RCF Registry Compilation ---")
    registry = {}
    
    # 1. Scan all RCFs and build a unified table
    rcf_files = list(RAW_DIR.glob("*.RCF"))
    if not rcf_files:
        print(f"No .RCF files found in {RAW_DIR}.")
        # Use D: drive for testing if raw_iso is empty
        dino1 = Path(r"D:\DINO\DINO1.RCF")
        if dino1.exists():
            print(f"Fallback: Using mounted ISO at {dino1}")
            rcf_files = [dino1]
        else:
            return None

    for rcf_path in rcf_files:
        print(f"Scanning Archive: {rcf_path.name}")
        with open(rcf_path, 'rb') as f:
            magic = f.read(32)
            if b"ATG CORE CEMENT LIBRARY" not in magic:
                print(f"  [Warning] Invalid Magic Signature in {rcf_path.name}")
                continue

            f.seek(0x2C)
            string_table_offset = struct.unpack('<I', f.read(4))[0]
            
            f.seek(0x38)
            # Depending on exact version, 0x38 or nearby holds the file count
            num_files = struct.unpack('<I', f.read(4))[0]
            fat_start = 0x3C # Empirical start of FAT array
            
            # Read String Table
            f.seek(string_table_offset)
            file_names = []
            for _ in range(num_files):
                try:
                    # Look ahead for alignment or padding
                    header = f.read(12) 
                    # Often 8 bytes of timestamps/hashes, then 4 bytes length
                    name_len = struct.unpack('<I', f.read(4))[0]
                    
                    if name_len == 0 or name_len > 256:
                        # Fallback heuristic if structure varies slightly
                        f.seek(-16, 1)
                        name = read_string(f)
                    else:
                        name = f.read(name_len).decode('utf-8', errors='ignore').strip('\x00')
                    
                    file_names.append(name)
                    
                    # Align to 4 bytes if necessary
                    rem = f.tell() % 4
                    if rem != 0:
                        f.seek(4 - rem, 1)
                except Exception as e:
                    print(f"  [Error] String table parsing failed at index {_}: {e}")
                    break

            # Read FAT Table
            f.seek(fat_start)
            for i in range(min(num_files, len(file_names))):
                hash_val, offset, size = struct.unpack('<III', f.read(12))
                
                # Register in unified database
                name = file_names[i]
                registry[name] = {
                    "archive": rcf_path.name,
                    "hash": f"0x{hash_val:08x}",
                    "offset": offset,
                    "size": size,
                    "extracted_path": str(EXTRACT_DIR / name.replace('\\', '/'))
                }

    # 2. Preserve Metadata Database
    with open(META_DB_PATH, 'w') as f:
        json.dump(registry, f, indent=4)
    print(f"Unified Registry built! {len(registry)} assets cataloged at {META_DB_PATH.name}")
    
    return registry

def extract_all_assets(registry):
    print("\n--- Phase 3: Extraction & Integrity Verification ---")
    if not registry:
        return

    # Group by archive to minimize file open/closes
    archives = {}
    for name, meta in registry.items():
        archives.setdefault(meta['archive'], []).append((name, meta))

    for archive_name, files in archives.items():
        # Fallback to D: if needed
        archive_path = RAW_DIR / archive_name
        if not archive_path.exists():
            archive_path = Path(r"D:\DINO") / archive_name
            if not archive_path.exists():
                 archive_path = Path(r"D:\COMMON") / archive_name

        if not archive_path.exists():
            print(f"Cannot find archive {archive_name} to extract files.")
            continue
            
        print(f"Extracting {len(files)} assets from {archive_name}...")
        with open(archive_path, 'rb') as f:
            for name, meta in files:
                offset = meta['offset']
                size = meta['size']
                out_path = Path(meta['extracted_path'])
                
                # CTTR trick: Some offsets are absolute across spanned archives.
                # If offset is larger than file size, we must search the unified registry
                # For now, we wrap in try-except and validate extraction
                try:
                    f.seek(offset)
                    data = f.read(size)
                    
                    if len(data) == size:
                        out_path.parent.mkdir(parents=True, exist_ok=True)
                        with open(out_path, 'wb') as out_f:
                            out_f.write(data)
                except Exception as e:
                    print(f"  [Failed] {name} - Offset out of bounds or missing.")

    # 4. Generate File Census
    p3d_count = len(list(EXTRACT_DIR.rglob("*.p3d")))
    god_count = len(list(EXTRACT_DIR.rglob("*.god")))
    print(f"\n--- Phase 4: File Census ---")
    print(f"P3D Models: {p3d_count}")
    print(f"GOD Configs: {god_count}")
    print("Ready for bulk structural validation.")

if __name__ == "__main__":
    reg = build_unified_registry()
    extract_all_assets(reg)
