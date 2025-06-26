#pragma once

#include "UnityRecastWrapper.h"
#include "DetourNavMeshQuery.h"
#include <vector>

class UnityPathfinding {
public:
    UnityPathfinding();
    ~UnityPathfinding();
    
    // NavMesh 설정
    void SetNavMesh(dtNavMesh* navMesh, dtNavMeshQuery* navMeshQuery);
    
    // 경로 찾기
    UnityPathResult FindPath(
        float startX, float startY, float startZ,
        float endX, float endY, float endZ
    );
    
    // 경로 스무딩
    UnityPathResult SmoothPath(const UnityPathResult* path, float maxSmoothDistance);
    
    // 경로 단순화
    UnityPathResult SimplifyPath(const UnityPathResult* path, float tolerance);
    
    // 경로 검증
    bool ValidatePath(const UnityPathResult* path);
    
    // 경로 길이 계산
    float CalculatePathLength(const UnityPathResult* path);
    
    // 경로 포인트 수 계산
    int GetPathPointCount(const UnityPathResult* path);
    
    // 경로 포인트 가져오기
    bool GetPathPoint(const UnityPathResult* path, int index, float* x, float* y, float* z);
    
    // 경로 방향 벡터 계산
    bool GetPathDirection(const UnityPathResult* path, int index, float* dirX, float* dirY, float* dirZ);
    
    // 경로 곡률 계산
    float GetPathCurvature(const UnityPathResult* path, int index);
    
private:
    // NavMesh 쿼리 객체
    dtNavMeshQuery* m_navMeshQuery;
    
    // 경로 찾기 설정
    dtQueryFilter m_filter;
    
    // 경로 찾기 결과
    std::vector<dtPolyRef> m_pathPolys;
    std::vector<float> m_pathPoints;
    
    // 유틸리티 함수
    bool FindNearestPoly(float x, float y, float z, dtPolyRef& polyRef, float* nearestPt);
    bool Raycast(float startX, float startY, float startZ, float endX, float endY, float endZ, float* hitPoint);
    void SmoothPathPoints(const std::vector<float>& input, std::vector<float>& output, float maxSmoothDistance);
    void SimplifyPathPoints(const std::vector<float>& input, std::vector<float>& output, float tolerance);
    float CalculateDistance(float x1, float y1, float z1, float x2, float y2, float z2);
    float CalculateCurvature(const std::vector<float>& points, int index);
}; 