#pragma once

#include "RecastNavigationUnity.h"
#include <memory>

// Forward declarations
struct dtNavMesh;
struct dtNavMeshQuery;

class UnityNavMeshQuery {
public:
    UnityNavMeshQuery();
    ~UnityNavMeshQuery();

    std::shared_ptr<dtNavMeshQuery> CreateQuery(NavMeshHandle navMesh, int maxNodes);

    PathResult FindPath(
        NavMeshQueryHandle query,
        UnityVector3 startPos,
        UnityVector3 endPos,
        QueryFilter* filter);

    UnityVector3 GetClosestPoint(
        NavMeshQueryHandle query,
        UnityVector3 position,
        QueryFilter* filter);

    int Raycast(
        NavMeshQueryHandle query,
        UnityVector3 startPos,
        UnityVector3 endPos,
        QueryFilter* filter,
        UnityVector3* hitPos,
        UnityVector3* hitNormal);
}; 