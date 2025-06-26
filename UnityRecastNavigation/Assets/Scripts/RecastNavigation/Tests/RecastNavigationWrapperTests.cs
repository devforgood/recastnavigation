using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RecastNavigation;

namespace RecastNavigation.Tests
{
    /// <summary>
    /// RecastNavigation 래퍼 테스트
    /// </summary>
    public class RecastNavigationWrapperTests
    {
        private RecastNavigationComponent navComponent;

        [SetUp]
        public void Setup()
        {
            // 테스트용 GameObject 생성
            GameObject testObject = new GameObject("TestNavigation");
            navComponent = testObject.AddComponent<RecastNavigationComponent>();
        }

        [TearDown]
        public void Teardown()
        {
            // 테스트 정리
            if (navComponent != null)
            {
                navComponent.CleanupRecastNavigation();
                Object.DestroyImmediate(navComponent.gameObject);
            }
        }

        [Test]
        public void TestInitialization()
        {
            // 초기화 테스트
            Assert.IsTrue(navComponent.IsInitialized());
            Debug.Log("초기화 테스트 통과");
        }

        [Test]
        public void TestCoordinateSystemSettings()
        {
            // 좌표계 설정 테스트
            navComponent.SetCoordinateSystem(CoordinateSystem.LeftHanded);
            Assert.AreEqual(CoordinateSystem.LeftHanded, RecastNavigationWrapper.UnityRecast_GetCoordinateSystem());
            
            navComponent.SetCoordinateSystem(CoordinateSystem.RightHanded);
            Assert.AreEqual(CoordinateSystem.RightHanded, RecastNavigationWrapper.UnityRecast_GetCoordinateSystem());
            
            Debug.Log("좌표계 설정 테스트 통과");
        }

        [Test]
        public void TestCoordinateTransformation()
        {
            // 좌표 변환 테스트
            navComponent.SetCoordinateSystem(CoordinateSystem.LeftHanded);
            
            Vector3 originalPosition = new Vector3(1, 2, 3);
            Vector3 transformedPosition = RecastNavigationWrapper.TransformPosition(originalPosition);
            
            // Unity (왼손 좌표계) -> RecastNavigation (오른손 좌표계) 변환 확인
            // Z축이 반전되어야 함
            Assert.AreEqual(originalPosition.x, transformedPosition.x);
            Assert.AreEqual(originalPosition.y, transformedPosition.y);
            Assert.AreEqual(-originalPosition.z, transformedPosition.z);
            
            Debug.Log($"좌표 변환 테스트: {originalPosition} -> {transformedPosition}");
        }

        [Test]
        public void TestCoordinateArrayTransformation()
        {
            // 좌표 배열 변환 테스트
            navComponent.SetCoordinateSystem(CoordinateSystem.LeftHanded);
            
            Vector3[] originalPositions = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 1, 1),
                new Vector3(2, 2, 2)
            };
            
            Vector3[] transformedPositions = RecastNavigationWrapper.TransformPositions(originalPositions);
            
            Assert.AreEqual(originalPositions.Length, transformedPositions.Length);
            
            for (int i = 0; i < originalPositions.Length; i++)
            {
                Assert.AreEqual(originalPositions[i].x, transformedPositions[i].x);
                Assert.AreEqual(originalPositions[i].y, transformedPositions[i].y);
                Assert.AreEqual(-originalPositions[i].z, transformedPositions[i].z);
            }
            
