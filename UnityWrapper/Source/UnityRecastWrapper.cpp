#include "UnityRecastWrapper.h"
#include "UnityNavMeshBuilder.h"
#include "UnityPathfinding.h"
#include <memory>
#include <cstring>

// 전역 인스턴스
static std::unique_ptr<UnityNavMeshBuilder> g_navMeshBuilder;
static std::unique_ptr<UnityPathfinding> g_pathfinding;
static bool g_initialized = false;

extern "C" {

UNITY_API bool UnityRecast_Initialize() {
    if (g_initialized) {
        return true;
    }
    
    try {
        g_navMeshBuilder = std::make_unique<UnityNavMeshBuilder>();
        g_pathfinding = std::make_unique<UnityPathfinding>();
        g_initialized = true;
        return true;
    }
    catch (...) {
        return false;
    }
}

UNITY_API void UnityRecast_Cleanup() {
    g_pathfinding.reset();
    g_navMeshBuilder.reset();
    g_initialized = false;
}

UNITY_API UnityNavMeshResult UnityRecast_BuildNavMesh(
    const UnityMeshData* meshData,
    const UnityNavMeshBuildSettings* settings
) {
    UnityNavMeshResult result = {0};
    
    if (!g_initialized) {
        result.success = false;
        result.errorMessage = const_cast<char*>("RecastNavigation not initialized");
        return result;
    }
    
    if (!meshData || !settings) {
        result.success = false;
        result.errorMessage = const_cast<char*>("Invalid parameters");
        return result;
    }
    
    try {
        result = g_navMeshBuilder->BuildNavMesh(meshData, settings);
        
        if (result.success) {
            // NavMesh가 성공적으로 빌드되면 Pathfinding에 설정
            g_pathfinding->SetNavMesh(
                g_navMeshBuilder->GetNavMesh(),
                g_navMeshBuilder->GetNavMeshQuery()
            );
        }
    }
    catch (const std::exception& e) {
        result.success = false;
        result.errorMessage = const_cast<char*>(e.what());
    }
    catch (...) {
        result.success = false;
        result.errorMessage = const_cast<char*>("Unknown error occurred");
    }
    
    return result;
}

UNITY_API void UnityRecast_FreeNavMeshData(UnityNavMeshResult* result) {
    if (result && result->navMeshData) {
        delete[] result->navMeshData;
        result->navMeshData = nullptr;
        result->dataSize = 0;
    }
    
    if (result && result->errorMessage) {
        delete[] result->errorMessage;
        result->errorMessage = nullptr;
    }
}

UNITY_API bool UnityRecast_LoadNavMesh(const unsigned char* data, int dataSize) {
    if (!g_initialized) {
        return false;
    }
    
    if (!data || dataSize <= 0) {
        return false;
    }
    
    try {
        bool success = g_navMeshBuilder->LoadNavMesh(data, dataSize);
        
        if (success) {
            // NavMesh가 성공적으로 로드되면 Pathfinding에 설정
            g_pathfinding->SetNavMesh(
                g_navMeshBuilder->GetNavMesh(),
                g_navMeshBuilder->GetNavMeshQuery()
            );
        }
        
        return success;
    }
    catch (...) {
        return false;
    }
}

UNITY_API UnityPathResult UnityRecast_FindPath(
    float startX, float startY, float startZ,
    float endX, float endY, float endZ
) {
    UnityPathResult result = {0};
    
    if (!g_initialized) {
        result.success = false;
        result.errorMessage = const_cast<char*>("RecastNavigation not initialized");
        return result;
    }
    
    try {
        result = g_pathfinding->FindPath(startX, startY, startZ, endX, endY, endZ);
    }
    catch (const std::exception& e) {
        result.success = false;
        result.errorMessage = const_cast<char*>(e.what());
    }
    catch (...) {
        result.success = false;
        result.errorMessage = const_cast<char*>("Unknown error occurred during pathfinding");
    }
    
    return result;
}

UNITY_API void UnityRecast_FreePathResult(UnityPathResult* result) {
    if (result && result->pathPoints) {
        delete[] result->pathPoints;
        result->pathPoints = nullptr;
        result->pointCount = 0;
    }
    
    if (result && result->errorMessage) {
        delete[] result->errorMessage;
        result->errorMessage = nullptr;
    }
}

UNITY_API int UnityRecast_GetPolyCount() {
    if (!g_initialized || !g_navMeshBuilder) {
        return 0;
    }
    
    return g_navMeshBuilder->GetPolyCount();
}

UNITY_API int UnityRecast_GetVertexCount() {
    if (!g_initialized || !g_navMeshBuilder) {
        return 0;
    }
    
    return g_navMeshBuilder->GetVertexCount();
}

// 디버그 기능들 (향후 구현)
UNITY_API void UnityRecast_SetDebugDraw(bool enabled) {
    // TODO: 디버그 드로잉 기능 구현
}

UNITY_API void UnityRecast_GetDebugVertices(float* vertices, int* vertexCount) {
    // TODO: 디버그 정점 정보 반환
    if (vertexCount) {
        *vertexCount = 0;
    }
}

UNITY_API void UnityRecast_GetDebugIndices(int* indices, int* indexCount) {
    // TODO: 디버그 인덱스 정보 반환
    if (indexCount) {
        *indexCount = 0;
    }
}

} // extern "C" 