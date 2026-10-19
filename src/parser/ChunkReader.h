#pragma once
#include <vector>
#include <cstdint>
#include <string>
#include <stdexcept>
#include <cstring>

namespace CTTR {
namespace Parser {

    struct P3DChunkHeader {
        uint32_t type;
        uint32_t size;
    };

    class ChunkReader {
    public:
        ChunkReader(const std::vector<uint8_t>& data) 
            : m_data(data), m_offset(0) {}

        // Reads a primitive type (handles endianness assumptions - assume little endian for PS2 data files initially, but easily swappable here)
        template<typename T>
        T Read() {
            if (m_offset + sizeof(T) > m_data.size()) {
                throw std::runtime_error("ChunkReader out of bounds read.");
            }
            T val = *reinterpret_cast<const T*>(m_data.data() + m_offset);
            m_offset += sizeof(T);
            return val;
        }

        std::string ReadString(size_t length) {
            if (m_offset + length > m_data.size()) {
                throw std::runtime_error("ChunkReader out of bounds string read.");
            }
            std::string str(reinterpret_cast<const char*>(m_data.data() + m_offset), length);
            m_offset += length;
            // Trim null terminators if present in fixed-size buffers
            size_t nullPos = str.find('\0');
            if (nullPos != std::string::npos) str = str.substr(0, nullPos);
            return str;
        }

        // Radical P3D strings are 1-byte length prefixed and the 
        // entire entry (length + string + padding) is aligned to 4 bytes.
        std::string ReadP3DString() {
            uint8_t paddedLength = Read<uint8_t>();
            std::string str = ReadString(paddedLength);
            return str;
        }

        void ReadBytes(void* dest, size_t length) {
            if (m_offset + length > m_data.size()) {
                throw std::runtime_error("ChunkReader out of bounds byte read.");
            }
            std::memcpy(dest, m_data.data() + m_offset, length);
            m_offset += length;
        }

        void Seek(size_t offset) {
            if (offset > m_data.size()) {
                throw std::runtime_error("ChunkReader seek out of bounds.");
            }
            m_offset = offset;
        }
        void Skip(size_t bytes) { Seek(m_offset + bytes); }
        size_t Tell() const { return m_offset; }
        bool HasRemaining() const { return m_offset < m_data.size(); }

    private:
        const std::vector<uint8_t>& m_data;
        size_t m_offset;
    };

} // namespace Parser
} // namespace CTTR
