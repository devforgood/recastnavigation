#include "UnityNavMeshBuilder.h"
#include "RecastNavigationUnity.h"
#include "DetourNavMesh.h"
#include "DetourNavMeshBuilder.h"
#include "Recast.h"
#include "RecastAlloc.h"
#include <memory>
#include <vector>

UnityNavMeshBuilder::UnityNavMeshBuilder() {
}

UnityNavMeshBuilder::~UnityNavMeshBuilder() {
}

std::shared_ptr<dtNavMesh> UnityNavMeshBuilder::BuildNavMesh(
    UnityVector3* vertices, int vertexCount,
    int* indices, int indexCount,
    BuildSettings* settings) {
    
    if (!vertices || !indices || !settings || vertexCount <= 0 || indexCount <= 0) {
        return nullptr;
    }

    // Create recast context
    rcContext* ctx = new rcContext(true);
    
    // Create heightfield
    rcHeightfield* hf = rcAllocHeightfield();
    if (!hf) {
        delete ctx;
        return nullptr;
    }
    
    if (!rcCreateHeightfield(ctx, *hf, settings->width, settings->height, 
                           settings->bmin, settings->bmax, 
                           settings->cellSize, settings->cellHeight)) {
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }

    // Mark walkable areas
    std::vector<unsigned char> triAreaIDs(indexCount / 3, RC_WALKABLE_AREA);
    rcMarkWalkableTriangles(ctx, settings->walkableSlopeAngle,
                           reinterpret_cast<const float*>(vertices), vertexCount, 
                           indices, indexCount / 3, triAreaIDs.data());
    
    // Rasterize triangles
    if (!rcRasterizeTriangles(ctx, reinterpret_cast<const float*>(vertices), vertexCount,
                             indices, triAreaIDs.data(), indexCount / 3, *hf)) {
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }

    // Filter walkable surfaces
	rcFilterLowHangingWalkableObstacles(ctx, settings->walkableClimb, *hf);
	rcFilterLedgeSpans(ctx, settings->walkableHeight, settings->walkableClimb, *hf);
	rcFilterWalkableLowHeightSpans(ctx, settings->walkableHeight, *hf);

    // Build compact heightfield
    rcCompactHeightfield* chf = rcAllocCompactHeightfield();
    if (!chf) {
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }
    
    if (!rcBuildCompactHeightfield(ctx, settings->walkableHeight, settings->walkableClimb, *hf, *chf)) {
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }

    // Erode walkable areas
    if (!rcErodeWalkableArea(ctx, settings->walkableRadius, *chf)) {
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }

    // Build distance field
    if (!rcBuildDistanceField(ctx, *chf)) {
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }

    // Build regions
    if (!rcBuildRegions(ctx, *chf, 0, settings->minRegionArea, settings->mergeRegionArea)) {
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }

    // Build contours
    rcContourSet* cset = rcAllocContourSet();
    if (!cset) {
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }
    
    if (!rcBuildContours(ctx, *chf, settings->maxSimplificationError, settings->maxEdgeLen, *cset, RC_CONTOUR_TESS_WALL_EDGES)) {
        rcFreeContourSet(cset);
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }

    // Build polygon mesh
    rcPolyMesh* pmesh = rcAllocPolyMesh();
    if (!pmesh) {
        rcFreeContourSet(cset);
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }
    
    if (!rcBuildPolyMesh(ctx, *cset, settings->maxVertsPerPoly, *pmesh)) {
        rcFreePolyMesh(pmesh);
        rcFreeContourSet(cset);
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }

    // Build detail mesh
    rcPolyMeshDetail* dmesh = rcAllocPolyMeshDetail();
    if (!dmesh) {
        rcFreePolyMesh(pmesh);
        rcFreeContourSet(cset);
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }
    
    if (!rcBuildPolyMeshDetail(ctx, *pmesh, *chf, settings->detailSampleDist, settings->detailSampleMaxError, *dmesh)) {
        rcFreePolyMeshDetail(dmesh);
        rcFreePolyMesh(pmesh);
        rcFreeContourSet(cset);
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }

    // Create Detour navmesh
    dtNavMesh* navMesh = dtAllocNavMesh();
    if (!navMesh) {
        rcFreePolyMeshDetail(dmesh);
        rcFreePolyMesh(pmesh);
        rcFreeContourSet(cset);
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }

    // Initialize navmesh params
    dtNavMeshCreateParams params;
    memset(&params, 0, sizeof(params));
    params.verts = pmesh->verts;
    params.vertCount = pmesh->nverts;
    params.polys = pmesh->polys;
    params.polyAreas = pmesh->areas;
    params.polyFlags = pmesh->flags;
    params.polyCount = pmesh->npolys;
    params.nvp = pmesh->nvp;
    params.detailMeshes = dmesh->meshes;
    params.detailVerts = dmesh->verts;
    params.detailVertsCount = dmesh->nverts;
    params.detailTris = dmesh->tris;
    params.detailTriCount = dmesh->ntris;
    params.offMeshConVerts = 0;
    params.offMeshConRad = 0;
    params.offMeshConDir = 0;
    params.offMeshConAreas = 0;
    params.offMeshConFlags = 0;
    params.offMeshConUserID = 0;
    params.offMeshConCount = 0;
    params.walkableHeight = settings->walkableHeight;
    params.walkableRadius = settings->walkableRadius;
    params.walkableClimb = settings->walkableClimb;
    params.tileX = 0;
    params.tileY = 0;
    params.tileLayer = 0;
    params.bmin[0] = pmesh->bmin[0];
    params.bmin[1] = pmesh->bmin[1];
    params.bmin[2] = pmesh->bmin[2];
    params.bmax[0] = pmesh->bmax[0];
    params.bmax[1] = pmesh->bmax[1];
    params.bmax[2] = pmesh->bmax[2];
    params.cs = settings->cellSize;
    params.ch = settings->cellHeight;
    params.buildBvTree = true;

    // Create navmesh data
    unsigned char* navData = 0;
    int navDataSize = 0;
    if (!dtCreateNavMeshData(&params, &navData, &navDataSize)) {
        dtFreeNavMesh(navMesh);
        rcFreePolyMeshDetail(dmesh);
        rcFreePolyMesh(pmesh);
        rcFreeContourSet(cset);
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }

    // Initialize navmesh
    if (dtStatusFailed(navMesh->init(navData, navDataSize, DT_TILE_FREE_DATA))) {
        dtFreeNavMesh(navMesh);
        dtFree(navData);
        rcFreePolyMeshDetail(dmesh);
        rcFreePolyMesh(pmesh);
        rcFreeContourSet(cset);
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }

    // Cleanup
    rcFreePolyMeshDetail(dmesh);
    rcFreePolyMesh(pmesh);
    rcFreeContourSet(cset);
    rcFreeCompactHeightfield(chf);
    rcFreeHeightField(hf);
    delete ctx;

    return std::shared_ptr<dtNavMesh>(navMesh, [](dtNavMesh* mesh) {
        if (mesh) dtFreeNavMesh(mesh);
    });
}

