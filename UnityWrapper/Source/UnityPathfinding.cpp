#include "UnityPathfinding.h"
#include "DetourNavMeshQuery.h"
#include "Recast.h"
#include <cmath>
#include <algorithm>

UnityPathfinding::UnityPathfinding() : m_navMeshQuery(nullptr) {
    // Default filter settings
    m_filter.setIncludeFlags(0xffff);
    m_filter.setExcludeFlags(0);
    m_filter.setAreaCost(RC_WALKABLE_AREA, 1.0f);
}

UnityPathfinding::~UnityPathfinding() {
}

void UnityPathfinding::SetNavMesh(dtNavMesh* navMesh, dtNavMeshQuery* navMeshQuery) {
    (void)navMesh; // Suppress unused parameter warning
    m_navMeshQuery = navMeshQuery;
}

UnityPathResult UnityPathfinding::FindPath(
    float startX, float startY, float startZ,
    float endX, float endY, float endZ
) {
    UnityPathResult result = {0};
    
    if (!m_navMeshQuery) {
        result.success = false;
        result.errorMessage = const_cast<char*>("NavMesh not initialized");
        return result;
    }
    
    try {
        // 시작점과 끝점의 가장 가까운 폴리곤 찾기
        dtPolyRef startRef, endRef;
        float startPt[3], endPt[3];
        
        if (!FindNearestPoly(startX, startY, startZ, startRef, startPt)) {
            result.success = false;
            result.errorMessage = const_cast<char*>("Cannot find start polygon");
            return result;
        }
        
        if (!FindNearestPoly(endX, endY, endZ, endRef, endPt)) {
            result.success = false;
            result.errorMessage = const_cast<char*>("Cannot find end polygon");
            return result;
        }
        
        // 경로 찾기
        m_pathPolys.clear();
        m_pathPoints.clear();
        
        int pathCount = 0;
        const int maxPath = 256;
        dtPolyRef path[maxPath];
        
        dtStatus status = m_navMeshQuery->findPath(startRef, endRef, startPt, endPt, &m_filter, path, &pathCount, maxPath);
        
        if (dtStatusFailed(status)) {
            result.success = false;
            result.errorMessage = const_cast<char*>("Path finding failed");
            return result;
        }
        
        if (pathCount == 0) {
            result.success = false;
            result.errorMessage = const_cast<char*>("No path found");
            return result;
        }
        
        // 경로 스무딩
        const int maxStraightPath = 256;
        float straightPath[maxStraightPath * 3];
        unsigned char straightPathFlags[maxStraightPath];
        dtPolyRef straightPathRefs[maxStraightPath];
        int straightPathCount = 0;
        
        status = m_navMeshQuery->findStraightPath(startPt, endPt, path, pathCount,
                                                 straightPath, straightPathFlags, straightPathRefs,
                                                 &straightPathCount, maxStraightPath);
        
        if (dtStatusFailed(status)) {
            result.success = false;
            result.errorMessage = const_cast<char*>("Path straightening failed");
            return result;
        }
        
        // 결과 복사
        if (straightPathCount > 0) {
            result.pointCount = straightPathCount;
            result.pathPoints = new float[straightPathCount * 3];
            std::copy(straightPath, straightPath + straightPathCount * 3, result.pathPoints);
            result.success = true;
            result.errorMessage = nullptr;
        } else {
            result.success = false;
            result.errorMessage = const_cast<char*>("No path points generated");
        }
        
    }
    catch (const std::exception& e) {
        result.success = false;
        result.errorMessage = const_cast<char*>(e.what());
    }
    catch (...) {
        result.success = false;
        result.errorMessage = const_cast<char*>("Unknown error during pathfinding");
    }
    
    return result;
}

UnityPathResult UnityPathfinding::SmoothPath(const UnityPathResult* path, float maxSmoothDistance) {
    UnityPathResult result = {0};
    
    if (!path || !path->success || !path->pathPoints || path->pointCount <= 0) {
        result.success = false;
        result.errorMessage = const_cast<char*>("Invalid path input");
        return result;
    }
    
    try {
        std::vector<float> inputPoints(path->pathPoints, path->pathPoints + path->pointCount * 3);
        std::vector<float> outputPoints;
        
        SmoothPathPoints(inputPoints, outputPoints, maxSmoothDistance);
        
        if (outputPoints.empty()) {
            result.success = false;
            result.errorMessage = const_cast<char*>("Smoothing failed");
            return result;
        }
        
        result.pointCount = static_cast<int>(outputPoints.size()) / 3;
        result.pathPoints = new float[outputPoints.size()];
        std::copy(outputPoints.begin(), outputPoints.end(), result.pathPoints);
        result.success = true;
        result.errorMessage = nullptr;
        
    }
    catch (const std::exception& e) {
        result.success = false;
        result.errorMessage = const_cast<char*>(e.what());
    }
    catch (...) {
        result.success = false;
        result.errorMessage = const_cast<char*>("Unknown error during path smoothing");
    }
    
    return result;
}

