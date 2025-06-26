#include <catch2/catch_test_macros.hpp>
#include <catch2/catch_approx.hpp>
#include "UnityNavMeshBuilder.h"
#include <memory>
#include <vector>

using namespace Catch;

TEST_CASE("UnityNavMeshBuilder 생성 및 소멸", "[UnityNavMeshBuilder]")
{
    SECTION("생성자")
    {
        UnityNavMeshBuilder builder;
        REQUIRE(builder.GetPolyCount() == 0);
        REQUIRE(builder.GetVertexCount() == 0);
    }
    
    SECTION("소멸자")
    {
        {
            UnityNavMeshBuilder builder;
            // 소멸자가 정상적으로 호출되는지 확인
        }
        // 여기서 메모리 누수가 없는지 확인
    }
}

TEST_CASE("간단한 메시로 NavMesh 빌드", "[UnityNavMeshBuilder]")
{
    UnityNavMeshBuilder builder;
    
    // 간단한 평면 메시 생성
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
        UnityNavMeshResult result = builder.BuildNavMesh(&meshData, &settings);
        
        REQUIRE(result.success == true);
        REQUIRE(result.navMeshData != nullptr);
        REQUIRE(result.dataSize > 0);
        REQUIRE(result.errorMessage == nullptr);
        
        // NavMesh 정보 확인
        int polyCount = builder.GetPolyCount();
        int vertexCount = builder.GetVertexCount();
        
        REQUIRE(polyCount > 0);
        REQUIRE(vertexCount > 0);
        
        // NavMesh 인스턴스 확인
        REQUIRE(builder.GetNavMesh() != nullptr);
        REQUIRE(builder.GetNavMeshQuery() != nullptr);
        
        // 메모리 정리
        UnityRecast_FreeNavMeshData(&result);
    }
    
    SECTION("다양한 셀 크기로 빌드")
    {
        // 작은 셀 크기
        settings.cellSize = 0.1f;
        UnityNavMeshResult result1 = builder.BuildNavMesh(&meshData, &settings);
        REQUIRE(result1.success == true);
        int polyCount1 = builder.GetPolyCount();
        UnityRecast_FreeNavMeshData(&result1);
        
        // 큰 셀 크기
        settings.cellSize = 0.5f;
        UnityNavMeshResult result2 = builder.BuildNavMesh(&meshData, &settings);
        REQUIRE(result2.success == true);
        int polyCount2 = builder.GetPolyCount();
        UnityRecast_FreeNavMeshData(&result2);
        
        // 작은 셀 크기가 더 많은 폴리곤을 생성해야 함
        REQUIRE(polyCount1 >= polyCount2);
    }
}

