#include <catch2/catch_test_macros.hpp>
#include <catch2/catch_approx.hpp>
#include "UnityPathfinding.h"
#include "UnityNavMeshBuilder.h"
#include <memory>
#include <vector>
#include <cmath>

using namespace Catch;

TEST_CASE("UnityPathfinding 생성 및 소멸", "[UnityPathfinding]")
{
    SECTION("생성자")
    {
        UnityPathfinding pathfinding;
        // 기본 생성자가 정상적으로 작동하는지 확인
    }
    
    SECTION("소멸자")
    {
        {
            UnityPathfinding pathfinding;
            // 소멸자가 정상적으로 호출되는지 확인
        }
        // 여기서 메모리 누수가 없는지 확인
    }
}

TEST_CASE("간단한 경로 찾기", "[UnityPathfinding]")
{
    UnityPathfinding pathfinding;
    UnityNavMeshBuilder builder;
    
    // 간단한 평면 메시로 NavMesh 생성
    std::vector<float> vertices = {
        -1.0f, 0.0f, -1.0f,  // 0
         1.0f, 0.0f, -1.0f,  // 1
         1.0f, 0.0f,  1.0f,  // 2
        -1.0f, 0.0f,  1.0f   // 3
    };
    
    std::vector<int> indices = {
        0, 1, 2,  // 첫 번째 삼각형
        0, 2, 3   // 두 번째 삼각형
    };
    
    UnityMeshData meshData;
    meshData.vertices = vertices.data();
    meshData.indices = indices.data();
    meshData.vertexCount = static_cast<int>(vertices.size()) / 3;
    meshData.indexCount = static_cast<int>(indices.size());
    
    UnityNavMeshBuildSettings settings;
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
    
    // NavMesh를 Pathfinding에 설정
    pathfinding.SetNavMesh(builder.GetNavMesh(), builder.GetNavMeshQuery());
    
    SECTION("직선 경로 찾기")
    {
        UnityPathResult result = pathfinding.FindPath(-0.5f, 0.0f, -0.5f, 0.5f, 0.0f, 0.5f);
        
        REQUIRE(result.success == true);
        REQUIRE(result.pathPoints != nullptr);
        REQUIRE(result.pointCount >= 2);
        REQUIRE(result.errorMessage == nullptr);
        
        // 경로 길이 계산
        float pathLength = 0.0f;
        for (int i = 1; i < result.pointCount; i++)
        {
            float dx = result.pathPoints[i * 3] - result.pathPoints[(i - 1) * 3];
            float dy = result.pathPoints[i * 3 + 1] - result.pathPoints[(i - 1) * 3 + 1];
            float dz = result.pathPoints[i * 3 + 2] - result.pathPoints[(i - 1) * 3 + 2];
            pathLength += std::sqrt(dx * dx + dy * dy + dz * dz);
        }
        
        // 경로 길이가 합리적인 범위에 있어야 함
        REQUIRE(pathLength > 0.0f);
        REQUIRE(pathLength < 10.0f);
        
        UnityRecast_FreePathResult(&result);
    }
    
    SECTION("같은 지점으로의 경로")
    {
        UnityPathResult result = pathfinding.FindPath(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
        
        // 같은 지점으로의 경로는 실패하거나 매우 짧아야 함
        if (result.success)
        {
            REQUIRE(result.pointCount <= 2);
        }
        
        UnityRecast_FreePathResult(&result);
    }
    
    UnityRecast_FreeNavMeshData(&buildResult);
}

TEST_CASE("복잡한 지형에서의 경로 찾기", "[UnityPathfinding]")
{
    UnityPathfinding pathfinding;
    UnityNavMeshBuilder builder;
    
    // 복잡한 지형 메시 생성 (장애물이 있는 지형)
    std::vector<float> vertices = {
        // 바닥
        -2.0f, 0.0f, -2.0f,   // 0
         2.0f, 0.0f, -2.0f,   // 1
         2.0f, 0.0f,  2.0f,   // 2
        -2.0f, 0.0f,  2.0f,   // 3
        
        // 장애물 1
        -0.5f, 0.0f, -0.5f,   // 4
         0.5f, 0.0f, -0.5f,   // 5
         0.5f, 1.0f, -0.5f,   // 6
        -0.5f, 1.0f, -0.5f,   // 7
        
        // 장애물 2
        -0.5f, 0.0f,  0.5f,   // 8
         0.5f, 0.0f,  0.5f,   // 9
         0.5f, 1.0f,  0.5f,   // 10
        -0.5f, 1.0f,  0.5f    // 11
    };
    
    std::vector<int> indices = {
        // 바닥
        0, 1, 2, 0, 2, 3,
        // 장애물 1 측면
        4, 5, 6, 4, 6, 7,
        5, 9, 10, 5, 10, 6,
        9, 8, 11, 9, 11, 10,
        8, 4, 7, 8, 7, 11,
        // 장애물 1 상단
        7, 6, 10, 7, 10, 11,
        // 장애물 2 측면
        8, 9, 10, 8, 10, 11
    };
    
    UnityMeshData meshData;
    meshData.vertices = vertices.data();
    meshData.indices = indices.data();
    meshData.vertexCount = static_cast<int>(vertices.size()) / 3;
    meshData.indexCount = static_cast<int>(indices.size());
    
    UnityNavMeshBuildSettings settings;
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
    
    SECTION("장애물 우회 경로")
    {
        // 장애물을 우회하는 경로 찾기
        UnityPathResult result = pathfinding.FindPath(-1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f);
        
        REQUIRE(result.success == true);
        REQUIRE(result.pathPoints != nullptr);
        REQUIRE(result.pointCount >= 2);
        
        // 경로가 장애물을 우회하는지 확인 (간단한 검증)
        bool pathGoesAroundObstacle = false;
        for (int i = 0; i < result.pointCount; i++)
        {
            float x = result.pathPoints[i * 3];
            float z = result.pathPoints[i * 3 + 2];
            
            // 경로가 장애물 영역을 벗어나는지 확인
            if (x < -0.6f || x > 0.6f || z < -0.6f || z > 0.6f)
            {
                pathGoesAroundObstacle = true;
                break;
            }
        }
        
        // 경로가 장애물을 우회해야 함
        REQUIRE(pathGoesAroundObstacle == true);
        
        UnityRecast_FreePathResult(&result);
    }
    
    UnityRecast_FreeNavMeshData(&buildResult);
}

TEST_CASE("경로 유틸리티 함수 테스트", "[UnityPathfinding]")
{
    UnityPathfinding pathfinding;
    UnityNavMeshBuilder builder;
    
    // 간단한 메시로 NavMesh 생성
    std::vector<float> vertices = { -1.0f, 0.0f, -1.0f, 1.0f, 0.0f, -1.0f, 1.0f, 0.0f, 1.0f };
    std::vector<int> indices = { 0, 1, 2 };
    
    UnityMeshData meshData;
    meshData.vertices = vertices.data();
    meshData.indices = indices.data();
    meshData.vertexCount = static_cast<int>(vertices.size()) / 3;
    meshData.indexCount = static_cast<int>(indices.size());
    
    UnityNavMeshBuildSettings settings;
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
    
    UnityPathResult pathResult = pathfinding.FindPath(-0.5f, 0.0f, -0.5f, 0.5f, 0.0f, 0.5f);
    REQUIRE(pathResult.success == true);
    
    SECTION("경로 검증")
    {
        bool isValid = pathfinding.ValidatePath(&pathResult);
        REQUIRE(isValid == true);
    }
    
    SECTION("경로 길이 계산")
    {
        float pathLength = pathfinding.CalculatePathLength(&pathResult);
        REQUIRE(pathLength > 0.0f);
        REQUIRE(pathLength < 10.0f);
    }
    
    SECTION("경로 포인트 수")
    {
        int pointCount = pathfinding.GetPathPointCount(&pathResult);
        REQUIRE(pointCount == pathResult.pointCount);
        REQUIRE(pointCount > 0);
    }
    
    SECTION("경로 포인트 가져오기")
    {
        float x, y, z;
        bool success = pathfinding.GetPathPoint(&pathResult, 0, &x, &y, &z);
        REQUIRE(success == true);
        
        // 첫 번째 포인트는 시작점 근처여야 함
        float dist = std::sqrt(std::pow(x - (-0.5f), 2) + std::pow(y - 0.0f, 2) + std::pow(z - (-0.5f), 2));
        REQUIRE(dist < 1.0f);
    }
    
    SECTION("경로 방향 계산")
    {
        if (pathResult.pointCount > 1)
        {
            float dirX, dirY, dirZ;
            bool success = pathfinding.GetPathDirection(&pathResult, 0, &dirX, &dirY, &dirZ);
            REQUIRE(success == true);
            
            // 방향 벡터의 길이는 1에 가까워야 함
            float length = std::sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
            REQUIRE(length > 0.9f);
            REQUIRE(length < 1.1f);
        }
    }
    
    SECTION("경로 곡률 계산")
    {
        if (pathResult.pointCount > 2)
        {
            float curvature = pathfinding.GetPathCurvature(&pathResult, 1);
            REQUIRE(curvature >= 0.0f);
        }
    }
    
    UnityRecast_FreePathResult(&pathResult);
    UnityRecast_FreeNavMeshData(&buildResult);
}

TEST_CASE("경로 스무딩 및 단순화", "[UnityPathfinding]")
{
    UnityPathfinding pathfinding;
    UnityNavMeshBuilder builder;
    
    // 복잡한 지형으로 NavMesh 생성
    std::vector<float> vertices = {
        -2.0f, 0.0f, -2.0f,
         2.0f, 0.0f, -2.0f,
         2.0f, 0.0f,  2.0f,
        -2.0f, 0.0f,  2.0f,
        -1.0f, 0.5f, -1.0f,
         1.0f, 0.5f, -1.0f,
         1.0f, 0.5f,  1.0f,
        -1.0f, 0.5f,  1.0f
    };
    
    std::vector<int> indices = {
        0, 1, 2, 0, 2, 3,
        0, 4, 5, 0, 5, 1,
        1, 5, 6, 1, 6, 2,
        2, 6, 7, 2, 7, 3,
        3, 7, 4, 3, 4, 0,
        4, 5, 6, 4, 6, 7
    };
    
    UnityMeshData meshData;
    meshData.vertices = vertices.data();
    meshData.indices = indices.data();
    meshData.vertexCount = static_cast<int>(vertices.size()) / 3;
    meshData.indexCount = static_cast<int>(indices.size());
    
    UnityNavMeshBuildSettings settings;
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
    
    UnityPathResult originalPath = pathfinding.FindPath(-1.5f, 0.0f, -1.5f, 1.5f, 0.0f, 1.5f);
    REQUIRE(originalPath.success == true);
    
    SECTION("경로 스무딩")
    {
        UnityPathResult smoothedPath = pathfinding.SmoothPath(&originalPath, 0.5f);
        
        REQUIRE(smoothedPath.success == true);
        REQUIRE(smoothedPath.pathPoints != nullptr);
        REQUIRE(smoothedPath.pointCount > 0);
        
        // 스무딩된 경로의 길이는 원본과 비슷해야 함
        float originalLength = pathfinding.CalculatePathLength(&originalPath);
        float smoothedLength = pathfinding.CalculatePathLength(&smoothedPath);
        
        REQUIRE(std::abs(originalLength - smoothedLength) < 2.0f);
        
        UnityRecast_FreePathResult(&smoothedPath);
    }
    
    SECTION("경로 단순화")
    {
        UnityPathResult simplifiedPath = pathfinding.SimplifyPath(&originalPath, 0.1f);
        
        REQUIRE(simplifiedPath.success == true);
        REQUIRE(simplifiedPath.pathPoints != nullptr);
        REQUIRE(simplifiedPath.pointCount > 0);
        
        // 단순화된 경로는 원본보다 포인트가 적거나 같아야 함
        REQUIRE(simplifiedPath.pointCount <= originalPath.pointCount);
        
        UnityRecast_FreePathResult(&simplifiedPath);
    }
    
    UnityRecast_FreePathResult(&originalPath);
    UnityRecast_FreeNavMeshData(&buildResult);
}

TEST_CASE("에러 처리 테스트", "[UnityPathfinding]")
{
    UnityPathfinding pathfinding;
    
    SECTION("NavMesh가 설정되지 않은 상태에서 경로 찾기")
    {
        UnityPathResult result = pathfinding.FindPath(0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f);
        REQUIRE(result.success == false);
        REQUIRE(result.errorMessage != nullptr);
    }
    
    SECTION("null 경로로 유틸리티 함수 호출")
    {
        REQUIRE(pathfinding.ValidatePath(nullptr) == false);
        REQUIRE(pathfinding.CalculatePathLength(nullptr) == 0.0f);
        REQUIRE(pathfinding.GetPathPointCount(nullptr) == 0);
        
        float x, y, z;
        REQUIRE(pathfinding.GetPathPoint(nullptr, 0, &x, &y, &z) == false);
        REQUIRE(pathfinding.GetPathDirection(nullptr, 0, &x, &y, &z) == false);
        REQUIRE(pathfinding.GetPathCurvature(nullptr, 0) == 0.0f);
    }
    
    SECTION("무효한 인덱스로 접근")
    {
        // 더미 경로 결과 생성
        UnityPathResult dummyPath;
        dummyPath.success = true;
        dummyPath.pathPoints = nullptr;
        dummyPath.pointCount = 0;
        dummyPath.errorMessage = nullptr;
        
        float x, y, z;
        REQUIRE(pathfinding.GetPathPoint(&dummyPath, -1, &x, &y, &z) == false);
        REQUIRE(pathfinding.GetPathPoint(&dummyPath, 0, &x, &y, &z) == false);
        REQUIRE(pathfinding.GetPathDirection(&dummyPath, 0, &x, &y, &z) == false);
        REQUIRE(pathfinding.GetPathCurvature(&dummyPath, 0) == 0.0f);
    }
} 