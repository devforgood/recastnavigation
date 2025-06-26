using NUnit.Framework;
using UnityEngine;
using Unity.Mathematics;

namespace UnityRecastNavigation.Tests
{
    [TestFixture]
    public class RecastNavigationWrapperTests
    {
        private RecastNavigationWrapper wrapper;

        [SetUp]
        public void Setup()
        {
            wrapper = new RecastNavigationWrapper();
        }

        [TearDown]
        public void Teardown()
        {
            wrapper?.Dispose();
        }

        [Test]
        public void TestInitialization()
        {
            Assert.IsNotNull(wrapper);
            Assert.IsFalse(wrapper.IsInitialized);
        }

        [Test]
        public void TestBuildNavMesh_SimpleMesh()
        {
            // 간단한 평면 메시 생성
            Vector3[] vertices = {
                new Vector3(-1, 0, -1),
                new Vector3(1, 0, -1),
                new Vector3(1, 0, 1),
                new Vector3(-1, 0, 1)
            };

            int[] indices = {
                0, 1, 2,
                0, 2, 3
            };

            var settings = new NavMeshBuildSettings
            {
                cellSize = 0.3f,
                cellHeight = 0.2f,
                walkableHeight = 2.0f,
                walkableRadius = 0.6f,
                walkableClimb = 0.9f,
                walkableSlopeAngle = 45.0f,
                minRegionArea = 8.0f,
                mergeRegionArea = 20.0f,
                maxVertsPerPoly = 6,
                detailSampleDist = 6.0f,
                detailSampleMaxError = 1.0f
            };

            var result = wrapper.BuildNavMesh(vertices, indices, settings);

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.NavMeshData);
            Assert.Greater(result.DataSize, 0);
        }

        [Test]
        public void TestFindPath_SimplePath()
        {
            // 먼저 NavMesh 빌드
            Vector3[] vertices = {
                new Vector3(-1, 0, -1),
                new Vector3(1, 0, -1),
                new Vector3(1, 0, 1),
                new Vector3(-1, 0, 1)
            };

            int[] indices = {
                0, 1, 2,
                0, 2, 3
            };

            var settings = new NavMeshBuildSettings
            {
                cellSize = 0.3f,
                cellHeight = 0.2f,
                walkableHeight = 2.0f,
                walkableRadius = 0.6f,
                walkableClimb = 0.9f,
                walkableSlopeAngle = 45.0f,
                minRegionArea = 8.0f,
                mergeRegionArea = 20.0f,
                maxVertsPerPoly = 6,
                detailSampleDist = 6.0f,
                detailSampleMaxError = 1.0f
            };

            var buildResult = wrapper.BuildNavMesh(vertices, indices, settings);
            Assert.IsTrue(buildResult.Success);

            // 경로 찾기 테스트
            Vector3 start = new Vector3(-0.5f, 0, -0.5f);
            Vector3 end = new Vector3(0.5f, 0, 0.5f);

            var pathResult = wrapper.FindPath(start, end);

            Assert.IsTrue(pathResult.Success);
            Assert.IsNotNull(pathResult.Path);
            Assert.Greater(pathResult.Path.Length, 0);
        }

        [Test]
        public void TestLoadNavMesh()
        {
            // NavMesh 빌드
            Vector3[] vertices = {
                new Vector3(-1, 0, -1),
                new Vector3(1, 0, -1),
                new Vector3(1, 0, 1),
                new Vector3(-1, 0, 1)
            };

            int[] indices = {
                0, 1, 2,
                0, 2, 3
            };

            var settings = new NavMeshBuildSettings
            {
                cellSize = 0.3f,
                cellHeight = 0.2f,
                walkableHeight = 2.0f,
                walkableRadius = 0.6f,
                walkableClimb = 0.9f,
                walkableSlopeAngle = 45.0f,
                minRegionArea = 8.0f,
                mergeRegionArea = 20.0f,
                maxVertsPerPoly = 6,
                detailSampleDist = 6.0f,
                detailSampleMaxError = 1.0f
            };

            var buildResult = wrapper.BuildNavMesh(vertices, indices, settings);
            Assert.IsTrue(buildResult.Success);

            // 새로운 wrapper에 NavMesh 로드
            var newWrapper = new RecastNavigationWrapper();
            bool loadResult = newWrapper.LoadNavMesh(buildResult.NavMeshData, buildResult.DataSize);

            Assert.IsTrue(loadResult);
            Assert.IsTrue(newWrapper.IsInitialized);

            newWrapper.Dispose();
        }

        [Test]
        public void TestErrorHandling_InvalidMesh()
        {
            // 유효하지 않은 메시로 테스트
            Vector3[] vertices = { };
            int[] indices = { };

            var settings = new NavMeshBuildSettings
            {
                cellSize = 0.3f,
                cellHeight = 0.2f,
                walkableHeight = 2.0f,
                walkableRadius = 0.6f,
                walkableClimb = 0.9f,
                walkableSlopeAngle = 45.0f,
                minRegionArea = 8.0f,
                mergeRegionArea = 20.0f,
                maxVertsPerPoly = 6,
                detailSampleDist = 6.0f,
                detailSampleMaxError = 1.0f
            };

            var result = wrapper.BuildNavMesh(vertices, indices, settings);

            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.ErrorMessage);
        }
    }
} 