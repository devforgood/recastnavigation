#pragma once

#include "RecastNavigationUnity.h"
#include <memory>

// Forward declarations
struct dtNavMesh;

class UnityNavMeshBuilder {
public:
    UnityNavMeshBuilder();
    ~UnityNavMeshBuilder();

    std::shared_ptr<dtNavMesh> BuildNavMesh(
        UnityVector3* vertices, int vertexCount,
        int* indices, int indexCount,
        BuildSettings* settings);

    std::shared_ptr<dtNavMesh> BuildNavMeshFromHeightfield(
        float* heightfield, int width, int height,
        float originX, float originY, float originZ,
        float cellSize, float cellHeight,
        BuildSettings* settings);
}; 