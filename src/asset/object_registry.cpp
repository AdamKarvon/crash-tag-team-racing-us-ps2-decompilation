#include "object_registry.h"
#include <iostream>
#include <filesystem>
#include <algorithm>
#include <cctype>

namespace CTTR {
namespace Asset {

    ObjectRegistry& ObjectRegistry::Get() {
        static ObjectRegistry instance;
        return instance;
    }

    std::shared_ptr<GodObject> ObjectRegistry::CreateObject(const std::string& typeName, const std::string& objectName) {
        std::lock_guard<std::mutex> lock(m_mutex);
        auto obj = std::make_shared<GodObject>(typeName, objectName);
        m_objects[objectName] = obj;
        return obj;
    }

    bool ObjectRegistry::DestroyObject(const std::string& objectName) {
        std::lock_guard<std::mutex> lock(m_mutex);
        auto it = m_objects.find(objectName);
        if (it != m_objects.end()) {
            m_objects.erase(it);
            return true;
        }
        return false;
    }

    std::shared_ptr<GodObject> ObjectRegistry::FindObject(const std::string& objectName) const {
        std::lock_guard<std::mutex> lock(m_mutex);
        auto it = m_objects.find(objectName);
        if (it != m_objects.end()) return it->second;

        // Try searching without path prefix (e.g. "PermanentObjects/RaceMotif" vs "RaceMotif")
        size_t slash = objectName.find_last_of("/\\");
        if (slash != std::string::npos) {
            std::string leaf = objectName.substr(slash + 1);
            it = m_objects.find(leaf);
            if (it != m_objects.end()) return it->second;
        }

        return nullptr;
    }

    std::shared_ptr<GodObject> ObjectRegistry::FindObject(const std::string& typeName, const std::string& objectName) const {
        auto obj = FindObject(objectName);
        if (obj && (typeName.empty() || obj->GetTypeName() == typeName)) {
            return obj;
        }
        return nullptr;
    }

    bool ObjectRegistry::ObjectExists(const std::string& objectName) const {
        return FindObject(objectName) != nullptr;
    }

    bool ObjectRegistry::LoadObjects(const std::string& filePath, const std::string& loadSetName, const std::string& inventoryGroup) {
        std::lock_guard<std::mutex> lock(m_mutex);

        LoadSetInfo info;
        info.loadSetName = loadSetName;
        info.filePath = filePath;
        info.inventoryGroup = inventoryGroup;
        info.isLoaded = true;

        // If it's a .god file, parse it and register its objects
        std::string lowerPath = filePath;
        std::transform(lowerPath.begin(), lowerPath.end(), lowerPath.begin(), ::tolower);

        if (lowerPath.find(".god") != std::string::npos) {
            // Check if file exists in game_data/ or direct path
            std::string resolvedPath = filePath;
            if (!std::filesystem::exists(resolvedPath)) {
                resolvedPath = "game_data/" + filePath;
            }

            if (std::filesystem::exists(resolvedPath)) {
                GodParser parser;
                GodScript script;
                if (parser.ParseFile(resolvedPath, script)) {
                    for (const auto& obj : script.objects) {
                        m_objects[obj->GetObjectName()] = obj;
                        info.registeredObjectNames.push_back(obj->GetObjectName());
                    }
                }
            }
        }

        m_loadSets[loadSetName] = info;
        return true;
    }

    bool ObjectRegistry::UnloadObjects(const std::string& loadSetName) {
        std::lock_guard<std::mutex> lock(m_mutex);
        auto it = m_loadSets.find(loadSetName);
        if (it != m_loadSets.end()) {
            for (const auto& name : it->second.registeredObjectNames) {
                m_objects.erase(name);
            }
            it->second.isLoaded = false;
            return true;
        }
        return false;
    }

    bool ObjectRegistry::AreObjectsLoaded(const std::string& loadSetName) const {
        std::lock_guard<std::mutex> lock(m_mutex);
        auto it = m_loadSets.find(loadSetName);
        if (it != m_loadSets.end()) {
            return it->second.isLoaded;
        }
        return false;
    }

    std::vector<std::string> ObjectRegistry::GetLoadedLoadSets() const {
        std::lock_guard<std::mutex> lock(m_mutex);
        std::vector<std::string> sets;
        for (const auto& pair : m_loadSets) {
            if (pair.second.isLoaded) {
                sets.push_back(pair.first);
            }
        }
        return sets;
    }

    std::vector<std::shared_ptr<GodObject>> ObjectRegistry::GetAllObjects() const {
        std::lock_guard<std::mutex> lock(m_mutex);
        std::vector<std::shared_ptr<GodObject>> list;
        list.reserve(m_objects.size());
        for (const auto& pair : m_objects) {
            list.push_back(pair.second);
        }
        return list;
    }

    std::vector<std::shared_ptr<GodObject>> ObjectRegistry::GetObjectsByType(const std::string& typeName) const {
        std::lock_guard<std::mutex> lock(m_mutex);
        std::vector<std::shared_ptr<GodObject>> list;
        for (const auto& pair : m_objects) {
            if (pair.second->GetTypeName() == typeName) {
                list.push_back(pair.second);
            }
        }
        return list;
    }

    void ObjectRegistry::Reset() {
        std::lock_guard<std::mutex> lock(m_mutex);
        m_objects.clear();
        m_loadSets.clear();
    }

} // namespace Asset
} // namespace CTTR
