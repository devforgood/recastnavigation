#include "catch2/catch_all.hpp"
#include "Recast.h"
#include "DetourNavMesh.h"
#include "DetourNavMeshBuilder.h"
#include "RecastNavigationUnity.h"
#include <memory>
#include <vector>
#include <fstream>
#include <sstream>
#include <iostream>
#include <filesystem>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <cmath>

// Helper functions from MeshLoaderObj.cpp (forward declarations)
static char* parseRow(char* buf, char* bufEnd, char* row, int len);
static int parseFace(char* row, int* data, int n, int vcnt);

// Test data structures for comparison
struct NavMeshData {
    std::vector<float> vertices;
    std::vector<int> indices;
    std::vector<unsigned short> polys;
    std::vector<unsigned short> verts;
    std::vector<unsigned char> areas;
    std::vector<unsigned short> flags;
    int polyCount;
    int vertCount;
    int maxVertsPerPoly;
    float bmin[3];
    float bmax[3];
    float cellSize;
    float cellHeight;
};

// Helper function to load OBJ file (based on MeshLoaderObj.cpp)
bool LoadObjFile(const std::string& filename, std::vector<float>& vertices, std::vector<int>& indices) {
    FILE* fp = fopen(filename.c_str(), "rb");
    if (!fp) {
        return false;
    }
    
    // Get file size
    if (fseek(fp, 0, SEEK_END) != 0) {
        fclose(fp);
        return false;
    }
    long bufSize = ftell(fp);
    if (bufSize < 0) {
        fclose(fp);
        return false;
    }
    if (fseek(fp, 0, SEEK_SET) != 0) {
        fclose(fp);
        return false;
    }
    
    // Read file content
    char* buf = new char[bufSize];
    if (!buf) {
        fclose(fp);
        return false;
    }
    size_t readLen = fread(buf, bufSize, 1, fp);
    fclose(fp);
    
    if (readLen != 1) {
        delete[] buf;
        return false;
    }
    
    // Parse OBJ file
    char* src = buf;
    char* srcEnd = buf + bufSize;
    char row[512];
    int face[32];
    float x, y, z;
    int nv;
    int vcap = 0;
    int tcap = 0;
    int vertCount = 0;
    int triCount = 0;
    
    // Temporary storage
    std::vector<float> tempVertices;
    std::vector<int> tempIndices;
    
    while (src < srcEnd) {
        // Parse one row
        row[0] = '\0';
        src = parseRow(src, srcEnd, row, sizeof(row)/sizeof(char));
        
        // Skip comments
        if (row[0] == '#') continue;
        
        if (row[0] == 'v' && row[1] != 'n' && row[1] != 't') {
            // Vertex position
            sscanf(row+1, "%f %f %f", &x, &y, &z);
            
            // Add vertex with scaling (scale = 1.0f)
            float scale = 1.0f;
            tempVertices.push_back(x * scale);
            tempVertices.push_back(y * scale);
            tempVertices.push_back(z * scale);
            vertCount++;
        }
        else if (row[0] == 'f') {
            // Faces
            nv = parseFace(row+1, face, 32, vertCount);
            for (int i = 2; i < nv; ++i) {
                const int a = face[0];
                const int b = face[i-1];
                const int c = face[i];
                if (a < 0 || a >= vertCount || b < 0 || b >= vertCount || c < 0 || c >= vertCount)
                    continue;
                tempIndices.push_back(a);
                tempIndices.push_back(b);
                tempIndices.push_back(c);
                triCount++;
            }
        }
    }
    
    delete[] buf;
    
    vertices = tempVertices;
    indices = tempIndices;
    return true;
}

// Helper functions from MeshLoaderObj.cpp
static char* parseRow(char* buf, char* bufEnd, char* row, int len) {
    bool start = true;
    bool done = false;
    int n = 0;
    while (!done && buf < bufEnd) {
        char c = *buf;
        buf++;
        // multirow
        switch (c) {
            case '\\':
                break;
            case '\n':
                if (start) break;
                done = true;
                break;
            case '\r':
                break;
            case '\t':
            case ' ':
                if (start) break;
                // else falls through
            default:
                start = false;
                row[n++] = c;
                if (n >= len-1)
                    done = true;
                break;
        }
    }
    row[n] = '\0';
    return buf;
}

