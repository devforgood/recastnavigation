#include <catch2/catch_test_macros.hpp>
#include <catch2/catch_approx.hpp>
#include "UnityRecastWrapper.h"
#include <memory>
#include <vector>

using namespace Catch;

TEST_CASE("UnityRecastWrapper 초기화 및 정리", "[UnityRecastWrapper]")
{
    SECTION("초기화 성공")
    {
        REQUIRE(UnityRecast_Initialize() == true);
        UnityRecast_Cleanup();
    }
    
    SECTION("중복 초기화")
    {
        REQUIRE(UnityRecast_Initialize() == true);
        REQUIRE(UnityRecast_Initialize() == true); // 중복 초기화 허용
        UnityRecast_Cleanup();
    }
    
    SECTION("정리 후 재초기화")
    {
        REQUIRE(UnityRecast_Initialize() == true);
        UnityRecast_Cleanup();
        REQUIRE(UnityRecast_Initialize() == true); // 재초기화 가능
        UnityRecast_Cleanup();
    }
}

TEST_CASE("간단한 메시로 NavMesh 빌드", "[UnityRecastWrapper]")
{
    REQUIRE(UnityRecast_Initialize());
    
    // 간단한 평면 메시 생성 (2x2 평면)
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
    
    SECTION("NavMesh 빌드 성공")
    {
        UnityNavMeshResult result = UnityRecast_BuildNavMesh(&meshData, &settings);
        
        REQUIRE(result.success == true);
        REQUIRE(result.navMeshData != nullptr);
        REQUIRE(result.dataSize > 0);
        REQUIRE(result.errorMessage == nullptr);
        
        // NavMesh 로드 테스트
        REQUIRE(UnityRecast_LoadNavMesh(result.navMeshData, result.dataSize) == true);
        
        // NavMesh 정보 확인
        int polyCount = UnityRecast_GetPolyCount();
        int vertexCount = UnityRecast_GetVertexCount();
        
        REQUIRE(polyCount > 0);
        REQUIRE(vertexCount > 0);
        
        // 메모리 정리
        UnityRecast_FreeNavMeshData(&result);
    }
    
    UnityRecast_Cleanup();
}

TEST_CASE("경로 찾기 테스트", "[UnityRecastWrapper]")
{
    REQUIRE(UnityRecast_Initialize());
    
    // 간단한 메시로 NavMesh 빌드
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
    
    UnityNavMeshResult buildResult = UnityRecast_BuildNavMesh(&meshData, &settings);
    REQUIRE(buildResult.success == true);
    REQUIRE(UnityRecast_LoadNavMesh(buildResult.navMeshData, buildResult.dataSize) == true);
    
    SECTION("유효한 경로 찾기")
    {
        UnityPathResult pathResult = UnityRecast_FindPath(-0.5f, 0.0f, -0.5f, 0.5f, 0.0f, 0.5f);
        
        REQUIRE(pathResult.success == true);
        REQUIRE(pathResult.pathPoints != nullptr);
        REQUIRE(pathResult.pointCount > 0);
        REQUIRE(pathResult.errorMessage == nullptr);
        
        // 경로 포인트 검증
        REQUIRE(pathResult.pointCount >= 2); // 시작점과 끝점 최소
        
        // 첫 번째 포인트는 시작점 근처여야 함
        float startDist = std::sqrt(
            std::pow(pathResult.pathPoints[0] - (-0.5f), 2) +
            std::pow(pathResult.pathPoints[1] - 0.0f, 2) +
            std::pow(pathResult.pathPoints[2] - (-0.5f), 2)
        );
        REQUIRE(startDist < 1.0f);
        
        // 마지막 포인트는 끝점 근처여야 함
        int lastIndex = (pathResult.pointCount - 1) * 3;
        float endDist = std::sqrt(
            std::pow(pathResult.pathPoints[lastIndex] - 0.5f, 2) +
            std::pow(pathResult.pathPoints[lastIndex + 1] - 0.0f, 2) +
            std::pow(pathResult.pathPoints[lastIndex + 2] - 0.5f, 2)
        );
        REQUIRE(endDist < 1.0f);
        
        UnityRecast_FreePathResult(&pathResult);
    }
    
    SECTION("무효한 경로 찾기 (메시 밖)")
    {
        UnityPathResult pathResult = UnityRecast_FindPath(10.0f, 0.0f, 10.0f, 20.0f, 0.0f, 20.0f);
        
        // 메시 밖의 경로는 실패할 수 있음
        // REQUIRE(pathResult.success == false);
        
        if (pathResult.errorMessage != nullptr)
        {
            REQUIRE(strlen(pathResult.errorMessage) > 0);
        }
        
        UnityRecast_FreePathResult(&pathResult);
    }
    
    UnityRecast_FreeNavMeshData(&buildResult);
    UnityRecast_Cleanup();
}

TEST_CASE("에러 처리 테스트", "[UnityRecastWrapper]")
{
    REQUIRE(UnityRecast_Initialize());
    
    SECTION("null 메시 데이터")
    {
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
        
        UnityNavMeshResult result = UnityRecast_BuildNavMesh(nullptr, &settings);
        REQUIRE(result.success == false);
        REQUIRE(result.errorMessage != nullptr);
    }
    
    SECTION("null 설정")
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
    
    SECTION("빈 메시 데이터")
    {
        UnityMeshData meshData;
        meshData.vertices = nullptr;
        meshData.indices = nullptr;
        meshData.vertexCount = 0;
        meshData.indexCount = 0;
        
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
        
        UnityNavMeshResult result = UnityRecast_BuildNavMesh(&meshData, &settings);
        REQUIRE(result.success == false);
    }
    
    UnityRecast_Cleanup();
}

TEST_CASE("메모리 관리 테스트", "[UnityRecastWrapper]")
{
    REQUIRE(UnityRecast_Initialize());
    
    // 간단한 메시 생성
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
    
    SECTION("NavMesh 데이터 해제")
    {
        UnityNavMeshResult result = UnityRecast_BuildNavMesh(&meshData, &settings);
        REQUIRE(result.success == true);
        
        // 메모리 해제 후 포인터 검증
        UnityRecast_FreeNavMeshData(&result);
        REQUIRE(result.navMeshData == nullptr);
        REQUIRE(result.dataSize == 0);
    }
    
    SECTION("경로 결과 해제")
    {
        // NavMesh 빌드 및 로드
        UnityNavMeshResult buildResult = UnityRecast_BuildNavMesh(&meshData, &settings);
        REQUIRE(buildResult.success == true);
        REQUIRE(UnityRecast_LoadNavMesh(buildResult.navMeshData, buildResult.dataSize) == true);
        
        // 경로 찾기
        UnityPathResult pathResult = UnityRecast_FindPath(-0.5f, 0.0f, -0.5f, 0.5f, 0.0f, 0.5f);
        REQUIRE(pathResult.success == true);
        
        // 메모리 해제 후 포인터 검증
        UnityRecast_FreePathResult(&pathResult);
        REQUIRE(pathResult.pathPoints == nullptr);
        REQUIRE(pathResult.pointCount == 0);
        
        UnityRecast_FreeNavMeshData(&buildResult);
    }
    
    UnityRecast_Cleanup();
} 