#include "UnityNavMeshQuery.h"
#include "RecastNavigationUnity.h"
#include "DetourNavMesh.h"
#include "DetourNavMeshQuery.h"
#include <memory>
#include <vector>

UnityNavMeshQuery::UnityNavMeshQuery() {
}

UnityNavMeshQuery::~UnityNavMeshQuery() {
}

std::shared_ptr<dtNavMeshQuery> UnityNavMeshQuery::CreateQuery(NavMeshHandle navMesh, int maxNodes) {
    if (!navMesh) {
        return nullptr;
    }

    dtNavMeshQuery* query = dtAllocNavMeshQuery();
    if (!query) {
        return nullptr;
    }

    if (dtStatusFailed(query->init(static_cast<dtNavMesh*>(navMesh), maxNodes))) {
        dtFreeNavMeshQuery(query);
        return nullptr;
    }

    return std::shared_ptr<dtNavMeshQuery>(query, [](dtNavMeshQuery* q) {
        if (q) dtFreeNavMeshQuery(q);
    });
}

PathResult UnityNavMeshQuery::FindPath(
    NavMeshQueryHandle query,
    UnityVector3 startPos,
    UnityVector3 endPos,
    QueryFilter* filter) {
    
    PathResult result = { nullptr, 0, 0 };
    
    if (!query) {
        return result;
    }

    dtNavMeshQuery* navQuery = static_cast<dtNavMeshQuery*>(query);
    
    // Convert Unity coordinates to Detour coordinates
    float start[3] = { startPos.x, startPos.y, startPos.z };
    float end[3] = { endPos.x, endPos.y, endPos.z };
    
    // Find nearest polygons
    dtPolyRef startRef, endRef;
    float startNearest[3], endNearest[3];
    
    if (dtStatusFailed(navQuery->findNearestPoly(start, nullptr, nullptr, &startRef, startNearest))) {
        result.status = 0;
        return result;
    }
    
    if (dtStatusFailed(navQuery->findNearestPoly(end, nullptr, nullptr, &endRef, endNearest))) {
        result.status = 0;
        return result;
    }
    
    // Create query filter
    dtQueryFilter queryFilter;
    if (filter) {
        for (int i = 0; i < 64; i++) {
            queryFilter.setAreaCost(i, filter->walkableAreaCost[i]);
        }
        queryFilter.setIncludeFlags(filter->includeFlags);
        queryFilter.setExcludeFlags(filter->excludeFlags);
    }
    
    // Find path
    const int MAX_POLYS = 256;
    dtPolyRef polys[MAX_POLYS];
    int polyCount = 0;
    
    dtStatus status = navQuery->findPath(startRef, endRef, startNearest, endNearest, &queryFilter, polys, &polyCount, MAX_POLYS);
    
    if (dtStatusFailed(status)) {
        result.status = 0;
        return result;
    }
    
    if (polyCount == 0) {
        result.status = 1; // No path found
        return result;
    }
    
    // Find straight path
    const int MAX_STRAIGHT_PATH = 256;
    float straightPath[MAX_STRAIGHT_PATH * 3];
    unsigned char straightPathFlags[MAX_STRAIGHT_PATH];
    dtPolyRef straightPathPolys[MAX_STRAIGHT_PATH];
    int straightPathCount = 0;
    
    status = navQuery->findStraightPath(startNearest, endNearest, polys, polyCount, 
                                       straightPath, straightPathFlags, straightPathPolys, 
                                       &straightPathCount, MAX_STRAIGHT_PATH);
    
    if (dtStatusFailed(status)) {
        result.status = 0;
        return result;
    }
    
    // Convert to Unity format
    result.pathLength = straightPathCount;
    result.path = new UnityVector3[straightPathCount];
    
    for (int i = 0; i < straightPathCount; i++) {
        result.path[i].x = straightPath[i * 3];
        result.path[i].y = straightPath[i * 3 + 1];
        result.path[i].z = straightPath[i * 3 + 2];
    }
    
    result.status = 1; // Success
    return result;
}

UnityVector3 UnityNavMeshQuery::GetClosestPoint(
    NavMeshQueryHandle query,
    UnityVector3 position,
    QueryFilter* filter) {
    
    UnityVector3 result = { 0, 0, 0 };
    
    if (!query) {
        return result;
    }

    dtNavMeshQuery* navQuery = static_cast<dtNavMeshQuery*>(query);
    
    // Convert Unity coordinates to Detour coordinates
    float pos[3] = { position.x, position.y, position.z };
    float nearest[3];
    dtPolyRef polyRef;
    
    // Create query filter
    dtQueryFilter queryFilter;
    if (filter) {
        for (int i = 0; i < 64; i++) {
            queryFilter.setAreaCost(i, filter->walkableAreaCost[i]);
        }
        queryFilter.setIncludeFlags(filter->includeFlags);
        queryFilter.setExcludeFlags(filter->excludeFlags);
    }
    
    if (dtStatusSucceed(navQuery->findNearestPoly(pos, nullptr, nullptr, &polyRef, nearest))) {
        result.x = nearest[0];
        result.y = nearest[1];
        result.z = nearest[2];
    }
    
    return result;
}

int UnityNavMeshQuery::Raycast(
    NavMeshQueryHandle query,
    UnityVector3 startPos,
    UnityVector3 endPos,
    QueryFilter* filter,
    UnityVector3* hitPos,
    UnityVector3* hitNormal) {
    
    if (!query) {
        return 0;
    }

    dtNavMeshQuery* navQuery = static_cast<dtNavMeshQuery*>(query);
    
    // Convert Unity coordinates to Detour coordinates
    float start[3] = { startPos.x, startPos.y, startPos.z };
    float end[3] = { endPos.x, endPos.y, endPos.z };
    
    // Find nearest polygon to start position
    dtPolyRef startRef;
    float startNearest[3];
    
    if (dtStatusFailed(navQuery->findNearestPoly(start, nullptr, nullptr, &startRef, startNearest))) {
        return 0;
    }
    
    // Create query filter
    dtQueryFilter queryFilter;
    if (filter) {
        for (int i = 0; i < 64; i++) {
            queryFilter.setAreaCost(i, filter->walkableAreaCost[i]);
        }
        queryFilter.setIncludeFlags(filter->includeFlags);
        queryFilter.setExcludeFlags(filter->excludeFlags);
    }
    
    // Perform raycast
    float t;
    float hitNormalVec[3];
    dtPolyRef path[256];
    int pathCount;
    
    dtStatus status = navQuery->raycast(startRef, startNearest, end, &queryFilter, &t, hitNormalVec, path, &pathCount, 256);
    
    if (dtStatusSucceed(status)) {
        if (hitPos) {
            // Calculate hit position based on t parameter
            hitPos->x = startNearest[0] + (end[0] - startNearest[0]) * t;
            hitPos->y = startNearest[1] + (end[1] - startNearest[1]) * t;
            hitPos->z = startNearest[2] + (end[2] - startNearest[2]) * t;
        }
        if (hitNormal) {
            hitNormal->x = hitNormalVec[0];
            hitNormal->y = hitNormalVec[1];
            hitNormal->z = hitNormalVec[2];
        }
        return 1; // Hit
    }
    
    return 0; // No hit
} 