#include <iostream>
#include <fstream>
#include <vector>
#include <iomanip>
#include <string>
#include <cstdint>

// Known Radical Pure3D Chunk Signatures (To be expanded via fingerprinting)
std::string GetChunkName(uint32_t id) {
    switch (id) {
        case 0x00010000: return "MESH";
        case 0x00010001: return "SKELETON";
        case 0x00010002: return "BONE";
        case 0x00010003: return "LOCATOR";
        // Typical Pure3D root/header signatures (Examples)
        case 0x00000000: return "PADDING/ALIGN";
        case 0x50334400: return "P3D_HEADER_MAGIC"; // "P3D\0"
        default: return "UNKNOWN";
    }
}

// Utility for Hex Fingerprinting
void PrintHex(const std::vector<uint8_t>& data, size_t offset, size_t length, const std::string& indent) {
    size_t end = std::min(offset + length, data.size());
    for (size_t i = offset; i < end; i += 16) {
        std::cout << indent << "0x" << std::hex << std::setw(8) << std::setfill('0') << i << ": ";
        
        // Print Hex Bytes
        for (size_t j = 0; j < 16; ++j) {
            if (i + j < end)
                std::cout << std::hex << std::setw(2) << std::setfill('0') << (int)data[i + j] << " ";
            else
                std::cout << "   ";
        }
        
        std::cout << " |";
        // Print ASCII Interpretation
        for (size_t j = 0; j < 16; ++j) {
            if (i + j < end) {
                char c = data[i + j];
                std::cout << (c >= 32 && c <= 126 ? c : '.');
            }
        }
        std::cout << "|\n";
    }
    std::cout << std::dec; // Reset to decimal
}

// Recursive Structural Dumper
void WalkChunks(const std::vector<uint8_t>& data, size_t startOffset, size_t endOffset, int depth = 0) {
    size_t currentOffset = startOffset;
    std::string indent(depth * 4, ' ');
    std::string treeBranch = depth > 0 ? "|-- " : "";

    while (currentOffset + 12 <= endOffset) { // 12 bytes = typical P3D header (ID, Size, DataSize)
        uint32_t chunkId = *reinterpret_cast<const uint32_t*>(&data[currentOffset]);
        uint32_t chunkSize = *reinterpret_cast<const uint32_t*>(&data[currentOffset + 4]);
        
        // Safety / Alignment Check
        if (chunkSize == 0) {
            std::cout << indent << treeBranch << "WARNING: Chunk Size 0 at 0x" << std::hex << currentOffset << "\n";
            break;
        }

        std::string chunkName = GetChunkName(chunkId);
        
        std::cout << indent << treeBranch << "Chunk [0x" << std::hex << std::setw(8) << std::setfill('0') << chunkId << std::dec 
                  << "] : " << chunkName 
                  << " | Size: " << chunkSize << " bytes" 
                  << " @ Offset 0x" << std::hex << currentOffset << std::dec << "\n";

        // Dump the first 32 bytes of the payload for Fingerprinting (skip header)
        size_t payloadOffset = currentOffset + 12; // Assuming 12-byte header
        size_t payloadSize = chunkSize > 12 ? chunkSize - 12 : 0;
        
        if (chunkName == "UNKNOWN") {
            PrintHex(data, payloadOffset, std::min((size_t)32, payloadSize), indent + "    ");
        }

        // --- HIERARCHY DETECTION (Guessing) ---
        // If a chunk is larger than a reasonable primitive, it might contain nested chunks.
        // In Radical P3D, chunks often encapsulate others (e.g., Skeleton -> Bones)
        // We will attempt to recursively parse if we see valid chunk headers inside.
        if (payloadSize > 24) {
            uint32_t possibleNestedId = *reinterpret_cast<const uint32_t*>(&data[payloadOffset]);
            // Heuristic: Does it look like a valid P3D chunk ID? (Often low numbers like 0x0001XXXX)
            if ((possibleNestedId & 0xFFFF0000) == 0x00010000 || possibleNestedId == 0x00000000) {
                WalkChunks(data, payloadOffset, currentOffset + chunkSize, depth + 1);
            }
        }

        // Move to next chunk, honoring PS2 16-byte alignment rules
        currentOffset += chunkSize;
        // currentOffset = (currentOffset + 15) & ~15; // Enable if strict DMA alignment is observed
    }
}

int main(int argc, char* argv[]) {
    if (argc < 2) {
        std::cerr << "Usage: p3d_dumper.exe <file.p3d>\n";
        return 1;
    }

    std::cout << "--- Pure3D Hex Fingerprint & Structural Dumper ---\n";
    std::string filepath = argv[1];

    std::ifstream file(filepath, std::ios::binary | std::ios::ate);
    if (!file.is_open()) {
        std::cerr << "Failed to open file: " << filepath << "\n";
        return 1;
    }

    size_t fileSize = file.tellg();
    file.seekg(0, std::ios::beg);

    std::vector<uint8_t> data(fileSize);
    file.read(reinterpret_cast<char*>(data.data()), fileSize);

    std::cout << "Loaded " << filepath << " (" << fileSize << " bytes)\n\n";

    // Begin Structural Tree Dump
    WalkChunks(data, 0, fileSize, 0);

    return 0;
}
