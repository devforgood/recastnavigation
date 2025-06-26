using UnityEngine;
using System.Collections.Generic;

namespace RecastNavigation
{
    /// <summary>
    /// RecastNavigation을 Unity에서 사용하기 위한 컴포넌트
    /// </summary>
    public class RecastNavigationComponent : MonoBehaviour
    {
        [Header("NavMesh 설정")]
        [SerializeField] private NavMeshBuildSettings buildSettings = NavMeshBuildSettings.CreateDefault();
        [SerializeField] private bool autoTransformCoordinates = true;
        [SerializeField] private CoordinateSystem coordinateSystem = CoordinateSystem.LeftHanded;
        [SerializeField] private YAxisRotation yAxisRotation = YAxisRotation.None;

        [Header("경로 찾기 설정")]
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField] private bool autoFindPath = false;
        [SerializeField] private float pathUpdateInterval = 0.1f;

        [Header("디버그")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool drawNavMesh = true;
        [SerializeField] private bool drawPath = true;
        [SerializeField] private Color navMeshColor = Color.green;
        [SerializeField] private Color pathColor = Color.red;

        private byte[] navMeshData;
        private bool isInitialized = false;
        private List<Vector3> currentPath = new List<Vector3>();

        // 이벤트
        public System.Action<Vector3[]> OnPathFound;
        public System.Action<string> OnError;

        // 프로퍼티
        public bool IsInitialized => isInitialized;
        public Vector3[] CurrentPath => currentPath.ToArray();
        public int PolyCount => isInitialized ? RecastNavigationWrapper.UnityRecast_GetPolyCount() : 0;
        public int VertexCount => isInitialized ? RecastNavigationWrapper.UnityRecast_GetVertexCount() : 0;

        #region Unity 이벤트

        private void Awake()
        {
            InitializeRecastNavigation();
        }

        private void Update()
        {
            if (!isInitialized) return;

            // 자동 경로 찾기
            if (autoFindPath && startPoint != null && endPoint != null)
            {
                if (Time.time % pathUpdateInterval < Time.deltaTime)
                {
                    FindPath(startPoint.position, endPoint.position);
                }
            }
        }

        private void OnDestroy()
        {
            CleanupRecastNavigation();
        }

        private void OnDrawGizmos()
        {
            if (!showDebugInfo) return;

            // NavMesh 그리기
            if (drawNavMesh && navMeshData != null)
            {
                Gizmos.color = navMeshColor;
                // TODO: NavMesh 폴리곤 그리기 구현
            }

            // 경로 그리기
            if (drawPath && currentPath.Count > 0)
            {
                Gizmos.color = pathColor;
                for (int i = 1; i < currentPath.Count; i++)
                {
                    Gizmos.DrawLine(currentPath[i - 1], currentPath[i]);
                }

                // 경로 포인트 표시
                Gizmos.color = Color.yellow;
                for (int i = 0; i < currentPath.Count; i++)
                {
                    Gizmos.DrawWireSphere(currentPath[i], 0.1f);
                }
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            // 화면에 디버그 정보 표시
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label("RecastNavigation Debug Info");
            GUILayout.Label($"초기화됨: {isInitialized}");
            GUILayout.Label($"좌표계: {coordinateSystem}");
            GUILayout.Label($"Y축 회전: {yAxisRotation}");
            GUILayout.Label($"자동 좌표 변환: {autoTransformCoordinates}");

            if (isInitialized)
            {
                GUILayout.Label($"폴리곤 수: {PolyCount}");
                GUILayout.Label($"정점 수: {VertexCount}");
            }

            if (currentPath.Count > 0)
            {
                GUILayout.Label($"경로 포인트 수: {currentPath.Count}");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion

        #region 초기화 및 정리

        /// <summary>
        /// RecastNavigation 초기화
        /// </summary>
        private void InitializeRecastNavigation()
        {
            if (isInitialized) return;

            try
            {
                // 좌표계 설정
                RecastNavigationWrapper.UnityRecast_SetCoordinateSystem(coordinateSystem);
                
                // Y축 회전 설정
                RecastNavigationWrapper.UnityRecast_SetYAxisRotation(yAxisRotation);

                // RecastNavigation 초기화
                bool success = RecastNavigationWrapper.UnityRecast_Initialize();
                if (success)
                {
                    isInitialized = true;
                    Debug.Log("RecastNavigation 초기화 성공");
                }
                else
                {
                    Debug.LogError("RecastNavigation 초기화 실패");
                    OnError?.Invoke("RecastNavigation 초기화 실패");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RecastNavigation 초기화 중 오류: {e.Message}");
                OnError?.Invoke($"RecastNavigation 초기화 중 오류: {e.Message}");
            }
        }

        /// <summary>
        /// RecastNavigation 정리
        /// </summary>
        private void CleanupRecastNavigation()
        {
            if (!isInitialized) return;

            try
            {
                RecastNavigationWrapper.UnityRecast_Cleanup();
                isInitialized = false;
                navMeshData = null;
                currentPath.Clear();
                Debug.Log("RecastNavigation 정리 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RecastNavigation 정리 중 오류: {e.Message}");
            }
        }

        #endregion

        #region NavMesh 빌드

        /// <summary>
        /// 현재 씬의 모든 Mesh에서 NavMesh 빌드
        /// </summary>
        public bool BuildNavMeshFromScene()
        {
            if (!isInitialized)
            {
                Debug.LogError("RecastNavigation이 초기화되지 않았습니다.");
                return false;
            }

            try
            {
                // 씬의 모든 MeshRenderer 찾기
                MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
                List<Vector3> allVertices = new List<Vector3>();
                List<int> allIndices = new List<int>();

                int vertexOffset = 0;

                foreach (MeshRenderer renderer in renderers)
                {
                    MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        Mesh mesh = meshFilter.sharedMesh;
                        Vector3[] vertices = mesh.vertices;
                        int[] indices = mesh.triangles;

                        // 월드 좌표로 변환
                        Transform transform = renderer.transform;
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            vertices[i] = transform.TransformPoint(vertices[i]);
                        }

                        // 좌표 변환 적용
                        if (autoTransformCoordinates)
                        {
                            vertices = RecastNavigationWrapper.TransformPositions(vertices);
                        }

                        // 정점과 인덱스 추가
                        allVertices.AddRange(vertices);
                        for (int i = 0; i < indices.Length; i++)
                        {
                            allIndices.Add(indices[i] + vertexOffset);
                        }
                        vertexOffset += vertices.Length;
                    }
                }

                if (allVertices.Count == 0)
                {
                    Debug.LogWarning("씬에서 Mesh를 찾을 수 없습니다.");
                    return false;
                }

                return BuildNavMesh(allVertices.ToArray(), allIndices.ToArray());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NavMesh 빌드 중 오류: {e.Message}");
                OnError?.Invoke($"NavMesh 빌드 중 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 지정된 Mesh에서 NavMesh 빌드
        /// </summary>
        public bool BuildNavMesh(Mesh mesh)
        {
            if (mesh == null)
            {
                Debug.LogError("Mesh가 null입니다.");
                return false;
            }

            Vector3[] vertices = mesh.vertices;
            int[] indices = mesh.triangles;

            // 좌표 변환 적용
            if (autoTransformCoordinates)
            {
                vertices = RecastNavigationWrapper.TransformPositions(vertices);
            }

            return BuildNavMesh(vertices, indices);
        }

        /// <summary>
        /// 정점과 인덱스 배열에서 NavMesh 빌드
        /// </summary>
        public bool BuildNavMesh(Vector3[] vertices, int[] indices)
        {
            if (!isInitialized)
            {
                Debug.LogError("RecastNavigation이 초기화되지 않았습니다.");
                return false;
            }

            if (vertices == null || indices == null || vertices.Length == 0 || indices.Length == 0)
            {
                Debug.LogError("유효하지 않은 메시 데이터입니다.");
                return false;
            }

            try
            {
                // 메시 데이터 준비
                UnityMeshData meshData = new UnityMeshData
                {
                    vertexCount = vertices.Length,
                    indexCount = indices.Length,
                    transformCoordinates = false // 이미 변환됨
                };

                // 정점 데이터 변환
                float[] vertexArray = new float[vertices.Length * 3];
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertexArray[i * 3] = vertices[i].x;
                    vertexArray[i * 3 + 1] = vertices[i].y;
                    vertexArray[i * 3 + 2] = vertices[i].z;
                }

                // 메모리 핀
                var vertexHandle = System.Runtime.InteropServices.GCHandle.Alloc(vertexArray, System.Runtime.InteropServices.GCHandleType.Pinned);
                var indexHandle = System.Runtime.InteropServices.GCHandle.Alloc(indices, System.Runtime.InteropServices.GCHandleType.Pinned);

                try
                {
                    meshData.vertices = vertexHandle.AddrOfPinnedObject();
                    meshData.indices = indexHandle.AddrOfPinnedObject();

                    // NavMesh 빌드 설정
                    UnityNavMeshBuildSettings settings = new UnityNavMeshBuildSettings
                    {
                        cellSize = buildSettings.cellSize,
                        cellHeight = buildSettings.cellHeight,
                        walkableSlopeAngle = buildSettings.walkableSlopeAngle,
                        walkableHeight = buildSettings.walkableHeight,
                        walkableRadius = buildSettings.walkableRadius,
                        walkableClimb = buildSettings.walkableClimb,
                        minRegionArea = buildSettings.minRegionArea,
                        mergeRegionArea = buildSettings.mergeRegionArea,
                        maxVertsPerPoly = buildSettings.maxVertsPerPoly,
                        detailSampleDist = buildSettings.detailSampleDist,
                        detailSampleMaxError = buildSettings.detailSampleMaxError,
                        autoTransformCoordinates = false // 이미 변환됨
                    };

                    // NavMesh 빌드
                    UnityNavMeshResult result = RecastNavigationWrapper.UnityRecast_BuildNavMesh(ref meshData, ref settings);

                    if (result.success)
                    {
                        navMeshData = RecastNavigationWrapper.GetNavMeshData(result);
                        RecastNavigationWrapper.UnityRecast_FreeNavMeshData(ref result);

                        Debug.Log($"NavMesh 빌드 성공 - 폴리곤: {RecastNavigationWrapper.UnityRecast_GetPolyCount()}, 정점: {RecastNavigationWrapper.UnityRecast_GetVertexCount()}");
                        return true;
                    }
                    else
                    {
                        string error = RecastNavigationWrapper.GetErrorMessage(result.errorMessage);
                        RecastNavigationWrapper.UnityRecast_FreeNavMeshData(ref result);
                        Debug.LogError($"NavMesh 빌드 실패: {error}");
                        OnError?.Invoke($"NavMesh 빌드 실패: {error}");
                        return false;
                    }
                }
                finally
                {
                    vertexHandle.Free();
                    indexHandle.Free();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NavMesh 빌드 중 오류: {e.Message}");
                OnError?.Invoke($"NavMesh 빌드 중 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// NavMesh 데이터 로드
        /// </summary>
        public bool LoadNavMesh(byte[] data)
        {
            if (!isInitialized)
            {
                Debug.LogError("RecastNavigation이 초기화되지 않았습니다.");
                return false;
            }

            if (data == null || data.Length == 0)
            {
                Debug.LogError("NavMesh 데이터가 유효하지 않습니다.");
                return false;
            }

            try
            {
                bool success = RecastNavigationWrapper.UnityRecast_LoadNavMesh(data, data.Length);
                if (success)
                {
                    navMeshData = data;
                    Debug.Log("NavMesh 로드 성공");
                    return true;
                }
                else
                {
                    Debug.LogError("NavMesh 로드 실패");
                    OnError?.Invoke("NavMesh 로드 실패");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NavMesh 로드 중 오류: {e.Message}");
                OnError?.Invoke($"NavMesh 로드 중 오류: {e.Message}");
                return false;
            }
        }

        #endregion

        #region 경로 찾기

        /// <summary>
        /// 경로 찾기
        /// </summary>
        public PathfindingResult FindPath(Vector3 start, Vector3 end)
        {
            if (!isInitialized)
            {
                return new PathfindingResult { Success = false, ErrorMessage = "RecastNavigation이 초기화되지 않았습니다." };
            }

            if (navMeshData == null)
            {
                return new PathfindingResult { Success = false, ErrorMessage = "NavMesh가 로드되지 않았습니다." };
            }

            try
            {
                // 좌표 변환 적용
                if (autoTransformCoordinates)
                {
                    start = RecastNavigationWrapper.TransformPosition(start);
                    end = RecastNavigationWrapper.TransformPosition(end);
                }

                UnityPathResult result = RecastNavigationWrapper.UnityRecast_FindPath(
                    start.x, start.y, start.z,
                    end.x, end.y, end.z
                );

                if (result.success)
                {
                    Vector3[] pathPoints = RecastNavigationWrapper.GetPathPoints(result);
                    RecastNavigationWrapper.UnityRecast_FreePathResult(ref result);

                    // 좌표 변환 적용 (결과를 Unity 좌표계로)
                    if (autoTransformCoordinates)
                    {
                        pathPoints = RecastNavigationWrapper.TransformPositions(pathPoints);
                    }

                    currentPath.Clear();
                    currentPath.AddRange(pathPoints);

                    OnPathFound?.Invoke(pathPoints);

                    return new PathfindingResult
                    {
                        Success = true,
                        PathPoints = pathPoints
                    };
                }
                else
                {
                    string error = RecastNavigationWrapper.GetErrorMessage(result.errorMessage);
                    RecastNavigationWrapper.UnityRecast_FreePathResult(ref result);
                    return new PathfindingResult { Success = false, ErrorMessage = error };
                }
            }
            catch (System.Exception e)
            {
                return new PathfindingResult { Success = false, ErrorMessage = e.Message };
            }
        }

        /// <summary>
        /// 현재 경로 가져오기
        /// </summary>
        public Vector3[] GetCurrentPath()
        {
            return currentPath.ToArray();
        }

        /// <summary>
        /// 경로 지우기
        /// </summary>
        public void ClearPath()
        {
            currentPath.Clear();
        }

        #endregion

        #region 설정

        /// <summary>
        /// 좌표계 설정
        /// </summary>
        public void SetCoordinateSystem(CoordinateSystem system)
        {
            coordinateSystem = system;
            if (isInitialized)
            {
                RecastNavigationWrapper.UnityRecast_SetCoordinateSystem(system);
            }
        }

        /// <summary>
        /// Y축 회전 설정
        /// </summary>
        public void SetYAxisRotation(YAxisRotation rotation)
        {
            yAxisRotation = rotation;
            if (isInitialized)
            {
                RecastNavigationWrapper.UnityRecast_SetYAxisRotation(rotation);
            }
        }

        /// <summary>
        /// 빌드 설정 업데이트
        /// </summary>
        public void UpdateBuildSettings(NavMeshBuildSettings settings)
        {
            buildSettings = settings;
        }

        /// <summary>
        /// 자동 좌표 변환 설정
        /// </summary>
        public void SetAutoTransformCoordinates(bool enabled)
        {
            autoTransformCoordinates = enabled;
        }

        #endregion

        #region 정보 조회

        /// <summary>
        /// NavMesh 폴리곤 수 가져오기
        /// </summary>
        public int GetPolyCount()
        {
            if (!isInitialized) return 0;
            return RecastNavigationWrapper.UnityRecast_GetPolyCount();
        }

        /// <summary>
        /// NavMesh 정점 수 가져오기
        /// </summary>
        public int GetVertexCount()
        {
            if (!isInitialized) return 0;
            return RecastNavigationWrapper.UnityRecast_GetVertexCount();
        }

        /// <summary>
        /// NavMesh 데이터 가져오기
        /// </summary>
        public byte[] GetNavMeshData()
        {
            return navMeshData;
        }

        /// <summary>
        /// 초기화 상태 확인
        /// </summary>
        public bool IsInitialized()
        {
            return isInitialized;
        }

        #endregion
    }
} 