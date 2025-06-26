#include "catch_all.hpp"
#include "UnityNavMeshBuilder.h"
#include <memory>
#include <vector>

using namespace Catch;

TEST_CASE("UnityNavMeshBuilder creation and destruction", "[UnityNavMeshBuilder]")
{
    SECTION("Constructor")
    {
        UnityNavMeshBuilder builder;
        REQUIRE(builder.GetPolyCount() == 0);
        REQUIRE(builder.GetVertexCount() == 0);
    }
    
    SECTION("Destructor")
    {
        {
            UnityNavMeshBuilder builder;
            // Check if destructor is called properly
        }
        // Check for memory leaks here
    }
}

TEST_CASE("Build NavMesh with simple mesh", "[UnityNavMeshBuilder]")
{
    UnityNavMeshBuilder builder;
    
    // Create simple plane mesh
    std::vector<float> vertices = {
        -1.0f, 0.0f, -1.0f,  // 0
         1.0f, 0.0f, -1.0f,  // 1
         1.0f, 0.0f,  1.0f,  // 2
        -1.0f, 0.0f,  1.0f   // 3
    };
    
    std::vector<int> indices = {
        0, 1, 2,  // First triangle
        0, 2, 3   // Second triangle
    };
    
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
    
    SECTION("NavMesh build success")
    {
        UnityNavMeshResult result = builder.BuildNavMesh(&meshData, &settings);
        
        REQUIRE(result.success == true);
        REQUIRE(result.navMeshData != nullptr);
        REQUIRE(result.dataSize > 0);
        REQUIRE(result.errorMessage == nullptr);
        
        // Check NavMesh info
        int polyCount = builder.GetPolyCount();
        int vertexCount = builder.GetVertexCount();
        
        REQUIRE(polyCount > 0);
        REQUIRE(vertexCount > 0);
        
        // Check NavMesh instances
        REQUIRE(builder.GetNavMesh() != nullptr);
        REQUIRE(builder.GetNavMeshQuery() != nullptr);
        
        // Memory cleanup
        UnityRecast_FreeNavMeshData(&result);
    }
    
    SECTION("Build with different cell sizes")
    {
        // Small cell size
        settings.cellSize = 0.1f;
        UnityNavMeshResult result1 = builder.BuildNavMesh(&meshData, &settings);
        REQUIRE(result1.success == true);
        int polyCount1 = builder.GetPolyCount();
        UnityRecast_FreeNavMeshData(&result1);
        
        // Large cell size
        settings.cellSize = 0.5f;
        UnityNavMeshResult result2 = builder.BuildNavMesh(&meshData, &settings);
        REQUIRE(result2.success == true);
        int polyCount2 = builder.GetPolyCount();
        UnityRecast_FreeNavMeshData(&result2);
        
        // Smaller cell size should generate more polygons
        REQUIRE(polyCount1 >= polyCount2);
    }
}

