using UnityEngine;
using System.Runtime.InteropServices;
using System;

namespace RecastNavigation
{
    /// <summary>
    /// RecastNavigation DLL을 Unity에서 사용할 수 있도록 래핑한 클래스
    /// </summary>
    public static class RecastNavigationWrapper
    {
        #region DLL Import

        // 초기화 및 정리
        [DllImport("RecastNavigationUnity")]
        private static extern bool InitializeRecastNavigation();

        [DllImport("RecastNavigationUnity")]
        private static extern void CleanupRecastNavigation();

        // NavMesh 빌드
        [DllImport("RecastNavigationUnity")]
        private static extern bool BuildNavMeshFromMesh(
            [In] Vector3[] vertices, int vertexCount,
            [In] int[] indices, int indexCount,
            [In] ref NavMeshBuildSettings settings,
            [Out] out IntPtr navMeshData, [Out] out int dataSize,
            [Out] out IntPtr errorMessage);

        // NavMesh 로드
        [DllImport("RecastNavigationUnity")]
        private static extern bool LoadNavMeshFromData(
            [In] byte[] data, int dataSize);

        // 경로 찾기
        [DllImport("RecastNavigationUnity")]
        private static extern bool FindPathBetweenPoints(
            [In] Vector3 start, [In] Vector3 end,
            [Out] out IntPtr pathPoints, [Out] out int pointCount,
            [Out] out IntPtr errorMessage);

        // NavMesh 정보
        [DllImport("RecastNavigationUnity")]
        private static extern int GetNavMeshPolyCount();

        [DllImport("RecastNavigationUnity")]
        private static extern int GetNavMeshVertexCount();

        // 메모리 해제
        [DllImport("RecastNavigationUnity")]
        private static extern void FreeMemory(IntPtr ptr);

        #endregion

        #region Public API

