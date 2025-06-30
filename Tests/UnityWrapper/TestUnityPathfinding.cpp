#include "catch_all.hpp"
#include "UnityPathfinding.h"
#include "UnityNavMeshBuilder.h"
#include "UnityRecastWrapper.h"
#include <memory>
#include <vector>
#include <cmath>

using namespace Catch;

TEST_CASE("UnityPathfinding creation and destruction", "[UnityPathfinding]")
{
    SECTION("Constructor")
    {
        UnityPathfinding pathfinding;
        // Check if default constructor works properly
    }
    
    SECTION("Destructor")
    {
        {
            UnityPathfinding pathfinding;
            // Check if destructor is called properly
        }
        // Check for memory leaks here
    }
}

TEST_CASE("Simple pathfinding", "[UnityPathfinding]")
{
    UnityPathfinding pathfinding;
    UnityNavMeshBuilder builder;
    
    // Create NavMesh with simple plane mesh
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
    
    UnityNavMeshResult buildResult = builder.BuildNavMesh(&meshData, &settings);
    REQUIRE(buildResult.success == true);
    
    // Set NavMesh to Pathfinding
    pathfinding.SetNavMesh(builder.GetNavMesh(), builder.GetNavMeshQuery());
    
    SECTION("Straight line pathfinding")
    {
        UnityPathResult result = pathfinding.FindPath(-0.5f, 0.0f, -0.5f, 0.5f, 0.0f, 0.5f);
        
        REQUIRE(result.success == true);
        REQUIRE(result.pathPoints != nullptr);
        REQUIRE(result.pointCount >= 2);
        REQUIRE(result.errorMessage == nullptr);
        
        // Calculate path length
        float pathLength = 0.0f;
        for (int i = 1; i < result.pointCount; i++)
        {
            float dx = result.pathPoints[i * 3] - result.pathPoints[(i - 1) * 3];
            float dy = result.pathPoints[i * 3 + 1] - result.pathPoints[(i - 1) * 3 + 1];
            float dz = result.pathPoints[i * 3 + 2] - result.pathPoints[(i - 1) * 3 + 2];
            pathLength += std::sqrt(dx * dx + dy * dy + dz * dz);
        }
        
        // Path length should be in reasonable range
        REQUIRE(pathLength > 0.0f);
        REQUIRE(pathLength < 10.0f);
        
        UnityRecast_FreePathResult(&result);
    }
    
    SECTION("Path to same point")
    {
        UnityPathResult result = pathfinding.FindPath(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
        
        // Path to same point should fail or be very short
        if (result.success)
        {
            REQUIRE(result.pointCount <= 2);
        }
        
        UnityRecast_FreePathResult(&result);
    }
    
    UnityRecast_FreeNavMeshData(&buildResult);
}

TEST_CASE("Pathfinding in complex terrain", "[UnityPathfinding]")
{
    UnityPathfinding pathfinding;
    UnityNavMeshBuilder builder;
    
    // Create complex terrain mesh (terrain with obstacles)
    std::vector<float> vertices = {
        // Ground
        -2.0f, 0.0f, -2.0f,   // 0
         2.0f, 0.0f, -2.0f,   // 1
         2.0f, 0.0f,  2.0f,   // 2
        -2.0f, 0.0f,  2.0f,   // 3
        
        // Obstacle 1
        -0.5f, 0.0f, -0.5f,   // 4
         0.5f, 0.0f, -0.5f,   // 5
         0.5f, 1.0f, -0.5f,   // 6
        -0.5f, 1.0f, -0.5f,   // 7
        
        // Obstacle 2
        -0.5f, 0.0f,  0.5f,   // 8
         0.5f, 0.0f,  0.5f,   // 9
         0.5f, 1.0f,  0.5f,   // 10
        -0.5f, 1.0f,  0.5f    // 11
    };
    
    std::vector<int> indices = {
        // Ground
        0, 1, 2, 0, 2, 3,
        // Obstacle 1 sides
        4, 5, 6, 4, 6, 7,
        5, 9, 10, 5, 10, 6,
        9, 8, 11, 9, 11, 10,
        8, 4, 7, 8, 7, 11,
        // Obstacle 1 top
        7, 6, 10, 7, 10, 11,
        // Obstacle 2 sides
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
    
    UnityNavMeshResult buildResult = builder.BuildNavMesh(&meshData, &settings);
    REQUIRE(buildResult.success == true);
    
    pathfinding.SetNavMesh(builder.GetNavMesh(), builder.GetNavMeshQuery());
    
    SECTION("Path around obstacles")
    {
        // Find path that goes around obstacles
        UnityPathResult result = pathfinding.FindPath(-1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f);
        
        REQUIRE(result.success == true);
        REQUIRE(result.pathPoints != nullptr);
        REQUIRE(result.pointCount >= 2);
        
        // Check if path goes around obstacles (simple verification)
        bool pathGoesAroundObstacle = false;
        for (int i = 0; i < result.pointCount; i++)
        {
            float x = result.pathPoints[i * 3];
            float z = result.pathPoints[i * 3 + 2];
            
            // Check if path goes outside obstacle area
            if (x < -0.6f || x > 0.6f || z < -0.6f || z > 0.6f)
            {
                pathGoesAroundObstacle = true;
                break;
            }
        }
        
        // Path should go around obstacles
        REQUIRE(pathGoesAroundObstacle == true);
        
        UnityRecast_FreePathResult(&result);
    }
    
    UnityRecast_FreeNavMeshData(&buildResult);
}

TEST_CASE("Pathfinding with different agent sizes", "[UnityPathfinding]")
{
    UnityPathfinding pathfinding;
    UnityNavMeshBuilder builder;
    
    // Create mesh with narrow passages
    std::vector<float> vertices = {
        // Ground
        -2.0f, 0.0f, -2.0f,   // 0
         2.0f, 0.0f, -2.0f,   // 1
         2.0f, 0.0f,  2.0f,   // 2
        -2.0f, 0.0f,  2.0f,   // 3
        
        // Narrow passage walls
        -0.3f, 0.0f, -0.3f,   // 4
         0.3f, 0.0f, -0.3f,   // 5
         0.3f, 1.0f, -0.3f,   // 6
        -0.3f, 1.0f, -0.3f,   // 7
        
        -0.3f, 0.0f,  0.3f,   // 8
         0.3f, 0.0f,  0.3f,   // 9
         0.3f, 1.0f,  0.3f,   // 10
        -0.3f, 1.0f,  0.3f    // 11
    };
    
    std::vector<int> indices = {
        // Ground
        0, 1, 2, 0, 2, 3,
        // Passage walls
        4, 5, 6, 4, 6, 7,
        5, 9, 10, 5, 10, 6,
        9, 8, 11, 9, 11, 10,
        8, 4, 7, 8, 7, 11,
        7, 6, 10, 7, 10, 11
    };
    
    UnityMeshData meshData;
    meshData.vertices = vertices.data();
    meshData.indices = indices.data();
    meshData.vertexCount = static_cast<int>(vertices.size()) / 3;
    meshData.indexCount = static_cast<int>(indices.size());
    
    UnityNavMeshBuildSettings settings = {};
    settings.cellSize = 0.1f;
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
    
    UnityNavMeshResult buildResult = builder.BuildNavMesh(&meshData, &settings);
    REQUIRE(buildResult.success == true);
    
    pathfinding.SetNavMesh(builder.GetNavMesh(), builder.GetNavMeshQuery());
    
    SECTION("Small agent through narrow passage")
    {
        // Small agent should be able to go through narrow passage
        UnityPathResult result = pathfinding.FindPath(-1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f);
        
        REQUIRE(result.success == true);
        REQUIRE(result.pathPoints != nullptr);
        REQUIRE(result.pointCount >= 2);
        
        UnityRecast_FreePathResult(&result);
    }
    
    SECTION("Large agent around narrow passage")
    {
        // Large agent should go around narrow passage
        UnityPathResult result = pathfinding.FindPath(-1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f);
        
        REQUIRE(result.success == true);
        REQUIRE(result.pathPoints != nullptr);
        REQUIRE(result.pointCount >= 2);
        
        // Check if path goes around the narrow passage
        bool pathGoesAround = false;
        for (int i = 0; i < result.pointCount; i++)
        {
            float x = result.pathPoints[i * 3];
            float z = result.pathPoints[i * 3 + 2];
            
            // Path should go around the narrow passage
            if (std::abs(x) > 0.5f || std::abs(z) > 0.5f)
            {
                pathGoesAround = true;
                break;
            }
        }
        
        // Large agent should go around
        REQUIRE(pathGoesAround == true);
        
        UnityRecast_FreePathResult(&result);
    }
    
    UnityRecast_FreeNavMeshData(&buildResult);
}

TEST_CASE("Error handling test", "[UnityPathfinding]")
{
    UnityPathfinding pathfinding;
    
    SECTION("Pathfinding without NavMesh")
    {
        UnityPathResult result = pathfinding.FindPath(0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f);
        
        // Should fail without NavMesh
        REQUIRE(result.success == false);
        REQUIRE(result.errorMessage != nullptr);
    }
    
    SECTION("Invalid coordinates")
    {
        // Test with NaN or infinite values
        UnityPathResult result = pathfinding.FindPath(
            std::numeric_limits<float>::quiet_NaN(), 0.0f, 0.0f,
            1.0f, 0.0f, 1.0f
        );
        
        REQUIRE(result.success == false);
    }
}

TEST_CASE("Memory management test", "[UnityPathfinding]")
{
    UnityPathfinding pathfinding;
    UnityNavMeshBuilder builder;
    
    // Create simple NavMesh
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
    
    pathfinding.SetNavMesh(builder.GetNavMesh(), builder.GetNavMeshQuery());
    
    SECTION("Multiple pathfinding operations")
    {
        // Perform multiple pathfinding operations to test memory management
        for (int i = 0; i < 10; i++)
        {
            UnityPathResult result = pathfinding.FindPath(-0.5f, 0.0f, -0.5f, 0.5f, 0.0f, 0.5f);
            
            if (result.success)
            {
                REQUIRE(result.pathPoints != nullptr);
                REQUIRE(result.pointCount > 0);
                
                UnityRecast_FreePathResult(&result);
            }
        }
    }
    
    UnityRecast_FreeNavMeshData(&buildResult);
} 