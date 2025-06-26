#pragma once

#ifdef UNITY_EXPORT
    #ifdef _WIN32
        #define UNITY_API __declspec(dllexport)
    #else
        #define UNITY_API __attribute__((visibility("default")))
    #endif
#else
    #define UNITY_API
#endif

#include <cstdint>

extern "C" {
    // Unity에서 사용할 수 있는 C 스타일 인터페이스
    
    // 초기화 및 정리
    UNITY_API bool UnityRecast_Initialize();
    UNITY_API void UnityRecast_Cleanup();
    
    // 메시 데이터 구조체
    struct UnityMeshData {
        float* vertices;      // 3D 정점 배열 (x, y, z)
        int* indices;         // 삼각형 인덱스 배열
        int vertexCount;      // 정점 개수
        int indexCount;       // 인덱스 개수
    };
    
    // NavMesh 빌드 설정
    struct UnityNavMeshBuildSettings {
        float cellSize;           // 셀 크기
        float cellHeight;         // 셀 높이
        float walkableSlopeAngle; // 이동 가능한 경사각
        float walkableHeight;     // 이동 가능한 높이
        float walkableRadius;     // 이동 가능한 반지름
        float walkableClimb;      // 이동 가능한 오르기 높이
        float minRegionArea;      // 최소 영역 크기
        float mergeRegionArea;    // 병합 영역 크기
        int maxVertsPerPoly;      // 폴리곤당 최대 정점 수
        float detailSampleDist;   // 상세 샘플링 거리
        float detailSampleMaxError; // 상세 샘플링 최대 오차
    };
    
    // NavMesh 빌드 결과
    struct UnityNavMeshResult {
        unsigned char* navMeshData;  // NavMesh 데이터
        int dataSize;                // 데이터 크기
        bool success;                // 성공 여부
        char* errorMessage;          // 오류 메시지
    };
    
    // 경로 찾기 결과
    struct UnityPathResult {
        float* pathPoints;      // 경로 포인트 배열
        int pointCount;         // 포인트 개수
        bool success;           // 성공 여부
        char* errorMessage;     // 오류 메시지
    };
    
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
} 