        /// <summary>
        /// RecastNavigation 초기화
        /// </summary>
        public static bool Initialize()
        {
            try
            {
                return InitializeRecastNavigation();
            }
            catch (Exception e)
            {
                Debug.LogError($"RecastNavigation 초기화 실패: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// RecastNavigation 정리
        /// </summary>
        public static void Cleanup()
        {
            try
            {
                CleanupRecastNavigation();
            }
            catch (Exception e)
            {
                Debug.LogError($"RecastNavigation 정리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// Mesh에서 NavMesh 빌드
        /// </summary>
        public static NavMeshBuildResult BuildNavMesh(Mesh mesh, NavMeshBuildSettings settings)
        {
            if (mesh == null)
            {
                return new NavMeshBuildResult { Success = false, ErrorMessage = "Mesh가 null입니다." };
            }

            try
            {
                Vector3[] vertices = mesh.vertices;
                int[] indices = mesh.triangles;

                IntPtr navMeshData;
                int dataSize;
                IntPtr errorMessage;

                bool success = BuildNavMeshFromMesh(
                    vertices, vertices.Length,
                    indices, indices.Length,
                    ref settings,
                    out navMeshData, out dataSize,
                    out errorMessage);

                if (success)
                {
                    byte[] data = new byte[dataSize];
                    Marshal.Copy(navMeshData, data, 0, dataSize);
                    FreeMemory(navMeshData);

                    return new NavMeshBuildResult
                    {
                        Success = true,
                        NavMeshData = data
                    };
                }
                else
                {
                    string error = Marshal.PtrToStringAnsi(errorMessage);
                    FreeMemory(errorMessage);
                    return new NavMeshBuildResult { Success = false, ErrorMessage = error };
                }
            }
            catch (Exception e)
            {
                return new NavMeshBuildResult { Success = false, ErrorMessage = e.Message };
            }
        }

        /// <summary>
        /// NavMesh 데이터 로드
        /// </summary>
        public static bool LoadNavMesh(byte[] navMeshData)
        {
            if (navMeshData == null || navMeshData.Length == 0)
            {
                Debug.LogError("NavMesh 데이터가 null이거나 비어있습니다.");
                return false;
            }

            try
            {
                return LoadNavMeshFromData(navMeshData, navMeshData.Length);
            }
            catch (Exception e)
            {
                Debug.LogError($"NavMesh 로드 실패: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 경로 찾기
        /// </summary>
        public static PathfindingResult FindPath(Vector3 start, Vector3 end)
        {
            try
            {
                IntPtr pathPoints;
                int pointCount;
                IntPtr errorMessage;

                bool success = FindPathBetweenPoints(start, end, out pathPoints, out pointCount, out errorMessage);

                if (success)
                {
                    Vector3[] points = new Vector3[pointCount];
                    Marshal.Copy(pathPoints, points, 0, pointCount);
                    FreeMemory(pathPoints);

                    return new PathfindingResult
                    {
                        Success = true,
                        PathPoints = points
                    };
                }
                else
                {
                    string error = Marshal.PtrToStringAnsi(errorMessage);
                    FreeMemory(errorMessage);
                    return new PathfindingResult { Success = false, ErrorMessage = error };
                }
            }
            catch (Exception e)
            {
                return new PathfindingResult { Success = false, ErrorMessage = e.Message };
            }
        }

        /// <summary>
        /// NavMesh 폴리곤 수 가져오기
        /// </summary>
        public static int GetPolyCount()
        {
            try
            {
                return GetNavMeshPolyCount();
            }
            catch (Exception e)
            {
                Debug.LogError($"폴리곤 수 가져오기 실패: {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// NavMesh 정점 수 가져오기
        /// </summary>
        public static int GetVertexCount()
        {
            try
            {
                return GetNavMeshVertexCount();
            }
            catch (Exception e)
            {
                Debug.LogError($"정점 수 가져오기 실패: {e.Message}");
                return 0;
            }
        }

        #endregion
    }

    /// <summary>
    /// NavMesh 빌드 설정
    /// </summary>
    [Serializable]
    public struct NavMeshBuildSettings
    {
        public float cellSize;
        public float cellHeight;
        public float walkableSlopeAngle;
        public float walkableHeight;
        public float walkableRadius;
        public float walkableClimb;
        public float minRegionArea;
        public float mergeRegionArea;
        public int maxVertsPerPoly;
        public float detailSampleDist;
        public float detailSampleMaxError;
    }

    /// <summary>
    /// NavMesh 빌드 결과
    /// </summary>
    public struct NavMeshBuildResult
    {
        public bool Success;
        public byte[] NavMeshData;
        public string ErrorMessage;
    }

    /// <summary>
    /// 경로 찾기 결과
    /// </summary>
    public struct PathfindingResult
    {
        public bool Success;
        public Vector3[] PathPoints;
        public string ErrorMessage;
    }

    /// <summary>
    /// NavMesh 빌드 설정 확장 메서드
    /// </summary>
    public static class NavMeshBuildSettingsExtensions
    {
        /// <summary>
        /// 기본 설정 생성
        /// </summary>
        public static NavMeshBuildSettings CreateDefault()
        {
            return new NavMeshBuildSettings
            {
                cellSize = 0.3f,
                cellHeight = 0.2f,
                walkableSlopeAngle = 45f,
                walkableHeight = 2.0f,
                walkableRadius = 0.6f,
                walkableClimb = 0.9f,
                minRegionArea = 8f,
                mergeRegionArea = 20f,
                maxVertsPerPoly = 6,
                detailSampleDist = 6f,
                detailSampleMaxError = 1f
            };
        }

        /// <summary>
        /// 높은 품질 설정 생성
        /// </summary>
        public static NavMeshBuildSettings CreateHighQuality()
        {
            return new NavMeshBuildSettings
            {
                cellSize = 0.1f,
                cellHeight = 0.1f,
                walkableSlopeAngle = 45f,
                walkableHeight = 2.0f,
                walkableRadius = 0.6f,
                walkableClimb = 0.9f,
                minRegionArea = 4f,
                mergeRegionArea = 10f,
                maxVertsPerPoly = 6,
                detailSampleDist = 3f,
                detailSampleMaxError = 0.5f
            };
        }

        /// <summary>
        /// 낮은 품질 설정 생성 (빠른 빌드)
        /// </summary>
        public static NavMeshBuildSettings CreateLowQuality()
        {
            return new NavMeshBuildSettings
            {
                cellSize = 0.5f,
                cellHeight = 0.3f,
                walkableSlopeAngle = 45f,
                walkableHeight = 2.0f,
                walkableRadius = 0.6f,
                walkableClimb = 0.9f,
                minRegionArea = 16f,
                mergeRegionArea = 40f,
                maxVertsPerPoly = 6,
                detailSampleDist = 12f,
                detailSampleMaxError = 2f
            };
        }
    }
} 