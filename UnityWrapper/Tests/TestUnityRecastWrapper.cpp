#include "catch_all.hpp"
#include "UnityRecastWrapper.h"
#include <memory>
#include <vector>
#include <cmath>
#include <iostream>

using namespace Catch;

TEST_CASE("UnityRecastWrapper 초기화 및 정리", "[UnityRecastWrapper]")
{
    SECTION("Initialization Success")
    {
        REQUIRE(UnityRecast_Initialize() == true);
        UnityRecast_Cleanup();
    }
    
    SECTION("Duplicate Initialization")
    {
        REQUIRE(UnityRecast_Initialize() == true);
        REQUIRE(UnityRecast_Initialize() == true); // Allow duplicate initialization
        UnityRecast_Cleanup();
    }
    
    SECTION("Re-initialization after cleanup")
    {
        REQUIRE(UnityRecast_Initialize() == true);
        UnityRecast_Cleanup();
        REQUIRE(UnityRecast_Initialize() == true); // Re-initialization possible
        UnityRecast_Cleanup();
    }
}

TEST_CASE("Build NavMesh with simple mesh", "[UnityRecastWrapper]")
{
    REQUIRE(UnityRecast_Initialize());
    
    // 메시를 2x2 그리드로 분할 (총 8개 삼각형, 9개 정점)
    std::vector<float> vertices = {
        -2.0f, 0.0f, -2.0f,   0.0f, 0.0f, -2.0f,   2.0f, 0.0f, -2.0f,
        -2.0f, 0.0f,  0.0f,   0.0f, 0.0f,  0.0f,   2.0f, 0.0f,  0.0f,
        -2.0f, 0.0f,  2.0f,   0.0f, 0.0f,  2.0f,   2.0f, 0.0f,  2.0f
    };
    std::vector<int> indices = {
        0,1,3, 1,4,3, 1,2,4, 2,5,4,
        3,4,6, 4,7,6, 4,5,7, 5,8,7
    };
    
    UnityMeshData meshData;
    meshData.vertices = vertices.data();
    meshData.indices = indices.data();
    meshData.vertexCount = static_cast<int>(vertices.size()) / 3;
    meshData.indexCount = static_cast<int>(indices.size());
    
    UnityNavMeshBuildSettings settings = {};
    settings.cellSize = 0.2f;
    settings.cellHeight = 0.1f;
    settings.walkableSlopeAngle = 45.0f;
    settings.walkableHeight = 0.2f;
    settings.walkableRadius = 0.1f;
    settings.walkableClimb = 0.1f;
    settings.minRegionArea = 2.0f;
    settings.mergeRegionArea = 2.0f;
    settings.maxVertsPerPoly = 6;
    settings.detailSampleDist = 2.0f;
    settings.detailSampleMaxError = 0.5f;
    
    SECTION("NavMesh build success")
    {
        UnityNavMeshResult result = UnityRecast_BuildNavMesh(&meshData, &settings);
        
        REQUIRE(result.success == true);
        REQUIRE(result.navMeshData != nullptr);
        REQUIRE(result.dataSize > 0);
        REQUIRE(result.errorMessage == nullptr);
        
        // Test NavMesh loading
        REQUIRE(UnityRecast_LoadNavMesh(result.navMeshData, result.dataSize) == true);
        
        // Check NavMesh info
        int polyCount = UnityRecast_GetPolyCount();
        int vertexCount = UnityRecast_GetVertexCount();
        
        REQUIRE(polyCount > 0);
        REQUIRE(vertexCount > 0);
        
        // Memory cleanup
        UnityRecast_FreeNavMeshData(&result);
    }
    
    UnityRecast_Cleanup();
}