UnityPathResult UnityPathfinding::SimplifyPath(const UnityPathResult* path, float tolerance) {
    UnityPathResult result = {0};
    
    if (!path || !path->success || !path->pathPoints || path->pointCount <= 0) {
        result.success = false;
        result.errorMessage = const_cast<char*>("Invalid path input");
        return result;
    }
    
    try {
        std::vector<float> inputPoints(path->pathPoints, path->pathPoints + path->pointCount * 3);
        std::vector<float> outputPoints;
        
        SimplifyPathPoints(inputPoints, outputPoints, tolerance);
        
        if (outputPoints.empty()) {
            result.success = false;
            result.errorMessage = const_cast<char*>("Simplification failed");
            return result;
        }
        
        result.pointCount = static_cast<int>(outputPoints.size()) / 3;
        result.pathPoints = new float[outputPoints.size()];
        std::copy(outputPoints.begin(), outputPoints.end(), result.pathPoints);
        result.success = true;
        result.errorMessage = nullptr;
        
    }
    catch (const std::exception& e) {
        result.success = false;
        result.errorMessage = const_cast<char*>(e.what());
    }
    catch (...) {
        result.success = false;
        result.errorMessage = const_cast<char*>("Unknown error during path simplification");
    }
    
    return result;
}

bool UnityPathfinding::ValidatePath(const UnityPathResult* path) {
    if (!path || !path->success || !path->pathPoints || path->pointCount <= 0) {
        return false;
    }
    
    // 간단한 경로 검증: 연속된 포인트들이 너무 멀지 않은지 확인
    for (int i = 1; i < path->pointCount; ++i) {
        float dist = CalculateDistance(
            path->pathPoints[(i-1) * 3], path->pathPoints[(i-1) * 3 + 1], path->pathPoints[(i-1) * 3 + 2],
            path->pathPoints[i * 3], path->pathPoints[i * 3 + 1], path->pathPoints[i * 3 + 2]
        );
        
        if (dist > 100.0f) { // 임계값 설정
            return false;
        }
    }
    
    return true;
}

float UnityPathfinding::CalculatePathLength(const UnityPathResult* path) {
    if (!path || !path->success || !path->pathPoints || path->pointCount <= 1) {
        return 0.0f;
    }
    
    float totalLength = 0.0f;
    for (int i = 1; i < path->pointCount; ++i) {
        totalLength += CalculateDistance(
            path->pathPoints[(i-1) * 3], path->pathPoints[(i-1) * 3 + 1], path->pathPoints[(i-1) * 3 + 2],
            path->pathPoints[i * 3], path->pathPoints[i * 3 + 1], path->pathPoints[i * 3 + 2]
        );
    }
    
    return totalLength;
}

int UnityPathfinding::GetPathPointCount(const UnityPathResult* path) {
    if (!path) {
        return 0;
    }
    return path->pointCount;
}

bool UnityPathfinding::GetPathPoint(const UnityPathResult* path, int index, float* x, float* y, float* z) {
    if (!path || !path->pathPoints || index < 0 || index >= path->pointCount) {
        return false;
    }
    
    if (x) *x = path->pathPoints[index * 3];
    if (y) *y = path->pathPoints[index * 3 + 1];
    if (z) *z = path->pathPoints[index * 3 + 2];
    
    return true;
}

bool UnityPathfinding::GetPathDirection(const UnityPathResult* path, int index, float* dirX, float* dirY, float* dirZ) {
    if (!path || !path->pathPoints || index < 0 || index >= path->pointCount - 1) {
        return false;
    }
    
    float dx = path->pathPoints[(index + 1) * 3] - path->pathPoints[index * 3];
    float dy = path->pathPoints[(index + 1) * 3 + 1] - path->pathPoints[index * 3 + 1];
    float dz = path->pathPoints[(index + 1) * 3 + 2] - path->pathPoints[index * 3 + 2];
    
    float length = std::sqrt(dx * dx + dy * dy + dz * dz);
    if (length > 0.0f) {
        if (dirX) *dirX = dx / length;
        if (dirY) *dirY = dy / length;
        if (dirZ) *dirZ = dz / length;
        return true;
    }
    
    return false;
}

float UnityPathfinding::GetPathCurvature(const UnityPathResult* path, int index) {
    if (!path || !path->pathPoints || index < 1 || index >= path->pointCount - 1) {
        return 0.0f;
    }
    
    std::vector<float> points(path->pathPoints, path->pathPoints + path->pointCount * 3);
    return CalculateCurvature(points, index);
}

bool UnityPathfinding::FindNearestPoly(float x, float y, float z, dtPolyRef& polyRef, float* nearestPt) {
    if (!m_navMeshQuery) {
        return false;
    }
    
    float center[3] = { x, y, z };
    float extents[3] = { 2.0f, 4.0f, 2.0f };
    dtStatus status = m_navMeshQuery->findNearestPoly(center, extents, &m_filter, &polyRef, nearestPt);
    
    return !dtStatusFailed(status) && polyRef != 0;
}

