#pragma once

#include "UnityRecastWrapper.h"
#include "DetourNavMeshQuery.h"
#include <vector>

class UnityPathfinding {
public:
    UnityPathfinding();
    ~UnityPathfinding();
    
    // NavMesh settings
    void SetNavMesh(dtNavMesh* navMesh, dtNavMeshQuery* navMeshQuery);
    
    // Pathfinding
    UnityPathResult FindPath(
        float startX, float startY, float startZ,
        float endX, float endY, float endZ
    );
    
    // Path smoothing
    UnityPathResult SmoothPath(const UnityPathResult* path, float maxSmoothDistance);
    
    // Path simplification
    UnityPathResult SimplifyPath(const UnityPathResult* path, float tolerance);
    
    // Path validation
    bool ValidatePath(const UnityPathResult* path);
    
    // Path length calculation
    float CalculatePathLength(const UnityPathResult* path);
    
    // Path point count calculation
    int GetPathPointCount(const UnityPathResult* path);
    
    // Get path point
    bool GetPathPoint(const UnityPathResult* path, int index, float* x, float* y, float* z);
    
    // Calculate path direction vector
    bool GetPathDirection(const UnityPathResult* path, int index, float* dirX, float* dirY, float* dirZ);
    
    // Calculate path curvature
    float GetPathCurvature(const UnityPathResult* path, int index);
    
private:
    // NavMesh query object
    dtNavMeshQuery* m_navMeshQuery;
    
    // Pathfinding settings
    dtQueryFilter m_filter;
    
    // Pathfinding results
    std::vector<dtPolyRef> m_pathPolys;
    std::vector<float> m_pathPoints;
    
    // Utility functions
    bool FindNearestPoly(float x, float y, float z, dtPolyRef& polyRef, float* nearestPt);
    bool Raycast(float startX, float startY, float startZ, float endX, float endY, float endZ, float* hitPoint);
    void SmoothPathPoints(const std::vector<float>& input, std::vector<float>& output, float maxSmoothDistance);
    void SimplifyPathPoints(const std::vector<float>& input, std::vector<float>& output, float tolerance);
    float CalculateDistance(float x1, float y1, float z1, float x2, float y2, float z2);
    float CalculateCurvature(const std::vector<float>& points, int index);
}; 