#include "RecastNavigationUnity.h"
#include "UnityNavMeshBuilder.h"
#include "UnityNavMeshQuery.h"
#include "UnityCrowdManager.h"
#include <memory>
#include <unordered_map>

// Global managers
static std::unique_ptr<UnityNavMeshBuilder> g_navMeshBuilder;
static std::unique_ptr<UnityNavMeshQuery> g_navMeshQuery;
static std::unique_ptr<UnityCrowdManager> g_crowdManager;

// Handle management
static std::unordered_map<NavMeshHandle, std::shared_ptr<dtNavMesh>> g_navMeshes;
static std::unordered_map<NavMeshQueryHandle, std::shared_ptr<dtNavMeshQuery>> g_navMeshQueries;
static std::unordered_map<CrowdHandle, std::shared_ptr<dtCrowd>> g_crowds;

extern "C" {

RECASTNAVIGATIONUNITY_API int InitializeRecastNavigation() {
    try {
        g_navMeshBuilder = std::make_unique<UnityNavMeshBuilder>();
        g_navMeshQuery = std::make_unique<UnityNavMeshQuery>();
        g_crowdManager = std::make_unique<UnityCrowdManager>();
        return 1; // Success
    }
    catch (...) {
        return 0; // Failure
    }
}

RECASTNAVIGATIONUNITY_API void CleanupRecastNavigation() {
    g_crowds.clear();
    g_navMeshQueries.clear();
    g_navMeshes.clear();
    g_crowdManager.reset();
    g_navMeshQuery.reset();
    g_navMeshBuilder.reset();
}

RECASTNAVIGATIONUNITY_API NavMeshHandle BuildNavMesh(
    UnityVector3* vertices, int vertexCount,
    int* indices, int indexCount,
    BuildSettings* settings) {
    
    if (!g_navMeshBuilder || !vertices || !indices || !settings) {
        return nullptr;
    }

    try {
        auto navMesh = g_navMeshBuilder->BuildNavMesh(vertices, vertexCount, indices, indexCount, settings);
        if (navMesh) {
            g_navMeshes[navMesh.get()] = navMesh;
            return navMesh.get();
        }
    }
    catch (...) {
        // Handle error
    }
    
    return nullptr;
}

RECASTNAVIGATIONUNITY_API NavMeshHandle BuildNavMeshFromHeightfield(
    float* heightfield, int width, int height,
    float originX, float originY, float originZ,
    float cellSize, float cellHeight,
    BuildSettings* settings) {
    
    if (!g_navMeshBuilder || !heightfield || !settings) {
        return nullptr;
    }

    try {
        auto navMesh = g_navMeshBuilder->BuildNavMeshFromHeightfield(
            heightfield, width, height,
            originX, originY, originZ,
            cellSize, cellHeight, settings);
        
        if (navMesh) {
            g_navMeshes[navMesh.get()] = navMesh;
            return navMesh.get();
        }
    }
    catch (...) {
        // Handle error
    }
    
    return nullptr;
}

RECASTNAVIGATIONUNITY_API void DestroyNavMesh(NavMeshHandle navMesh) {
    if (navMesh && g_navMeshes.find(navMesh) != g_navMeshes.end()) {
        g_navMeshes.erase(navMesh);
    }
}

RECASTNAVIGATIONUNITY_API NavMeshQueryHandle CreateNavMeshQuery(NavMeshHandle navMesh, int maxNodes) {
    if (!g_navMeshQuery || !navMesh) {
        return nullptr;
    }

    try {
        auto query = g_navMeshQuery->CreateQuery(navMesh, maxNodes);
        if (query) {
            g_navMeshQueries[query.get()] = query;
            return query.get();
        }
    }
    catch (...) {
        // Handle error
    }
    
    return nullptr;
}

RECASTNAVIGATIONUNITY_API void DestroyNavMeshQuery(NavMeshQueryHandle query) {
    if (query && g_navMeshQueries.find(query) != g_navMeshQueries.end()) {
        g_navMeshQueries.erase(query);
    }
}

RECASTNAVIGATIONUNITY_API PathResult FindPath(
    NavMeshQueryHandle query,
    UnityVector3 startPos,
    UnityVector3 endPos,
    QueryFilter* filter) {
    
    PathResult result = { nullptr, 0, 0 };
    
    if (!g_navMeshQuery || !query) {
        return result;
    }

    try {
        result = g_navMeshQuery->FindPath(query, startPos, endPos, filter);
    }
    catch (...) {
        // Handle error
    }
    
    return result;
}

RECASTNAVIGATIONUNITY_API UnityVector3 GetClosestPoint(
    NavMeshQueryHandle query,
    UnityVector3 position,
    QueryFilter* filter) {
    
    UnityVector3 result = { 0, 0, 0 };
    
    if (!g_navMeshQuery || !query) {
        return result;
    }

    try {
        result = g_navMeshQuery->GetClosestPoint(query, position, filter);
    }
    catch (...) {
        // Handle error
    }
    
    return result;
}

RECASTNAVIGATIONUNITY_API int Raycast(
    NavMeshQueryHandle query,
    UnityVector3 startPos,
    UnityVector3 endPos,
    QueryFilter* filter,
    UnityVector3* hitPos,
    UnityVector3* hitNormal) {
    
    if (!g_navMeshQuery || !query) {
        return 0;
    }

    try {
        return g_navMeshQuery->Raycast(query, startPos, endPos, filter, hitPos, hitNormal);
    }
    catch (...) {
        // Handle error
    }
    
    return 0;
}

RECASTNAVIGATIONUNITY_API CrowdHandle CreateCrowd(NavMeshHandle navMesh, int maxAgents, float maxAgentRadius) {
    if (!g_crowdManager || !navMesh) {
        return nullptr;
    }

    try {
        auto crowd = g_crowdManager->CreateCrowd(navMesh, maxAgents, maxAgentRadius);
        if (crowd) {
            g_crowds[crowd.get()] = crowd;
            return crowd.get();
        }
    }
    catch (...) {
        // Handle error
    }
    
    return nullptr;
}

RECASTNAVIGATIONUNITY_API void DestroyCrowd(CrowdHandle crowd) {
    if (crowd && g_crowds.find(crowd) != g_crowds.end()) {
        g_crowds.erase(crowd);
    }
}

RECASTNAVIGATIONUNITY_API AgentHandle AddAgent(
    CrowdHandle crowd,
    UnityVector3 position,
    AgentParams* params) {
    
    if (!g_crowdManager || !crowd || !params) {
        return -1;
    }

    try {
        return g_crowdManager->AddAgent(crowd, position, params);
    }
    catch (...) {
        // Handle error
    }
    
    return -1;
}

RECASTNAVIGATIONUNITY_API void RemoveAgent(CrowdHandle crowd, AgentHandle agent) {
    if (!g_crowdManager || !crowd) {
        return;
    }

    try {
        g_crowdManager->RemoveAgent(crowd, agent);
    }
    catch (...) {
        // Handle error
    }
}

RECASTNAVIGATIONUNITY_API int SetAgentTarget(
    CrowdHandle crowd,
    AgentHandle agent,
    UnityVector3 target) {
    
    if (!g_crowdManager || !crowd) {
        return 0;
    }

    try {
        return g_crowdManager->SetAgentTarget(crowd, agent, target);
    }
    catch (...) {
        // Handle error
    }
    
    return 0;
}

RECASTNAVIGATIONUNITY_API UnityVector3 GetAgentPosition(CrowdHandle crowd, AgentHandle agent) {
    UnityVector3 result = { 0, 0, 0 };
    
    if (!g_crowdManager || !crowd) {
        return result;
    }

    try {
        result = g_crowdManager->GetAgentPosition(crowd, agent);
    }
    catch (...) {
        // Handle error
    }
    
    return result;
}

RECASTNAVIGATIONUNITY_API UnityVector3 GetAgentVelocity(CrowdHandle crowd, AgentHandle agent) {
    UnityVector3 result = { 0, 0, 0 };
    
    if (!g_crowdManager || !crowd) {
        return result;
    }

    try {
        result = g_crowdManager->GetAgentVelocity(crowd, agent);
    }
    catch (...) {
        // Handle error
    }
    
    return result;
}

RECASTNAVIGATIONUNITY_API void UpdateCrowd(CrowdHandle crowd, float deltaTime) {
    if (!g_crowdManager || !crowd) {
        return;
    }

    try {
        g_crowdManager->UpdateCrowd(crowd, deltaTime);
    }
    catch (...) {
        // Handle error
    }
}

RECASTNAVIGATIONUNITY_API void FreePathResult(PathResult* result) {
    if (result && result->path) {
        delete[] result->path;
        result->path = nullptr;
        result->pathLength = 0;
    }
}

} 