static int parseFace(char* row, int* data, int n, int vcnt) {
    int j = 0;
    while (*row != '\0') {
        // Skip initial white space
        while (*row != '\0' && (*row == ' ' || *row == '\t'))
            row++;
        char* s = row;
        // Find vertex delimiter and terminated the string there for conversion.
        while (*row != '\0' && *row != ' ' && *row != '\t') {
            if (*row == '/') *row = '\0';
            row++;
        }
        if (*s == '\0')
            continue;
        int vi = atoi(s);
        data[j++] = vi < 0 ? vi+vcnt : vi-1;
        if (j >= n) return j;
    }
    return j;
}

// Helper function to extract NavMesh data from dtNavMesh
NavMeshData ExtractNavMeshData(const dtNavMesh* navMesh) {
    NavMeshData data;
    const dtNavMeshParams* params = navMesh->getParams();
    for (int i = 0; i < 3; ++i) {
        data.bmin[i] = params->orig[i];
        data.bmax[i] = params->orig[i] + (i == 0 ? params->tileWidth : (i == 2 ? params->tileHeight : 0));
    }
    data.maxVertsPerPoly = params->maxPolys; // 실제로는 nvp가 필요하지만, params에 없으므로 비교에서 제외

    data.polyCount = 0;
    data.vertCount = 0;
    int tileCount = navMesh->getMaxTiles();
    for (int i = 0; i < tileCount; ++i) {
        const dtMeshTile* tile = navMesh->getTile(i);
        if (!tile || !tile->header) continue;
        for (int j = 0; j < tile->header->vertCount; ++j) {
            data.verts.push_back(tile->verts[j * 3]);
            data.verts.push_back(tile->verts[j * 3 + 1]);
            data.verts.push_back(tile->verts[j * 3 + 2]);
            data.vertCount++;
        }
        for (int j = 0; j < tile->header->polyCount; ++j) {
            const dtPoly* poly = &tile->polys[j];
            for (int k = 0; k < poly->vertCount; ++k) {
                data.polys.push_back(poly->verts[k]);
            }
            data.areas.push_back(poly->getArea());
            data.flags.push_back(poly->flags);
            data.polyCount++;
        }
    }
    return data;
}