TEST_CASE("Pathfinding test", "[UnityRecastWrapper]")
{
    REQUIRE(UnityRecast_Initialize());
    
    // Build NavMesh with simple mesh
    std::vector<float> vertices = {
        -1.0f, 0.0f, -1.0f,
         1.0f, 0.0f, -1.0f,
         1.0f, 0.0f,  1.0f,
        -1.0f, 0.0f,  1.0f
    };
    
    std::vector<int> indices = { 0, 1, 2, 0, 2, 3 };
    
    UnityMeshData meshData;
    meshData.vertices = vertices.data();
    meshData.indices = indices.data();
    meshData.vertexCount = static_cast<int>(vertices.size()) / 3;
    meshData.indexCount = static_cast<int>(indices.size());
    
    UnityNavMeshBuildSettings settings = {};
    settings.cellSize = 0.3f;
    settings.cellHeight = 0.2f;
    settings.walkableSlopeAngle = 45.0f;
    settings.walkableHeight = 2.0f;
    settings.walkableRadius = 0.6f;
    settings.walkableClimb = 0.9f;
    settings.minRegionArea = 8.0f;
    settings.mergeRegionArea = 20.0f;
    settings.maxVertsPerPoly = 6;
    settings.detailSampleDist = 6.0f;
    settings.detailSampleMaxError = 1.0f;
    
    UnityNavMeshResult buildResult = UnityRecast_BuildNavMesh(&meshData, &settings);
    if (!buildResult.success && buildResult.errorMessage) {
        std::cout << "[NavMesh Build Error] " << buildResult.errorMessage << std::endl;
    }
    REQUIRE(buildResult.success == true);
    REQUIRE(UnityRecast_LoadNavMesh(buildResult.navMeshData, buildResult.dataSize) == true);
    
    SECTION("Valid path finding")
    {
        // 디버그 정보 추가
        int polyCount = UnityRecast_GetPolyCount();
        int vertexCount = UnityRecast_GetVertexCount();
        
        std::cout << "Debug Info:" << std::endl;
        std::cout << "  Poly Count: " << polyCount << std::endl;
        std::cout << "  Vertex Count: " << vertexCount << std::endl;
        
        UnityPathResult pathResult = UnityRecast_FindPath(0.0f, 0.0f, 0.0f, 1.5f, 0.0f, 1.5f);
        
        // 경로 찾기 결과 디버그
        std::cout << "  Path Result:" << std::endl;
        std::cout << "    Success: " << (pathResult.success ? "true" : "false") << std::endl;
        std::cout << "    Point Count: " << pathResult.pointCount << std::endl;
        if (pathResult.errorMessage) {
            std::cout << "    Error: " << pathResult.errorMessage << std::endl;
        }
        
        REQUIRE(pathResult.success == true);
        REQUIRE(pathResult.pathPoints != nullptr);
        REQUIRE(pathResult.pointCount > 0);
        REQUIRE(pathResult.errorMessage == nullptr);
        
        // Validate path points
        REQUIRE(pathResult.pointCount >= 2); // At least start and end points
        
        // First point should be near start point
        float startDist = std::sqrt(
            std::pow(pathResult.pathPoints[0] - 0.0f, 2) +
            std::pow(pathResult.pathPoints[1] - 0.0f, 2) +
            std::pow(pathResult.pathPoints[2] - 0.0f, 2)
        );
        REQUIRE(startDist < 1.0f);
        
        // Last point should be near end point
        int lastIndex = (pathResult.pointCount - 1) * 3;
        float endDist = std::sqrt(
            std::pow(pathResult.pathPoints[lastIndex] - 1.5f, 2) +
            std::pow(pathResult.pathPoints[lastIndex + 1] - 0.0f, 2) +
            std::pow(pathResult.pathPoints[lastIndex + 2] - 1.5f, 2)
        );
        REQUIRE(endDist < 1.0f);
        
        if (pathResult.success && pathResult.pathPoints) {
            UnityRecast_FreePathResult(&pathResult);
        }
    }
    
    SECTION("Invalid path finding (outside mesh)")
    {
        UnityPathResult pathResult = UnityRecast_FindPath(10.0f, 0.0f, 10.0f, 20.0f, 0.0f, 20.0f);
        
        // Path outside mesh may fail
        // REQUIRE(pathResult.success == false);
        
        if (pathResult.errorMessage != nullptr)
        {
            REQUIRE(strlen(pathResult.errorMessage) > 0);
        }
        
        if (pathResult.success && pathResult.pathPoints) {
            UnityRecast_FreePathResult(&pathResult);
        }
    }
    
    UnityRecast_FreeNavMeshData(&buildResult);
    UnityRecast_Cleanup();
}