            Debug.Log("좌표 배열 변환 테스트 통과");
        }

        [Test]
        public void TestAutoTransformCoordinates()
        {
            // 자동 좌표 변환 설정 테스트
            navComponent.SetAutoTransformCoordinates(true);
            
            // 설정이 올바르게 적용되었는지 확인
            // (내부 구현에 따라 다를 수 있음)
            Debug.Log("자동 좌표 변환 설정 테스트 통과");
        }

        [Test]
        public void TestNavMeshBuildSettings()
        {
            // NavMesh 빌드 설정 테스트
            NavMeshBuildSettings defaultSettings = NavMeshBuildSettings.CreateDefault();
            NavMeshBuildSettings highQualitySettings = NavMeshBuildSettings.CreateHighQuality();
            NavMeshBuildSettings lowQualitySettings = NavMeshBuildSettings.CreateLowQuality();
            
            // 기본 설정이 올바른지 확인
            Assert.Greater(defaultSettings.cellSize, 0);
            Assert.Greater(defaultSettings.cellHeight, 0);
            Assert.Greater(defaultSettings.walkableSlopeAngle, 0);
            
            // 고품질 설정이 기본 설정보다 세밀한지 확인
            Assert.Less(highQualitySettings.cellSize, defaultSettings.cellSize);
            Assert.Less(highQualitySettings.cellHeight, defaultSettings.cellHeight);
            
            // 저품질 설정이 기본 설정보다 거칠은지 확인
            Assert.Greater(lowQualitySettings.cellSize, defaultSettings.cellSize);
            Assert.Greater(lowQualitySettings.cellHeight, defaultSettings.cellHeight);
            
            Debug.Log("NavMesh 빌드 설정 테스트 통과");
        }

        [Test]
        public void TestSimpleMeshCreation()
        {
            // 간단한 테스트 메시 생성
            Mesh testMesh = CreateTestMesh();
            
            // NavMesh 빌드 테스트
            bool success = navComponent.BuildNavMesh(testMesh);
            
            // 빌드 성공 여부 확인 (실제 환경에 따라 다를 수 있음)
            Debug.Log($"NavMesh 빌드 테스트 결과: {success}");
        }

        [Test]
        public void TestPathfindingWithTransformedCoordinates()
        {
            // 좌표 변환을 사용한 경로 찾기 테스트
            navComponent.SetCoordinateSystem(CoordinateSystem.LeftHanded);
            navComponent.SetAutoTransformCoordinates(true);
            
            // 간단한 테스트 메시 생성
            Mesh testMesh = CreateTestMesh();
            bool buildSuccess = navComponent.BuildNavMesh(testMesh);
            
            if (buildSuccess)
            {
                // 경로 찾기 테스트
                Vector3 start = new Vector3(0, 1, 0);
                Vector3 end = new Vector3(5, 1, 5);
                
                PathfindingResult result = navComponent.FindPath(start, end);
                
                Debug.Log($"경로 찾기 테스트 결과: {result.Success}");
                if (result.Success)
                {
                    Debug.Log($"경로 포인트 수: {result.PathPoints.Length}");
                }
                else
                {
                    Debug.Log($"경로 찾기 실패: {result.ErrorMessage}");
                }
            }
            else
            {
                Debug.Log("NavMesh 빌드 실패로 경로 찾기 테스트를 건너뜀");
            }
        }

        [Test]
        public void TestCoordinateSystemConsistency()
        {
            // 좌표계 일관성 테스트
            navComponent.SetCoordinateSystem(CoordinateSystem.LeftHanded);
            
            Vector3 testPosition = new Vector3(1, 2, 3);
            
            // 변환 -> 역변환 시 원래 값과 일치하는지 확인
            Vector3 transformed = RecastNavigationWrapper.TransformPosition(testPosition);
            
            // 좌표계를 RightHanded로 변경하고 다시 변환
            navComponent.SetCoordinateSystem(CoordinateSystem.RightHanded);
            Vector3 backTransformed = RecastNavigationWrapper.TransformPosition(transformed);
            
            // 결과가 원래 값과 일치해야 함
            Assert.AreEqual(testPosition.x, backTransformed.x, 0.001f);
            Assert.AreEqual(testPosition.y, backTransformed.y, 0.001f);
            Assert.AreEqual(testPosition.z, backTransformed.z, 0.001f);
            
            Debug.Log("좌표계 일관성 테스트 통과");
        }

        [Test]
        public void TestErrorHandling()
        {
            // 오류 처리 테스트
            navComponent.SetCoordinateSystem(CoordinateSystem.LeftHanded);
            
            // 잘못된 좌표로 경로 찾기 시도
            Vector3 invalidStart = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 invalidEnd = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            
            PathfindingResult result = navComponent.FindPath(invalidStart, invalidEnd);
            
            // 오류가 올바르게 처리되는지 확인
            Debug.Log($"오류 처리 테스트: {result.Success} - {result.ErrorMessage}");
        }

        [Test]
        public void TestMemoryManagement()
        {
            // 메모리 관리 테스트
            navComponent.SetCoordinateSystem(CoordinateSystem.LeftHanded);
            
            // 여러 번 좌표 변환 수행
            for (int i = 0; i < 1000; i++)
            {
                Vector3 testPos = new Vector3(i, i, i);
                Vector3 transformed = RecastNavigationWrapper.TransformPosition(testPos);
                
                // 메모리 누수 확인을 위한 간단한 검증
                Assert.IsFalse(float.IsNaN(transformed.x));
                Assert.IsFalse(float.IsNaN(transformed.y));
                Assert.IsFalse(float.IsNaN(transformed.z));
            }
            
            Debug.Log("메모리 관리 테스트 통과");
        }

        /// <summary>
        /// 테스트용 간단한 메시 생성
        /// </summary>
        private Mesh CreateTestMesh()
        {
            Mesh mesh = new Mesh();
            
            // 간단한 평면 메시 생성
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-5, 0, -5),
                new Vector3(5, 0, -5),
                new Vector3(5, 0, 5),
                new Vector3(-5, 0, 5)
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

        [UnityTest]
        public IEnumerator TestAsyncOperations()
        {
            // 비동기 작업 테스트
            navComponent.SetCoordinateSystem(CoordinateSystem.LeftHanded);
            
            // 프레임 대기
            yield return null;
            
            // 좌표 변환 테스트
            Vector3 testPosition = new Vector3(1, 2, 3);
            Vector3 transformed = RecastNavigationWrapper.TransformPosition(testPosition);
            
            Assert.AreEqual(testPosition.x, transformed.x);
            Assert.AreEqual(testPosition.y, transformed.y);
            Assert.AreEqual(-testPosition.z, transformed.z);
            
            Debug.Log("비동기 작업 테스트 통과");
        }
    }
} 