// RecastDemo style NavMesh building
NavMeshData BuildNavMeshRecastDemoStyle(const std::vector<float>& vertices, const std::vector<int>& indices) {
    // Default build settings (similar to RecastDemo)
    rcConfig config;
    memset(&config, 0, sizeof(config));
    config.cs = 0.3f;
    config.ch = 0.2f;
    config.walkableSlopeAngle = 45.0f;
    config.walkableHeight = 6;
    config.walkableClimb = 4;
    config.walkableRadius = 6;
    config.maxEdgeLen = 12;
    config.maxSimplificationError = 1.3f;
    config.minRegionArea = 8;
    config.mergeRegionArea = 20;
    config.maxVertsPerPoly = 6;
    config.detailSampleDist = 6.0f;
    config.detailSampleMaxError = 1.0f;

    // Calculate bounds
    rcCalcBounds(vertices.data(), vertices.size() / 3, config.bmin, config.bmax);
    rcCalcGridSize(config.bmin, config.bmax, config.cs, &config.width, &config.height);

    // Create context
    rcContext* ctx = new rcContext(true);

    // Create heightfield
    rcHeightfield* hf = rcAllocHeightfield();
    if (!rcCreateHeightfield(ctx, *hf, config.width, config.height, config.bmin, config.bmax, config.cs, config.ch)) {
        delete ctx;
        return NavMeshData();
    }

    // Mark walkable areas
    std::vector<unsigned char> triAreaIDs(indices.size() / 3, RC_WALKABLE_AREA);
    rcMarkWalkableTriangles(ctx, config.walkableSlopeAngle,
                           vertices.data(), vertices.size() / 3,
                           indices.data(), indices.size() / 3, triAreaIDs.data());

    // Rasterize triangles
    if (!rcRasterizeTriangles(ctx, vertices.data(), vertices.size() / 3,
                             indices.data(), triAreaIDs.data(), indices.size() / 3, *hf)) {
        rcFreeHeightField(hf);
        delete ctx;
        return NavMeshData();
    }

    // Filter walkable surfaces
    rcFilterLowHangingWalkableObstacles(ctx, config.walkableClimb, *hf);
    rcFilterLedgeSpans(ctx, config.walkableHeight, config.walkableClimb, *hf);
    rcFilterWalkableLowHeightSpans(ctx, config.walkableHeight, *hf);

    // Build compact heightfield
    rcCompactHeightfield* chf = rcAllocCompactHeightfield();
    if (!rcBuildCompactHeightfield(ctx, config.walkableHeight, config.walkableClimb, *hf, *chf)) {
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return NavMeshData();
    }

    // Erode walkable areas
    if (!rcErodeWalkableArea(ctx, config.walkableRadius, *chf)) {
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return NavMeshData();
    }

    // Build distance field
    if (!rcBuildDistanceField(ctx, *chf)) {
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return NavMeshData();
    }

    // Build regions
    if (!rcBuildRegions(ctx, *chf, 0, config.minRegionArea, config.mergeRegionArea)) {
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return NavMeshData();
    }

    // Build contours
    rcContourSet* cset = rcAllocContourSet();
    if (!rcBuildContours(ctx, *chf, config.maxSimplificationError, config.maxEdgeLen, *cset, RC_CONTOUR_TESS_WALL_EDGES)) {
        rcFreeContourSet(cset);
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return NavMeshData();
    }

    // Build polygon mesh
    rcPolyMesh* pmesh = rcAllocPolyMesh();
    if (!rcBuildPolyMesh(ctx, *cset, config.maxVertsPerPoly, *pmesh)) {
        rcFreePolyMesh(pmesh);
        rcFreeContourSet(cset);
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return NavMeshData();
    }

    // Build detail mesh
    rcPolyMeshDetail* dmesh = rcAllocPolyMeshDetail();
    if (!rcBuildPolyMeshDetail(ctx, *pmesh, *chf, config.detailSampleDist, config.detailSampleMaxError, *dmesh)) {
        rcFreePolyMeshDetail(dmesh);
        rcFreePolyMesh(pmesh);
        rcFreeContourSet(cset);
        rcFreeCompactHeightfield(chf);
        rcFreeHeightField(hf);
        delete ctx;
        return NavMeshData();
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
        return NavMeshData();
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
    params.walkableHeight = config.walkableHeight;
    params.walkableRadius = config.walkableRadius;
    params.walkableClimb = config.walkableClimb;
    params.tileX = 0;
    params.tileY = 0;
    params.tileLayer = 0;
    params.bmin[0] = pmesh->bmin[0];
    params.bmin[1] = pmesh->bmin[1];
    params.bmin[2] = pmesh->bmin[2];
    params.bmax[0] = pmesh->bmax[0];
    params.bmax[1] = pmesh->bmax[1];
    params.bmax[2] = pmesh->bmax[2];
    params.cs = config.cs;
    params.ch = config.ch;
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
        return NavMeshData();
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
        return NavMeshData();
    }

    // Extract data
    NavMeshData result = ExtractNavMeshData(navMesh);

    // Cleanup
    dtFreeNavMesh(navMesh);
    rcFreePolyMeshDetail(dmesh);
    rcFreePolyMesh(pmesh);
    rcFreeContourSet(cset);
    rcFreeCompactHeightfield(chf);
    rcFreeHeightField(hf);
    delete ctx;

    return result;
}

// UnityWrapper style NavMesh building
NavMeshData BuildNavMeshUnityWrapperStyle(const std::vector<float>& vertices, const std::vector<int>& indices) {
    // Convert to Unity format
    std::vector<UnityVector3> unityVertices;
    for (size_t i = 0; i < vertices.size(); i += 3) {
        UnityVector3 v = { vertices[i], vertices[i + 1], vertices[i + 2] };
        unityVertices.push_back(v);
    }

    // Create build settings
    BuildSettings settings;
    settings.cellSize = 0.3f;
    settings.cellHeight = 0.2f;
    settings.walkableSlopeAngle = 45.0f;
    settings.walkableHeight = 6;
    settings.walkableRadius = 6;
    settings.walkableClimb = 4;
    settings.minRegionArea = 8;
    settings.mergeRegionArea = 20;
    settings.maxVertsPerPoly = 6;
    settings.detailSampleDist = 6.0f;
    settings.detailSampleMaxError = 1.0f;
    settings.tileSize = 0;
    settings.maxSimplificationError = 1.3f;
    settings.maxEdgeLen = 12;

    // Calculate bounds
    float bmin[3], bmax[3];
    rcCalcBounds(vertices.data(), vertices.size() / 3, bmin, bmax);
    settings.bmin[0] = bmin[0];
    settings.bmin[1] = bmin[1];
    settings.bmin[2] = bmin[2];
    settings.bmax[0] = bmax[0];
    settings.bmax[1] = bmax[1];
    settings.bmax[2] = bmax[2];

    // Calculate grid size
    int width, height;
    rcCalcGridSize(bmin, bmax, settings.cellSize, &width, &height);
    settings.width = width;
    settings.height = height;

    // Build NavMesh using UnityWrapper
    NavMeshHandle navMeshHandle = BuildNavMesh(unityVertices.data(), unityVertices.size(),
                                              const_cast<int*>(indices.data()), indices.size(),
                                              &settings);

    if (!navMeshHandle) {
        return NavMeshData();
    }

    // Extract data from the NavMesh handle
    dtNavMesh* navMesh = static_cast<dtNavMesh*>(navMeshHandle);
    NavMeshData result = ExtractNavMeshData(navMesh);

    // Cleanup
    DestroyNavMesh(navMeshHandle);

    return result;
}