bool UnityPathfinding::Raycast(float startX, float startY, float startZ, float endX, float endY, float endZ, float* hitPoint) {
    if (!m_navMeshQuery) {
        return false;
    }
    
    dtPolyRef startRef;
    float startPt[3] = { startX, startY, startZ };
    
    if (!FindNearestPoly(startX, startY, startZ, startRef, startPt)) {
        return false;
    }
    
    float endPt[3] = { endX, endY, endZ };
    float t;
    float normal[3];
    dtPolyRef path[32];
    int pathCount = 0;
    
    dtStatus status = m_navMeshQuery->raycast(startRef, startPt, endPt, &m_filter, &t, normal, path, &pathCount, 32);
    
    if (dtStatusFailed(status)) {
        return false;
    }
    
    if (hitPoint) {
        hitPoint[0] = startPt[0] + (endPt[0] - startPt[0]) * t;
        hitPoint[1] = startPt[1] + (endPt[1] - startPt[1]) * t;
        hitPoint[2] = startPt[2] + (endPt[2] - startPt[2]) * t;
    }
    
    return true;
}

void UnityPathfinding::SmoothPathPoints(const std::vector<float>& input, std::vector<float>& output, float maxSmoothDistance) {
    if (input.size() < 6) { // 최소 2개 포인트 필요
        output = input;
        return;
    }
    
    output.clear();
    output.push_back(input[0]);
    output.push_back(input[1]);
    output.push_back(input[2]);
    
    for (size_t i = 3; i < input.size() - 3; i += 3) {
        float dist = CalculateDistance(
            input[i-3], input[i-2], input[i-1],
            input[i+3], input[i+4], input[i+5]
        );
        
        if (dist > maxSmoothDistance) {
            output.push_back(input[i]);
            output.push_back(input[i+1]);
            output.push_back(input[i+2]);
        }
    }
    
    output.push_back(input[input.size()-3]);
    output.push_back(input[input.size()-2]);
    output.push_back(input[input.size()-1]);
}

void UnityPathfinding::SimplifyPathPoints(const std::vector<float>& input, std::vector<float>& output, float tolerance) {
    if (input.size() < 6) { // 최소 2개 포인트 필요
        output = input;
        return;
    }
    
    output.clear();
    output.push_back(input[0]);
    output.push_back(input[1]);
    output.push_back(input[2]);
    
    for (size_t i = 3; i < input.size() - 3; i += 3) {
        float dist = std::abs(CalculateDistance(
            input[i-3], input[i-2], input[i-1],
            input[i], input[i+1], input[i+2]
        ) + CalculateDistance(
            input[i], input[i+1], input[i+2],
            input[i+3], input[i+4], input[i+5]
        ) - CalculateDistance(
            input[i-3], input[i-2], input[i-1],
            input[i+3], input[i+4], input[i+5]
        ));
        
        if (dist > tolerance) {
            output.push_back(input[i]);
            output.push_back(input[i+1]);
            output.push_back(input[i+2]);
        }
    }
    
    output.push_back(input[input.size()-3]);
    output.push_back(input[input.size()-2]);
    output.push_back(input[input.size()-1]);
}

float UnityPathfinding::CalculateDistance(float x1, float y1, float z1, float x2, float y2, float z2) {
    float dx = x2 - x1;
    float dy = y2 - y1;
    float dz = z2 - z1;
    return std::sqrt(dx * dx + dy * dy + dz * dz);
}

float UnityPathfinding::CalculateCurvature(const std::vector<float>& points, int index) {
    if (index < 1 || index >= static_cast<int>(points.size()) / 3 - 1) {
        return 0.0f;
    }
    
    // 세 점을 이용한 곡률 계산
    float x1 = points[(index - 1) * 3];
    float y1 = points[(index - 1) * 3 + 1];
    float z1 = points[(index - 1) * 3 + 2];
    
    float x2 = points[index * 3];
    float y2 = points[index * 3 + 1];
    float z2 = points[index * 3 + 2];
    
    float x3 = points[(index + 1) * 3];
    float y3 = points[(index + 1) * 3 + 1];
    float z3 = points[(index + 1) * 3 + 2];
    
    // 벡터 계산
    float v1x = x2 - x1;
    float v1y = y2 - y1;
    float v1z = z2 - z1;
    
    float v2x = x3 - x2;
    float v2y = y3 - y2;
    float v2z = z3 - z2;
    
    // 외적 계산
    float crossX = v1y * v2z - v1z * v2y;
    float crossY = v1z * v2x - v1x * v2z;
    float crossZ = v1x * v2y - v1y * v2x;
    
    float crossLength = std::sqrt(crossX * crossX + crossY * crossY + crossZ * crossZ);
    float v1Length = std::sqrt(v1x * v1x + v1y * v1y + v1z * v1z);
    float v2Length = std::sqrt(v2x * v2x + v2y * v2y + v2z * v2z);
    
    if (v1Length > 0.0f && v2Length > 0.0f) {
        return crossLength / (v1Length * v2Length);
    }
    
    return 0.0f;
} 