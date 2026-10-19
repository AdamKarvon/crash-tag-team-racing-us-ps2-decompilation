#!/usr/bin/env python3
"""
PS2 MIPS R5900 (Emotion Engine) Binary Extractor & Decompiler Helper for CTTR (SLES_534.39)
Provides ELF overview, string search, cross-reference (xref) resolution, procedure boundary
discovery, RTTI & vtable recovery, Pure3D chunk handler table extraction, Script Reflection
method & struct offset recovery, MIPS-III / R5900 COP1 disassembly, and pseudo-C lifting.
"""

import sys
import struct
import re
import json
import argparse
from pathlib import Path

DEFAULT_ELF = Path(r"c:\Users\Adam\Documents\Project\CTTR_Online\assets\raw_iso\SLES_534.39")

GPR_NAMES = [
    "zero", "at", "v0", "v1", "a0", "a1", "a2", "a3",
    "t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7",
    "s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7",
    "t8", "t9", "k0", "k1", "gp", "sp", "fp", "ra"
]

class PS2Binary:
    def __init__(self, elf_path: Path = DEFAULT_ELF):
        self.path = elf_path
        self.data = elf_path.read_bytes()
        self.sections = []
        self.segments = []
        self.gp_value = 0
        self.entry = 0
        self._parse_elf()

    def _parse_elf(self):
        data = self.data
        e_type, e_machine, e_version, e_entry, e_phoff, e_shoff, e_flags, e_ehsize, e_phentsize, e_phnum, e_shentsize, e_shnum, e_shstrndx = struct.unpack('<HHIIIIIHHHHHH', data[16:52])
        self.entry = e_entry
        self.e_machine = e_machine
        self.e_flags = e_flags

        for i in range(e_phnum):
            off = e_phoff + i * e_phentsize
            p_type, p_offset, p_vaddr, p_paddr, p_filesz, p_memsz, p_flags, p_align = struct.unpack('<8I', data[off:off+32])
            self.segments.append({
                'type': p_type, 'offset': p_offset, 'vaddr': p_vaddr, 'paddr': p_paddr,
                'filesz': p_filesz, 'memsz': p_memsz, 'flags': p_flags, 'align': p_align
            })

        shstr_off_hdr = e_shoff + e_shstrndx * e_shentsize
        _, _, _, _, shstr_offset, shstr_size, _, _, _, _ = struct.unpack('<10I', data[shstr_off_hdr:shstr_off_hdr+40])
        shstrtab = data[shstr_offset:shstr_offset+shstr_size]

        for i in range(range(e_shnum).__len__()):
            off = e_shoff + i * e_shentsize
            sh_name, sh_type, sh_flags, sh_addr, sh_offset, sh_size, sh_link, sh_info, sh_addralign, sh_entsize = struct.unpack('<10I', data[off:off+40])
            end_str = shstrtab.find(b'\x00', sh_name)
            name = shstrtab[sh_name:end_str].decode('ascii', errors='ignore')
            self.sections.append({
                'index': i, 'name': name, 'type': sh_type, 'flags': sh_flags,
                'vma': sh_addr, 'offset': sh_offset, 'size': sh_size, 'align': sh_addralign
            })
            if name == '.reginfo' and sh_size >= 24:
                ri = struct.unpack('<6I', data[sh_offset:sh_offset+24])
                self.gp_value = ri[5]

    def vma_to_offset(self, vma: int):
        for s in self.sections:
            if s['vma'] > 0 and s['type'] != 8:  # Not SHT_NOBITS
                if s['vma'] <= vma < s['vma'] + s['size']:
                    return s['offset'] + (vma - s['vma'])
        return None

    def offset_to_vma(self, offset: int):
        for s in self.sections:
            if s['vma'] > 0 and s['type'] != 8:
                if s['offset'] <= offset < s['offset'] + s['size']:
                    return s['vma'] + (offset - s['offset'])
        return None

    def read_u32(self, vma: int):
        off = self.vma_to_offset(vma)
        if off is None or off + 4 > len(self.data):
            return None
        return struct.unpack_from('<I', self.data, off)[0]

    def read_f32(self, vma: int):
        off = self.vma_to_offset(vma)
        if off is None or off + 4 > len(self.data):
            return None
        return struct.unpack_from('<f', self.data, off)[0]

    def read_cstring(self, vma: int, max_len: int = 256):
        off = self.vma_to_offset(vma)
        if off is None or off >= len(self.data):
            return None
        end = self.data.find(b'\x00', off, min(len(self.data), off + max_len))
        if end == -1:
            return None
        raw = self.data[off:end]
        if len(raw) == 0:
            return ""
        if all(32 <= b <= 126 or b in (9, 10, 13) for b in raw):
            return raw.decode('ascii')
        return None

    def get_all_strings(self, min_len: int = 4):
        results = []
        for s in self.sections:
            if s['name'] in ('.data', '.rodata', '.sdata'):
                chunk = self.data[s['offset']:s['offset'] + s['size']]
                pattern = rb'[\x20-\x7e]{' + str(min_len).encode() + rb',}\x00'
                for m in re.finditer(pattern, chunk):
                    vma = s['vma'] + m.start()
                    text = m.group(0)[:-1].decode('ascii', errors='ignore')
                    results.append((vma, s['offset'] + m.start(), s['name'], text))
        return results

    def find_containing_procedure(self, vma: int, max_scan_back: int = 0x4000):
        off = self.vma_to_offset(vma)
        if off is None:
            return None
        text_sec = next(s for s in self.sections if s['name'] == '.text')
        min_off = max(text_sec['offset'], off - max_scan_back)
        curr = off & ~3
        while curr >= min_off:
            word = struct.unpack_from('<I', self.data, curr)[0]
            if (word & 0xffff8000) == 0x27bd8000:
                return self.offset_to_vma(curr)
            if curr >= text_sec['offset'] + 8:
                prev2 = struct.unpack_from('<I', self.data, curr - 8)[0]
                if prev2 == 0x03e00008:  # jr $ra
                    return self.offset_to_vma(curr)
            curr -= 4
        return None

    def find_procedure_end(self, start_vma: int, max_size: int = 0x4000):
        off = self.vma_to_offset(start_vma)
        if off is None:
            return start_vma + 0x100
        text_sec = next(s for s in self.sections if s['name'] == '.text')
        max_off = min(text_sec['offset'] + text_sec['size'], off + max_size)
        curr = off + 4
        max_branch_target = start_vma
        while curr < max_off:
            word = struct.unpack_from('<I', self.data, curr)[0]
            cur_vma = self.offset_to_vma(curr)
            op = (word >> 26) & 0x3f
            if op in (1, 4, 5, 6, 7, 20, 21, 22, 23) or (op == 17 and ((word >> 21) & 0x1f) == 8):
                imm = word & 0xffff
                if imm & 0x8000:
                    imm -= 0x10000
                target = cur_vma + 4 + (imm << 2)
                if target > max_branch_target and target < start_vma + max_size:
                    max_branch_target = target
            if (word & 0xffff8000) == 0x27bd8000 and cur_vma > max_branch_target:
                return cur_vma
            if word == 0x03e00008 and cur_vma >= max_branch_target:
                return cur_vma + 8
            curr += 4
        return self.offset_to_vma(curr)

    def find_xrefs(self, target_vma: int, tolerance: int = 0):
        xrefs = []
        for s in self.sections:
            if s['name'] in ('.data', '.rodata'):
                chunk = self.data[s['offset']:s['offset'] + s['size']]
                for off in range(0, len(chunk) - 3, 4):
                    val = struct.unpack_from('<I', chunk, off)[0]
                    if target_vma <= val <= target_vma + tolerance:
                        vma = s['vma'] + off
                        s_str = self.read_cstring(val)
                        xrefs.append({
                            'type': 'data_ptr',
                            'from_vma': f"0x{vma:08x}",
                            'section': s['name'],
                            'target_vma': f"0x{val:08x}",
                            'target_str': s_str
                        })

        text_sec = next(s for s in self.sections if s['name'] == '.text')
        text_bytes = self.data[text_sec['offset']:text_sec['offset'] + text_sec['size']]
        text_vma_base = text_sec['vma']

        if (target_vma & 0x0fffffff) == target_vma and (target_vma & 3) == 0 and tolerance == 0:
            idx_26 = (target_vma >> 2) & 0x03ffffff
            jal_word = (3 << 26) | idx_26
            j_word = (2 << 26) | idx_26
        else:
            jal_word = None
            j_word = None

        lui_state = {}
        n_insns = len(text_bytes) // 4
        words = struct.unpack(f'<{n_insns}I', text_bytes)

        hi_min = (target_vma >> 16)
        hi_max = ((target_vma + tolerance + 0x8000) >> 16)

        for i, w in enumerate(words):
            insn_vma = text_vma_base + (i << 2)
            if w == jal_word or w == j_word:
                proc = self.find_containing_procedure(insn_vma)
                xrefs.append({
                    'type': 'jal' if w == jal_word else 'j',
                    'from_vma': f"0x{insn_vma:08x}",
                    'procedure': f"0x{proc:08x}" if proc else None,
                    'target_vma': f"0x{target_vma:08x}"
                })
                continue

            op = (w >> 26) & 0x3f
            if op == 15:
                rt = (w >> 16) & 0x1f
                imm = w & 0xffff
                lui_state[rt] = (imm, insn_vma)
            elif op in (8, 9, 13, 32, 33, 35, 36, 37, 40, 41, 43, 49, 57):
                rs = (w >> 21) & 0x1f
                if rs in lui_state:
                    upper, set_vma = lui_state[rs]
                    if insn_vma - set_vma <= 128:
                        if hi_min <= upper <= hi_max:
                            imm = w & 0xffff
                            if op == 13:
                                addr = (upper << 16) | imm
                            else:
                                simm = imm - 0x10000 if (imm & 0x8000) else imm
                                addr = ((upper << 16) + simm) & 0xffffffff
                            if target_vma <= addr <= target_vma + tolerance:
                                proc = self.find_containing_procedure(insn_vma)
                                s_str = self.read_cstring(addr)
                                xrefs.append({
                                    'type': 'addr_ref',
                                    'from_vma': f"0x{insn_vma:08x}",
                                    'lui_vma': f"0x{set_vma:08x}",
                                    'procedure': f"0x{proc:08x}" if proc else None,
                                    'target_vma': f"0x{addr:08x}",
                                    'target_str': s_str
                                })
                    else:
                        del lui_state[rs]
                if rs == 28 and self.gp_value:
                    imm = w & 0xffff
                    simm = imm - 0x10000 if (imm & 0x8000) else imm
                    addr = (self.gp_value + simm) & 0xffffffff
                    if target_vma <= addr <= target_vma + tolerance:
                        proc = self.find_containing_procedure(insn_vma)
                        xrefs.append({
                            'type': 'gp_ref',
                            'from_vma': f"0x{insn_vma:08x}",
                            'procedure': f"0x{proc:08x}" if proc else None,
                            'target_vma': f"0x{addr:08x}"
                        })
        return xrefs

    def find_immediate_refs(self, imm_val: int):
        results = []
        text_sec = next(s for s in self.sections if s['name'] == '.text')
        text_bytes = self.data[text_sec['offset']:text_sec['offset'] + text_sec['size']]
        text_vma_base = text_sec['vma']
        n_insns = len(text_bytes) // 4
        words = struct.unpack(f'<{n_insns}I', text_bytes)

        hi_raw = (imm_val >> 16) & 0xffff
        hi_alg = ((imm_val + 0x8000) >> 16) & 0xffff
        lo_raw = imm_val & 0xffff

        lui_state = {}
        for i, w in enumerate(words):
            insn_vma = text_vma_base + (i << 2)
            op = (w >> 26) & 0x3f
            rs = (w >> 21) & 0x1f
            rt = (w >> 16) & 0x1f
            imm = w & 0xffff
            simm = imm - 0x10000 if (imm & 0x8000) else imm

            if op == 15:  # LUI
                lui_state[rt] = (imm, insn_vma)
                if lo_raw == 0 and imm == hi_raw and hi_raw != 0:
                    proc = self.find_containing_procedure(insn_vma)
                    results.append({
                        'vma': f"0x{insn_vma:08x}",
                        'procedure': f"0x{proc:08x}" if proc else None,
                        'kind': 'lui_exact',
                        'value': f"0x{imm_val:08x}"
                    })
            elif op in (8, 9, 13, 14):
                if rs == 0 and lo_raw == imm_val and (imm == lo_raw or (simm & 0xffffffff) == imm_val):
                    proc = self.find_containing_procedure(insn_vma)
                    results.append({
                        'vma': f"0x{insn_vma:08x}",
                        'procedure': f"0x{proc:08x}" if proc else None,
                        'kind': 'li16',
                        'value': f"0x{imm_val:08x}"
                    })
                elif rs in lui_state:
                    upper, set_vma = lui_state[rs]
                    if insn_vma - set_vma <= 64 and upper in (hi_raw, hi_alg):
                        val = ((upper << 16) | imm) if op in (13, 14) else (((upper << 16) + simm) & 0xffffffff)
                        if val == imm_val:
                            proc = self.find_containing_procedure(insn_vma)
                            results.append({
                                'vma': f"0x{insn_vma:08x}",
                                'lui_vma': f"0x{set_vma:08x}",
                                'procedure': f"0x{proc:08x}" if proc else None,
                                'kind': 'lui+imm',
                                'value': f"0x{imm_val:08x}"
                            })
        return results

    def extract_rtti_name_from_tf(self, tf_vma: int):
        """Given a GCC __tf... function VMA, extract the RTTI class name string it references."""
        if not tf_vma or not (0x00100000 <= tf_vma < 0x00506380):
            return None
        lui_map = {}
        for vma in range(tf_vma, tf_vma + 0x80, 4):
            w = self.read_u32(vma)
            if w is None:
                break
            op = (w >> 26) & 0x3f
            rs = (w >> 21) & 0x1f
            rt = (w >> 16) & 0x1f
            imm = w & 0xffff
            simm = imm - 0x10000 if (imm & 0x8000) else imm
            if op == 15:
                lui_map[rt] = imm
            elif op in (8, 9) and rs in lui_map:
                addr = ((lui_map[rs] << 16) + simm) & 0xffffffff
                s = self.read_cstring(addr)
                if s and len(s) >= 2:
                    return s
            if w == 0x03e00008:
                break
        return None

    def extract_p3d_handlers(self):
        """Trace all 53 calls to tP3DFileHandler::AddHandler (0x003bef28) and inspect each loader."""
        xrefs = self.find_xrefs(0x003bef28)
        handlers = []
        for x in xrefs:
            call_vma = int(x['from_vma'], 16)
            # Scan window [call_vma - 36 .. call_vma + 4] for chunk ID ($a2 = reg 6), alloc size ($a0), ctor JAL
            chunk_id = None
            ctor_vma = None
            alloc_size = None
            a2_hi = None
            for vma in range(call_vma - 40, call_vma + 8, 4):
                w = self.read_u32(vma)
                if w is None:
                    continue
                op = (w >> 26) & 0x3f
                rs = (w >> 21) & 0x1f
                rt = (w >> 16) & 0x1f
                imm = w & 0xffff
                simm = imm - 0x10000 if (imm & 0x8000) else imm
                if op == 15 and rt == 6:  # lui $a2, imm
                    a2_hi = imm << 16
                elif op == 13 and rs == 6 and rt == 6 and a2_hi is not None:
                    chunk_id = a2_hi | imm
                elif op in (8, 9) and rs == 6 and rt == 6 and a2_hi is not None:
                    chunk_id = (a2_hi + simm) & 0xffffffff
                elif op in (8, 9) and rs == 0 and rt == 6:
                    chunk_id = simm & 0xffffffff
                elif op in (8, 9) and rs == 0 and rt == 4 and vma < call_vma - 8:
                    alloc_size = simm
                elif op == 3 and vma < call_vma:
                    target = ((vma + 4) & 0xf0000000) | ((w & 0x03ffffff) << 2)
                    if target != 0x003bf3b0 and target != 0x003bef28:
                        ctor_vma = target

            # Inspect constructor if found to get vtable and internal chunk_id
            vtable_vma = None
            ctor_chunk_id = None
            rtti_name = None
            load_object_vma = None
            check_chunk_vma = None
            if ctor_vma:
                lui_map = {}
                for cvma in range(ctor_vma, ctor_vma + 0x80, 4):
                    cw = self.read_u32(cvma)
                    if cw is None:
                        break
                    cop = (cw >> 26) & 0x3f
                    crs = (cw >> 21) & 0x1f
                    crt = (cw >> 16) & 0x1f
                    cimm = cw & 0xffff
                    csimm = cimm - 0x10000 if (cimm & 0x8000) else cimm
                    if cop == 15:
                        lui_map[crt] = cimm << 16
                    elif cop == 13 and crs in lui_map:
                        lui_map[crt] = lui_map[crs] | cimm
                    elif cop in (8, 9) and crs in lui_map:
                        lui_map[crt] = (lui_map[crs] + csimm) & 0xffffffff
                    elif cop in (8, 9) and crs == 0:
                        lui_map[crt] = csimm & 0xffffffff
                    elif cop == 43:  # sw rt, off(rs)
                        if csimm == 0 and crt in lui_map and 0x0050eb80 <= lui_map[crt] < 0x005bae80:
                            vtable_vma = lui_map[crt]
                        elif csimm == 8 and crt in lui_map:
                            ctor_chunk_id = lui_map[crt]
                    if cw == 0x03e00008:
                        break

            if chunk_id is None and a2_hi is not None:
                chunk_id = a2_hi
            if chunk_id is None and ctor_chunk_id is not None:
                chunk_id = ctor_chunk_id

            if vtable_vma:
                tf_func = self.read_u32(vtable_vma + 4)
                rtti_name = self.extract_rtti_name_from_tf(tf_func)
                check_chunk_vma = self.read_u32(vtable_vma + 0x2c)
                load_object_vma = self.read_u32(vtable_vma + 0x34)
                if not load_object_vma or not (0x00100000 <= load_object_vma < 0x00506380):
                    load_object_vma = self.read_u32(vtable_vma + 0x24)

            handlers.append({
                'call_vma': f"0x{call_vma:08x}",
                'reg_proc': x['procedure'],
                'chunk_id': f"0x{chunk_id:08x}" if chunk_id is not None else None,
                'alloc_size': alloc_size,
                'ctor_vma': f"0x{ctor_vma:08x}" if ctor_vma else None,
                'vtable_vma': f"0x{vtable_vma:08x}" if vtable_vma else None,
                'rtti_class': rtti_name,
                'load_object_vma': f"0x{load_object_vma:08x}" if load_object_vma else None,
                'check_chunk_vma': f"0x{check_chunk_vma:08x}" if check_chunk_vma else None,
            })
        return handlers

    def extract_script_methods(self, pattern: str = ""):
        """
        Trace all calls to RegisterScriptMethod (0x002712c8) in .text to recover
        exact (class_proc, method_name, signature, ptmf_vma, func_vma, body_preview).
        """
        xrefs = self.find_xrefs(0x002712c8)
        rx = re.compile(pattern, re.IGNORECASE) if pattern else None
        entries = []

        # Group xrefs by registration procedure
        by_proc = {}
        for x in xrefs:
            call_vma = int(x['from_vma'], 16)
            reg_proc = int(x['procedure'], 16) if x['procedure'] else (call_vma - 0x48)
            by_proc.setdefault(reg_proc, []).append(call_vma)

        for reg_proc, call_vmas in sorted(by_proc.items()):
            call_set = set(call_vmas)
            max_vma = max(call_vmas) + 8
            cname = None
            reg_val = {0: 0}
            last_ptmf_vma = None

            vma = reg_proc
            while vma <= max_vma:
                w = self.read_u32(vma)
                if w is None:
                    break

                def step_insn(word):
                    nonlocal cname, last_ptmf_vma
                    op = (word >> 26) & 0x3f
                    rs = (word >> 21) & 0x1f
                    rt = (word >> 16) & 0x1f
                    rd = (word >> 11) & 0x1f
                    funct = word & 0x3f
                    imm = word & 0xffff
                    simm = imm - 0x10000 if (imm & 0x8000) else imm
                    if op == 15:
                        reg_val[rt] = imm << 16
                    elif op in (8, 9):
                        if rs == 0:
                            reg_val[rt] = simm & 0xffffffff
                        elif rs in reg_val:
                            addr = (reg_val[rs] + simm) & 0xffffffff
                            reg_val[rt] = addr
                            if cname is None and vma < reg_proc + 160:
                                s = self.read_cstring(addr)
                                if s and len(s) >= 2 and not s.startswith("void ") and "(" not in s:
                                    cname = s
                    elif op == 0 and funct in (0x21, 0x25, 0x2d):
                        if rt == 0 and rs in reg_val:
                            reg_val[rd] = reg_val[rs]
                        elif rs == 0 and rt in reg_val:
                            reg_val[rd] = reg_val[rt]
                    elif op == 26 and simm == 7 and rs != 29:
                        if rs in reg_val:
                            last_ptmf_vma = reg_val[rs]

                if vma in call_set:
                    # Execute branch delay slot first
                    w_delay = self.read_u32(vma + 4)
                    if w_delay is not None:
                        step_insn(w_delay)
                    class_name = cname or f"proc_0x{reg_proc:08x}"
                    ptmf_vma = last_ptmf_vma
                    method_name = self.read_cstring(reg_val.get(6, 0)) if 6 in reg_val else None
                    sig_str = self.read_cstring(reg_val.get(7, 0)) if 7 in reg_val else None

                    func_vma = None
                    virt_idx = None
                    body_preview = ""
                    if ptmf_vma:
                        w0 = self.read_u32(ptmf_vma)
                        w1 = self.read_u32(ptmf_vma + 4)
                        if w0 is not None and w1 is not None:
                            idx = struct.unpack('<h', struct.pack('<H', (w0 >> 16) & 0xffff))[0]
                            if idx < 0 and (0x00100000 <= w1 < 0x00506380):
                                func_vma = w1
                            elif idx >= 0:
                                virt_idx = idx

                    if func_vma:
                        insns = []
                        lui_t = {}
                        for ivma in range(func_vma, func_vma + 32, 4):
                            iw = self.read_u32(ivma)
                            if iw is None:
                                break
                            asm, cm = self.disasm_insn(iw, ivma, lui_t)
                            insns.append(f"{asm}" + (f" [{cm}]" if cm else ""))
                            if iw == 0x03e00008:
                                iw_d = self.read_u32(ivma + 4)
                                if iw_d is not None:
                                    asm_d, cm_d = self.disasm_insn(iw_d, ivma + 4, lui_t)
                                    insns.append(f"{asm_d}" + (f" [{cm_d}]" if cm_d else ""))
                                break
                        body_preview = "; ".join(insns)
                    elif virt_idx is not None:
                        body_preview = f"virtual vtable[{virt_idx}]"

                    blob = f"{class_name}::{method_name or ''} | {sig_str or ''} | {body_preview}"
                    if not rx or rx.search(blob):
                        entries.append({
                            'class': class_name,
                            'method': method_name,
                            'signature': sig_str,
                            'func_vma': f"0x{func_vma:08x}" if func_vma else f"vtable[{virt_idx}]",
                            'ptmf_vma': f"0x{ptmf_vma:08x}" if ptmf_vma else None,
                            'reg_proc': f"0x{reg_proc:08x}",
                            'body': body_preview
                        })
                    vma += 8
                    continue

                step_insn(w)
                vma += 4

        return entries

    def disasm_insn(self, word: int, vma: int, lui_tracker: dict = None):
        if word == 0:
            return "nop", None
        op = (word >> 26) & 0x3f
        rs = (word >> 21) & 0x1f
        rt = (word >> 16) & 0x1f
        rd = (word >> 11) & 0x1f
        shamt = (word >> 6) & 0x1f
        funct = word & 0x3f
        imm = word & 0xffff
        simm = imm - 0x10000 if (imm & 0x8000) else imm
        jtarget = ((vma + 4) & 0xf0000000) | ((word & 0x03ffffff) << 2)
        btarget = vma + 4 + (simm << 2)

        r_rs, r_rt, r_rd = GPR_NAMES[rs], GPR_NAMES[rt], GPR_NAMES[rd]
        comment = None

        if op == 0:  # SPECIAL
            if funct == 0x00: return f"sll ${r_rd}, ${r_rt}, {shamt}", None
            if funct == 0x02: return f"srl ${r_rd}, ${r_rt}, {shamt}", None
            if funct == 0x03: return f"sra ${r_rd}, ${r_rt}, {shamt}", None
            if funct == 0x04: return f"sllv ${r_rd}, ${r_rt}, ${r_rs}", None
            if funct == 0x06: return f"srlv ${r_rd}, ${r_rt}, ${r_rs}", None
            if funct == 0x07: return f"srav ${r_rd}, ${r_rt}, ${r_rs}", None
            if funct == 0x08: return f"jr ${r_rs}", None
            if funct == 0x09: return f"jalr ${r_rd}, ${r_rs}", None
            if funct == 0x0a: return f"movz ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x0b: return f"movn ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x10: return f"mfhi ${r_rd}", None
            if funct == 0x12: return f"mflo ${r_rd}", None
            if funct == 0x18: return f"mult ${r_rs}, ${r_rt}", None
            if funct == 0x19: return f"multu ${r_rs}, ${r_rt}", None
            if funct == 0x1a: return f"div ${r_rs}, ${r_rt}", None
            if funct == 0x1b: return f"divu ${r_rs}, ${r_rt}", None
            if funct == 0x20: return f"add ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x21:
                if rt == 0: return f"move ${r_rd}, ${r_rs}", None
                if rs == 0: return f"move ${r_rd}, ${r_rt}", None
                return f"addu ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x22: return f"sub ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x23: return f"subu ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x24: return f"and ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x25:
                if rt == 0: return f"move ${r_rd}, ${r_rs}", None
                return f"or ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x26: return f"xor ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x27: return f"nor ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x2a: return f"slt ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x2b: return f"sltu ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x2d:
                if rt == 0: return f"daddu ${r_rd}, ${r_rs}, zero", None
                return f"daddu ${r_rd}, ${r_rs}, ${r_rt}", None
            return f"special.0x{funct:02x} ${r_rd}, ${r_rs}, ${r_rt}", None

        if op == 1:
            regimm_map = {0: "bltz", 1: "bgez", 2: "bltzl", 3: "bgezl", 16: "bltzal", 17: "bgezal"}
            m = regimm_map.get(rt, f"regimm.{rt}")
            return f"{m} ${r_rs}, 0x{btarget:08x}", None

        if op == 2: return f"j 0x{jtarget:08x}", None
        if op == 3: return f"jal 0x{jtarget:08x}", None
        if op == 4:
            if rs == rt: return f"b 0x{btarget:08x}", None
            if rt == 0: return f"beqz ${r_rs}, 0x{btarget:08x}", None
            return f"beq ${r_rs}, ${r_rt}, 0x{btarget:08x}", None
        if op == 5:
            if rt == 0: return f"bnez ${r_rs}, 0x{btarget:08x}", None
            return f"bne ${r_rs}, ${r_rt}, 0x{btarget:08x}", None
        if op == 6: return f"blez ${r_rs}, 0x{btarget:08x}", None
        if op == 7: return f"bgtz ${r_rs}, 0x{btarget:08x}", None
        if op == 20: return f"beql ${r_rs}, ${r_rt}, 0x{btarget:08x}", None
        if op == 21: return f"bnel ${r_rs}, ${r_rt}, 0x{btarget:08x}", None
        if op == 22: return f"blezl ${r_rs}, 0x{btarget:08x}", None
        if op == 23: return f"bgtzl ${r_rs}, 0x{btarget:08x}", None

        if op == 15:
            if lui_tracker is not None:
                lui_tracker[rt] = imm
            return f"lui ${r_rt}, 0x{imm:04x}", None

        eff_addr = None
        if lui_tracker is not None and rs in lui_tracker:
            if op in (8, 9, 32, 33, 35, 36, 37, 40, 41, 43, 49, 57):
                eff_addr = ((lui_tracker[rs] << 16) + simm) & 0xffffffff
            elif op == 13:
                eff_addr = (lui_tracker[rs] << 16) | imm
        elif rs == 28 and self.gp_value and op in (8, 9, 32, 33, 35, 36, 37, 40, 41, 43, 49, 57):
            eff_addr = (self.gp_value + simm) & 0xffffffff

        if eff_addr is not None:
            s_val = self.read_cstring(eff_addr)
            if s_val is not None and len(s_val) >= 2:
                comment = f"0x{eff_addr:08x} -> {s_val!r}"
            elif op == 49:
                fval = self.read_f32(eff_addr)
                if fval is not None:
                    comment = f"0x{eff_addr:08x} -> float({fval:.6g})"
            else:
                uval = self.read_u32(eff_addr)
                if uval is not None:
                    comment = f"0x{eff_addr:08x} (val=0x{uval:08x})"
                else:
                    comment = f"0x{eff_addr:08x}"

        if op in (8, 9):
            if lui_tracker is not None and rt in lui_tracker and rs != rt:
                del lui_tracker[rt]
            if rs == 0:
                return f"li ${r_rt}, {simm} (0x{imm:04x})", comment
            return f"addiu ${r_rt}, ${r_rs}, {simm}", comment
        if op == 10: return f"slti ${r_rt}, ${r_rs}, {simm}", None
        if op == 11: return f"sltiu ${r_rt}, ${r_rs}, {simm}", None
        if op == 12: return f"andi ${r_rt}, ${r_rs}, 0x{imm:04x}", None
        if op == 13: return f"ori ${r_rt}, ${r_rs}, 0x{imm:04x}", comment
        if op == 14: return f"xori ${r_rt}, ${r_rs}, 0x{imm:04x}", None

        if op == 17:
            fmt = rs
            ft = rt
            fs = rd
            fd = shamt
            if fmt == 0: return f"mfc1 ${r_rt}, $f{fs}", None
            if fmt == 4: return f"mtc1 ${r_rt}, $f{fs}", None
            if fmt == 8:
                bc_map = {0: "bc1f", 1: "bc1t", 2: "bc1fl", 3: "bc1tl"}
                return f"{bc_map.get(rt, 'bc1?')} 0x{btarget:08x}", None
            if fmt == 16:
                fmap = {
                    0x00: f"add.s $f{fd}, $f{fs}, $f{ft}",
                    0x01: f"sub.s $f{fd}, $f{fs}, $f{ft}",
                    0x02: f"mul.s $f{fd}, $f{fs}, $f{ft}",
                    0x03: f"div.s $f{fd}, $f{fs}, $f{ft}",
                    0x04: f"sqrt.s $f{fd}, $f{ft}",
                    0x05: f"abs.s $f{fd}, $f{fs}",
                    0x06: f"mov.s $f{fd}, $f{fs}",
                    0x07: f"neg.s $f{fd}, $f{fs}",
                    0x16: f"rsqrt.s $f{fd}, $f{fs}, $f{ft}",
                    0x18: f"adda.s $f{fs}, $f{ft}",
                    0x19: f"suba.s $f{fs}, $f{ft}",
                    0x1a: f"mula.s $f{fs}, $f{ft}",
                    0x1c: f"madd.s $f{fd}, $f{fs}, $f{ft}",
                    0x1d: f"msub.s $f{fd}, $f{fs}, $f{ft}",
                    0x1e: f"madda.s $f{fs}, $f{ft}",
                    0x1f: f"msuba.s $f{fs}, $f{ft}",
                    0x24: f"cvt.w.s $f{fd}, $f{fs}",
                    0x28: f"max.s $f{fd}, $f{fs}, $f{ft}",
                    0x29: f"min.s $f{fd}, $f{fs}, $f{ft}",
                    0x30: f"c.f.s $f{fs}, $f{ft}",
                    0x32: f"c.eq.s $f{fs}, $f{ft}",
                    0x34: f"c.lt.s $f{fs}, $f{ft}",
                    0x36: f"c.le.s $f{fs}, $f{ft}",
                }
                return fmap.get(funct, f"cop1.s.0x{funct:02x} $f{fd}, $f{fs}, $f{ft}"), None
            if fmt == 20 and funct == 0x20:
                return f"cvt.s.w $f{fd}, $f{fs}", None
            return f"cop1.{fmt}.0x{funct:02x}", None

        if op == 26: return f"ldl ${r_rt}, {simm}(${r_rs})", comment
        if op == 27: return f"ldr ${r_rt}, {simm}(${r_rs})", comment
        if op == 28:
            if funct == 0x00: return f"madd ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x01: return f"maddu ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x18: return f"mult1 ${r_rd}, ${r_rs}, ${r_rt}", None
            if funct == 0x1a: return f"div1 ${r_rs}, ${r_rt}", None
            if funct == 0x1b: return f"divu1 ${r_rs}, ${r_rt}", None
            return f"mmi.0x{funct:02x} ${r_rd}, ${r_rs}, ${r_rt}", None

        if op == 30: return f"lq ${r_rt}, {simm}(${r_rs})", comment
        if op == 31: return f"sq ${r_rt}, {simm}(${r_rs})", comment
        if op == 32: return f"lb ${r_rt}, {simm}(${r_rs})", comment
        if op == 33: return f"lh ${r_rt}, {simm}(${r_rs})", comment
        if op == 35: return f"lw ${r_rt}, {simm}(${r_rs})", comment
        if op == 36: return f"lbu ${r_rt}, {simm}(${r_rs})", comment
        if op == 37: return f"lhu ${r_rt}, {simm}(${r_rs})", comment
        if op == 40: return f"sb ${r_rt}, {simm}(${r_rs})", comment
        if op == 41: return f"sh ${r_rt}, {simm}(${r_rs})", comment
        if op == 43: return f"sw ${r_rt}, {simm}(${r_rs})", comment
        if op == 44: return f"sdl ${r_rt}, {simm}(${r_rs})", comment
        if op == 45: return f"sdr ${r_rt}, {simm}(${r_rs})", comment
        if op == 49: return f"lwc1 $f{rt}, {simm}(${r_rs})", comment
        if op == 55: return f"ld ${r_rt}, {simm}(${r_rs})", comment
        if op == 57: return f"swc1 $f{rt}, {simm}(${r_rs})", comment
        if op == 63: return f"sd ${r_rt}, {simm}(${r_rs})", comment

        return f"op{op} (0x{word:08x})", None

    def disassemble_range(self, start_vma: int, end_vma: int):
        lines = []
        lui_tracker = {}
        for vma in range(start_vma, end_vma, 4):
            w = self.read_u32(vma)
            if w is None:
                break
            asm, comment = self.disasm_insn(w, vma, lui_tracker)
            line = f"0x{vma:08x}:  {w:08x}  {asm:<32}"
            if comment:
                line += f" ; {comment}"
            lines.append(line)
        return lines

    def dump_data_table(self, start_vma: int, count: int = 64):
        lines = []
        for i in range(count):
            vma = start_vma + i * 4
            val = self.read_u32(vma)
            if val is None:
                break
            fval = self.read_f32(vma)
            note = ""
            if 0x00100000 <= val < 0x00506380:
                note = f" -> .text func 0x{val:08x}"
            elif 0x0050eb80 <= val < 0x005bae80:
                s = self.read_cstring(val)
                if s is not None:
                    note = f" -> str {s!r}"
                else:
                    note = f" -> data 0x{val:08x}"
            elif fval is not None and 1e-5 <= abs(fval) <= 1e7:
                note = f" (float: {fval:.6g})"
            lines.append(f"0x{vma:08x}:  0x{val:08x}{note}")
        return lines

