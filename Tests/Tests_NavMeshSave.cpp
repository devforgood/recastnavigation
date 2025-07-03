#include "catch2/catch_all.hpp"
#include "RecastNavigationUnity.h"
#include "DetourNavMesh.h"
#include "DetourNavMeshBuilder.h"
#include "Recast.h"
#include "TestHelpers.h"
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

// Helper function to get NavMesh properties
struct NavMeshProperties {
    int polyCount;
    int vertCount;
    float bmin[3];
    float bmax[3];
};

NavMeshProperties GetNavMeshProperties(NavMeshHandle navMeshHandle) {
    NavMeshProperties props = {0, 0, {0, 0, 0}, {0, 0, 0}};
    
    if (!navMeshHandle) {
        return props;
    }

    const dtNavMesh* navMesh = static_cast<const dtNavMesh*>(navMeshHandle);
    if (!navMesh) {
        return props;
    }

    // Count polygons and vertices using public API
    int tileCount = navMesh->getMaxTiles();
    for (int i = 0; i < tileCount; ++i) {
        const dtMeshTile* tile = navMesh->getTile(i);
        if (!tile || !tile->header) continue;
        
        props.polyCount += tile->header->polyCount;
        props.vertCount += tile->header->vertCount;
    }

    // Get bounds from first tile
    for (int i = 0; i < tileCount; ++i) {
        const dtMeshTile* tile = navMesh->getTile(i);
        if (!tile || !tile->header) continue;
        
        props.bmin[0] = tile->header->bmin[0];
        props.bmin[1] = tile->header->bmin[1];
        props.bmin[2] = tile->header->bmin[2];
        props.bmax[0] = tile->header->bmax[0];
        props.bmax[1] = tile->header->bmax[1];
        props.bmax[2] = tile->header->bmax[2];
        break;
    }

    return props;
}

// NavMesh binary file format constants
static const int NAVMESHSET_MAGIC = 'M'<<24 | 'S'<<16 | 'E'<<8 | 'T'; //'MSET';
static const int NAVMESHSET_VERSION = 1;

struct NavMeshSetHeader
{
    int magic;
    int version;
    int numTiles;
    dtNavMeshParams params;
};

struct NavMeshTileHeader
{
    dtTileRef tileRef;
    int dataSize;
};

// Helper function to save NavMesh data to binary file (based on Sample::saveAll)
bool SaveNavMeshToBinary(const NavMeshHandle navMeshHandle, const std::string& filename) {
    if (!navMeshHandle) {
        return false;
    }

    // Cast to dtNavMesh
    const dtNavMesh* navMesh = static_cast<const dtNavMesh*>(navMeshHandle);
    if (!navMesh) {
        return false;
    }

    FILE* fp = fopen(filename.c_str(), "wb");
    if (!fp) {
        return false;
    }

    // Store header
    NavMeshSetHeader header;
    header.magic = NAVMESHSET_MAGIC;
    header.version = NAVMESHSET_VERSION;
    header.numTiles = 0;
    for (int i = 0; i < navMesh->getMaxTiles(); ++i) {
        const dtMeshTile* tile = navMesh->getTile(i);
        if (!tile || !tile->header || !tile->dataSize) continue;
        header.numTiles++;
    }
    memcpy(&header.params, navMesh->getParams(), sizeof(dtNavMeshParams));
    fwrite(&header, sizeof(NavMeshSetHeader), 1, fp);

    // Store tiles
    for (int i = 0; i < navMesh->getMaxTiles(); ++i) {
        const dtMeshTile* tile = navMesh->getTile(i);
        if (!tile || !tile->header || !tile->dataSize) continue;

        NavMeshTileHeader tileHeader;
        tileHeader.tileRef = navMesh->getTileRef(tile);
        tileHeader.dataSize = tile->dataSize;
        fwrite(&tileHeader, sizeof(tileHeader), 1, fp);

        fwrite(tile->data, tile->dataSize, 1, fp);
    }

    fclose(fp);
    return true;
}

