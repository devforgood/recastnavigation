#pragma once

#include "UnityCommonTypes.h"

extern "C" {
    // Unity에서 사용할 수 있는 C 스타일 인터페이스
    
    // 초기화 및 정리
    UNITY_API bool UnityRecast_Initialize();
    UNITY_API void UnityRecast_Cleanup();
    
    UNITY_API void UnityRecast_SetCoordinateSystem(UnityCoordinateSystem system);
    UNITY_API UnityCoordinateSystem UnityRecast_GetCoordinateSystem();
    UNITY_API void UnityRecast_SetYAxisRotation(UnityYAxisRotation rotation);
    UNITY_API UnityYAxisRotation UnityRecast_GetYAxisRotation();
    
    // 좌표 변환 함수들
    UNITY_API void UnityRecast_TransformVertex(float* x, float* y, float* z);
    UNITY_API void UnityRecast_TransformPathPoint(float* x, float* y, float* z);
    UNITY_API void UnityRecast_TransformPathPoints(float* points, int pointCount);
    
    // NavMesh 빌드 함수
    UNITY_API UnityNavMeshResult UnityRecast_BuildNavMesh(
        const UnityMeshData* meshData,
        const UnityNavMeshBuildSettings* settings
    );
    
    // NavMesh 데이터 해제
    UNITY_API void UnityRecast_FreeNavMeshData(UnityNavMeshResult* result);
    
    // NavMesh 로드
    UNITY_API bool UnityRecast_LoadNavMesh(const unsigned char* data, int dataSize);
    
    // 경로 찾기
    UNITY_API UnityPathResult UnityRecast_FindPath(
        float startX, float startY, float startZ,
        float endX, float endY, float endZ
    );
    
    // 경로 결과 해제
    UNITY_API void UnityRecast_FreePathResult(UnityPathResult* result);
    
    // NavMesh 정보 가져오기
    UNITY_API int UnityRecast_GetPolyCount();
    UNITY_API int UnityRecast_GetVertexCount();
    
    // 디버그 정보
    UNITY_API void UnityRecast_SetDebugDraw(bool enabled);
    UNITY_API void UnityRecast_GetDebugVertices(float* vertices, int* vertexCount);
    UNITY_API void UnityRecast_GetDebugIndices(int* indices, int* indexCount);
    
    // 로깅 시스템 함수들
    UNITY_API bool UnityRecast_InitializeLogging(const char* logFilePath, int logLevel, int output);
    UNITY_API void UnityRecast_SetLogLevel(int level);
    UNITY_API void UnityRecast_SetLogOutput(int output);
    UNITY_API void UnityRecast_SetLogFilePath(const char* filePath);
    UNITY_API void UnityRecast_ShutdownLogging();
} 