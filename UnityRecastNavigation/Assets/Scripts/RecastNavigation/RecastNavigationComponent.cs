using UnityEngine;
using System.Collections.Generic;
using System;

namespace RecastNavigation
{
    /// <summary>
    /// RecastNavigation을 Unity에서 사용하기 위한 컴포넌트
    /// </summary>
    public class RecastNavigationComponent : MonoBehaviour
    {
        [Header("NavMesh 설정")]
        [SerializeField] private NavMeshBuildSettings buildSettings = NavMeshBuildSettingsExtensions.CreateDefault();
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
        [SerializeField] private bool autoAddGizmo = true;

        private byte[] navMeshData;
        private bool isInitialized = false;
        private List<Vector3> currentPath = new List<Vector3>();
        private NavMeshGizmo navMeshGizmo;

        // 이벤트
        public System.Action<Vector3[]> OnPathFound;
        public System.Action<string> OnError;

        // 프로퍼티
        public bool IsInitialized => isInitialized;
        public bool IsNavMeshLoaded => navMeshData != null && navMeshData.Length > 0;
        public Vector3[] CurrentPath => currentPath.ToArray();
        public int PathLength => currentPath.Count;
        public int PolyCount => isInitialized ? RecastNavigationWrapper.UnityRecast_GetPolyCount() : 0;
        public int VertexCount => isInitialized ? RecastNavigationWrapper.UnityRecast_GetVertexCount() : 0;

        #region Unity 이벤트

