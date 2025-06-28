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
    UNITY_LOG_INFO("=== UnityRecast_BuildNavMesh Start ===");
    UnityNavMeshResult result = {0};
    
    // 1. Check initialization status
    UNITY_LOG_INFO("1. Checking initialization status...");
    if (!g_initialized) {
        UNITY_LOG_ERROR("RecastNavigation not initialized!");
        result.success = false;
        result.errorMessage = const_cast<char*>("RecastNavigation not initialized");
        return result;
    }
    UNITY_LOG_INFO("Initialization status: OK");
    
    // 2. Validate parameters
    UNITY_LOG_INFO("2. Validating parameters...");
    if (!meshData || !settings) {
        UNITY_LOG_ERROR("Invalid parameters! meshData=%p, settings=%p", meshData, settings);
        result.success = false;
        result.errorMessage = const_cast<char*>("Invalid parameters");
        return result;
    }
    
    UNITY_LOG_INFO("Mesh data: vertexCount=%d, indexCount=%d", meshData->vertexCount, meshData->indexCount);
    UNITY_LOG_INFO("Build settings: cellSize=%.3f, cellHeight=%.3f", settings->cellSize, settings->cellHeight);
    UNITY_LOG_INFO("Build settings: walkableHeight=%.3f, walkableRadius=%.3f", settings->walkableHeight, settings->walkableRadius);
    UNITY_LOG_INFO("Coordinate transform: autoTransform=%s, meshTransform=%s", 
                   settings->autoTransformCoordinates ? "true" : "false",
                   meshData->transformCoordinates ? "true" : "false");
    
    try {
        // 3. Process coordinate transformation
        UNITY_LOG_INFO("3. Processing coordinate transformation...");
        UnityMeshData transformedMeshData = *meshData;
        std::vector<float> transformedVertices;
        
        if (settings->autoTransformCoordinates || meshData->transformCoordinates) {
            UNITY_LOG_INFO("Applying coordinate transformation...");
            transformedVertices.resize(meshData->vertexCount * 3);
            std::memcpy(transformedVertices.data(), meshData->vertices, meshData->vertexCount * 3 * sizeof(float));
            
            // Apply coordinate transformation to all vertices
            for (int i = 0; i < meshData->vertexCount; ++i) {
                float* x = &transformedVertices[i * 3];
                float* y = &transformedVertices[i * 3 + 1];
                float* z = &transformedVertices[i * 3 + 2];
                TransformVertex(x, y, z);
            }
            
            transformedMeshData.vertices = transformedVertices.data();
            transformedMeshData.transformCoordinates = false; // Already transformed
            UNITY_LOG_INFO("Coordinate transformation completed");
        }
        else {
            UNITY_LOG_INFO("Skipping coordinate transformation");
        }
        
        // 4. Execute NavMesh build
        UNITY_LOG_INFO("4. Executing NavMesh build...");
        result = g_navMeshBuilder->BuildNavMesh(&transformedMeshData, settings);
        
        if (result.success) {
            UNITY_LOG_INFO("NavMesh build successful! Data size: %d bytes", result.dataSize);
            
            // 5. Setup Pathfinding
            UNITY_LOG_INFO("5. Setting up Pathfinding...");
            g_pathfinding->SetNavMesh(
                g_navMeshBuilder->GetNavMesh(),
                g_navMeshBuilder->GetNavMeshQuery()
            );
            UNITY_LOG_INFO("Pathfinding setup completed");
            UNITY_LOG_INFO("=== UnityRecast_BuildNavMesh Completed ===");
        }
        else {
            UNITY_LOG_ERROR("NavMesh build failed: %s", result.errorMessage ? result.errorMessage : "Unknown error");
        }
    }
    catch (const std::exception& e) {
        UNITY_LOG_ERROR("Exception during NavMesh build: %s", e.what());
        result.success = false;
        result.errorMessage = const_cast<char*>(e.what());
    }
    catch (...) {
        UNITY_LOG_ERROR("Unknown exception during NavMesh build");
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
    UNITY_LOG_INFO("=== UnityRecast_LoadNavMesh Start ===");
    
    // 1. Check initialization status
    UNITY_LOG_INFO("1. Checking initialization status...");
    if (!g_initialized) {
        UNITY_LOG_ERROR("RecastNavigation not initialized!");
        return false;
    }
    UNITY_LOG_INFO("Initialization status: OK");
    
    // 2. Validate parameters
    UNITY_LOG_INFO("2. Validating parameters...");
    if (!data || dataSize <= 0) {
        UNITY_LOG_ERROR("Invalid parameters! data=%p, dataSize=%d", data, dataSize);
        return false;
    }
    UNITY_LOG_INFO("NavMesh data: %d bytes", dataSize);
    
    try {
        // 3. Attempt NavMesh load
        UNITY_LOG_INFO("3. Attempting NavMesh load...");
        bool success = g_navMeshBuilder->LoadNavMesh(data, dataSize);
        
        if (success) {
            UNITY_LOG_INFO("NavMesh load successful!");
            
            // 4. Setup Pathfinding
            UNITY_LOG_INFO("4. Setting up Pathfinding...");
            g_pathfinding->SetNavMesh(
                g_navMeshBuilder->GetNavMesh(),
                g_navMeshBuilder->GetNavMeshQuery()
            );
            UNITY_LOG_INFO("Pathfinding setup completed");
            
            // 5. Output loaded NavMesh information
            int polyCount = g_navMeshBuilder->GetPolyCount();
            int vertCount = g_navMeshBuilder->GetVertexCount();
            UNITY_LOG_INFO("Loaded NavMesh info: polygons=%d, vertices=%d", polyCount, vertCount);
            
            UNITY_LOG_INFO("=== UnityRecast_LoadNavMesh Completed ===");
        }
        else {
            UNITY_LOG_ERROR("NavMesh load failed! LoadNavMesh function returned false.");
        }
        
        return success;
    }
    catch (const std::exception& e) {
        UNITY_LOG_ERROR("Exception during NavMesh load: %s", e.what());
        return false;
    }
    catch (...) {
        UNITY_LOG_ERROR("Unknown exception during NavMesh load");
        return false;
    }
}

