#include <iostream>
#include <fstream>
#include <vector>
#include <iomanip>
#include "src/parser/P3DParser.h"
#include "src/parser/P3DParser.cpp"

void HexDump(const uint8_t* data, size_t size) {
    for (size_t i = 0; i < size; ++i) {
        std::cout << std::hex << std::setw(2) << std::setfill('0') << (int)data[i] << " ";
        if ((i + 1) % 16 == 0) std::cout << std::endl;
    }
    std::cout << std::dec << std::endl;
}

int main() {
    const char* filePath = "C:/Users/Adam/Desktop/eXTRACTED/adefault/default/art/cars/CrashStang/CrashStang.p3d";
    std::ifstream file(filePath, std::ios::binary | std::ios::ate);
    if (!file.is_open()) return 1;
    std::streamsize size = file.tellg();
    file.seekg(0, std::ios::beg);
    std::vector<uint8_t> buffer(size);
    file.read(reinterpret_cast<char*>(buffer.data()), size);

    CTTR::Parser::ChunkReader reader(buffer);
    while (reader.HasRemaining()) {
        uint32_t id = reader.Read<uint32_t>();
        uint32_t sz = reader.Read<uint32_t>();
        if (id == 0x23001) { // SkeletonJoint2
            std::cout << "--- SkeletonJoint2 Hex Dump (Size " << sz << ") ---" << std::endl;
            HexDump(buffer.data() + reader.Tell() - 8, sz);
            break; 
        }
        if (sz < 8) break;
        reader.Skip(sz - 8);
    }
    return 0;
}