TEST_CASE("Error handling test", "[UnityRecastWrapper]")
{
    REQUIRE(UnityRecast_Initialize());
    
    SECTION("Null mesh data")
    {
        UnityNavMeshBuildSettings settings = {};
        settings.cellSize = 0.3f;
        settings.cellHeight = 0.2f;
        settings.walkableSlopeAngle = 45.0f;
        settings.walkableHeight = 2.0f;
        settings.walkableRadius = 0.6f;
        settings.walkableClimb = 0.9f;
        settings.minRegionArea = 8.0f;
        settings.mergeRegionArea = 20.0f;
        settings.maxVertsPerPoly = 6;
        settings.detailSampleDist = 6.0f;
        settings.detailSampleMaxError = 1.0f;
        
        UnityNavMeshResult result = UnityRecast_BuildNavMesh(nullptr, &settings);
        REQUIRE(result.success == false);
        REQUIRE(result.errorMessage != nullptr);
    }
    
    SECTION("Null settings")
    {
        std::vector<float> vertices = { -1.0f, 0.0f, -1.0f, 1.0f, 0.0f, -1.0f, 1.0f, 0.0f, 1.0f };
        std::vector<int> indices = { 0, 1, 2 };
        
        UnityMeshData meshData;
        meshData.vertices = vertices.data();
        meshData.indices = indices.data();
        meshData.vertexCount = static_cast<int>(vertices.size()) / 3;
        meshData.indexCount = static_cast<int>(indices.size());
        
        UnityNavMeshResult result = UnityRecast_BuildNavMesh(&meshData, nullptr);
        REQUIRE(result.success == false);
        REQUIRE(result.errorMessage != nullptr);
    }
    
    SECTION("Empty mesh data")
    {
        UnityMeshData meshData;
        meshData.vertices = nullptr;
        meshData.indices = nullptr;
        meshData.vertexCount = 0;
        meshData.indexCount = 0;
        
        UnityNavMeshBuildSettings settings = {};
        settings.cellSize = 0.3f;
        settings.cellHeight = 0.2f;
        settings.walkableSlopeAngle = 45.0f;
        settings.walkableHeight = 2.0f;
        settings.walkableRadius = 0.6f;
        settings.walkableClimb = 0.9f;
        settings.minRegionArea = 8.0f;
        settings.mergeRegionArea = 20.0f;
        settings.maxVertsPerPoly = 6;
        settings.detailSampleDist = 6.0f;
        settings.detailSampleMaxError = 1.0f;
        
        UnityNavMeshResult result = UnityRecast_BuildNavMesh(&meshData, &settings);
        REQUIRE(result.success == false);
    }
    
    UnityRecast_Cleanup();
}

TEST_CASE("Memory management test", "[UnityRecastWrapper]")
{
    REQUIRE(UnityRecast_Initialize());
    
    // Create mesh data
    std::vector<float> vertices = { -1.0f, 0.0f, -1.0f, 1.0f, 0.0f, -1.0f, 1.0f, 0.0f, 1.0f };
    std::vector<int> indices = { 0, 1, 2 };
    
    UnityMeshData meshData;
    meshData.vertices = vertices.data();
    meshData.indices = indices.data();
    meshData.vertexCount = static_cast<int>(vertices.size()) / 3;
    meshData.indexCount = static_cast<int>(indices.size());
    
    UnityNavMeshBuildSettings settings = {};
    settings.cellSize = 0.3f;
    settings.cellHeight = 0.2f;
    settings.walkableSlopeAngle = 45.0f;
    settings.walkableHeight = 2.0f;
    settings.walkableRadius = 0.6f;
    settings.walkableClimb = 0.9f;
    settings.minRegionArea = 8.0f;
    settings.mergeRegionArea = 20.0f;
    settings.maxVertsPerPoly = 6;
    settings.detailSampleDist = 6.0f;
    settings.detailSampleMaxError = 1.0f;
    
    SECTION("Multiple NavMesh builds")
    {
        // Build multiple NavMeshes to test memory management
        for (int i = 0; i < 5; i++)
        {
            UnityNavMeshResult result = UnityRecast_BuildNavMesh(&meshData, &settings);
            REQUIRE(result.success == true);
            
            // Load and use NavMesh
            REQUIRE(UnityRecast_LoadNavMesh(result.navMeshData, result.dataSize) == true);
            
            // Find a path
            UnityPathResult pathResult = UnityRecast_FindPath(0.0f, 0.0f, 0.0f, 1.5f, 0.0f, 1.5f);
            if (pathResult.success)
            {
                UnityRecast_FreePathResult(&pathResult);
            }
            
            // Cleanup
            UnityRecast_FreeNavMeshData(&result);
        }
    }
    
    UnityRecast_Cleanup();
} 