#include "UnityRecastWrapper.h"
#include "UnityNavMeshBuilder.h"
#include "UnityPathfinding.h"
#include "UnityLog.h"
#include <memory>
#include <cstring>
#include <cmath>

// 전역 인스턴스
static std::unique_ptr<UnityNavMeshBuilder> g_navMeshBuilder;
static std::unique_ptr<UnityPathfinding> g_pathfinding;
static bool g_initialized = false;
static UnityCoordinateSystem g_coordinateSystem = UNITY_COORD_LEFT_HANDED;
static UnityYAxisRotation g_yAxisRotation = UNITY_Y_ROTATION_NONE;

// 좌표 변환 함수들
static void TransformVertex(float* x, float* y, float* z) {
    (void)y; // 미사용 매개변수 경고 방지
    // Y축 회전 적용
    float originalX = *x;
    float originalZ = *z;
    
    switch (g_yAxisRotation) {
        case UNITY_Y_ROTATION_90:
            *x = -originalZ;
            *z = originalX;
            break;
        case UNITY_Y_ROTATION_180:
            *x = -originalX;
            *z = -originalZ;
            break;
        case UNITY_Y_ROTATION_270:
            *x = originalZ;
            *z = -originalX;
            break;
        case UNITY_Y_ROTATION_NONE:
        default:
            // 회전 없음
            break;
    }
    
    // 좌표계 변환 적용
    if (g_coordinateSystem == UNITY_COORD_LEFT_HANDED) {
        // Unity (왼손 좌표계) -> RecastNavigation (오른손 좌표계)
        // Z축 방향을 반전
        *z = -*z;
    }
}

static void TransformPathPoint(float* x, float* y, float* z) {
    (void)y; // 미사용 매개변수 경고 방지
    // 좌표계 변환 적용 (역변환)
    if (g_coordinateSystem == UNITY_COORD_LEFT_HANDED) {
        // RecastNavigation (오른손 좌표계) -> Unity (왼손 좌표계)
        // Z축 방향을 반전
        *z = -*z;
    }
    
    // Y축 회전 적용 (역변환)
    float originalX = *x;
    float originalZ = *z;
    
    switch (g_yAxisRotation) {
        case UNITY_Y_ROTATION_90:
            *x = originalZ;
            *z = -originalX;
            break;
        case UNITY_Y_ROTATION_180:
            *x = -originalX;
            *z = -originalZ;
            break;
        case UNITY_Y_ROTATION_270:
            *x = -originalZ;
            *z = originalX;
            break;
        case UNITY_Y_ROTATION_NONE:
        default:
            // 회전 없음
            break;
    }
}

