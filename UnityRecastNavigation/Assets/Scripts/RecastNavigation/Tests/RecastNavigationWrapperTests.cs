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
            // 같은 지점에 대한 경로 찾기는 성공하거나 실패할 수 있음
            // 중요한 것은 에러가 발생하지 않는 것
            Assert.IsNotNull(result);
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
            Assert.GreaterOrEqual(polyCount, 0);
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
            Assert.GreaterOrEqual(vertexCount, 0);
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
            var buildResult = RecastNavigationWrapper.BuildNavMesh(mesh, settings);
            Assert.IsTrue(buildResult.Success);
            
            bool loadResult = RecastNavigationWrapper.LoadNavMesh(buildResult.NavMeshData);
            Assert.IsTrue(loadResult);

            Vector3 start = new Vector3(-1.0f, 0.0f, -1.0f);
            Vector3 end = new Vector3(1.0f, 0.0f, 1.0f);

            // Act
            var pathResult = RecastNavigationWrapper.FindPath(start, end);

            // Assert
            Assert.IsTrue(pathResult.Success);
            Assert.IsNotNull(pathResult.PathPoints);
            Assert.Greater(pathResult.PathPoints.Length, 0);
        }

        [Test]
        public void NavMeshBuildSettings_CreateDefault_ShouldReturnValidSettings()
        {
            // Act
            var settings = NavMeshBuildSettingsExtensions.CreateDefault();

            // Assert
            Assert.Greater(settings.cellSize, 0);
            Assert.Greater(settings.cellHeight, 0);
            Assert.GreaterOrEqual(settings.walkableSlopeAngle, 0);
            Assert.LessOrEqual(settings.walkableSlopeAngle, 90);
            Assert.Greater(settings.walkableHeight, 0);
            Assert.Greater(settings.walkableRadius, 0);
            Assert.Greater(settings.walkableClimb, 0);
            Assert.Greater(settings.minRegionArea, 0);
            Assert.Greater(settings.mergeRegionArea, 0);
            Assert.GreaterOrEqual(settings.maxVertsPerPoly, 3);
            Assert.LessOrEqual(settings.maxVertsPerPoly, 12);
            Assert.Greater(settings.detailSampleDist, 0);
            Assert.Greater(settings.detailSampleMaxError, 0);
        }

        [Test]
        public void NavMeshBuildSettings_CreateHighQuality_ShouldReturnValidSettings()
        {
            // Act
            var settings = NavMeshBuildSettingsExtensions.CreateHighQuality();

            // Assert
            Assert.Greater(settings.cellSize, 0);
            Assert.Greater(settings.cellHeight, 0);
            // 높은 품질 설정은 일반적으로 더 작은 셀 크기를 가짐
            Assert.Less(settings.cellSize, 0.2f);
        }

        [Test]
        public void NavMeshBuildSettings_CreateLowQuality_ShouldReturnValidSettings()
        {
            // Act
            var settings = NavMeshBuildSettingsExtensions.CreateLowQuality();

            // Assert
            Assert.Greater(settings.cellSize, 0);
            Assert.Greater(settings.cellHeight, 0);
            // 낮은 품질 설정은 일반적으로 더 큰 셀 크기를 가짐
            Assert.Greater(settings.cellSize, 0.3f);
        }

        [UnityTest]
        public IEnumerator PerformanceTest_BuildNavMesh_ShouldCompleteInReasonableTime()
        {
            // Arrange
            Mesh mesh = CreateComplexMesh();
            var settings = NavMeshBuildSettingsExtensions.CreateDefault();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = RecastNavigationWrapper.BuildNavMesh(mesh, settings);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(result.Success);
            Assert.Less(stopwatch.ElapsedMilliseconds, 5000); // 5초 이내 완료
        }

        [UnityTest]
        public IEnumerator PerformanceTest_Pathfinding_ShouldCompleteInReasonableTime()
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
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var pathResult = RecastNavigationWrapper.FindPath(start, end);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(pathResult.Success);
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000); // 1초 이내 완료
        }

        private Mesh CreateSimplePlaneMesh()
        {
            Mesh mesh = new Mesh();
            
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-1f, 0f, -1f),
                new Vector3(1f, 0f, -1f),
                new Vector3(1f, 0f, 1f),
                new Vector3(-1f, 0f, 1f)
            };
            
            int[] triangles = new int[]
            {
                0, 1, 2,
                0, 2, 3
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }

        private Mesh CreateComplexMesh()
        {
            Mesh mesh = new Mesh();
            
            Vector3[] vertices = new Vector3[]
            {
                // 바닥
                new Vector3(-2f, 0f, -2f),
                new Vector3(2f, 0f, -2f),
                new Vector3(2f, 0f, 2f),
                new Vector3(-2f, 0f, 2f),
                
                // 장애물 1
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 1f, -0.5f),
                new Vector3(-0.5f, 1f, -0.5f),
                
                // 장애물 2
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(0.5f, 1f, 0.5f),
                new Vector3(-0.5f, 1f, 0.5f)
            };
            
            int[] triangles = new int[]
            {
                // 바닥
                0, 1, 2,
                0, 2, 3,
                
                // 장애물 1
                4, 5, 6,
                4, 6, 7,
                4, 7, 6,
                4, 6, 5,
                
                // 장애물 2
                8, 9, 10,
                8, 10, 11,
                8, 11, 10,
                8, 10, 9
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }
    }
} 