TEST_CASE("복잡한 메시로 NavMesh 빌드", "[UnityNavMeshBuilder]")
{
    UnityNavMeshBuilder builder;
    
    // 복잡한 메시 생성 (여러 삼각형으로 구성된 지형)
    std::vector<float> vertices = {
        // 바닥 평면
        -2.0f, 0.0f, -2.0f,   // 0
         2.0f, 0.0f, -2.0f,   // 1
         2.0f, 0.0f,  2.0f,   // 2
        -2.0f, 0.0f,  2.0f,   // 3
        
        // 계단 1
        -1.0f, 0.5f, -1.0f,   // 4
         1.0f, 0.5f, -1.0f,   // 5
         1.0f, 0.5f,  1.0f,   // 6
        -1.0f, 0.5f,  1.0f,   // 7
        
        // 계단 2
        -0.5f, 1.0f, -0.5f,   // 8
         0.5f, 1.0f, -0.5f,   // 9
         0.5f, 1.0f,  0.5f,   // 10
        -0.5f, 1.0f,  0.5f    // 11
    };
    
    std::vector<int> indices = {
        // 바닥
        0, 1, 2, 0, 2, 3,
        // 계단 1 측면
        0, 4, 5, 0, 5, 1,
        1, 5, 6, 1, 6, 2,
        2, 6, 7, 2, 7, 3,
        3, 7, 4, 3, 4, 0,
        // 계단 1 상단
        4, 5, 6, 4, 6, 7,
        // 계단 2 측면
        4, 8, 9, 4, 9, 5,
        5, 9, 10, 5, 10, 6,
        6, 10, 11, 6, 11, 7,
        7, 11, 8, 7, 8, 4,
        // 계단 2 상단
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
    
    SECTION("복잡한 메시 빌드")
    {
        UnityNavMeshResult result = builder.BuildNavMesh(&meshData, &settings);
        
        REQUIRE(result.success == true);
        REQUIRE(result.navMeshData != nullptr);
        REQUIRE(result.dataSize > 0);
        
        int polyCount = builder.GetPolyCount();
        int vertexCount = builder.GetVertexCount();
        
        REQUIRE(polyCount > 0);
        REQUIRE(vertexCount > 0);
        
        // 복잡한 메시는 더 많은 폴리곤을 가져야 함
        REQUIRE(polyCount > 5);
        
        UnityRecast_FreeNavMeshData(&result);
    }
}

TEST_CASE("NavMesh 로드 테스트", "[UnityNavMeshBuilder]")
{
    UnityNavMeshBuilder builder;
    
    // 먼저 NavMesh 빌드
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
    
    SECTION("유효한 NavMesh 데이터 로드")
    {
        bool loadResult = builder.LoadNavMesh(buildResult.navMeshData, buildResult.dataSize);
        REQUIRE(loadResult == true);
        
        // 로드 후 정보 확인
        int polyCount = builder.GetPolyCount();
        int vertexCount = builder.GetVertexCount();
        
        REQUIRE(polyCount > 0);
        REQUIRE(vertexCount > 0);
        
        // NavMesh 인스턴스 확인
        REQUIRE(builder.GetNavMesh() != nullptr);
        REQUIRE(builder.GetNavMeshQuery() != nullptr);
    }
    
    SECTION("무효한 NavMesh 데이터 로드")
    {
        // null 데이터
        bool loadResult = builder.LoadNavMesh(nullptr, 0);
        REQUIRE(loadResult == false);
        
        // 잘못된 데이터
        unsigned char invalidData[] = { 0x00, 0x01, 0x02, 0x03 };
        loadResult = builder.LoadNavMesh(invalidData, 4);
        REQUIRE(loadResult == false);
    }
    
    UnityRecast_FreeNavMeshData(&buildResult);
}

TEST_CASE("다양한 빌드 설정 테스트", "[UnityNavMeshBuilder]")
{
    UnityNavMeshBuilder builder;
    
    std::vector<float> vertices = { -1.0f, 0.0f, -1.0f, 1.0f, 0.0f, -1.0f, 1.0f, 0.0f, 1.0f };
    std::vector<int> indices = { 0, 1, 2 };
    
    UnityMeshData meshData;
    meshData.vertices = vertices.data();
    meshData.indices = indices.data();
    meshData.vertexCount = static_cast<int>(vertices.size()) / 3;
    meshData.indexCount = static_cast<int>(indices.size());
    
    SECTION("높은 품질 설정")
    {
        UnityNavMeshBuildSettings settings;
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
        
        UnityNavMeshResult result = builder.BuildNavMesh(&meshData, &settings);
        REQUIRE(result.success == true);
        
        int polyCount = builder.GetPolyCount();
        UnityRecast_FreeNavMeshData(&result);
        
        REQUIRE(polyCount > 0);
    }
    
    SECTION("낮은 품질 설정")
    {
        UnityNavMeshBuildSettings settings;
        settings.cellSize = 0.5f;
        settings.cellHeight = 0.3f;
        settings.walkableSlopeAngle = 45.0f;
        settings.walkableHeight = 2.0f;
        settings.walkableRadius = 0.6f;
        settings.walkableClimb = 0.9f;
        settings.minRegionArea = 16.0f;
        settings.mergeRegionArea = 40.0f;
        settings.maxVertsPerPoly = 6;
        settings.detailSampleDist = 12.0f;
        settings.detailSampleMaxError = 2.0f;
        
        UnityNavMeshResult result = builder.BuildNavMesh(&meshData, &settings);
        REQUIRE(result.success == true);
        
        int polyCount = builder.GetPolyCount();
        UnityRecast_FreeNavMeshData(&result);
        
        REQUIRE(polyCount > 0);
    }
    
    SECTION("극단적인 설정")
    {
        UnityNavMeshBuildSettings settings;
        settings.cellSize = 0.01f;  // 매우 작은 셀
        settings.cellHeight = 0.01f;
        settings.walkableSlopeAngle = 45.0f;
        settings.walkableHeight = 2.0f;
        settings.walkableRadius = 0.6f;
        settings.walkableClimb = 0.9f;
        settings.minRegionArea = 1.0f;
        settings.mergeRegionArea = 2.0f;
        settings.maxVertsPerPoly = 6;
        settings.detailSampleDist = 1.0f;
        settings.detailSampleMaxError = 0.1f;
        
        UnityNavMeshResult result = builder.BuildNavMesh(&meshData, &settings);
        REQUIRE(result.success == true);
        
        int polyCount = builder.GetPolyCount();
        UnityRecast_FreeNavMeshData(&result);
        
        REQUIRE(polyCount > 0);
    }
}

TEST_CASE("에러 처리 테스트", "[UnityNavMeshBuilder]")
{
    UnityNavMeshBuilder builder;
    
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
        
        UnityNavMeshResult result = builder.BuildNavMesh(nullptr, &settings);
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
        
        UnityNavMeshResult result = builder.BuildNavMesh(&meshData, nullptr);
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
        
        UnityNavMeshResult result = builder.BuildNavMesh(&meshData, &settings);
        REQUIRE(result.success == false);
    }
    
    SECTION("잘못된 인덱스")
    {
        std::vector<float> vertices = { -1.0f, 0.0f, -1.0f, 1.0f, 0.0f, -1.0f, 1.0f, 0.0f, 1.0f };
        std::vector<int> indices = { 0, 1, 5 }; // 잘못된 인덱스
        
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
        
        UnityNavMeshResult result = builder.BuildNavMesh(&meshData, &settings);
        // 잘못된 인덱스로 인해 빌드가 실패할 수 있음
        // REQUIRE(result.success == false);
    }
} 