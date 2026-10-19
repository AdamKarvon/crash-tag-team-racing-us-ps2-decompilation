import os
import struct
import json
from pathlib import Path

PROJECT_ROOT = Path(r"c:\Users\Adam\Documents\Project\CTTR_Online")
ISO_PATH = Path(r"C:\Users\Adam\Desktop\Crash Tag Team Racing (Europe, Australia) (En,Fr,De,Es,It,Nl).iso")
RAW_RCF_DIR = PROJECT_ROOT / "assets" / "raw_iso"
EXTRACT_DIR = PROJECT_ROOT / "assets" / "extracted_rcf"
GAME_DATA_DIR = PROJECT_ROOT / "game_data"
META_DB_PATH = PROJECT_ROOT / "assets" / "rcf_registry.json"

def parse_iso_dir(f, lba, size):
    f.seek(lba * 2048)
    dir_data = f.read(size)
    off = 0
    entries = []
    while off < len(dir_data):
        length = dir_data[off]
        if length == 0:
            off = ((off + 2047) // 2048) * 2048
            continue
        rec = dir_data[off:off+length]
        rec_lba = struct.unpack('<I', rec[2:6])[0]
        rec_size = struct.unpack('<I', rec[10:14])[0]
        flags = rec[25]
        name_len = rec[32]
        name = rec[33:33+name_len].decode('latin1', errors='ignore')
        
        is_dir = bool(flags & 2)
        entries.append({
            'name': name,
            'lba': rec_lba,
            'size': rec_size,
            'is_dir': is_dir
        })
        off += length
    return entries

def walk_iso_tree(f, lba, size, current_path=""):
    entries = parse_iso_dir(f, lba, size)
    files = []
    for e in entries:
        name = e['name'].split(';')[0]
        if name in ('\x00', '\x01', '.', '..'):
            continue
        path = f"{current_path}/{name}".lstrip('/')
        if e['is_dir']:
            files.extend(walk_iso_tree(f, e['lba'], e['size'], path))
        else:
            files.append((path, e['lba'], e['size']))
    return files

def read_rcf_file_list(rcf_data):
    if b"ATG CORE CEMENT LIBRARY" not in rcf_data[:32]:
        return []
    
    string_table_offset = struct.unpack('<I', rcf_data[0x2C:0x30])[0]
    num_files = struct.unpack('<I', rcf_data[0x38:0x3C])[0]
    
    fat = []
    fat_off = 0x3C
    for i in range(num_files):
        h_val, offset, fsize = struct.unpack('<III', rcf_data[fat_off:fat_off+12])
        fat.append({'hash': h_val, 'offset': offset, 'size': fsize})
        fat_off += 12
        
    fat_by_offset = sorted(fat, key=lambda x: (x['offset'], x['size']))
    
    off = string_table_offset + 8
    files = []
    
    for i in range(num_files):
        if off + 16 > len(rcf_data): break
        
        h_val, meta1, meta2, name_len = struct.unpack('<IIII', rcf_data[off:off+16])
        if name_len == 0 or name_len > 1024 or off + 16 + name_len > len(rcf_data):
            break
            
        name_bytes = rcf_data[off+16:off+16+name_len]
        name = name_bytes.decode('utf-8', errors='ignore').rstrip('\x00')
        
        entry = dict(fat_by_offset[i]) if i < len(fat_by_offset) else {}
        entry['name'] = name
        files.append(entry)
        
        off += 16 + name_len + 3
        
    return files

def main():
    print(f"=== CTTR ISO Master Extractor ===")
    print(f"Source ISO: {ISO_PATH}")
    
    RAW_RCF_DIR.mkdir(parents=True, exist_ok=True)
    EXTRACT_DIR.mkdir(parents=True, exist_ok=True)
    GAME_DATA_DIR.mkdir(parents=True, exist_ok=True)
    
    with open(ISO_PATH, 'rb') as f:
        f.seek(0x8000 + 156)
        root_rec = f.read(34)
        root_lba = struct.unpack('<I', root_rec[2:6])[0]
        root_size = struct.unpack('<I', root_rec[10:14])[0]
        
        iso_files = walk_iso_tree(f, root_lba, root_size)
        rcf_iso_files = [x for x in iso_files if x[0].upper().endswith('.RCF')]
        
        print(f"Found {len(rcf_iso_files)} RCF archive files inside ISO.")
        
        unified_registry = {}
        extracted_p3d_count = 0
        extracted_god_count = 0
        
        for iso_rel_path, lba, size in rcf_iso_files:
            print(f"\nProcessing RCF: {iso_rel_path} ({size:,} bytes)...")
            f.seek(lba * 2048)
            rcf_data = f.read(size)
            
            # Save raw RCF archive to assets/raw_iso/
            out_rcf_path = RAW_RCF_DIR / iso_rel_path
            out_rcf_path.parent.mkdir(parents=True, exist_ok=True)
            with open(out_rcf_path, 'wb') as rcf_out:
                rcf_out.write(rcf_data)
                
            # Parse RCF files
            rcf_entries = read_rcf_file_list(rcf_data)
            print(f"  Extracted index: {len(rcf_entries)} files cataloged.")
            
            for entry in rcf_entries:
                asset_name = entry.get('name', '')
                if not asset_name: continue
                offset = entry['offset']
                fsize = entry['size']
                
                if offset + fsize <= len(rcf_data):
                    payload = rcf_data[offset:offset+fsize]
                    
                    # Target output paths (normalize slashes and strip leading slashes)
                    clean_rel = asset_name.replace('\\', '/').lstrip('/')
                    out_path = EXTRACT_DIR / clean_rel
                    out_path.parent.mkdir(parents=True, exist_ok=True)
                    with open(out_path, 'wb') as out_f:
                        out_f.write(payload)
                        
                    # Mirror into game_data/ preserving directory hierarchy
                    game_data_out = GAME_DATA_DIR / clean_rel
                    game_data_out.parent.mkdir(parents=True, exist_ok=True)
                    with open(game_data_out, 'wb') as out_g:
                        out_g.write(payload)
                        
                    if clean_rel.endswith('.p3d'): extracted_p3d_count += 1
                    if clean_rel.endswith('.god'): extracted_god_count += 1
                    
                    unified_registry[clean_rel] = {
                        "archive": iso_rel_path,
                        "hash": f"0x{entry['hash']:08x}",
                        "offset": offset,
                        "size": fsize,
                        "extracted_path": str(out_path),
                        "game_data_path": str(game_data_out)
                    }

    with open(META_DB_PATH, 'w') as reg_f:
        json.dump(unified_registry, reg_f, indent=4)
        
    print(f"\n==========================================")
    print(f"=== ISO Master Extraction Complete! ===")
    print(f"==========================================")
    print(f"Cataloged Archives: {len(rcf_iso_files)}")
    print(f"Total Assets Extracted: {len(unified_registry)}")
    print(f"P3D Model Assets: {extracted_p3d_count}")
    print(f"GOD Config Assets: {extracted_god_count}")
    print(f"Registry DB: {META_DB_PATH}")

if __name__ == '__main__':
    main()
