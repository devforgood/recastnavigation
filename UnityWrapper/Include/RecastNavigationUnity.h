#pragma once

#ifdef _WIN32
    #ifdef RECASTNAVIGATIONUNITY_EXPORTS
        #define RECASTNAVIGATIONUNITY_API __declspec(dllexport)
    #else
        #define RECASTNAVIGATIONUNITY_API __declspec(dllimport)
    #endif
#else
    #define RECASTNAVIGATIONUNITY_API
#endif

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

// Unity Vector3 structure
typedef struct {
    float x, y, z;
} UnityVector3;

// Unity Vector2 structure
typedef struct {
    float x, y;
} UnityVector2;

// NavMesh handle
typedef void* NavMeshHandle;

// NavMeshQuery handle
typedef void* NavMeshQueryHandle;

// Crowd handle
typedef void* CrowdHandle;

// Agent handle
typedef int32_t AgentHandle;

// Build settings
typedef struct {
    float cellSize;
    float cellHeight;
    float walkableSlopeAngle;
    int walkableHeight;
    int walkableRadius;
    int walkableClimb;
    int minRegionArea;
    int mergeRegionArea;
    int maxVertsPerPoly;
    float detailSampleDist;
    float detailSampleMaxError;
    int tileSize;
} BuildSettings;

// Query filter
typedef struct {
    float walkableAreaCost[64];
    float walkableAreaFlags[64];
    float walkableAreaWeight[64];
    int includeFlags;
    int excludeFlags;
} QueryFilter;

// Path finding result
typedef struct {
    UnityVector3* path;
    int pathLength;
    int status;
} PathResult;

// Crowd agent parameters
typedef struct {
    float radius;
    float height;
    float maxAcceleration;
    float maxSpeed;
    float collisionQueryRange;
    float pathOptimizationRange;
    float separationWeight;
    int updateFlags;
    int obstacleAvoidanceType;
    QueryFilter queryFilter;
} AgentParams;

// Initialize RecastNavigation
RECASTNAVIGATIONUNITY_API int InitializeRecastNavigation();

// Cleanup RecastNavigation
RECASTNAVIGATIONUNITY_API void CleanupRecastNavigation();

// NavMesh building functions
RECASTNAVIGATIONUNITY_API NavMeshHandle BuildNavMesh(
    UnityVector3* vertices, int vertexCount,
    int* indices, int indexCount,
    BuildSettings* settings
);

RECASTNAVIGATIONUNITY_API NavMeshHandle BuildNavMeshFromHeightfield(
    float* heightfield, int width, int height,
    float originX, float originY, float originZ,
    float cellSize, float cellHeight,
    BuildSettings* settings
);

RECASTNAVIGATIONUNITY_API void DestroyNavMesh(NavMeshHandle navMesh);

// NavMeshQuery functions
RECASTNAVIGATIONUNITY_API NavMeshQueryHandle CreateNavMeshQuery(NavMeshHandle navMesh, int maxNodes);

RECASTNAVIGATIONUNITY_API void DestroyNavMeshQuery(NavMeshQueryHandle query);

RECASTNAVIGATIONUNITY_API PathResult FindPath(
    NavMeshQueryHandle query,
    UnityVector3 startPos,
    UnityVector3 endPos,
    QueryFilter* filter
);

RECASTNAVIGATIONUNITY_API UnityVector3 GetClosestPoint(
    NavMeshQueryHandle query,
    UnityVector3 position,
    QueryFilter* filter
);

RECASTNAVIGATIONUNITY_API int Raycast(
    NavMeshQueryHandle query,
    UnityVector3 startPos,
    UnityVector3 endPos,
    QueryFilter* filter,
    UnityVector3* hitPos,
    UnityVector3* hitNormal
);

// Crowd functions
RECASTNAVIGATIONUNITY_API CrowdHandle CreateCrowd(NavMeshHandle navMesh, int maxAgents, float maxAgentRadius);

RECASTNAVIGATIONUNITY_API void DestroyCrowd(CrowdHandle crowd);

RECASTNAVIGATIONUNITY_API AgentHandle AddAgent(
    CrowdHandle crowd,
    UnityVector3 position,
    AgentParams* params
);

RECASTNAVIGATIONUNITY_API void RemoveAgent(CrowdHandle crowd, AgentHandle agent);

RECASTNAVIGATIONUNITY_API int SetAgentTarget(
    CrowdHandle crowd,
    AgentHandle agent,
    UnityVector3 target
);

RECASTNAVIGATIONUNITY_API UnityVector3 GetAgentPosition(CrowdHandle crowd, AgentHandle agent);

RECASTNAVIGATIONUNITY_API UnityVector3 GetAgentVelocity(CrowdHandle crowd, AgentHandle agent);

RECASTNAVIGATIONUNITY_API void UpdateCrowd(CrowdHandle crowd, float deltaTime);

// Utility functions
RECASTNAVIGATIONUNITY_API void FreePathResult(PathResult* result);

#ifdef __cplusplus
}
#endif 