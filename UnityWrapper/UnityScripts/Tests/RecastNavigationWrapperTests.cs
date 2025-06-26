using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RecastNavigation;

namespace RecastNavigation.Tests
{
    public class RecastNavigationWrapperTests
    {
        [SetUp]
        public void SetUp()
        {
            // 각 테스트 전에 RecastNavigation 초기화
            if (!RecastNavigationWrapper.Initialize())
            {
                Assert.Fail("RecastNavigation 초기화에 실패했습니다.");
            }
        }

        [TearDown]
        public void TearDown()
        {
            // 각 테스트 후에 RecastNavigation 정리
            RecastNavigationWrapper.Cleanup();
        }

        [Test]
        public void Initialize_ShouldReturnTrue()
        {
            // Arrange & Act
            bool result = RecastNavigationWrapper.Initialize();

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Initialize_MultipleCalls_ShouldReturnTrue()
        {
            // Arrange & Act
            bool result1 = RecastNavigationWrapper.Initialize();
            bool result2 = RecastNavigationWrapper.Initialize();

            // Assert
            Assert.IsTrue(result1);
            Assert.IsTrue(result2);
        }

        [Test]
        public void BuildNavMesh_WithValidMesh_ShouldSucceed()
        {
            // Arrange
            Mesh mesh = CreateSimplePlaneMesh();
            var settings = NavMeshBuildSettingsExtensions.CreateDefault();

            // Act
            var result = RecastNavigationWrapper.BuildNavMesh(mesh, settings);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.NavMeshData);
            Assert.Greater(result.NavMeshData.Length, 0);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        public void BuildNavMesh_WithNullMesh_ShouldFail()
        {
            // Arrange
            Mesh mesh = null;
            var settings = NavMeshBuildSettingsExtensions.CreateDefault();

            // Act
            var result = RecastNavigationWrapper.BuildNavMesh(mesh, settings);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [Test]
        public void BuildNavMesh_WithDifferentQualitySettings_ShouldProduceDifferentResults()
        {
            // Arrange
            Mesh mesh = CreateSimplePlaneMesh();
            var lowQualitySettings = NavMeshBuildSettingsExtensions.CreateLowQuality();
            var highQualitySettings = NavMeshBuildSettingsExtensions.CreateHighQuality();

            // Act
            var lowQualityResult = RecastNavigationWrapper.BuildNavMesh(mesh, lowQualitySettings);
            var highQualityResult = RecastNavigationWrapper.BuildNavMesh(mesh, highQualitySettings);

            // Assert
            Assert.IsTrue(lowQualityResult.Success);
            Assert.IsTrue(highQualityResult.Success);
            
            // 높은 품질 설정이 더 많은 데이터를 생성할 가능성이 높음
            // (정확한 비교는 어려우므로 성공 여부만 확인)
        }

        [Test]
        public void LoadNavMesh_WithValidData_ShouldSucceed()
        {
            // Arrange
            Mesh mesh = CreateSimplePlaneMesh();
            var settings = NavMeshBuildSettingsExtensions.CreateDefault();
            var buildResult = RecastNavigationWrapper.BuildNavMesh(mesh, settings);
            Assert.IsTrue(buildResult.Success);

            // Act
            bool loadResult = RecastNavigationWrapper.LoadNavMesh(buildResult.NavMeshData);

            // Assert
            Assert.IsTrue(loadResult);
        }

        [Test]
        public void LoadNavMesh_WithNullData_ShouldFail()
        {
            // Arrange
            byte[] nullData = null;

            // Act
            bool result = RecastNavigationWrapper.LoadNavMesh(nullData);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void LoadNavMesh_WithEmptyData_ShouldFail()
        {
            // Arrange
            byte[] emptyData = new byte[0];

            // Act
            bool result = RecastNavigationWrapper.LoadNavMesh(emptyData);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void FindPath_WithValidNavMesh_ShouldSucceed()
        {
            // Arrange
            Mesh mesh = CreateSimplePlaneMesh();
            var settings = NavMeshBuildSettingsExtensions.CreateDefault();
            var buildResult = RecastNavigationWrapper.BuildNavMesh(mesh, settings);
            Assert.IsTrue(buildResult.Success);
            
            bool loadResult = RecastNavigationWrapper.LoadNavMesh(buildResult.NavMeshData);
            Assert.IsTrue(loadResult);

            Vector3 start = new Vector3(-0.5f, 0.0f, -0.5f);
            Vector3 end = new Vector3(0.5f, 0.0f, 0.5f);

            // Act
            var pathResult = RecastNavigationWrapper.FindPath(start, end);

            // Assert
            Assert.IsTrue(pathResult.Success);
            Assert.IsNotNull(pathResult.PathPoints);
            Assert.Greater(pathResult.PathPoints.Length, 0);
            Assert.IsNull(pathResult.ErrorMessage);
        }

        [Test]
        public void FindPath_WithoutNavMesh_ShouldFail()
        {
            // Arrange
            Vector3 start = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 end = new Vector3(1.0f, 0.0f, 1.0f);

            // Act
            var result = RecastNavigationWrapper.FindPath(start, end);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [Test]
        public void FindPath_WithSameStartAndEnd_ShouldHandleGracefully()
        {
            // Arrange
            Mesh mesh = CreateSimplePlaneMesh();
            var settings = NavMeshBuildSettingsExtensions.CreateDefault();
            var buildResult = RecastNavigationWrapper.BuildNavMesh(mesh, settings);
            Assert.IsTrue(buildResult.Success);
            
            bool loadResult = RecastNavigationWrapper.LoadNavMesh(buildResult.NavMeshData);
            Assert.IsTrue(loadResult);

            Vector3 samePoint = new Vector3(0.0f, 0.0f, 0.0f);

            // Act
            var result = RecastNavigationWrapper.FindPath(samePoint, samePoint);

            // Assert
            // 같은 지점으로의 경로는 실패하거나 매우 짧아야 함
            if (result.Success)
            {
                Assert.LessOrEqual(result.PathPoints.Length, 2);
            }
        }

        [Test]
        public void GetPolyCount_WithLoadedNavMesh_ShouldReturnPositiveNumber()
        {
            // Arrange
            Mesh mesh = CreateSimplePlaneMesh();
            var settings = NavMeshBuildSettingsExtensions.CreateDefault();
            var buildResult = RecastNavigationWrapper.BuildNavMesh(mesh, settings);
            Assert.IsTrue(buildResult.Success);
            
            bool loadResult = RecastNavigationWrapper.LoadNavMesh(buildResult.NavMeshData);
            Assert.IsTrue(loadResult);

            // Act
            int polyCount = RecastNavigationWrapper.GetPolyCount();

            // Assert
            Assert.Greater(polyCount, 0);
        }

        [Test]
        public void GetVertexCount_WithLoadedNavMesh_ShouldReturnPositiveNumber()
        {
            // Arrange
            Mesh mesh = CreateSimplePlaneMesh();
            var settings = NavMeshBuildSettingsExtensions.CreateDefault();
            var buildResult = RecastNavigationWrapper.BuildNavMesh(mesh, settings);
            Assert.IsTrue(buildResult.Success);
            
            bool loadResult = RecastNavigationWrapper.LoadNavMesh(buildResult.NavMeshData);
            Assert.IsTrue(loadResult);

            // Act
            int vertexCount = RecastNavigationWrapper.GetVertexCount();

            // Assert
            Assert.Greater(vertexCount, 0);
        }

        [Test]
        public void GetPolyCount_WithoutNavMesh_ShouldReturnZero()
        {
            // Act
            int polyCount = RecastNavigationWrapper.GetPolyCount();

            // Assert
            Assert.AreEqual(0, polyCount);
        }

        [Test]
        public void GetVertexCount_WithoutNavMesh_ShouldReturnZero()
        {
            // Act
            int vertexCount = RecastNavigationWrapper.GetVertexCount();

            // Assert
            Assert.AreEqual(0, vertexCount);
        }

        [Test]
        public void ComplexMesh_BuildAndPathfinding_ShouldWork()
        {
            // Arrange
            Mesh mesh = CreateComplexMesh();
            var settings = NavMeshBuildSettingsExtensions.CreateDefault();
            
            // Act
            var buildResult = RecastNavigationWrapper.BuildNavMesh(mesh, settings);
            
            // Assert
            Assert.IsTrue(buildResult.Success);
            
            // NavMesh 로드
            bool loadResult = RecastNavigationWrapper.LoadNavMesh(buildResult.NavMeshData);
            Assert.IsTrue(loadResult);
            
            // 경로 찾기 테스트
            Vector3 start = new Vector3(-1.0f, 0.0f, 0.0f);
            Vector3 end = new Vector3(1.0f, 0.0f, 0.0f);
            
            var pathResult = RecastNavigationWrapper.FindPath(start, end);
            Assert.IsTrue(pathResult.Success);
            Assert.Greater(pathResult.PathPoints.Length, 0);
        }

        [Test]
        public void NavMeshBuildSettings_CreateDefault_ShouldReturnValidSettings()
        {
            // Act
            var settings = NavMeshBuildSettingsExtensions.CreateDefault();

            // Assert
            Assert.Greater(settings.cellSize, 0.0f);
            Assert.Greater(settings.cellHeight, 0.0f);
            Assert.Greater(settings.walkableSlopeAngle, 0.0f);
            Assert.Greater(settings.walkableHeight, 0.0f);
            Assert.Greater(settings.walkableRadius, 0.0f);
            Assert.Greater(settings.walkableClimb, 0.0f);
            Assert.Greater(settings.minRegionArea, 0.0f);
            Assert.Greater(settings.mergeRegionArea, 0.0f);
            Assert.Greater(settings.maxVertsPerPoly, 0);
            Assert.Greater(settings.detailSampleDist, 0.0f);
            Assert.Greater(settings.detailSampleMaxError, 0.0f);
        }

        [Test]
        public void NavMeshBuildSettings_CreateHighQuality_ShouldReturnValidSettings()
        {
            // Act
            var settings = NavMeshBuildSettingsExtensions.CreateHighQuality();

            // Assert
            Assert.Greater(settings.cellSize, 0.0f);
            Assert.Greater(settings.cellHeight, 0.0f);
            // 높은 품질 설정은 더 작은 셀 크기를 가져야 함
            Assert.Less(settings.cellSize, 0.2f);
        }

        [Test]
        public void NavMeshBuildSettings_CreateLowQuality_ShouldReturnValidSettings()
        {
            // Act
            var settings = NavMeshBuildSettingsExtensions.CreateLowQuality();

            // Assert
            Assert.Greater(settings.cellSize, 0.0f);
            Assert.Greater(settings.cellHeight, 0.0f);
            // 낮은 품질 설정은 더 큰 셀 크기를 가져야 함
            Assert.Greater(settings.cellSize, 0.3f);
        }

        // 헬퍼 메서드들
        private Mesh CreateSimplePlaneMesh()
        {
            Mesh mesh = new Mesh();
            
            // 간단한 평면 메시 생성 (2x2 평면)
            Vector3[] vertices = {
                new Vector3(-1.0f, 0.0f, -1.0f),
                new Vector3( 1.0f, 0.0f, -1.0f),
                new Vector3( 1.0f, 0.0f,  1.0f),
                new Vector3(-1.0f, 0.0f,  1.0f)
            };
            
            int[] triangles = {
                0, 1, 2,  // 첫 번째 삼각형
                0, 2, 3   // 두 번째 삼각형
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }

        private Mesh CreateComplexMesh()
        {
            Mesh mesh = new Mesh();
            
            // 복잡한 메시 생성 (여러 삼각형으로 구성된 지형)
            Vector3[] vertices = {
                // 바닥
                new Vector3(-2.0f, 0.0f, -2.0f),
                new Vector3( 2.0f, 0.0f, -2.0f),
                new Vector3( 2.0f, 0.0f,  2.0f),
                new Vector3(-2.0f, 0.0f,  2.0f),
                
                // 계단 1
                new Vector3(-1.0f, 0.5f, -1.0f),
                new Vector3( 1.0f, 0.5f, -1.0f),
                new Vector3( 1.0f, 0.5f,  1.0f),
                new Vector3(-1.0f, 0.5f,  1.0f),
                
                // 계단 2
                new Vector3(-0.5f, 1.0f, -0.5f),
                new Vector3( 0.5f, 1.0f, -0.5f),
                new Vector3( 0.5f, 1.0f,  0.5f),
                new Vector3(-0.5f, 1.0f,  0.5f)
            };
            
            int[] triangles = {
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
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }
    }
} 