std::shared_ptr<dtNavMesh> UnityNavMeshBuilder::BuildNavMeshFromHeightfield(
    float* heightfield, int width, int height,
    float originX, float originY, float originZ,
    float cellSize, float cellHeight,
    BuildSettings* settings) {
    
    if (!heightfield || !settings || width <= 0 || height <= 0) {
        return nullptr;
    }

    // Create recast context
    rcContext* ctx = new rcContext(true);
    
    // Create heightfield
    rcHeightfield* hf = rcAllocHeightfield();
    if (!hf) {
        delete ctx;
        return nullptr;
    }
    
    // Calculate bounds
    float bmin[3] = { originX, originY, originZ };
    float bmax[3] = { 
        originX + width * cellSize, 
        originY + height * cellHeight, 
        originZ + width * cellSize 
    };
    
    if (!rcCreateHeightfield(ctx, *hf, width, height, bmin, bmax, cellSize, cellHeight)) {
        rcFreeHeightField(hf);
        delete ctx;
        return nullptr;
    }

    // Rasterize heightfield using rcAddSpan
    for (int z = 0; z < height; z++) {
        for (int x = 0; x < width; x++) {
            float h = heightfield[z * width + x];
            if (h > -9999.0f) {
                unsigned short spanMin = (unsigned short)((h - originY) / cellHeight);
                unsigned short spanMax = spanMin + 1;
                rcAddSpan(ctx, *hf, x, z, spanMin, spanMax, RC_WALKABLE_AREA, 1);
            }
        }
    }

    // Continue with the same process as BuildNavMesh...
    // (This is a simplified version - you would need to implement the full pipeline)
    
    // For now, return nullptr as this is a placeholder
    rcFreeHeightField(hf);
    delete ctx;
    return nullptr;
} 