        private void Awake()
        {
            InitializeRecastNavigation();
            
            // NavMeshGizmo 자동 추가
            if (autoAddGizmo)
            {
                AddNavMeshGizmo();
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            // 자동 경로 찾기
            if (autoFindPath && startPoint != null && endPoint != null)
            {
                if (Time.frameCount % Mathf.Max(1, Mathf.RoundToInt(pathUpdateInterval / Time.deltaTime)) == 0)
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
            if (drawNavMesh && isInitialized)
            {
                Gizmos.color = navMeshColor;
                Vector3[] vertices = RecastNavigationWrapper.GetDebugVertices();
                int[] indices = RecastNavigationWrapper.GetDebugIndices();

                if (vertices != null && indices != null)
                {
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        if (i + 2 < indices.Length)
                        {
                            Vector3 v1 = vertices[indices[i]];
                            Vector3 v2 = vertices[indices[i + 1]];
                            Vector3 v3 = vertices[indices[i + 2]];

                            Gizmos.DrawLine(v1, v2);
                            Gizmos.DrawLine(v2, v3);
                            Gizmos.DrawLine(v3, v1);
                        }
                    }
                }
            }

            // 경로 그리기
            if (drawPath && currentPath.Count > 1)
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
                GUILayout.Label($"경로 포인트 수: {PathLength}");
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

        #region NavMeshGizmo 관리

        /// <summary>
        /// NavMeshGizmo 컴포넌트 추가
        /// </summary>
        public void AddNavMeshGizmo()
        {
            if (navMeshGizmo != null) return;

            navMeshGizmo = GetComponent<NavMeshGizmo>();
            if (navMeshGizmo == null)
            {
                navMeshGizmo = gameObject.AddComponent<NavMeshGizmo>();
                Debug.Log("NavMeshGizmo 컴포넌트가 추가되었습니다.");
            }
        }

        /// <summary>
        /// NavMeshGizmo 컴포넌트 제거
        /// </summary>
        public void RemoveNavMeshGizmo()
        {
            if (navMeshGizmo != null)
            {
                DestroyImmediate(navMeshGizmo);
                navMeshGizmo = null;
                Debug.Log("NavMeshGizmo 컴포넌트가 제거되었습니다.");
            }
        }

        /// <summary>
        /// NavMeshGizmo 데이터 업데이트
        /// </summary>
        public void UpdateNavMeshGizmo()
        {
            if (navMeshGizmo != null)
            {
                navMeshGizmo.UpdateNavMeshData();
            }
        }

        /// <summary>
        /// NavMeshGizmo 설정
        /// </summary>
        /// <param name="showNavMesh">NavMesh 표시 여부</param>
        /// <param name="showWireframe">와이어프레임 표시 여부</param>
        /// <param name="showFaces">면 표시 여부</param>
        /// <param name="showVertices">정점 표시 여부</param>
        public void ConfigureNavMeshGizmo(bool showNavMesh = true, bool showWireframe = true, bool showFaces = true, bool showVertices = false)
        {
            if (navMeshGizmo != null)
            {
                navMeshGizmo.SetShowNavMesh(showNavMesh);
                navMeshGizmo.SetShowWireframe(showWireframe);
                navMeshGizmo.SetShowFaces(showFaces);
                navMeshGizmo.SetShowVertices(showVertices);
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
            // 초기화 확인 및 자동 시도
            if (!isInitialized)
            {
                Debug.Log("RecastNavigation이 초기화되지 않았습니다. 자동 초기화를 시도합니다.");
                InitializeRecastNavigation();
                
                if (!isInitialized)
                {
                    Debug.LogError("RecastNavigation 자동 초기화에 실패했습니다. Setup Guide를 사용하여 DLL을 설치해주세요.");
                    OnError?.Invoke("RecastNavigation 초기화 실패");
                    return false;
                }
                else
                {
                    Debug.Log("RecastNavigation 자동 초기화 성공!");
                }
            }

            if (vertices == null || indices == null || vertices.Length == 0 || indices.Length == 0)
            {
                Debug.LogError("유효하지 않은 메시 데이터입니다.");
                return false;
            }

            // 상세한 메시 분석 로깅
            Debug.Log("=== NavMesh 빌드 상세 정보 ===");
            Debug.Log($"입력 메시: {vertices.Length} 정점, {indices.Length/3} 삼각형");
            
            // 메시 바운딩 박스 계산
            Vector3 minBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxBounds = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            
            foreach (Vector3 vertex in vertices)
            {
                if (vertex.x < minBounds.x) minBounds.x = vertex.x;
                if (vertex.y < minBounds.y) minBounds.y = vertex.y;
                if (vertex.z < minBounds.z) minBounds.z = vertex.z;
                if (vertex.x > maxBounds.x) maxBounds.x = vertex.x;
                if (vertex.y > maxBounds.y) maxBounds.y = vertex.y;
                if (vertex.z > maxBounds.z) maxBounds.z = vertex.z;
            }
            
            Vector3 meshSize = maxBounds - minBounds;
            Debug.Log($"메시 바운딩 박스: Min{minBounds}, Max{maxBounds}");
            Debug.Log($"메시 크기: {meshSize} (가로={meshSize.x:F2}, 세로={meshSize.y:F2}, 높이={meshSize.z:F2})");
            
            // 현재 빌드 설정 로깅
            Debug.Log("=== NavMesh 빌드 설정 ===");
            Debug.Log($"cellSize: {buildSettings.cellSize:F3} (메시 너비의 {(buildSettings.cellSize / System.Math.Max(meshSize.x, 0.001f) * 100):F1}%)");
            Debug.Log($"cellHeight: {buildSettings.cellHeight:F3}");
            Debug.Log($"walkableHeight: {buildSettings.walkableHeight:F3}");
            Debug.Log($"walkableRadius: {buildSettings.walkableRadius:F3}");
            Debug.Log($"walkableClimb: {buildSettings.walkableClimb:F3}");
            Debug.Log($"minRegionArea: {buildSettings.minRegionArea:F1}");
            Debug.Log($"mergeRegionArea: {buildSettings.mergeRegionArea:F1}");
            Debug.Log($"autoTransformCoordinates: {autoTransformCoordinates}");
            
            // 설정 적합성 검사 및 권장사항
            bool hasWarnings = false;
            if (buildSettings.cellSize > meshSize.x * 0.5f && meshSize.x > 0)
            {
                Debug.LogWarning($"⚠️ cellSize({buildSettings.cellSize:F3})가 메시 너비({meshSize.x:F2})에 비해 너무 큽니다!");
                Debug.LogWarning($"권장 cellSize: {meshSize.x * 0.1f:F3} ~ {meshSize.x * 0.2f:F3}");
                hasWarnings = true;
            }
            if (buildSettings.cellSize > meshSize.z * 0.5f && meshSize.z > 0)
            {
                Debug.LogWarning($"⚠️ cellSize({buildSettings.cellSize:F3})가 메시 깊이({meshSize.z:F2})에 비해 너무 큽니다!");
                Debug.LogWarning($"권장 cellSize: {meshSize.z * 0.1f:F3} ~ {meshSize.z * 0.2f:F3}");
                hasWarnings = true;
            }
            if (buildSettings.minRegionArea > 50)
            {
                Debug.LogWarning($"⚠️ minRegionArea({buildSettings.minRegionArea:F1})가 너무 클 수 있습니다!");
                Debug.LogWarning("권장 minRegionArea: 8 ~ 20");
                hasWarnings = true;
            }
            
            if (hasWarnings)
            {
                Debug.LogWarning("설정 문제가 감지되었습니다. NavMesh 생성이 실패할 수 있습니다.");
                
                // 자동 권장 설정 제안
                Debug.Log("=== 권장 설정값 ===");
                float recommendedCellSize = System.Math.Max(meshSize.x, meshSize.z) * 0.15f;
                Debug.Log($"권장 cellSize: {recommendedCellSize:F3}");
                Debug.Log($"권장 minRegionArea: 8");
                Debug.Log($"권장 mergeRegionArea: 20");
            }
            
            // 추가 진단: 메시 품질 검사
            Debug.Log("=== 메시 품질 진단 ===");
            Debug.Log($"삼각형 밀도: {indices.Length/3} 삼각형 / {meshSize.x * meshSize.z:F1}m² = {(indices.Length/3) / (meshSize.x * meshSize.z):F4} 삼각형/m²");
            
            // 메시 높이 문제 체크
            if (meshSize.y < 0.1f)
            {
                Debug.LogWarning($"⚠️ 메시가 매우 평평합니다 (높이={meshSize.y:F3}m). NavMesh 생성에 문제가 될 수 있습니다.");
            }
            
            // 정점 샘플 출력 (처음 몇 개)
            Debug.Log("=== 정점 샘플 (처음 4개) ===");
            for (int i = 0; i < System.Math.Min(4, vertices.Length); i++)
            {
                Debug.Log($"  정점[{i}]: {vertices[i]}");
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
                        maxSimplificationError = 1.3f, // 기본값
                        maxEdgeLen = 12.0f, // 기본값
                        autoTransformCoordinates = false // 이미 변환됨
                    };

                    // NavMesh 빌드
                    Debug.Log("=== C++ DLL NavMesh 빌드 시작 ===");
                    UnityNavMeshResult result = RecastNavigationWrapper.UnityRecast_BuildNavMesh(ref meshData, ref settings);

                    Debug.Log($"DLL 빌드 결과: success={result.success}");
                    Debug.Log($"DLL 데이터 크기: {result.dataSize} bytes");
                    Debug.Log($"DLL 데이터 포인터: {result.navMeshData}");
                    
                    if (result.success)
                    {
                        navMeshData = RecastNavigationWrapper.GetNavMeshData(result);
                        Debug.Log($"Unity 측 navMeshData 크기: {navMeshData?.Length ?? 0} bytes");
                        
                        RecastNavigationWrapper.UnityRecast_FreeNavMeshData(ref result);

                        int polyCount = RecastNavigationWrapper.UnityRecast_GetPolyCount();
                        int vertexCount = RecastNavigationWrapper.UnityRecast_GetVertexCount();
                        
                                                 Debug.Log($"✅ NavMesh 빌드 성공! 폴리곤: {polyCount}, 정점: {vertexCount}");
                         
                         // 폴리곤이 0개인 경우 추가 진단 (하지만 이제는 성공할 것임)
                         if (polyCount == 0)
                         {
                             Debug.LogWarning("⚠️ 폴리곤이 0개입니다. 메시가 너무 작거나 설정을 조정해야 할 수 있습니다.");
                             Debug.LogWarning("권장사항: 더 큰 메시를 사용하거나 minRegionArea를 줄여보세요.");
                         }
                         
                         // NavMeshGizmo 컴포넌트 자동 추가
                         var gizmo = GetComponent<NavMeshGizmo>();
                         if (gizmo == null)
                         {
                             gizmo = gameObject.AddComponent<NavMeshGizmo>();
                             Debug.Log("🎨 NavMeshGizmo 컴포넌트가 자동으로 추가되었습니다.");
                         }
                         
                         // 시각화 설정 활성화
                         gizmo.SetShowNavMesh(true);
                         gizmo.SetShowWireframe(true);
                         gizmo.SetShowFaces(true);
                         gizmo.SetNavMeshColor(new Color(0.2f, 0.8f, 0.2f, 0.6f));
                         
                         // NavMesh 데이터 업데이트
                         gizmo.UpdateNavMeshData();
                         
                         // 기존 NavMeshGizmo 업데이트
                         UpdateNavMeshGizmo();
                         
                         Debug.Log("🔍 Scene View에서 NavMesh 시각화를 확인하세요!");
                         Debug.Log("💡 Inspector에서 NavMeshGizmo 설정을 조정할 수 있습니다.");
                        
                        return true;
                    }
                    else
                    {
                        string error = RecastNavigationWrapper.GetErrorMessage(result.errorMessage);
                        RecastNavigationWrapper.UnityRecast_FreeNavMeshData(ref result);
                        Debug.LogError($"❌ NavMesh 빌드 실패: {error}");
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
        /// 정점과 인덱스 배열로 NavMesh 빌드 (지정된 설정 사용)
        /// </summary>
        /// <param name="vertices">메시 정점 배열</param>
        /// <param name="indices">메시 인덱스 배열</param>
        /// <param name="settings">사용할 빌드 설정</param>
        /// <returns>빌드 성공 여부</returns>
        public bool BuildNavMesh(Vector3[] vertices, int[] indices, NavMeshBuildSettings settings)
        {
            // 현재 설정을 백업
            NavMeshBuildSettings originalSettings = buildSettings;
            
            try
            {
                // 임시로 새 설정 적용
                buildSettings = settings;
                
                // 기존 메서드로 빌드
                bool result = BuildNavMesh(vertices, indices);
                
                return result;
            }
            finally
            {
                // 원래 설정 복원
                buildSettings = originalSettings;
            }
        }

        /// <summary>
        /// NavMesh 데이터 로드
        /// </summary>
        public bool LoadNavMesh(byte[] data)
        {
            // 초기화 확인 및 자동 시도
            if (!isInitialized)
            {
                Debug.Log("RecastNavigation이 초기화되지 않았습니다. 자동 초기화를 시도합니다.");
                InitializeRecastNavigation();
                
                if (!isInitialized)
                {
                    Debug.LogError("RecastNavigation 자동 초기화에 실패했습니다. Setup Guide를 사용하여 DLL을 설치해주세요.");
                    OnError?.Invoke("RecastNavigation 초기화 실패");
                    return false;
                }
                else
                {
                    Debug.Log("RecastNavigation 자동 초기화 성공!");
                }
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
                    
                    // NavMeshGizmo 업데이트
                    UpdateNavMeshGizmo();
                    
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
            // 초기화 확인 및 자동 시도
            if (!isInitialized)
            {
                Debug.Log("RecastNavigation이 초기화되지 않았습니다. 자동 초기화를 시도합니다.");
                InitializeRecastNavigation();
                
                if (!isInitialized)
                {
                    Debug.LogError("RecastNavigation 자동 초기화에 실패했습니다. Setup Guide를 사용하여 DLL을 설치해주세요.");
                    return new PathfindingResult { Success = false, ErrorMessage = "RecastNavigation 초기화 실패" };
                }
                else
                {
                    Debug.Log("RecastNavigation 자동 초기화 성공!");
                }
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
                    
                    // NavMeshGizmo에 경로 전달
                    if (navMeshGizmo != null)
                    {
                        navMeshGizmo.SetPath(pathPoints);
                    }

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
        /// 현재 경로 지우기
        /// </summary>
        public void ClearPath()
        {
            currentPath.Clear();
            
            // NavMeshGizmo에서도 경로 지우기
            if (navMeshGizmo != null)
            {
                navMeshGizmo.ClearPath();
            }
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
        /// 메시 크기에 맞는 권장 설정 생성
        /// </summary>
        public NavMeshBuildSettings GetRecommendedSettings(Vector3[] vertices)
        {
            if (vertices == null || vertices.Length == 0)
                return buildSettings;
                
            // 메시 바운딩 박스 계산
            Vector3 minBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxBounds = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            
            foreach (Vector3 vertex in vertices)
            {
                if (vertex.x < minBounds.x) minBounds.x = vertex.x;
                if (vertex.y < minBounds.y) minBounds.y = vertex.y;
                if (vertex.z < minBounds.z) minBounds.z = vertex.z;
                if (vertex.x > maxBounds.x) maxBounds.x = vertex.x;
                if (vertex.y > maxBounds.y) maxBounds.y = vertex.y;
                if (vertex.z > maxBounds.z) maxBounds.z = vertex.z;
            }
            
            Vector3 meshSize = maxBounds - minBounds;
            float maxDimension = System.Math.Max(meshSize.x, meshSize.z);
            float meshHeight = meshSize.y;
            
            Debug.Log($"메시 크기 분석: 가로={meshSize.x:F2}m, 높이={meshHeight:F2}m, 세로={meshSize.z:F2}m");
            
            // 권장 설정 생성 (메시 높이에 맞춰 조정)
            var recommendedSettings = new NavMeshBuildSettings
            {
                cellSize = 0.3f,  // 고정값: 일반적인 에이전트에 최적화
                cellHeight = System.Math.Min(0.1f, meshHeight / 10.0f),  // 메시 높이의 1/10 또는 0.1m 중 작은 값
                walkableSlopeAngle = 45.0f,
                walkableHeight = System.Math.Max(0.5f, meshHeight * 0.8f),  // 메시 높이의 80% 또는 최소 0.5m
                walkableRadius = 0.3f,
                walkableClimb = System.Math.Min(0.2f, meshHeight * 0.3f),  // 메시 높이의 30% 또는 최대 0.2m
                minRegionArea = 0.5f,  // 더 작은 영역도 허용
                mergeRegionArea = 10.0f,  // 병합 임계값도 낮춤
                maxVertsPerPoly = 6,
                detailSampleDist = 6.0f,
                detailSampleMaxError = 1.0f,
                autoTransformCoordinates = false  // 좌표 변환 문제 방지
            };
            
            // 평평한 메시 (높이 ≤ 2m) - 특별 처리
            if (meshHeight <= 2.0f)
            {
                recommendedSettings.cellHeight = 0.05f;  // 더 세밀한 높이
                recommendedSettings.walkableHeight = meshHeight * 0.5f;
                recommendedSettings.walkableClimb = Math.Min(0.2f, meshHeight * 0.3f);
                recommendedSettings.walkableRadius = 0.3f;  // 더 작은 반지름으로 전체 영역 커버
                recommendedSettings.minRegionArea = 0.5f;   // 더 작은 영역도 허용
                
                Debug.Log($"평평한 메시 감지 (높이={meshHeight:F2}m) - 특별 설정 적용");
            }
            // 매우 얇은 메시 (높이 ≤ 0.5m) - 극특별 처리  
            else if (meshHeight <= 0.5f)
            {
                recommendedSettings.cellHeight = 0.02f;
                recommendedSettings.walkableHeight = 0.3f;
                recommendedSettings.walkableClimb = 0.1f;
                recommendedSettings.walkableRadius = 0.1f;  // 매우 작은 반지름
                recommendedSettings.minRegionArea = 0.2f;   // 매우 작은 영역도 허용
                
                Debug.Log($"매우 얇은 메시 감지 (높이={meshHeight:F2}m) - 극특별 설정 적용");
            }
            
            // 메시 크기별 cellSize 조정
            if (maxDimension < 10.0f)
            {
                // 작은 메시: 더 세밀하게
                recommendedSettings.cellSize = 0.1f;
                recommendedSettings.minRegionArea = 0.5f;
            }
            else if (maxDimension > 100.0f)
            {
                // 큰 메시: 성능 최적화
                recommendedSettings.cellSize = 0.5f;
                recommendedSettings.minRegionArea = 2.0f;
            }
            
            // 설정값이 너무 작아지지 않도록 최소값 보장
            recommendedSettings.cellHeight = System.Math.Max(recommendedSettings.cellHeight, 0.01f);
            recommendedSettings.walkableHeight = System.Math.Max(recommendedSettings.walkableHeight, 0.1f);
            recommendedSettings.walkableClimb = System.Math.Max(recommendedSettings.walkableClimb, 0.05f);
            
            Debug.Log($"최종 권장 설정:");
            Debug.Log($"  cellSize={recommendedSettings.cellSize:F3}, cellHeight={recommendedSettings.cellHeight:F3}");
            Debug.Log($"  walkableHeight={recommendedSettings.walkableHeight:F3}, walkableClimb={recommendedSettings.walkableClimb:F3}");
            Debug.Log($"  minRegionArea={recommendedSettings.minRegionArea:F1}");
            
            return recommendedSettings;
        }
        
        /// <summary>
        /// 권장 설정을 자동으로 적용하고 NavMesh 빌드
        /// </summary>
        public bool BuildNavMeshWithRecommendedSettings(Vector3[] vertices, int[] indices)
        {
            Debug.Log("=== 권장 설정으로 NavMesh 빌드 시도 ===");
            
            var originalSettings = buildSettings;
            var recommendedSettings = GetRecommendedSettings(vertices);
            
            Debug.Log("권장 설정 적용:");
            Debug.Log($"  cellSize: {originalSettings.cellSize:F3} → {recommendedSettings.cellSize:F3}");
            Debug.Log($"  minRegionArea: {originalSettings.minRegionArea:F1} → {recommendedSettings.minRegionArea:F1}");
            Debug.Log($"  mergeRegionArea: {originalSettings.mergeRegionArea:F1} → {recommendedSettings.mergeRegionArea:F1}");
            
            // 임시로 권장 설정 적용
            buildSettings = recommendedSettings;
            
            bool success = BuildNavMesh(vertices, indices);
            
            if (!success)
            {
                Debug.LogWarning("권장 설정으로도 빌드 실패. 원래 설정 복원.");
                buildSettings = originalSettings;
            }
            else
            {
                Debug.Log("✓ 권장 설정으로 NavMesh 빌드 성공!");
            }
            
            return success;
        }

        #endregion
    }
} 