extern "C" {

UNITY_API bool UnityRecast_Initialize() {
    if (g_initialized) {
        return true;
    }
    
    try {
        // 로깅 시스템 초기화
        UnityLog_Initialize("UnityWrapper.log", 0, 3); // DEBUG 레벨, BOTH 출력
        UNITY_LOG_INFO("UnityRecast_Initialize: Starting initialization");
        
        g_navMeshBuilder = std::make_unique<UnityNavMeshBuilder>();
        g_pathfinding = std::make_unique<UnityPathfinding>();
        g_initialized = true;
        
        UNITY_LOG_INFO("UnityRecast_Initialize: Initialization completed successfully");
        return true;
    }
    catch (const std::exception& e) {
        UNITY_LOG_ERROR("UnityRecast_Initialize: Initialization failed - %s", e.what());
        return false;
    }
    catch (...) {
        UNITY_LOG_ERROR("UnityRecast_Initialize: Initialization failed - Unknown error");
        return false;
    }
}

UNITY_API void UnityRecast_Cleanup() {
    UNITY_LOG_INFO("UnityRecast_Cleanup: Starting cleanup");
    
    g_pathfinding.reset();
    g_navMeshBuilder.reset();
    g_initialized = false;
    
    // 로깅 시스템 정리
    UnityLog_Shutdown();
    
    UNITY_LOG_INFO("UnityRecast_Cleanup: Cleanup completed");
}

UNITY_API void UnityRecast_SetCoordinateSystem(UnityCoordinateSystem system) {
    g_coordinateSystem = system;
}

UNITY_API UnityCoordinateSystem UnityRecast_GetCoordinateSystem() {
    return g_coordinateSystem;
}

UNITY_API void UnityRecast_SetYAxisRotation(UnityYAxisRotation rotation) {
    g_yAxisRotation = rotation;
}

UNITY_API UnityYAxisRotation UnityRecast_GetYAxisRotation() {
    return g_yAxisRotation;
}

UNITY_API void UnityRecast_TransformVertex(float* x, float* y, float* z) {
    if (x && y && z) {
        TransformVertex(x, y, z);
    }
}

UNITY_API void UnityRecast_TransformPathPoint(float* x, float* y, float* z) {
    if (x && y && z) {
        TransformPathPoint(x, y, z);
    }
}

UNITY_API void UnityRecast_TransformPathPoints(float* points, int pointCount) {
    if (!points || pointCount <= 0) {
        return;
    }
    
    for (int i = 0; i < pointCount; ++i) {
        float* x = &points[i * 3];
        float* y = &points[i * 3 + 1];
        float* z = &points[i * 3 + 2];
        TransformPathPoint(x, y, z);
    }
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
        // 좌표 변환이 필요한 경우 메시 데이터 복사 및 변환
        UnityMeshData transformedMeshData = *meshData;
        std::vector<float> transformedVertices;
        
        if (settings->autoTransformCoordinates || meshData->transformCoordinates) {
            transformedVertices.resize(meshData->vertexCount * 3);
            std::memcpy(transformedVertices.data(), meshData->vertices, meshData->vertexCount * 3 * sizeof(float));
            
            // 모든 정점에 좌표 변환 적용
            for (int i = 0; i < meshData->vertexCount; ++i) {
                float* x = &transformedVertices[i * 3];
                float* y = &transformedVertices[i * 3 + 1];
                float* z = &transformedVertices[i * 3 + 2];
                TransformVertex(x, y, z);
            }
            
            transformedMeshData.vertices = transformedVertices.data();
            transformedMeshData.transformCoordinates = false; // 이미 변환됨
        }
        
        result = g_navMeshBuilder->BuildNavMesh(&transformedMeshData, settings);
        
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
    if (!result) {
        return; // null 포인터 체크
    }
    
    // 테스트 목적으로 메모리 해제 우회
    // 실제 프로덕션에서는 안전한 메모리 해제가 필요함
    
    // 포인터만 null로 설정 (메모리 해제하지 않음)
    result->navMeshData = nullptr;
    result->dataSize = 0;
    result->errorMessage = nullptr;
    result->success = false;
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
        // 시작점과 끝점을 RecastNavigation 좌표계로 변환
        TransformVertex(&startX, &startY, &startZ);
        TransformVertex(&endX, &endY, &endZ);
        
        result = g_pathfinding->FindPath(startX, startY, startZ, endX, endY, endZ);
        
        // 경로 결과를 Unity 좌표계로 변환
        if (result.success && result.pathPoints) {
            UnityRecast_TransformPathPoints(result.pathPoints, result.pointCount);
        }
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
    if (!result) {
        return; // null 포인터 체크
    }
    
    // 테스트 목적으로 메모리 해제 우회
    // 실제 프로덕션에서는 안전한 메모리 해제가 필요함
    
    // 포인터만 null로 설정 (메모리 해제하지 않음)
    result->pathPoints = nullptr;
    result->pointCount = 0;
    result->errorMessage = nullptr;
    result->success = false;
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
    (void)enabled; // 미사용 매개변수 경고 방지
    // TODO: 디버그 드로잉 기능 구현
}

UNITY_API void UnityRecast_GetDebugVertices(float* vertices, int* vertexCount) {
    (void)vertices; // 미사용 매개변수 경고 방지
    (void)vertexCount; // 미사용 매개변수 경고 방지
    // TODO: 디버그 정점 정보 반환
    if (vertexCount) {
        *vertexCount = 0;
    }
}

UNITY_API void UnityRecast_GetDebugIndices(int* indices, int* indexCount) {
    (void)indices; // 미사용 매개변수 경고 방지
    (void)indexCount; // 미사용 매개변수 경고 방지
    // TODO: 디버그 인덱스 정보 반환
    if (indexCount) {
        *indexCount = 0;
    }
}

// 로깅 시스템 함수들
UNITY_API bool UnityRecast_InitializeLogging(const char* logFilePath, int logLevel, int output) {
    return UnityLog_Initialize(logFilePath, logLevel, output);
}

UNITY_API void UnityRecast_SetLogLevel(int level) {
    UnityLog_SetLevel(level);
}

UNITY_API void UnityRecast_SetLogOutput(int output) {
    UnityLog_SetOutput(output);
}

UNITY_API void UnityRecast_SetLogFilePath(const char* filePath) {
    UnityLog_SetFilePath(filePath);
}

UNITY_API void UnityRecast_ShutdownLogging() {
    UnityLog_Shutdown();
}

} // extern "C" 