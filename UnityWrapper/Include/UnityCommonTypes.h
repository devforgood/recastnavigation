#pragma once

#include <cstdint>

#ifdef UNITY_EXPORT
    #ifdef _WIN32
        #define UNITY_API __declspec(dllexport)
    #else
        #define UNITY_API __attribute__((visibility("default")))
    #endif
#else
    #define UNITY_API
#endif

// Unity 메시 데이터 구조체 (C# UnityMeshData와 동일)
struct UnityMeshData {
    float* vertices;      // 3D 정점 배열 (x, y, z)
    int* indices;         // 삼각형 인덱스 배열
    int vertexCount;      // 정점 개수
    int indexCount;       // 인덱스 개수
    bool transformCoordinates; // 좌표 변환 여부
};

// Unity NavMesh 빌드 설정 구조체 (C# UnityNavMeshBuildSettings와 동일)
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
    float maxSimplificationError; // 최대 단순화 오차
    float maxEdgeLen;         // 최대 엣지 길이
    bool autoTransformCoordinates; // 자동 좌표 변환
    int partitionType;        // 파티션 타입 (0: Watershed, 1: Monotone, 2: Layers)
};

// Unity NavMesh 결과 구조체 (C# UnityNavMeshResult와 동일)
struct UnityNavMeshResult {
    bool success;         // 성공 여부
    unsigned char* navMeshData;  // NavMesh 데이터
    int dataSize;        // 데이터 크기
    char* errorMessage; // 오류 메시지
};

// Unity 경로 결과 구조체 (C# UnityPathResult와 동일)
struct UnityPathResult {
    bool success;         // 성공 여부
    float* pathPoints;   // 경로 포인트 배열
    int pointCount;      // 포인트 개수
    char* errorMessage; // 오류 메시지
};

// 좌표계 변환 설정 (C# CoordinateSystem과 동일)
enum UnityCoordinateSystem {
    UNITY_COORD_LEFT_HANDED = 0,    // Unity 기본 (왼손 좌표계)
    UNITY_COORD_RIGHT_HANDED = 1    // RecastNavigation 기본 (오른손 좌표계)
};

// Y축 회전 설정 (C# YAxisRotation과 동일)
enum UnityYAxisRotation {
    UNITY_Y_ROTATION_NONE = 0,      // 회전 없음
    UNITY_Y_ROTATION_90 = 1,        // Y축 기준 90도 회전
    UNITY_Y_ROTATION_180 = 2,       // Y축 기준 180도 회전
    UNITY_Y_ROTATION_270 = 3        // Y축 기준 270도 회전
}; 