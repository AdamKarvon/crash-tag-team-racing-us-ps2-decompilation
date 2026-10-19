#pragma once
#include "god_parser.h"
#include <string>
#include <map>
#include <vector>
#include <memory>
#include <mutex>

namespace CTTR {
namespace Asset {

    struct LoadSetInfo {
        std::string loadSetName;
        std::string filePath;
        std::string inventoryGroup;
        std::vector<std::string> registeredObjectNames;
        bool isLoaded = false;
    };

    class ObjectRegistry {
    public:
        static ObjectRegistry& Get();

        // Object lifecycle
        std::shared_ptr<GodObject> CreateObject(const std::string& typeName, const std::string& objectName);
        bool DestroyObject(const std::string& objectName);
        std::shared_ptr<GodObject> FindObject(const std::string& objectName) const;
        std::shared_ptr<GodObject> FindObject(const std::string& typeName, const std::string& objectName) const;
        bool ObjectExists(const std::string& objectName) const;

        // Named Load Sets and Inventory Groups
        bool LoadObjects(const std::string& filePath, const std::string& loadSetName, const std::string& inventoryGroup = "Default");
        bool UnloadObjects(const std::string& loadSetName);
        bool AreObjectsLoaded(const std::string& loadSetName) const;
        std::vector<std::string> GetLoadedLoadSets() const;

        // Theme extraction helper: "fairy1" -> "fairy"
        static std::string ExtractTheme(const std::string& levelName) {
            return GodParser::ExtractTheme(levelName);
        }

        // Registry inspection
        size_t GetObjectCount() const { return m_objects.size(); }
        std::vector<std::shared_ptr<GodObject>> GetAllObjects() const;
        std::vector<std::shared_ptr<GodObject>> GetObjectsByType(const std::string& typeName) const;

        // Clear everything
        void Reset();

    private:
        ObjectRegistry() = default;
        ~ObjectRegistry() = default;
        ObjectRegistry(const ObjectRegistry&) = delete;
        ObjectRegistry& operator=(const ObjectRegistry&) = delete;

        mutable std::mutex m_mutex;
        std::map<std::string, std::shared_ptr<GodObject>> m_objects;
        std::map<std::string, LoadSetInfo> m_loadSets;
    };

} // namespace Asset
} // namespace CTTR
