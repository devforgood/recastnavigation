using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RecastNavigation
{
    /// <summary>
    /// Unity에서 RecastNavigation을 사용하기 위한 래퍼 클래스
    /// </summary>
    public static class RecastNavigationWrapper
    {
        #region DLL Import
        
        [DllImport("RecastNavigationUnity")]
        private static extern bool UnityRecast_Initialize();
        
        [DllImport("RecastNavigationUnity")]
        private static extern void UnityRecast_Cleanup();
        
        [DllImport("RecastNavigationUnity")]
        private static extern NavMeshResult UnityRecast_BuildNavMesh(
            [In] ref MeshData meshData,
            [In] ref NavMeshBuildSettings settings
        );
        
        [DllImport("RecastNavigationUnity")]
        private static extern void UnityRecast_FreeNavMeshData(ref NavMeshResult result);
        
        [DllImport("RecastNavigationUnity")]
        private static extern bool UnityRecast_LoadNavMesh(IntPtr data, int dataSize);
        
        [DllImport("RecastNavigationUnity")]
        private static extern PathResult UnityRecast_FindPath(
            float startX, float startY, float startZ,
            float endX, float endY, float endZ
        );
        
        [DllImport("RecastNavigationUnity")]
        private static extern void UnityRecast_FreePathResult(ref PathResult result);
        
        [DllImport("RecastNavigationUnity")]
        private static extern int UnityRecast_GetPolyCount();
        
        [DllImport("RecastNavigationUnity")]
        private static extern int UnityRecast_GetVertexCount();
        
        #endregion
        
        #region Structures
        
        [StructLayout(LayoutKind.Sequential)]
        public struct MeshData
        {
            public IntPtr vertices;      // float* vertices
            public IntPtr indices;       // int* indices
            public int vertexCount;
            public int indexCount;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct NavMeshBuildSettings
        {
            public float cellSize;           // 셀 크기
            public float cellHeight;         // 셀 높이
            public float walkableSlopeAngle; // 이동 가능한 경사각
            public float walkableHeight;     // 이동 가능한 높이
            public float walkableRadius;     // 이동 가능한 반지름
            public float walkableClimb;      // 이동 가능한 오르기 높이
            public float minRegionArea;      // 최소 영역 크기
            public float mergeRegionArea;    // 병합 영역 크기
            public int maxVertsPerPoly;      // 폴리곤당 최대 정점 수
            public float detailSampleDist;   // 상세 샘플링 거리
            public float detailSampleMaxError; // 상세 샘플링 최대 오차
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct NavMeshResult
        {
            public IntPtr navMeshData;  // unsigned char* navMeshData
            public int dataSize;        // 데이터 크기
            public bool success;        // 성공 여부
            public IntPtr errorMessage; // char* errorMessage
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct PathResult
        {
            public IntPtr pathPoints;   // float* pathPoints
            public int pointCount;      // 포인트 개수
            public bool success;        // 성공 여부
            public IntPtr errorMessage; // char* errorMessage
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// RecastNavigation 초기화
        /// </summary>
        /// <returns>초기화 성공 여부</returns>
        public static bool Initialize()
        {
            try
            {
                return UnityRecast_Initialize();
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
                UnityRecast_Cleanup();
            }
            catch (Exception e)
            {
                Debug.LogError($"RecastNavigation 정리 실패: {e.Message}");
            }
        }
        
        /// <summary>
        /// Unity Mesh에서 NavMesh 빌드
        /// </summary>
        /// <param name="mesh">Unity Mesh</param>
        /// <param name="settings">NavMesh 빌드 설정</param>
        /// <returns>NavMesh 빌드 결과</returns>
        public static NavMeshBuildResult BuildNavMesh(Mesh mesh, NavMeshBuildSettings settings)
        {
            if (mesh == null)
            {
                return new NavMeshBuildResult { Success = false, ErrorMessage = "Mesh가 null입니다." };
            }
            
            try
            {
                // Mesh 데이터 준비
                Vector3[] vertices = mesh.vertices;
                int[] indices = mesh.triangles;
                
                // GCHandle을 사용하여 메모리 고정
                GCHandle verticesHandle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
                GCHandle indicesHandle = GCHandle.Alloc(indices, GCHandleType.Pinned);
                
                try
                {
                    MeshData meshData = new MeshData
                    {
                        vertices = verticesHandle.AddrOfPinnedObject(),
                        indices = indicesHandle.AddrOfPinnedObject(),
                        vertexCount = vertices.Length,
                        indexCount = indices.Length
                    };
                    
                    // NavMesh 빌드
                    NavMeshResult result = UnityRecast_BuildNavMesh(ref meshData, ref settings);
                    
                    if (result.success)
                    {
                        // 성공한 경우 NavMesh 데이터 복사
                        byte[] navMeshData = new byte[result.dataSize];
                        Marshal.Copy(result.navMeshData, navMeshData, 0, result.dataSize);
                        
                        return new NavMeshBuildResult
                        {
                            Success = true,
                            NavMeshData = navMeshData
                        };
                    }
                    else
                    {
                        string errorMessage = "알 수 없는 오류";
                        if (result.errorMessage != IntPtr.Zero)
                        {
                            errorMessage = Marshal.PtrToStringAnsi(result.errorMessage);
                        }
                        
                        return new NavMeshBuildResult
                        {
                            Success = false,
                            ErrorMessage = errorMessage
                        };
                    }
                }
                finally
                {
                    // 메모리 해제
                    verticesHandle.Free();
                    indicesHandle.Free();
                    
                    // NavMesh 결과 정리
                    UnityRecast_FreeNavMeshData(ref result);
                }
            }
            catch (Exception e)
            {
                return new NavMeshBuildResult
                {
                    Success = false,
                    ErrorMessage = $"NavMesh 빌드 중 오류 발생: {e.Message}"
                };
            }
        }
        
        /// <summary>
        /// NavMesh 데이터 로드
        /// </summary>
        /// <param name="navMeshData">NavMesh 데이터</param>
        /// <returns>로드 성공 여부</returns>
        public static bool LoadNavMesh(byte[] navMeshData)
        {
            if (navMeshData == null || navMeshData.Length == 0)
            {
                Debug.LogError("NavMesh 데이터가 유효하지 않습니다.");
                return false;
            }
            
            try
            {
                GCHandle dataHandle = GCHandle.Alloc(navMeshData, GCHandleType.Pinned);
                
                try
                {
                    return UnityRecast_LoadNavMesh(dataHandle.AddrOfPinnedObject(), navMeshData.Length);
                }
                finally
                {
                    dataHandle.Free();
                }
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
        /// <param name="start">시작점</param>
        /// <param name="end">끝점</param>
        /// <returns>경로 찾기 결과</returns>
        public static PathFindResult FindPath(Vector3 start, Vector3 end)
        {
            try
            {
                PathResult result = UnityRecast_FindPath(start.x, start.y, start.z, end.x, end.y, end.z);
                
                if (result.success)
                {
                    // 경로 포인트 복사
                    Vector3[] pathPoints = new Vector3[result.pointCount];
                    float[] floatArray = new float[result.pointCount * 3];
                    
                    Marshal.Copy(result.pathPoints, floatArray, 0, result.pointCount * 3);
                    
                    for (int i = 0; i < result.pointCount; i++)
                    {
                        pathPoints[i] = new Vector3(
                            floatArray[i * 3],
                            floatArray[i * 3 + 1],
                            floatArray[i * 3 + 2]
                        );
                    }
                    
                    return new PathFindResult
                    {
                        Success = true,
                        PathPoints = pathPoints
                    };
                }
                else
                {
                    string errorMessage = "알 수 없는 오류";
                    if (result.errorMessage != IntPtr.Zero)
                    {
                        errorMessage = Marshal.PtrToStringAnsi(result.errorMessage);
                    }
                    
                    return new PathFindResult
                    {
                        Success = false,
                        ErrorMessage = errorMessage
                    };
                }
            }
            catch (Exception e)
            {
                return new PathFindResult
                {
                    Success = false,
                    ErrorMessage = $"경로 찾기 중 오류 발생: {e.Message}"
                };
            }
        }
        
        /// <summary>
        /// NavMesh 폴리곤 개수 가져오기
        /// </summary>
        /// <returns>폴리곤 개수</returns>
        public static int GetPolyCount()
        {
            try
            {
                return UnityRecast_GetPolyCount();
            }
            catch (Exception e)
            {
                Debug.LogError($"폴리곤 개수 가져오기 실패: {e.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// NavMesh 정점 개수 가져오기
        /// </summary>
        /// <returns>정점 개수</returns>
        public static int GetVertexCount()
        {
            try
            {
                return UnityRecast_GetVertexCount();
            }
            catch (Exception e)
            {
                Debug.LogError($"정점 개수 가져오기 실패: {e.Message}");
                return 0;
            }
        }
        
        #endregion
        
        #region Helper Classes
        
        /// <summary>
        /// NavMesh 빌드 결과
        /// </summary>
        public class NavMeshBuildResult
        {
            public bool Success { get; set; }
            public byte[] NavMeshData { get; set; }
            public string ErrorMessage { get; set; }
        }
        
        /// <summary>
        /// 경로 찾기 결과
        /// </summary>
        public class PathFindResult
        {
            public bool Success { get; set; }
            public Vector3[] PathPoints { get; set; }
            public string ErrorMessage { get; set; }
        }
        
        #endregion
    }
    
    /// <summary>
    /// NavMesh 빌드 설정을 위한 확장 메서드들
    /// </summary>
    public static class NavMeshBuildSettingsExtensions
    {
        /// <summary>
        /// 기본 설정으로 NavMesh 빌드 설정 생성
        /// </summary>
        /// <returns>기본 NavMesh 빌드 설정</returns>
        public static RecastNavigationWrapper.NavMeshBuildSettings CreateDefault()
        {
            return new RecastNavigationWrapper.NavMeshBuildSettings
            {
                cellSize = 0.3f,
                cellHeight = 0.2f,
                walkableSlopeAngle = 45.0f,
                walkableHeight = 2.0f,
                walkableRadius = 0.6f,
                walkableClimb = 0.9f,
                minRegionArea = 8.0f,
                mergeRegionArea = 20.0f,
                maxVertsPerPoly = 6,
                detailSampleDist = 6.0f,
                detailSampleMaxError = 1.0f
            };
        }
        
        /// <summary>
        /// 높은 품질 설정으로 NavMesh 빌드 설정 생성
        /// </summary>
        /// <returns>높은 품질 NavMesh 빌드 설정</returns>
        public static RecastNavigationWrapper.NavMeshBuildSettings CreateHighQuality()
        {
            return new RecastNavigationWrapper.NavMeshBuildSettings
            {
                cellSize = 0.1f,
                cellHeight = 0.1f,
                walkableSlopeAngle = 45.0f,
                walkableHeight = 2.0f,
                walkableRadius = 0.6f,
                walkableClimb = 0.9f,
                minRegionArea = 4.0f,
                mergeRegionArea = 10.0f,
                maxVertsPerPoly = 6,
                detailSampleDist = 3.0f,
                detailSampleMaxError = 0.5f
            };
        }
        
        /// <summary>
        /// 낮은 품질 설정으로 NavMesh 빌드 설정 생성 (빠른 빌드)
        /// </summary>
        /// <returns>낮은 품질 NavMesh 빌드 설정</returns>
        public static RecastNavigationWrapper.NavMeshBuildSettings CreateLowQuality()
        {
            return new RecastNavigationWrapper.NavMeshBuildSettings
            {
                cellSize = 0.5f,
                cellHeight = 0.3f,
                walkableSlopeAngle = 45.0f,
                walkableHeight = 2.0f,
                walkableRadius = 0.6f,
                walkableClimb = 0.9f,
                minRegionArea = 16.0f,
                mergeRegionArea = 40.0f,
                maxVertsPerPoly = 6,
                detailSampleDist = 12.0f,
                detailSampleMaxError = 2.0f
            };
        }
    }
} 