UNITY_API UnityPathResult UnityRecast_FindPath(
    float startX, float startY, float startZ,
    float endX, float endY, float endZ
) {
    UNITY_LOG_INFO("=== UnityRecast_FindPath Start ===");
    UnityPathResult result = {0};
    
    // 1. Check initialization status
    UNITY_LOG_INFO("1. Checking initialization status...");
    if (!g_initialized) {
        UNITY_LOG_ERROR("RecastNavigation not initialized!");
        result.success = false;
        result.errorMessage = const_cast<char*>("RecastNavigation not initialized");
        return result;
    }
    UNITY_LOG_INFO("Initialization status: OK");
    
    // 2. Log input coordinates
    UNITY_LOG_INFO("2. Input coordinates:");
    UNITY_LOG_INFO("  Start point: (%.3f, %.3f, %.3f)", startX, startY, startZ);
    UNITY_LOG_INFO("  End point: (%.3f, %.3f, %.3f)", endX, endY, endZ);
    
    try {
        // 3. Coordinate transformation
        UNITY_LOG_INFO("3. Coordinate transformation...");
        float origStartX = startX, origStartY = startY, origStartZ = startZ;
        float origEndX = endX, origEndY = endY, origEndZ = endZ;
        
        TransformVertex(&startX, &startY, &startZ);
        TransformVertex(&endX, &endY, &endZ);
        
        UNITY_LOG_INFO("  Transformed start point: (%.3f, %.3f, %.3f)", startX, startY, startZ);
        UNITY_LOG_INFO("  Transformed end point: (%.3f, %.3f, %.3f)", endX, endY, endZ);
        
        // 4. Execute pathfinding
        UNITY_LOG_INFO("4. Executing pathfinding...");
        result = g_pathfinding->FindPath(startX, startY, startZ, endX, endY, endZ);
        
        if (result.success) {
            UNITY_LOG_INFO("Pathfinding successful! Point count: %d", result.pointCount);
            
            // 5. Transform path results to Unity coordinate system
            if (result.pathPoints) {
                UNITY_LOG_INFO("5. Transforming path coordinates...");
                UnityRecast_TransformPathPoints(result.pathPoints, result.pointCount);
                
                // Log first and last points
                if (result.pointCount > 0) {
                    UNITY_LOG_INFO("  First point: (%.3f, %.3f, %.3f)", 
                                   result.pathPoints[0], result.pathPoints[1], result.pathPoints[2]);
                }
                if (result.pointCount > 1) {
                    int lastIdx = (result.pointCount - 1) * 3;
                    UNITY_LOG_INFO("  Last point: (%.3f, %.3f, %.3f)", 
                                   result.pathPoints[lastIdx], result.pathPoints[lastIdx + 1], result.pathPoints[lastIdx + 2]);
                }
            }
            
            UNITY_LOG_INFO("=== UnityRecast_FindPath Completed ===");
        }
        else {
            UNITY_LOG_ERROR("Pathfinding failed: %s", result.errorMessage ? result.errorMessage : "Unknown error");
        }
    }
    catch (const std::exception& e) {
        UNITY_LOG_ERROR("Exception during pathfinding: %s", e.what());
        result.success = false;
        result.errorMessage = const_cast<char*>(e.what());
    }
    catch (...) {
        UNITY_LOG_ERROR("Unknown exception during pathfinding");
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
    UNITY_LOG_DEBUG("UnityRecast_GetPolyCount called");
    
    if (!g_initialized || !g_navMeshBuilder) {
        UNITY_LOG_DEBUG("Not initialized or NavMeshBuilder is null");
        return 0;
    }
    
    int polyCount = g_navMeshBuilder->GetPolyCount();
    UNITY_LOG_DEBUG("Polygon count: %d", polyCount);
    return polyCount;
}

UNITY_API int UnityRecast_GetVertexCount() {
    UNITY_LOG_DEBUG("UnityRecast_GetVertexCount called");
    
    if (!g_initialized || !g_navMeshBuilder) {
        UNITY_LOG_DEBUG("Not initialized or NavMeshBuilder is null");
        return 0;
    }
    
    int vertexCount = g_navMeshBuilder->GetVertexCount();
    UNITY_LOG_DEBUG("Vertex count: %d", vertexCount);
    return vertexCount;
}

// 디버그 기능들 (완전 구현)
UNITY_API void UnityRecast_SetDebugDraw(bool enabled) {
    UNITY_LOG_INFO("Debug draw enabled: %s", enabled ? "true" : "false");
    // Unity NavMeshGizmo에서 직접 처리하므로 특별한 작업 불필요
}

UNITY_API void UnityRecast_GetDebugVertices(float* vertices, int* vertexCount) {
    UNITY_LOG_INFO("🎨 UnityRecast_GetDebugVertices called");
    
    if (!vertexCount) {
        UNITY_LOG_ERROR("vertexCount pointer is null");
        return;
    }
    
    // 초기화 체크
    if (!g_initialized || !g_navMeshBuilder) {
        UNITY_LOG_WARNING("Not initialized or NavMeshBuilder is null");
        *vertexCount = 0;
        return;
    }
    
    try {
        // NavMeshBuilder에서 DetailMesh 데이터 가져오기
        std::vector<float> debugVertices;
        bool success = g_navMeshBuilder->GetDebugVertices(debugVertices);
        
        if (!success || debugVertices.empty()) {
            UNITY_LOG_WARNING("No debug vertices available or failed to get vertices");
            *vertexCount = 0;
            return;
        }
        
        int totalVertexCount = static_cast<int>(debugVertices.size() / 3);
        UNITY_LOG_INFO("Debug vertices available: %d vertices (%d floats)", 
                       totalVertexCount, static_cast<int>(debugVertices.size()));
        
        // 첫 번째 호출: vertexCount만 설정 (Unity에서 메모리 할당을 위해)
        if (!vertices) {
            *vertexCount = totalVertexCount;
            UNITY_LOG_INFO("Returning vertex count: %d", totalVertexCount);
            return;
        }
        
        // 두 번째 호출: 실제 데이터 복사
        if (*vertexCount >= totalVertexCount) {
            std::memcpy(vertices, debugVertices.data(), debugVertices.size() * sizeof(float));
            *vertexCount = totalVertexCount;
            
            // 첫 번째 정점 로그 출력 (확인용)
            if (totalVertexCount > 0) {
                UNITY_LOG_INFO("First vertex: (%.3f, %.3f, %.3f)", 
                               debugVertices[0], debugVertices[1], debugVertices[2]);
            }
            
            UNITY_LOG_INFO("✅ Debug vertices copied successfully: %d vertices", totalVertexCount);
        } else {
            UNITY_LOG_ERROR("Buffer too small! Required: %d, provided: %d", totalVertexCount, *vertexCount);
            *vertexCount = 0;
        }
    }
    catch (const std::exception& e) {
        UNITY_LOG_ERROR("Exception in GetDebugVertices: %s", e.what());
        *vertexCount = 0;
    }
}

UNITY_API void UnityRecast_GetDebugIndices(int* indices, int* indexCount) {
    UNITY_LOG_INFO("🎨 UnityRecast_GetDebugIndices called");
    
    if (!indexCount) {
        UNITY_LOG_ERROR("indexCount pointer is null");
        return;
    }
    
    // 초기화 체크
    if (!g_initialized || !g_navMeshBuilder) {
        UNITY_LOG_WARNING("Not initialized or NavMeshBuilder is null");
        *indexCount = 0;
        return;
    }
    
    try {
        // NavMeshBuilder에서 DetailMesh 인덱스 데이터 가져오기
        std::vector<int> debugIndices;
        bool success = g_navMeshBuilder->GetDebugIndices(debugIndices);
        
        if (!success || debugIndices.empty()) {
            UNITY_LOG_WARNING("No debug indices available or failed to get indices");
            *indexCount = 0;
            return;
        }
        
        int totalIndexCount = static_cast<int>(debugIndices.size());
        UNITY_LOG_INFO("Debug indices available: %d indices (%d triangles)", 
                       totalIndexCount, totalIndexCount / 3);
        
        // 첫 번째 호출: indexCount만 설정 (Unity에서 메모리 할당을 위해)
        if (!indices) {
            *indexCount = totalIndexCount;
            UNITY_LOG_INFO("Returning index count: %d", totalIndexCount);
            return;
        }
        
        // 두 번째 호출: 실제 데이터 복사
        if (*indexCount >= totalIndexCount) {
            std::memcpy(indices, debugIndices.data(), debugIndices.size() * sizeof(int));
            *indexCount = totalIndexCount;
            
            // 첫 번째 삼각형 로그 출력 (확인용)
            if (totalIndexCount >= 3) {
                UNITY_LOG_INFO("First triangle indices: (%d, %d, %d)", 
                               debugIndices[0], debugIndices[1], debugIndices[2]);
            }
            
            UNITY_LOG_INFO("✅ Debug indices copied successfully: %d indices", totalIndexCount);
        } else {
            UNITY_LOG_ERROR("Buffer too small! Required: %d, provided: %d", totalIndexCount, *indexCount);
            *indexCount = 0;
        }
    }
    catch (const std::exception& e) {
        UNITY_LOG_ERROR("Exception in GetDebugIndices: %s", e.what());
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