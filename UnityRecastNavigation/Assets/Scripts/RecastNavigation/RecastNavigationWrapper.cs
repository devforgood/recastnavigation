using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RecastNavigation
{
    /// <summary>
    /// 좌표계 타입
    /// </summary>
    public enum CoordinateSystem
    {
        LeftHanded = 0,    // Unity 기본 (왼손 좌표계)
        RightHanded = 1    // RecastNavigation 기본 (오른손 좌표계)
    }

    /// <summary>
    /// Y축 회전 타입
    /// </summary>
    public enum YAxisRotation
    {
        None = 0,      // 회전 없음
        Rotate90 = 1,  // Y축 기준 90도 회전
        Rotate180 = 2, // Y축 기준 180도 회전
        Rotate270 = 3  // Y축 기준 270도 회전
    }

    /// <summary>
    /// Unity 메시 데이터 구조체
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UnityMeshData
    {
        public IntPtr vertices;      // 3D 정점 배열 (x, y, z)
        public IntPtr indices;       // 삼각형 인덱스 배열
        public int vertexCount;      // 정점 개수
        public int indexCount;       // 인덱스 개수
        [MarshalAs(UnmanagedType.Bool)]
        public bool transformCoordinates; // 좌표 변환 여부
    }

    /// <summary>
    /// Unity NavMesh 빌드 설정 구조체
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UnityNavMeshBuildSettings
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
        public float maxSimplificationError; // 최대 단순화 오차
        public float maxEdgeLen;         // 최대 엣지 길이
        [MarshalAs(UnmanagedType.Bool)]
        public bool autoTransformCoordinates; // 자동 좌표 변환
    }

    /// <summary>
    /// Unity NavMesh 결과 구조체
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UnityNavMeshResult
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool success;         // 성공 여부
        public IntPtr navMeshData;  // NavMesh 데이터
        public int dataSize;        // 데이터 크기
        public IntPtr errorMessage; // 오류 메시지
    }

    /// <summary>
    /// Unity 경로 결과 구조체
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UnityPathResult
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool success;         // 성공 여부
        public IntPtr pathPoints;   // 경로 포인트 배열
        public int pointCount;      // 포인트 개수
        public IntPtr errorMessage; // 오류 메시지
    }

    /// <summary>
    /// RecastNavigation DLL 래퍼 클래스
    /// </summary>
    public static class RecastNavigationWrapper
    {
        private const string DLL_NAME = "UnityWrapper";
        
        // DLL 사용 가능 여부 캐싱
        private static bool? _isDLLAvailable = null;

        #region DLL Import 함수들

        // 초기화 및 정리
        [DllImport(DLL_NAME)]
        public static extern bool UnityRecast_Initialize();

        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_Cleanup();

        // 주석처리: C++ 코드에 구현되지 않음
        // [DllImport(DLL_NAME)]
        // public static extern bool UnityRecast_IsInitialized();

        // 좌표계 설정
        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_SetCoordinateSystem(CoordinateSystem system);

        [DllImport(DLL_NAME)]
        public static extern CoordinateSystem UnityRecast_GetCoordinateSystem();

        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_SetYAxisRotation(YAxisRotation rotation);

        [DllImport(DLL_NAME)]
        public static extern YAxisRotation UnityRecast_GetYAxisRotation();

        // 좌표 변환
        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_TransformVertex(ref float x, ref float y, ref float z);

        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_TransformPathPoint(ref float x, ref float y, ref float z);

        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_TransformPathPoints([In, Out] float[] points, int pointCount);

        // NavMesh 빌드
        [DllImport(DLL_NAME)]
        public static extern UnityNavMeshResult UnityRecast_BuildNavMesh(
            ref UnityMeshData meshData,
            ref UnityNavMeshBuildSettings settings
        );

        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_FreeNavMeshData(ref UnityNavMeshResult result);

        [DllImport(DLL_NAME)]
        public static extern bool UnityRecast_LoadNavMesh(byte[] data, int dataSize);

        // 경로 찾기
        [DllImport(DLL_NAME)]
        public static extern UnityPathResult UnityRecast_FindPath(
            float startX, float startY, float startZ,
            float endX, float endY, float endZ
        );

        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_FreePathResult(ref UnityPathResult result);

        // 정보 조회
        [DllImport(DLL_NAME)]
        public static extern int UnityRecast_GetPolyCount();

        [DllImport(DLL_NAME)]
        public static extern int UnityRecast_GetVertexCount();

        // 디버그 기능
        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_SetDebugDraw(bool enabled);

        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_GetDebugVertices([Out] float[] vertices, ref int vertexCount);

        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_GetDebugIndices([Out] int[] indices, ref int indexCount);

        #endregion

        #region 고수준 API

        /// <summary>
        /// 초기화
        /// </summary>
        public static bool Initialize()
        {
            try
            {
                // DLL 로딩 가능 여부 확인
                if (!IsDLLAvailable())
                {
                    Debug.LogError($"RecastNavigation DLL을 찾을 수 없습니다. '{DLL_NAME}'가 Assets/Plugins 폴더에 있는지 확인하세요.");
                    return false;
                }
                
                return UnityRecast_Initialize();
            }
            catch (Exception e)
            {
                Debug.LogError($"RecastNavigation 초기화 실패: {e.Message}");
                Debug.LogError($"DLL 경로: {DLL_NAME}");
                Debug.LogError("가능한 해결 방법:");
                Debug.LogError("1. DLL 파일이 Assets/Plugins 폴더에 있는지 확인");
                Debug.LogError("2. DLL이 현재 플랫폼에 맞게 설정되어 있는지 확인");
                Debug.LogError("3. Visual C++ Redistributable이 설치되어 있는지 확인");
                return false;
            }
        }
        
        /// <summary>
        /// DLL 사용 가능 여부 확인
        /// </summary>
        private static bool IsDLLAvailable()
        {
            if (_isDLLAvailable.HasValue)
                return _isDLLAvailable.Value;
                
            try
            {
                // 간단한 함수 호출로 DLL 로딩 테스트 (확실히 존재하는 함수 사용)
                UnityRecast_GetCoordinateSystem();
                _isDLLAvailable = true;
                return true;
            }
            catch (System.DllNotFoundException)
            {
                Debug.LogError($"DLL '{DLL_NAME}'을 찾을 수 없습니다. Assets/Plugins 폴더를 확인하세요.");
                _isDLLAvailable = false;
                return false;
            }
            catch (System.BadImageFormatException)
            {
                Debug.LogError($"DLL '{DLL_NAME}'이 현재 플랫폼과 호환되지 않습니다. x64 빌드용 DLL인지 확인하세요.");
                _isDLLAvailable = false;
                return false;
            }
            catch (System.EntryPointNotFoundException e)
            {
                Debug.LogError($"DLL '{DLL_NAME}'에서 함수를 찾을 수 없습니다: {e.Message}");
                _isDLLAvailable = false;
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"DLL '{DLL_NAME}' 로딩 실패: {e.Message}");
                _isDLLAvailable = false;
                return false;
            }
        }

        /// <summary>
        /// 정리
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

                return BuildNavMesh(vertices, indices, settings);
            }
            catch (Exception e)
            {
                return new NavMeshBuildResult { Success = false, ErrorMessage = e.Message };
            }
        }

        /// <summary>
        /// 정점과 인덱스에서 NavMesh 빌드
        /// </summary>
        public static NavMeshBuildResult BuildNavMesh(Vector3[] vertices, int[] indices, NavMeshBuildSettings settings)
        {
            if (vertices == null || indices == null)
            {
                return new NavMeshBuildResult { Success = false, ErrorMessage = "정점 또는 인덱스가 null입니다." };
            }

            try
            {
                // 좌표 변환 적용
                if (settings.autoTransformCoordinates)
                {
                    vertices = TransformPositions(vertices);
                }

                UnityMeshData meshData = new UnityMeshData
                {
                    vertexCount = vertices.Length,
                    indexCount = indices.Length,
                    transformCoordinates = false // 이미 변환됨
                };

                // 정점 데이터 마샬링
                float[] vertexArray = new float[vertices.Length * 3];
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertexArray[i * 3] = vertices[i].x;
                    vertexArray[i * 3 + 1] = vertices[i].y;
                    vertexArray[i * 3 + 2] = vertices[i].z;
                }

                IntPtr vertexPtr = Marshal.AllocHGlobal(vertexArray.Length * sizeof(float));
                Marshal.Copy(vertexArray, 0, vertexPtr, vertexArray.Length);

                IntPtr indexPtr = Marshal.AllocHGlobal(indices.Length * sizeof(int));
                Marshal.Copy(indices, 0, indexPtr, indices.Length);

                meshData.vertices = vertexPtr;
                meshData.indices = indexPtr;

                // 빌드 설정 변환
                UnityNavMeshBuildSettings buildSettings = new UnityNavMeshBuildSettings
                {
                    cellSize = settings.cellSize,
                    cellHeight = settings.cellHeight,
                    walkableSlopeAngle = settings.walkableSlopeAngle,
                    walkableHeight = settings.walkableHeight,
                    walkableRadius = settings.walkableRadius,
                    walkableClimb = settings.walkableClimb,
                    minRegionArea = settings.minRegionArea,
                    mergeRegionArea = settings.mergeRegionArea,
                    maxVertsPerPoly = settings.maxVertsPerPoly,
                    detailSampleDist = settings.detailSampleDist,
                    detailSampleMaxError = settings.detailSampleMaxError,
                    maxSimplificationError = 1.3f, // 기본값
                    maxEdgeLen = 12.0f, // 기본값
                    autoTransformCoordinates = false
                };

                UnityNavMeshResult result = UnityRecast_BuildNavMesh(ref meshData, ref buildSettings);

                // 메모리 해제
                Marshal.FreeHGlobal(vertexPtr);
                Marshal.FreeHGlobal(indexPtr);

                // 빌드 결과 상세 분석
                Debug.Log($"=== DLL BuildNavMesh 결과 분석 ===");
                Debug.Log($"result.success: {result.success}");
                Debug.Log($"result.navMeshData: {result.navMeshData}");
                Debug.Log($"result.dataSize: {result.dataSize}");
                Debug.Log($"result.errorMessage: {result.errorMessage}");

                if (result.success)
                {
                    // NavMeshData 추출 전 상태 확인
                    bool hasValidData = result.navMeshData != IntPtr.Zero && result.dataSize > 0;
                    Debug.Log($"유효한 NavMesh 데이터 존재: {hasValidData}");
                    
                    if (!hasValidData)
                    {
                        Debug.LogError("빌드는 성공했지만 NavMesh 데이터가 유효하지 않습니다!");
                        Debug.LogError($"navMeshData: {result.navMeshData}, dataSize: {result.dataSize}");
                        UnityRecast_FreeNavMeshData(ref result);
                        return new NavMeshBuildResult 
                        { 
                            Success = false, 
                            ErrorMessage = "빌드는 성공했지만 NavMesh 데이터가 유효하지 않음" 
                        };
                    }

                    byte[] navMeshData = GetNavMeshData(result);
                    Debug.Log($"GetNavMeshData 결과: {(navMeshData != null ? $"{navMeshData.Length} 바이트" : "null")}");
                    
                    UnityRecast_FreeNavMeshData(ref result);

                    if (navMeshData == null || navMeshData.Length == 0)
                    {
                        Debug.LogError("GetNavMeshData가 null 또는 빈 배열을 반환했습니다!");
                        return new NavMeshBuildResult 
                        { 
                            Success = false, 
                            ErrorMessage = "NavMesh 데이터 추출 실패" 
                        };
                    }

                    return new NavMeshBuildResult
                    {
                        Success = true,
                        NavMeshData = navMeshData
                    };
                }
                else
                {
                    string error = GetErrorMessage(result.errorMessage);
                    Debug.LogError($"DLL BuildNavMesh 실패: {error}");
                    UnityRecast_FreeNavMeshData(ref result);
                    return new NavMeshBuildResult { Success = false, ErrorMessage = error };
                }
            }
            catch (Exception e)
            {
                return new NavMeshBuildResult { Success = false, ErrorMessage = e.Message };
            }
        }

        /// <summary>
        /// NavMesh 로드
        /// </summary>
        public static bool LoadNavMesh(byte[] navMeshData)
        {
            if (navMeshData == null || navMeshData.Length == 0)
            {
                return false;
            }

            try
            {
                return UnityRecast_LoadNavMesh(navMeshData, navMeshData.Length);
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
                UnityPathResult result = UnityRecast_FindPath(
                    start.x, start.y, start.z,
                    end.x, end.y, end.z
                );

                if (result.success)
                {
                    Vector3[] pathPoints = GetPathPoints(result);
                    UnityRecast_FreePathResult(ref result);

                    return new PathfindingResult
                    {
                        Success = true,
                        PathPoints = pathPoints ?? new Vector3[0] // null 대신 빈 배열 반환
                    };
                }
                else
                {
                    string error = GetErrorMessage(result.errorMessage);
                    UnityRecast_FreePathResult(ref result);
                    return new PathfindingResult { Success = false, ErrorMessage = error };
                }
            }
            catch (Exception e)
            {
                return new PathfindingResult { Success = false, ErrorMessage = e.Message };
            }
        }

        /// <summary>
        /// 폴리곤 개수 가져오기
        /// </summary>
        public static int GetPolyCount()
        {
            try
            {
                return UnityRecast_GetPolyCount();
            }
            catch (Exception e)
            {
                Debug.LogError($"폴리곤 개수 조회 실패: {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 정점 개수 가져오기
        /// </summary>
        public static int GetVertexCount()
        {
            try
            {
                return UnityRecast_GetVertexCount();
            }
            catch (Exception e)
            {
                Debug.LogError($"정점 개수 조회 실패: {e.Message}");
                return 0;
            }
        }

        #endregion

        #region 유틸리티 메서드

        /// <summary>
        /// Vector3 좌표 변환
        /// </summary>
        public static Vector3 TransformPosition(Vector3 position)
        {
            float x = position.x;
            float y = position.y;
            float z = position.z;
            UnityRecast_TransformVertex(ref x, ref y, ref z);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Vector3 배열 좌표 변환
        /// </summary>
        public static Vector3[] TransformPositions(Vector3[] positions)
        {
            if (positions == null || positions.Length == 0)
                return positions;

            float[] points = new float[positions.Length * 3];
            for (int i = 0; i < positions.Length; i++)
            {
                points[i * 3] = positions[i].x;
                points[i * 3 + 1] = positions[i].y;
                points[i * 3 + 2] = positions[i].z;
            }

            UnityRecast_TransformPathPoints(points, positions.Length);

            Vector3[] result = new Vector3[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                result[i] = new Vector3(points[i * 3], points[i * 3 + 1], points[i * 3 + 2]);
            }

            return result;
        }

        /// <summary>
        /// Y축 회전 적용
        /// </summary>
        public static Vector3 ApplyYAxisRotation(Vector3 position, YAxisRotation rotation)
        {
            switch (rotation)
            {
                case YAxisRotation.Rotate90:
                    return new Vector3(-position.z, position.y, position.x);
                case YAxisRotation.Rotate180:
                    return new Vector3(-position.x, position.y, -position.z);
                case YAxisRotation.Rotate270:
                    return new Vector3(position.z, position.y, -position.x);
                case YAxisRotation.None:
                default:
                    return position;
            }
        }

        /// <summary>
        /// Y축 회전 역변환 적용
        /// </summary>
        public static Vector3 ApplyYAxisRotationInverse(Vector3 position, YAxisRotation rotation)
        {
            switch (rotation)
            {
                case YAxisRotation.Rotate90:
                    return new Vector3(position.z, position.y, -position.x);
                case YAxisRotation.Rotate180:
                    return new Vector3(-position.x, position.y, -position.z);
                case YAxisRotation.Rotate270:
                    return new Vector3(-position.z, position.y, position.x);
                case YAxisRotation.None:
                default:
                    return position;
            }
        }

        /// <summary>
        /// 오류 메시지 문자열로 변환
        /// </summary>
        public static string GetErrorMessage(IntPtr errorPtr)
        {
            if (errorPtr == IntPtr.Zero)
                return string.Empty;

            return Marshal.PtrToStringAnsi(errorPtr);
        }

        /// <summary>
        /// NavMesh 결과를 바이트 배열로 변환
        /// </summary>
        public static byte[] GetNavMeshData(UnityNavMeshResult result)
        {
            if (!result.success || result.navMeshData == IntPtr.Zero || result.dataSize <= 0)
                return null;

            byte[] data = new byte[result.dataSize];
            Marshal.Copy(result.navMeshData, data, 0, result.dataSize);
            return data;
        }

        /// <summary>
        /// 경로 결과를 Vector3 배열로 변환
        /// </summary>
        public static Vector3[] GetPathPoints(UnityPathResult result)
        {
            if (!result.success || result.pathPoints == IntPtr.Zero || result.pointCount <= 0)
                return null;

            float[] points = new float[result.pointCount * 3];
            Marshal.Copy(result.pathPoints, points, 0, result.pointCount * 3);

            Vector3[] pathPoints = new Vector3[result.pointCount];
            for (int i = 0; i < result.pointCount; i++)
            {
                pathPoints[i] = new Vector3(points[i * 3], points[i * 3 + 1], points[i * 3 + 2]);
            }

            return pathPoints;
        }

        #endregion

        #region 디버그 및 시각화

        /// <summary>
        /// 디버그 드로잉 설정
        /// </summary>
        public static void SetDebugDraw(bool enabled)
        {
            try
            {
                UnityRecast_SetDebugDraw(enabled);
            }
            catch (Exception e)
            {
                Debug.LogError($"디버그 드로잉 설정 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 디버그 정점 데이터 가져오기
        /// </summary>
        public static Vector3[] GetDebugVertices()
        {
            try
            {
                int vertexCount = 0;
                UnityRecast_GetDebugVertices(null, ref vertexCount);
                
                if (vertexCount <= 0)
                    return new Vector3[0];

                float[] vertices = new float[vertexCount * 3];
                UnityRecast_GetDebugVertices(vertices, ref vertexCount);

                Vector3[] result = new Vector3[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    result[i] = new Vector3(vertices[i * 3], vertices[i * 3 + 1], vertices[i * 3 + 2]);
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"디버그 정점 데이터 가져오기 실패: {e.Message}");
                return new Vector3[0];
            }
        }

        /// <summary>
        /// 디버그 인덱스 데이터 가져오기
        /// </summary>
        public static int[] GetDebugIndices()
        {
            try
            {
                int indexCount = 0;
                UnityRecast_GetDebugIndices(null, ref indexCount);
                
                if (indexCount <= 0)
                    return new int[0];

                int[] indices = new int[indexCount];
                UnityRecast_GetDebugIndices(indices, ref indexCount);

                return indices;
            }
            catch (Exception e)
            {
                Debug.LogError($"디버그 인덱스 데이터 가져오기 실패: {e.Message}");
                return new int[0];
            }
        }

        /// <summary>
        /// 디버그 메시 데이터 가져오기
        /// </summary>
        public static NavMeshDebugData GetDebugMeshData()
        {
            Vector3[] vertices = GetDebugVertices();
            int[] indices = GetDebugIndices();

            return new NavMeshDebugData
            {
                Vertices = vertices,
                Indices = indices,
                TriangleCount = indices.Length / 3
            };
        }

        #endregion

        #region 좌표계 설정

        /// <summary>
        /// 자동 좌표 변환 설정
        /// </summary>
        public static void SetAutoTransformCoordinates(bool enabled)
        {
            try
            {
                Debug.Log($"자동 좌표 변환 설정: {enabled}");
            }
            catch (Exception e)
            {
                Debug.LogError($"자동 좌표 변환 설정 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 자동 좌표 변환 상태 가져오기
        /// </summary>
        public static bool GetAutoTransformCoordinates()
        {
            try
            {
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"자동 좌표 변환 상태 조회 실패: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 디버그 로깅 활성화/비활성화
        /// </summary>
        public static void EnableDebugLogging(bool enabled)
        {
            try
            {
                Debug.Log($"디버그 로깅 설정: {enabled}");
            }
            catch (Exception e)
            {
                Debug.LogError($"디버그 로깅 설정 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 현재 좌표계 가져오기
        /// </summary>
        public static CoordinateSystem GetCoordinateSystem()
        {
            try
            {
                return UnityRecast_GetCoordinateSystem();
            }
            catch (Exception e)
            {
                Debug.LogError($"좌표계 조회 실패: {e.Message}");
                return CoordinateSystem.LeftHanded;
            }
        }

        /// <summary>
        /// 현재 Y축 회전 가져오기
        /// </summary>
        public static YAxisRotation GetYAxisRotation()
        {
            try
            {
                return UnityRecast_GetYAxisRotation();
            }
            catch (Exception e)
            {
                Debug.LogError($"Y축 회전 조회 실패: {e.Message}");
                return YAxisRotation.None;
            }
        }

        #endregion
    }

    /// <summary>
    /// Unity NavMesh 빌드 설정
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
        public float maxSimplificationError;
        public float maxEdgeLen;
        public bool autoTransformCoordinates;
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
    /// NavMesh 디버그 데이터
    /// </summary>
    public struct NavMeshDebugData
    {
        public Vector3[] Vertices;
        public int[] Indices;
        public int TriangleCount;
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
                walkableSlopeAngle = 45.0f,
                walkableHeight = 2.0f,
                walkableRadius = 0.6f,
                walkableClimb = 0.9f,
                minRegionArea = 8.0f,
                mergeRegionArea = 20.0f,
                maxEdgeLen = 12.0f,
                maxSimplificationError = 1.3f,
                maxVertsPerPoly = 6,
                detailSampleDist = 6.0f,
                detailSampleMaxError = 1.0f,
                autoTransformCoordinates = true
            };
        }

        /// <summary>
        /// 고품질 설정 생성
        /// </summary>
        public static NavMeshBuildSettings CreateHighQuality()
        {
            return new NavMeshBuildSettings
            {
                cellSize = 0.1f,
                cellHeight = 0.1f,
                walkableSlopeAngle = 45.0f,
                walkableHeight = 2.0f,
                walkableRadius = 0.6f,
                walkableClimb = 0.9f,
                minRegionArea = 4.0f,
                mergeRegionArea = 10.0f,
                maxEdgeLen = 8.0f,
                maxSimplificationError = 0.8f,
                maxVertsPerPoly = 6,
                detailSampleDist = 3.0f,
                detailSampleMaxError = 0.5f,
                autoTransformCoordinates = true
            };
        }

        /// <summary>
        /// 저품질 설정 생성 (빠른 빌드)
        /// </summary>
        public static NavMeshBuildSettings CreateLowQuality()
        {
            return new NavMeshBuildSettings
            {
                cellSize = 0.5f,
                cellHeight = 0.3f,
                walkableSlopeAngle = 45.0f,
                walkableHeight = 2.0f,
                walkableRadius = 0.6f,
                walkableClimb = 0.9f,
                minRegionArea = 16.0f,
                mergeRegionArea = 40.0f,
                maxEdgeLen = 16.0f,
                maxSimplificationError = 2.0f,
                maxVertsPerPoly = 6,
                detailSampleDist = 12.0f,
                detailSampleMaxError = 2.0f,
                autoTransformCoordinates = true
            };
        }

        /// <summary>
        /// RecastDemo 검증된 설정 생성 (권장)
        /// RecastDemo에서 실제로 사용되는 검증된 매개변수들
        /// </summary>
        public static NavMeshBuildSettings CreateRecastDemoVerified()
        {
            return new NavMeshBuildSettings
            {
                cellSize = 0.3f,          // RecastDemo 기본값
                cellHeight = 0.2f,        // RecastDemo 기본값
                walkableSlopeAngle = 45.0f, // RecastDemo 기본값
                walkableHeight = 2.0f,    // RecastDemo 기본값 (agentHeight)
                walkableRadius = 0.6f,    // RecastDemo 기본값 (agentRadius)
                walkableClimb = 0.9f,     // RecastDemo 기본값 (agentMaxClimb)
                minRegionArea = 64.0f,    // RecastDemo: rcSqr(8) = 64
                mergeRegionArea = 400.0f, // RecastDemo: rcSqr(20) = 400
                maxEdgeLen = 12.0f,       // RecastDemo 기본값
                maxSimplificationError = 1.3f, // RecastDemo 기본값
                maxVertsPerPoly = 6,      // RecastDemo 기본값
                detailSampleDist = 6.0f,  // RecastDemo 기본값
                detailSampleMaxError = 1.0f, // RecastDemo 기본값
                autoTransformCoordinates = true
            };
        }

        /// <summary>
        /// RecastDemo 보수적 설정 - 작은 메시나 복잡한 메시에 적합
        /// </summary>
        public static NavMeshBuildSettings CreateRecastDemoConservative()
        {
            return new NavMeshBuildSettings
            {
                // 매우 정밀한 cellSize로 높은 해상도 보장
                cellSize = 0.05f,          // 기존 0.1f → 0.05f (더 정밀)
                cellHeight = 0.1f,         // 기존 0.2f → 0.1f (더 정밀)
                
                // Agent 설정 (RecastDemo 기본값)
                walkableHeight = 2.0f,
                walkableRadius = 0.02f,    // 기존 0.3f → 0.02f (매우 보수적, erosion 최소화)
                walkableClimb = 0.9f,
                walkableSlopeAngle = 45.0f,
                
                // Region 설정 (매우 관대한 설정)
                minRegionArea = 1.0f,      // 기존 4 → 1 (거의 모든 region 허용)
                mergeRegionArea = 5.0f,    // 기존 20 → 5 (작은 region도 유지)
                
                // 폴리곤 설정 (높은 품질)
                maxEdgeLen = 6.0f,         // 기존 12.0f → 6.0f (더 정밀한 경계)
                maxSimplificationError = 0.5f, // 기존 1.3f → 0.5f (더 정확한 형태)
                maxVertsPerPoly = 6,
                
                // Detail mesh 설정 (높은 품질)
                detailSampleDist = 3.0f,   // 기존 6.0f → 3.0f (더 정밀)
                detailSampleMaxError = 0.5f, // 기존 1.0f → 0.5f (더 정확)
                
                // 좌표 변환 설정
                autoTransformCoordinates = false
            };
        }
    }
} 