def main():
    parser = argparse.ArgumentParser(description="PS2 R5900 Binary Analyzer for CTTR")
    sub = parser.add_subparsers(dest="cmd")

    sub.add_parser("overview")

    p_str = sub.add_parser("search_strings")
    p_str.add_argument("pattern")
    p_str.add_argument("--limit", type=int, default=100)

    p_xref = sub.add_parser("xrefs")
    p_xref.add_argument("vma")
    p_xref.add_argument("--tolerance", type=lambda x: int(x, 0), default=0)

    p_imm = sub.add_parser("find_imm")
    p_imm.add_argument("val", type=lambda x: int(x, 0))

    p_proc = sub.add_parser("procedure")
    p_proc.add_argument("vma")
    p_proc.add_argument("--max_insns", type=int, default=200)
    p_proc.add_argument("--exact", action="store_true")
    p_proc.add_argument("--raw", action="store_true")

    p_data = sub.add_parser("dump_data")
    p_data.add_argument("vma")
    p_data.add_argument("--count", type=int, default=64)

    sub.add_parser("extract_p3d_handlers")

    p_sm = sub.add_parser("extract_script_methods")
    p_sm.add_argument("--pattern", default="")
    p_sm.add_argument("--limit", type=int, default=200)

    args = parser.parse_args()
    b = PS2Binary()

    if args.cmd == "overview":
        text_sec = next(s for s in b.sections if s['name'] == '.text')
        text_words = struct.unpack(f"<{text_sec['size']//4}I", b.data[text_sec['offset']:text_sec['offset']+text_sec['size']])
        prologues = sum(1 for w in text_words if (w & 0xffff8000) == 0x27bd8000)
        strings = b.get_all_strings()
        print(f"=== SLES_534.39 Binary Overview ===")
        print(f"Path: {b.path} ({len(b.data):,} bytes)")
        print(f"Architecture: MIPS-III / Toshiba-Sony R5900 (Emotion Engine) EABI64 little-endian")
        print(f"Entry Point: 0x{b.entry:08x} | e_flags: 0x{b.e_flags:08x} | $gp (.reginfo): 0x{b.gp_value:08x}")
        print(f"Estimated Procedures (.text stack prologues): {prologues:,}")
        print(f"Defined ASCII Strings (.data/.rodata): {len(strings):,}")
        print("\nSections:")
        for s in b.sections:
            if s['name']:
                print(f"  [{s['index']:2d}] {s['name']:<40} VMA=0x{s['vma']:08x}  Off=0x{s['offset']:08x}  Size=0x{s['size']:08x}")
    elif args.cmd == "search_strings":
        rx = re.compile(args.pattern, re.IGNORECASE)
        matches = [x for x in b.get_all_strings() if rx.search(x[3])]
        print(f"Found {len(matches)} string matches for /{args.pattern}/i:")
        for vma, off, sec, text in matches[:args.limit]:
            print(f"  0x{vma:08x} (off 0x{off:08x}, {sec}): {text!r}")
    elif args.cmd == "xrefs":
        target = int(args.vma, 16)
        xrefs = b.find_xrefs(target, args.tolerance)
        print(f"Found {len(xrefs)} xrefs to 0x{target:08x} (+0x{args.tolerance:x}):")
        for x in xrefs:
            print(" ", json.dumps(x))
    elif args.cmd == "find_imm":
        refs = b.find_immediate_refs(args.val)
        print(f"Found {len(refs)} immediate references to 0x{args.val:08x}:")
        for r in refs[:100]:
            print(" ", json.dumps(r))
    elif args.cmd == "procedure":
        vma = int(args.vma, 16)
        start = vma if (args.exact or args.raw) else (b.find_containing_procedure(vma) or vma)
        end = (start + args.max_insns * 4) if args.raw else b.find_procedure_end(start, max_size=args.max_insns * 4)
        print(f"=== Procedure at 0x{start:08x} .. 0x{end:08x} (queried 0x{vma:08x}) ===")
        for line in b.disassemble_range(start, min(end, start + args.max_insns * 4)):
            print(" ", line)
    elif args.cmd == "dump_data":
        vma = int(args.vma, 16)
        print(f"=== Data Dump at 0x{vma:08x} ({args.count} words) ===")
        for line in b.dump_data_table(vma, args.count):
            print(" ", line)
    elif args.cmd == "extract_p3d_handlers":
        handlers = b.extract_p3d_handlers()
        print(f"=== Extracted {len(handlers)} Pure3D Chunk Handlers ===")
        for h in handlers:
            print(" ", json.dumps(h))
    elif args.cmd == "extract_script_methods":
        entries = b.extract_script_methods(args.pattern)
        print(f"=== Extracted {len(entries)} Script Methods matching /{args.pattern}/i ===")
        for e in entries[:args.limit]:
            print(" ", json.dumps(e))

if __name__ == "__main__":
    main()
