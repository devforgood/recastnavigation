using UnityEngine;
using System.Runtime.InteropServices;
using System;

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
    /// Unity Mesh 데이터 구조체
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
    /// NavMesh 빌드 설정 구조체
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
        [MarshalAs(UnmanagedType.Bool)]
        public bool autoTransformCoordinates; // 자동 좌표 변환
    }

    /// <summary>
    /// NavMesh 빌드 결과 구조체
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UnityNavMeshResult
    {
        public IntPtr navMeshData;  // NavMesh 데이터
        public int dataSize;        // 데이터 크기
        [MarshalAs(UnmanagedType.Bool)]
        public bool success;        // 성공 여부
        public IntPtr errorMessage; // 오류 메시지
    }

    /// <summary>
    /// 경로 찾기 결과 구조체
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UnityPathResult
    {
        public IntPtr pathPoints;   // 경로 포인트 배열
        public int pointCount;      // 포인트 개수
        [MarshalAs(UnmanagedType.Bool)]
        public bool success;        // 성공 여부
        public IntPtr errorMessage; // 오류 메시지
    }

    /// <summary>
    /// RecastNavigation DLL을 Unity에서 사용할 수 있도록 래핑한 클래스
    /// </summary>
    public static class RecastNavigationWrapper
    {
        // DLL 이름 (플랫폼별로 자동 선택됨)
        private const string DLL_NAME = "UnityRecastWrapper";

        #region DLL Import

        // 초기화 및 정리
        [DllImport(DLL_NAME)]
        private static extern bool InitializeRecastNavigation();

        [DllImport(DLL_NAME)]
        private static extern void CleanupRecastNavigation();

        // NavMesh 빌드
        [DllImport(DLL_NAME)]
        private static extern bool BuildNavMeshFromMesh(
            [In] Vector3[] vertices, int vertexCount,
            [In] int[] indices, int indexCount,
            [In] ref NavMeshBuildSettings settings,
            [Out] out IntPtr navMeshData, [Out] out int dataSize,
            [Out] out IntPtr errorMessage);

        // NavMesh 로드
        [DllImport(DLL_NAME)]
        private static extern bool LoadNavMeshFromData(
            [In] byte[] data, int dataSize);

        // 경로 찾기
        [DllImport(DLL_NAME)]
        private static extern bool FindPathBetweenPoints(
            [In] Vector3 start, [In] Vector3 end,
            [Out] out IntPtr pathPoints, [Out] out int pointCount,
            [Out] out IntPtr errorMessage);

        // NavMesh 정보
        [DllImport(DLL_NAME)]
        private static extern int GetNavMeshPolyCount();

        [DllImport(DLL_NAME)]
        private static extern int GetNavMeshVertexCount();

        // 메모리 해제
        [DllImport(DLL_NAME)]
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

        #region 좌표계 변환

        /// <summary>
        /// 좌표계 설정
        /// </summary>
        /// <param name="system">좌표계 타입</param>
        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_SetCoordinateSystem(CoordinateSystem system);

        /// <summary>
        /// 현재 좌표계 가져오기
        /// </summary>
        /// <returns>현재 좌표계 타입</returns>
        [DllImport(DLL_NAME)]
        public static extern CoordinateSystem UnityRecast_GetCoordinateSystem();

        /// <summary>
        /// Y축 회전 설정
        /// </summary>
        /// <param name="rotation">Y축 회전 타입</param>
        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_SetYAxisRotation(YAxisRotation rotation);

        /// <summary>
        /// 현재 Y축 회전 가져오기
        /// </summary>
        /// <returns>현재 Y축 회전 타입</returns>
        [DllImport(DLL_NAME)]
        public static extern YAxisRotation UnityRecast_GetYAxisRotation();

        /// <summary>
        /// 정점 좌표 변환
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <param name="z">Z 좌표</param>
        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_TransformVertex(ref float x, ref float y, ref float z);

        /// <summary>
        /// 경로 포인트 좌표 변환
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <param name="z">Z 좌표</param>
        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_TransformPathPoint(ref float x, ref float y, ref float z);

        /// <summary>
        /// 경로 포인트 배열 좌표 변환
        /// </summary>
        /// <param name="points">포인트 배열</param>
        /// <param name="pointCount">포인트 개수</param>
        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_TransformPathPoints([In, Out] float[] points, int pointCount);

        #endregion

        #region NavMesh 빌드

        /// <summary>
        /// NavMesh 빌드
        /// </summary>
        /// <param name="meshData">메시 데이터</param>
        /// <param name="settings">빌드 설정</param>
        /// <returns>빌드 결과</returns>
        [DllImport(DLL_NAME)]
        public static extern UnityNavMeshResult UnityRecast_BuildNavMesh(
            ref UnityMeshData meshData,
            ref UnityNavMeshBuildSettings settings
        );

        /// <summary>
        /// NavMesh 데이터 해제
        /// </summary>
        /// <param name="result">NavMesh 결과</param>
        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_FreeNavMeshData(ref UnityNavMeshResult result);

        /// <summary>
        /// NavMesh 로드
        /// </summary>
        /// <param name="data">NavMesh 데이터</param>
        /// <param name="dataSize">데이터 크기</param>
        /// <returns>로드 성공 여부</returns>
        [DllImport(DLL_NAME)]
        public static extern bool UnityRecast_LoadNavMesh(byte[] data, int dataSize);

        #endregion

        #region 경로 찾기

        /// <summary>
        /// 경로 찾기
        /// </summary>
        /// <param name="startX">시작점 X</param>
        /// <param name="startY">시작점 Y</param>
        /// <param name="startZ">시작점 Z</param>
        /// <param name="endX">끝점 X</param>
        /// <param name="endY">끝점 Y</param>
        /// <param name="endZ">끝점 Z</param>
        /// <returns>경로 결과</returns>
        [DllImport(DLL_NAME)]
        public static extern UnityPathResult UnityRecast_FindPath(
            float startX, float startY, float startZ,
            float endX, float endY, float endZ
        );

        /// <summary>
        /// 경로 결과 해제
        /// </summary>
        /// <param name="result">경로 결과</param>
        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_FreePathResult(ref UnityPathResult result);

        #endregion

        #region 정보 조회

        /// <summary>
        /// 폴리곤 개수 가져오기
        /// </summary>
        /// <returns>폴리곤 개수</returns>
        [DllImport(DLL_NAME)]
        public static extern int UnityRecast_GetPolyCount();

        /// <summary>
        /// 정점 개수 가져오기
        /// </summary>
        /// <returns>정점 개수</returns>
        [DllImport(DLL_NAME)]
        public static extern int UnityRecast_GetVertexCount();

        #endregion

        #region 디버그 기능

        /// <summary>
        /// 디버그 드로잉 설정
        /// </summary>
        /// <param name="enabled">활성화 여부</param>
        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_SetDebugDraw(bool enabled);

        /// <summary>
        /// 디버그 정점 정보 가져오기
        /// </summary>
        /// <param name="vertices">정점 배열</param>
        /// <param name="vertexCount">정점 개수</param>
        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_GetDebugVertices([Out] float[] vertices, ref int vertexCount);

        /// <summary>
        /// 디버그 인덱스 정보 가져오기
        /// </summary>
        /// <param name="indices">인덱스 배열</param>
        /// <param name="indexCount">인덱스 개수</param>
        [DllImport(DLL_NAME)]
        public static extern void UnityRecast_GetDebugIndices([Out] int[] indices, ref int indexCount);

        #endregion

        #region 유틸리티 메서드

        /// <summary>
        /// Vector3 좌표 변환
        /// </summary>
        /// <param name="position">Unity Vector3</param>
        /// <returns>변환된 Vector3</returns>
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
        /// <param name="positions">Unity Vector3 배열</param>
        /// <returns>변환된 Vector3 배열</returns>
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
        /// <param name="position">원본 위치</param>
        /// <param name="rotation">회전 타입</param>
        /// <returns>회전된 위치</returns>
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
        /// <param name="position">회전된 위치</param>
        /// <param name="rotation">회전 타입</param>
        /// <returns>원본 위치</returns>
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
        /// <param name="errorPtr">오류 메시지 포인터</param>
        /// <returns>오류 메시지 문자열</returns>
        public static string GetErrorMessage(IntPtr errorPtr)
        {
            if (errorPtr == IntPtr.Zero)
                return string.Empty;

            return Marshal.PtrToStringAnsi(errorPtr);
        }

        /// <summary>
        /// NavMesh 결과를 바이트 배열로 변환
        /// </summary>
        /// <param name="result">NavMesh 결과</param>
        /// <returns>바이트 배열</returns>
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
        /// <param name="result">경로 결과</param>
        /// <returns>Vector3 배열</returns>
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
                detailSampleMaxError = 1f,
                autoTransformCoordinates = true
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
                detailSampleMaxError = 0.5f,
                autoTransformCoordinates = true
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
                detailSampleMaxError = 2f,
                autoTransformCoordinates = true
            };
        }
    }
} 