// Helper function to load NavMesh data from binary file (based on Sample::loadAll)
NavMeshHandle LoadNavMeshFromBinary(const std::string& filename) {
    FILE* fp = fopen(filename.c_str(), "rb");
    if (!fp) return nullptr;

    // Read header
    NavMeshSetHeader header;
    size_t readLen = fread(&header, sizeof(NavMeshSetHeader), 1, fp);
    if (readLen != 1) {
        fclose(fp);
        return nullptr;
    }
    if (header.magic != NAVMESHSET_MAGIC) {
        fclose(fp);
        return nullptr;
    }
    if (header.version != NAVMESHSET_VERSION) {
        fclose(fp);
        return nullptr;
    }

    dtNavMesh* mesh = dtAllocNavMesh();
    if (!mesh) {
        fclose(fp);
        return nullptr;
    }
    dtStatus status = mesh->init(&header.params);
    if (dtStatusFailed(status)) {
        dtFreeNavMesh(mesh);
        fclose(fp);
        return nullptr;
    }

    // Read tiles
    for (int i = 0; i < header.numTiles; ++i) {
        NavMeshTileHeader tileHeader;
        readLen = fread(&tileHeader, sizeof(tileHeader), 1, fp);
        if (readLen != 1) {
            dtFreeNavMesh(mesh);
            fclose(fp);
            return nullptr;
        }

        if (!tileHeader.tileRef || !tileHeader.dataSize)
            break;

        unsigned char* data = (unsigned char*)dtAlloc(tileHeader.dataSize, DT_ALLOC_PERM);
        if (!data) break;
        memset(data, 0, tileHeader.dataSize);
        readLen = fread(data, tileHeader.dataSize, 1, fp);
        if (readLen != 1) {
            dtFree(data);
            dtFreeNavMesh(mesh);
            fclose(fp);
            return nullptr;
        }

        mesh->addTile(data, tileHeader.dataSize, DT_TILE_FREE_DATA, tileHeader.tileRef, 0);
    }

    fclose(fp);
    return mesh;
}

// Helper function to compare two NavMesh handles
bool CompareNavMeshes(NavMeshHandle navMesh1, NavMeshHandle navMesh2) {
    if (!navMesh1 || !navMesh2) {
        return false;
    }

    NavMeshProperties props1 = GetNavMeshProperties(navMesh1);
    NavMeshProperties props2 = GetNavMeshProperties(navMesh2);

    // Compare basic properties
    if (props1.polyCount != props2.polyCount) {
        return false;
    }

    if (props1.vertCount != props2.vertCount) {
        return false;
    }

    // Compare bounds
    const float epsilon = 0.001f;
    for (int i = 0; i < 3; ++i) {
        if (std::abs(props1.bmin[i] - props2.bmin[i]) > epsilon) {
            return false;
        }
        if (std::abs(props1.bmax[i] - props2.bmax[i]) > epsilon) {
            return false;
        }
    }

    return true;
}

