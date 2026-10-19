#pragma once
#include <string>
#include <vector>
#include <map>
#include <memory>
#include <variant>
#include <glm/glm.hpp>

namespace CTTR {
namespace Asset {

    using GodValue = std::variant<
        std::monostate,
        bool,
        int64_t,
        double,
        std::string,
        glm::vec3,
        glm::vec4
    >;

    struct GodMethodCall {
        std::string methodName;
        std::vector<GodValue> arguments;
    };

    class GodObject {
    public:
        GodObject(const std::string& typeName = "", const std::string& objectName = "")
            : m_typeName(typeName), m_objectName(objectName) {}

        const std::string& GetTypeName() const { return m_typeName; }
        const std::string& GetObjectName() const { return m_objectName; }

        void SetTypeName(const std::string& typeName) { m_typeName = typeName; }
        void SetObjectName(const std::string& objectName) { m_objectName = objectName; }

        void SetProperty(const std::string& key, const GodValue& value) {
            m_properties[key] = value;
        }

        bool HasProperty(const std::string& key) const {
            return m_properties.find(key) != m_properties.end();
        }

        GodValue GetProperty(const std::string& key) const {
            auto it = m_properties.find(key);
            if (it != m_properties.end()) return it->second;
            return std::monostate{};
        }

        template<typename T>
        T GetPropertyAs(const std::string& key, const T& defaultValue = T()) const {
            auto it = m_properties.find(key);
            if (it != m_properties.end()) {
                if (std::holds_alternative<T>(it->second)) {
                    return std::get<T>(it->second);
                }
            }
            return defaultValue;
        }

        void AddMethodCall(const std::string& methodName, const std::vector<GodValue>& args) {
            m_methodCalls.push_back({methodName, args});
            DispatchMethod(methodName, args);
        }

        const std::vector<GodMethodCall>& GetMethodCalls() const { return m_methodCalls; }
        const std::map<std::string, GodValue>& GetProperties() const { return m_properties; }

        // Common standard properties directly accessible
        glm::vec3 GetPosition() const { return m_position; }
        glm::vec4 GetQuaternion() const { return m_quaternion; }
        const std::string& GetLoadSetName() const { return m_loadSetName; }
        bool IsEnabled() const { return m_enabled; }

    private:
        void DispatchMethod(const std::string& methodName, const std::vector<GodValue>& args);

        std::string m_typeName;
        std::string m_objectName;
        std::string m_loadSetName;
        bool m_enabled = true;
        glm::vec3 m_position{0.0f};
        glm::vec4 m_quaternion{0.0f, 0.0f, 0.0f, 1.0f};
        std::map<std::string, GodValue> m_properties;
        std::vector<GodMethodCall> m_methodCalls;
    };

    struct GodScript {
        std::string sourcePath;
        std::vector<std::shared_ptr<GodObject>> objects;
        std::map<std::string, std::shared_ptr<GodObject>> objectsByName;

        std::shared_ptr<GodObject> FindObject(const std::string& name) const {
            auto it = objectsByName.find(name);
            if (it != objectsByName.end()) return it->second;
            return nullptr;
        }
    };

    class GodParser {
    public:
        GodParser() = default;

        // Parse from file on disk
        bool ParseFile(const std::string& filePath, GodScript& outScript);

        // Parse from raw string/buffer
        bool ParseString(const std::string& scriptText, GodScript& outScript, const std::string& sourceName = "inline");

        // Helper: extract theme string e.g. "fairy1" -> "fairy", "dino2" -> "dino"
        static std::string ExtractTheme(const std::string& levelName);

        const std::string& GetLastError() const { return m_lastError; }

    private:
        std::string m_lastError;
    };

} // namespace Asset
} // namespace CTTR