// Comparison function
bool CompareNavMeshData(const NavMeshData& data1, const NavMeshData& data2) {
    // Compare basic properties
    if (data1.polyCount != data2.polyCount) return false;
    if (data1.vertCount != data2.vertCount) return false;
    if (data1.maxVertsPerPoly != data2.maxVertsPerPoly) return false;
    // cellSize, cellHeight 비교 제거
    // Compare bounds
    for (int i = 0; i < 3; ++i) {
        if (std::abs(data1.bmin[i] - data2.bmin[i]) > 0.001f) return false;
        if (std::abs(data1.bmax[i] - data2.bmax[i]) > 0.001f) return false;
    }
    // Compare vertices
    if (data1.verts.size() != data2.verts.size()) return false;
    for (size_t i = 0; i < data1.verts.size(); ++i) {
        if (std::abs(data1.verts[i] - data2.verts[i]) > 0.001f) return false;
    }
    // Compare polygons
    if (data1.polys.size() != data2.polys.size()) return false;
    for (size_t i = 0; i < data1.polys.size(); ++i) {
        if (data1.polys[i] != data2.polys[i]) return false;
    }
    // Compare areas
    if (data1.areas.size() != data2.areas.size()) return false;
    for (size_t i = 0; i < data1.areas.size(); ++i) {
        if (data1.areas[i] != data2.areas[i]) return false;
    }
    // Compare flags
    if (data1.flags.size() != data2.flags.size()) return false;
    for (size_t i = 0; i < data1.flags.size(); ++i) {
        if (data1.flags[i] != data2.flags[i]) return false;
    }
    return true;
}

TEST_CASE("NavMesh Generation Comparison", "[navmesh]") {
    // Initialize UnityWrapper
    REQUIRE(InitializeRecastNavigation() == 1);
    
    // Load test mesh
    std::vector<float> vertices;
    std::vector<int> indices;
	// 현재 실행 경로 출력
	std::cout << "Current working directory: " << std::filesystem::current_path() << std::endl;

    
    REQUIRE(LoadObjFile("nav_test.obj", vertices, indices));
    REQUIRE(vertices.size() > 0);
    REQUIRE(indices.size() > 0);

    SECTION("Compare RecastDemo vs UnityWrapper NavMesh generation") {
        // Build NavMesh using RecastDemo style
        NavMeshData recastDemoData = BuildNavMeshRecastDemoStyle(vertices, indices);
        REQUIRE(recastDemoData.polyCount > 0);

        // Build NavMesh using UnityWrapper style
        NavMeshData unityWrapperData = BuildNavMeshUnityWrapperStyle(vertices, indices);
        REQUIRE(unityWrapperData.polyCount > 0);

        // Compare results
        REQUIRE(CompareNavMeshData(recastDemoData, unityWrapperData));

        // Additional detailed checks
        INFO("RecastDemo polyCount: " << recastDemoData.polyCount);
        INFO("UnityWrapper polyCount: " << unityWrapperData.polyCount);
        INFO("RecastDemo vertCount: " << recastDemoData.vertCount);
        INFO("UnityWrapper vertCount: " << unityWrapperData.vertCount);
        INFO("RecastDemo vertex count: " << recastDemoData.verts.size());
        INFO("UnityWrapper vertex count: " << unityWrapperData.verts.size());
    }
    
    // Cleanup UnityWrapper
    CleanupRecastNavigation();
} 