TEST_CASE("NavMesh Save and Load Test", "[navmesh]") {
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

    SECTION("Save and Load NavMesh to/from binary file") {
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
        NavMeshHandle originalNavMesh = BuildNavMesh(unityVertices.data(), unityVertices.size(),
                                                    const_cast<int*>(indices.data()), indices.size(),
                                                    &settings);

        REQUIRE(originalNavMesh != nullptr);
        
        // Get original NavMesh properties
        NavMeshProperties originalProps = GetNavMeshProperties(originalNavMesh);

        INFO("Original NavMesh - PolyCount: " << originalProps.polyCount << ", VertCount: " << originalProps.vertCount);
        INFO("Original Bounds - Min: (" << originalProps.bmin[0] << ", " << originalProps.bmin[1] << ", " << originalProps.bmin[2] << ")");
        INFO("Original Bounds - Max: (" << originalProps.bmax[0] << ", " << originalProps.bmax[1] << ", " << originalProps.bmax[2] << ")");

        // Save NavMesh to binary file
        const std::string saveFilename = "test_navmesh.bin";
        REQUIRE(SaveNavMeshToBinary(originalNavMesh, saveFilename));

        // Verify file was created and has content
        REQUIRE(std::filesystem::exists(saveFilename));
        REQUIRE(std::filesystem::file_size(saveFilename) > 0);

        INFO("Saved NavMesh to: " << saveFilename);
        INFO("File size: " << std::filesystem::file_size(saveFilename) << " bytes");

        // Load NavMesh from binary file
        NavMeshHandle loadedNavMesh = LoadNavMeshFromBinary(saveFilename);
        REQUIRE(loadedNavMesh != nullptr);

        // Test pathfinding query on loaded NavMesh
        NavMeshQueryHandle loadedQuery = CreateNavMeshQuery(loadedNavMesh, 2048);
        REQUIRE(loadedQuery != nullptr);

        // Get loaded NavMesh properties
        NavMeshProperties loadedProps = GetNavMeshProperties(loadedNavMesh);

        INFO("Loaded NavMesh - PolyCount: " << loadedProps.polyCount << ", VertCount: " << loadedProps.vertCount);
        INFO("Loaded Bounds - Min: (" << loadedProps.bmin[0] << ", " << loadedProps.bmin[1] << ", " << loadedProps.bmin[2] << ")");
        INFO("Loaded Bounds - Max: (" << loadedProps.bmax[0] << ", " << loadedProps.bmax[1] << ", " << loadedProps.bmax[2] << ")");

        // Compare original and loaded NavMesh
        REQUIRE(CompareNavMeshes(originalNavMesh, loadedNavMesh));

        // Additional detailed checks
        REQUIRE(originalProps.polyCount == loadedProps.polyCount);
        REQUIRE(originalProps.vertCount == loadedProps.vertCount);

        const float epsilon = 0.001f;
        REQUIRE(std::abs(originalProps.bmin[0] - loadedProps.bmin[0]) < epsilon);
        REQUIRE(std::abs(originalProps.bmin[1] - loadedProps.bmin[1]) < epsilon);
        REQUIRE(std::abs(originalProps.bmin[2] - loadedProps.bmin[2]) < epsilon);
        REQUIRE(std::abs(originalProps.bmax[0] - loadedProps.bmax[0]) < epsilon);
        REQUIRE(std::abs(originalProps.bmax[1] - loadedProps.bmax[1]) < epsilon);
        REQUIRE(std::abs(originalProps.bmax[2] - loadedProps.bmax[2]) < epsilon);

        // Cleanup
        DestroyNavMesh(originalNavMesh);
        DestroyNavMesh(loadedNavMesh);

        // Clean up temporary file
        std::filesystem::remove(saveFilename);
    }

    SECTION("Test NavMesh query after save/load") {
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

        // Build NavMesh
        NavMeshHandle navMesh = BuildNavMesh(unityVertices.data(), unityVertices.size(),
                                            const_cast<int*>(indices.data()), indices.size(),
                                            &settings);

        REQUIRE(navMesh != nullptr);

        // Create NavMeshQuery for original NavMesh
        NavMeshQueryHandle query = CreateNavMeshQuery(navMesh, 2048);
        REQUIRE(query != nullptr);

        // Save and load NavMesh
        const std::string saveFilename = "test_navmesh_query.bin";
        REQUIRE(SaveNavMeshToBinary(navMesh, saveFilename));
        
        NavMeshHandle loadedNavMesh = LoadNavMeshFromBinary(saveFilename);
        REQUIRE(loadedNavMesh != nullptr);

        // Create NavMeshQuery for loaded NavMesh
        NavMeshQueryHandle loadedQuery = CreateNavMeshQuery(loadedNavMesh, 2048);
        REQUIRE(loadedQuery != nullptr);

        // Test pathfinding query on original NavMesh
        UnityVector3 startPos = { 0.0f, 0.0f, 0.0f };
        UnityVector3 endPos = { 10.0f, 0.0f, 10.0f };
        
        // Create a default query filter
        QueryFilter filter;
        memset(&filter, 0, sizeof(filter));
        filter.includeFlags = 0xffff;
        
        // Find path on original NavMesh
        PathResult originalPathResult = FindPath(query, startPos, endPos, &filter);
        
        INFO("Original path finding status: " << originalPathResult.status);
        INFO("Original path length: " << originalPathResult.pathLength);

        // Find path on loaded NavMesh
        PathResult loadedPathResult = FindPath(loadedQuery, startPos, endPos, &filter);
        
        INFO("Loaded path finding status: " << loadedPathResult.status);
        INFO("Loaded path length: " << loadedPathResult.pathLength);

        // Compare pathfinding results
        REQUIRE(originalPathResult.status == loadedPathResult.status);
        REQUIRE(originalPathResult.pathLength == loadedPathResult.pathLength);

        // Clean up path results
        FreePathResult(&originalPathResult);
        FreePathResult(&loadedPathResult);
        
        // Cleanup
        DestroyNavMeshQuery(query);
        DestroyNavMeshQuery(loadedQuery);

        // Cleanup
        DestroyNavMesh(navMesh);
        DestroyNavMesh(loadedNavMesh);
        std::filesystem::remove(saveFilename);
    }
    
    // Cleanup UnityWrapper
    CleanupRecastNavigation();
} 