TEST_CASE("Build NavMesh with complex mesh", "[UnityNavMeshBuilder]")
{
    UnityNavMeshBuilder builder;
    
    // Create complex mesh (terrain with multiple triangles)
    std::vector<float> vertices = {
        // Ground plane
        -2.0f, 0.0f, -2.0f,   // 0
         2.0f, 0.0f, -2.0f,   // 1
         2.0f, 0.0f,  2.0f,   // 2
        -2.0f, 0.0f,  2.0f,   // 3
        
        // Step 1
        -1.0f, 0.5f, -1.0f,   // 4
         1.0f, 0.5f, -1.0f,   // 5
         1.0f, 0.5f,  1.0f,   // 6
        -1.0f, 0.5f,  1.0f,   // 7
        
        // Step 2
        -0.5f, 1.0f, -0.5f,   // 8
         0.5f, 1.0f, -0.5f,   // 9
         0.5f, 1.0f,  0.5f,   // 10
        -0.5f, 1.0f,  0.5f    // 11
    };
    
    std::vector<int> indices = {
        // Ground
        0, 1, 2, 0, 2, 3,
        // Step 1 sides
        0, 4, 5, 0, 5, 1,
        1, 5, 6, 1, 6, 2,
        2, 6, 7, 2, 7, 3,
        3, 7, 4, 3, 4, 0,
        // Step 1 top
        4, 5, 6, 4, 6, 7,
        // Step 2 sides
        4, 8, 9, 4, 9, 5,
        5, 9, 10, 5, 10, 6,
        6, 10, 11, 6, 11, 7,
        7, 11, 8, 7, 8, 4,
        // Step 2 top
        8, 9, 10, 8, 10, 11
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
    settings.walkableHeight = 2.0f;
    settings.walkableRadius = 0.6f;
    settings.walkableClimb = 0.9f;
    settings.minRegionArea = 4.0f;
    settings.mergeRegionArea = 10.0f;
    settings.maxVertsPerPoly = 6;
    settings.detailSampleDist = 3.0f;
    settings.detailSampleMaxError = 0.5f;
    
    SECTION("Complex mesh build")
    {
        UnityNavMeshResult result = builder.BuildNavMesh(&meshData, &settings);
        
        REQUIRE(result.success == true);
        REQUIRE(result.navMeshData != nullptr);
        REQUIRE(result.dataSize > 0);
        
        int polyCount = builder.GetPolyCount();
        int vertexCount = builder.GetVertexCount();
        
        REQUIRE(polyCount > 0);
        REQUIRE(vertexCount > 0);
        
        // Complex mesh should have more polygons
        REQUIRE(polyCount > 5);
        
        UnityRecast_FreeNavMeshData(&result);
    }
}

TEST_CASE("NavMesh loading test", "[UnityNavMeshBuilder]")
{
    UnityNavMeshBuilder builder;
    
    // First build NavMesh
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
    
    UnityNavMeshResult buildResult = builder.BuildNavMesh(&meshData, &settings);
    REQUIRE(buildResult.success == true);
    
    SECTION("Load NavMesh from data")
    {
        // Create new builder and load NavMesh
        UnityNavMeshBuilder newBuilder;
        bool loadResult = newBuilder.LoadNavMesh(buildResult.navMeshData, buildResult.dataSize);
        
        REQUIRE(loadResult == true);
        REQUIRE(newBuilder.GetNavMesh() != nullptr);
        REQUIRE(newBuilder.GetNavMeshQuery() != nullptr);
        
        // Check if loaded NavMesh has same properties
        int polyCount = newBuilder.GetPolyCount();
        int vertexCount = newBuilder.GetVertexCount();
        
        REQUIRE(polyCount > 0);
        REQUIRE(vertexCount > 0);
    }
    
    UnityRecast_FreeNavMeshData(&buildResult);
}

TEST_CASE("Error handling test", "[UnityNavMeshBuilder]")
{
    UnityNavMeshBuilder builder;
    
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
        
        UnityNavMeshResult result = builder.BuildNavMesh(nullptr, &settings);
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
        
        UnityNavMeshResult result = builder.BuildNavMesh(&meshData, nullptr);
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
        
        UnityNavMeshResult result = builder.BuildNavMesh(&meshData, &settings);
        REQUIRE(result.success == false);
        REQUIRE(result.errorMessage != nullptr);
    }
}

TEST_CASE("Memory management test", "[UnityNavMeshBuilder]")
{
    UnityNavMeshBuilder builder;
    
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
    
    SECTION("Multiple builds")
    {
        // Build multiple NavMeshes to test memory management
        for (int i = 0; i < 5; i++)
        {
            UnityNavMeshResult result = builder.BuildNavMesh(&meshData, &settings);
            REQUIRE(result.success == true);
            
            // Check NavMesh properties
            int polyCount = builder.GetPolyCount();
            int vertexCount = builder.GetVertexCount();
            
            REQUIRE(polyCount > 0);
            REQUIRE(vertexCount > 0);
            
            // Cleanup
            UnityRecast_FreeNavMeshData(&result);
        }
    }
    
    SECTION("Load invalid data")
    {
        // Try to load invalid NavMesh data
        unsigned char invalidData[] = { 0x00, 0x01, 0x02, 0x03 };
        bool loadResult = builder.LoadNavMesh(invalidData, sizeof(invalidData));
        
        // Should fail with invalid data
        REQUIRE(loadResult == false);
    }
} 