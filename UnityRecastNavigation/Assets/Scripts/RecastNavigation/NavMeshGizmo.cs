using UnityEngine;
using System.Collections.Generic;

namespace RecastNavigation
{
    /// <summary>
    /// NavMesh를 Unity Scene에서 기즈모로 시각화하는 컴포넌트
    /// </summary>
    public class NavMeshGizmo : MonoBehaviour
    {
        [Header("시각화 설정")]
        [SerializeField] private bool showNavMesh = true;
        [SerializeField] private bool showWireframe = true;
        [SerializeField] private bool showFaces = true;
        [SerializeField] private bool showVertices = false;
        
        [Header("색상 설정")]
        [SerializeField] private Color navMeshColor = new Color(0.2f, 0.8f, 0.2f, 0.6f);
        [SerializeField] private Color wireframeColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);
        [SerializeField] private Color vertexColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        
        [Header("크기 설정")]
        [SerializeField] private float vertexSize = 0.1f;
        [SerializeField] private float lineWidth = 1.0f;
        
        [Header("자동 업데이트")]
        [SerializeField] private bool autoUpdate = true;
        [SerializeField] private float updateInterval = 1.0f;
        
        [Header("성능 최적화")]
        [SerializeField] private bool useMeshCaching = true;
        [SerializeField] private bool useLOD = true;
        [SerializeField] private float lodDistance = 50f;
        [SerializeField] private int maxVisibleTriangles = 1000;
        
        [Header("경로 시각화")]
        [SerializeField] private bool showPath = true;
        [SerializeField] private bool animatePath = false;
        [SerializeField] private float pathAnimationSpeed = 1f;
        [SerializeField] private Color pathStartColor = Color.green;
        [SerializeField] private Color pathEndColor = Color.red;
        [SerializeField] private bool showPathArrows = true;
        [SerializeField] private float arrowSize = 0.5f;
        
        [Header("인터랙션")]
        [SerializeField] private bool enableInteraction = true;
        [SerializeField] private bool showInfoOnHover = true;
        [SerializeField] private bool enableClickToEdit = false;
        
        [Header("면 색상")]
        [SerializeField] private Color faceColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        
        private NavMeshDebugData debugData;
        private List<Vector3> vertices = new List<Vector3>();
        private List<int> indices = new List<int>();
        private List<Vector3> triangleCenters = new List<Vector3>();
        private List<Vector3> triangleNormals = new List<Vector3>();
        
        // 성능 최적화를 위한 캐싱
        private Dictionary<int, Mesh> triangleMeshCache = new Dictionary<int, Mesh>();
        private Queue<Mesh> meshPool = new Queue<Mesh>();
        private const int MAX_POOL_SIZE = 100;
        
        // 경로 시각화
        private List<Vector3> currentPath = new List<Vector3>();
        private float pathAnimationTime = 0f;
        private int animatedPathIndex = 0;
        
        // 인터랙션
        public Vector3 hoveredPoint;
        private bool isHovering = false;
        private Camera sceneCamera;
        
        private float lastUpdateTime;
        private bool hasValidData = false;
        
        #region 공개 속성들 (Editor에서 사용)
        
        /// <summary>
        /// NavMesh가 로드되어 있는지 여부
        /// </summary>
        public bool IsNavMeshLoaded => hasValidData;
        
        /// <summary>
        /// 폴리곤 개수
        /// </summary>
        public int PolyCount => debugData.TriangleCount;
        
        /// <summary>
        /// 정점 개수
        /// </summary>
        public int VertexCount => vertices.Count;
        
        /// <summary>
        /// 경로 포인트 개수
        /// </summary>
        public int PathPointCount => currentPath.Count;
        
        /// <summary>
        /// 클릭 편집 활성화 여부
        /// </summary>
        public bool EnableClickToEdit 
        { 
            get => enableClickToEdit; 
            set => enableClickToEdit = value; 
        }
        
        #endregion
        
        private void Start()
        {
            // 초기화
            InitializeGizmo();
            
            // 카메라 참조 가져오기
            sceneCamera = Camera.main;
            if (sceneCamera == null)
                sceneCamera = FindObjectOfType<Camera>();
        }
        
        private void Update()
        {
            if (autoUpdate && Time.time - lastUpdateTime > updateInterval)
            {
                UpdateNavMeshData();
                lastUpdateTime = Time.time;
            }
            
            // 경로 애니메이션 업데이트
            if (animatePath && currentPath.Count > 0)
            {
                UpdatePathAnimation();
            }
            
            // 키보드 단축키 처리
            HandleKeyboardShortcuts();
        }
        
        /// <summary>
        /// 기즈모 초기화
        /// </summary>
        private void InitializeGizmo()
        {
            // NavMesh 디버그 드로잉 활성화
            RecastNavigationWrapper.SetDebugDraw(true);
            
            // 초기 데이터 업데이트
            UpdateNavMeshData();
        }
        
        /// <summary>
        /// NavMesh 데이터 업데이트
        /// </summary>
        public void UpdateNavMeshData()
        {
            try
            {
                debugData = RecastNavigationWrapper.GetDebugMeshData();
                
                if (debugData.Vertices != null && debugData.Indices != null && debugData.Vertices.Length > 0)
                {
                    vertices.Clear();
                    indices.Clear();
                    triangleCenters.Clear();
                    triangleNormals.Clear();
                    
                    // 정점 데이터 복사
                    vertices.AddRange(debugData.Vertices);
                    indices.AddRange(debugData.Indices);
                    
                    // 삼각형 중심점과 노멀 계산
                    CalculateTriangleData();
                    
                    hasValidData = true;
                }
                else
                {
                    hasValidData = false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"NavMesh 데이터 업데이트 실패: {e.Message}");
                hasValidData = false;
            }
        }
        
        /// <summary>
        /// 삼각형 중심점과 노멀 계산
        /// </summary>
        private void CalculateTriangleData()
        {
            triangleCenters.Clear();
            triangleNormals.Clear();
            
            for (int i = 0; i < indices.Count; i += 3)
            {
                if (i + 2 < indices.Count)
                {
                    int idx1 = indices[i];
                    int idx2 = indices[i + 1];
                    int idx3 = indices[i + 2];
                    
                    if (idx1 < vertices.Count && idx2 < vertices.Count && idx3 < vertices.Count)
                    {
                        Vector3 v1 = vertices[idx1];
                        Vector3 v2 = vertices[idx2];
                        Vector3 v3 = vertices[idx3];
                        
                        // 삼각형 중심점 계산
                        Vector3 center = (v1 + v2 + v3) / 3f;
                        triangleCenters.Add(center);
                        
                        // 삼각형 노멀 계산
                        Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
                        triangleNormals.Add(normal);
                    }
                }
            }
        }
        
        /// <summary>
        /// 기즈모 그리기
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showNavMesh || !hasValidData || vertices.Count == 0)
                return;
            
            // NavMesh 면 그리기
            if (showFaces)
            {
                DrawNavMeshFaces();
            }
            
            // 와이어프레임 그리기
            if (showWireframe)
            {
                DrawWireframe();
            }
            
            // 정점 그리기
            if (showVertices)
            {
                DrawVertices();
            }
            
            // 경로 그리기
            if (showPath && currentPath.Count > 0)
            {
                DrawPath();
            }
            
            // 호버 정보 그리기
            if (showInfoOnHover && isHovering)
            {
                DrawHoverInfo();
            }
        }
        
        /// <summary>
        /// NavMesh 면 그리기
        /// </summary>
        private void DrawNavMeshFaces()
        {
            Gizmos.color = navMeshColor;
            
            for (int i = 0; i < indices.Count; i += 3)
            {
                if (i + 2 < indices.Count)
                {
                    int idx1 = indices[i];
                    int idx2 = indices[i + 1];
                    int idx3 = indices[i + 2];
                    
                    if (idx1 < vertices.Count && idx2 < vertices.Count && idx3 < vertices.Count)
                    {
                        Vector3 v1 = vertices[idx1];
                        Vector3 v2 = vertices[idx2];
                        Vector3 v3 = vertices[idx3];
                        
                        // 삼각형 그리기
                        Gizmos.DrawMesh(GetCachedTriangleMesh(v1, v2, v3));
                    }
                }
            }
        }
        
        /// <summary>
        /// 와이어프레임 그리기
        /// </summary>
        private void DrawWireframe()
        {
            Gizmos.color = wireframeColor;
            
            for (int i = 0; i < indices.Count; i += 3)
            {
                if (i + 2 < indices.Count)
                {
                    int idx1 = indices[i];
                    int idx2 = indices[i + 1];
                    int idx3 = indices[i + 2];
                    
                    if (idx1 < vertices.Count && idx2 < vertices.Count && idx3 < vertices.Count)
                    {
                        Vector3 v1 = vertices[idx1];
                        Vector3 v2 = vertices[idx2];
                        Vector3 v3 = vertices[idx3];
                        
                        // 삼각형 엣지 그리기
                        Gizmos.DrawLine(v1, v2);
                        Gizmos.DrawLine(v2, v3);
                        Gizmos.DrawLine(v3, v1);
                    }
                }
            }
        }
        
        /// <summary>
        /// 정점 그리기
        /// </summary>
        private void DrawVertices()
        {
            Gizmos.color = vertexColor;
            
            foreach (Vector3 vertex in vertices)
            {
                Gizmos.DrawSphere(vertex, vertexSize);
            }
        }
        
        /// <summary>
        /// 경로 그리기
        /// </summary>
        private void DrawPath()
        {
            if (currentPath.Count < 2) return;
            
            // 경로 선 그리기
            for (int i = 1; i < currentPath.Count; i++)
            {
                Vector3 start = currentPath[i - 1];
                Vector3 end = currentPath[i];
                
                // 애니메이션된 경로
                if (animatePath && i <= animatedPathIndex)
                {
                    float t = (i == animatedPathIndex) ? pathAnimationTime : 1f;
                    Color pathColor = Color.Lerp(pathStartColor, pathEndColor, (float)i / currentPath.Count);
                    pathColor.a = t;
                    Gizmos.color = pathColor;
                }
                else
                {
                    Gizmos.color = Color.Lerp(pathStartColor, pathEndColor, (float)i / currentPath.Count);
                }
                
                Gizmos.DrawLine(start, end);
                
                // 경로 화살표 그리기
                if (showPathArrows && i < currentPath.Count - 1)
                {
                    DrawPathArrow(start, end);
                }
            }
            
            // 시작점과 끝점 표시
            Gizmos.color = pathStartColor;
            Gizmos.DrawWireSphere(currentPath[0], arrowSize);
            
            Gizmos.color = pathEndColor;
            Gizmos.DrawWireSphere(currentPath[currentPath.Count - 1], arrowSize);
        }
        
        /// <summary>
        /// 경로 화살표 그리기
        /// </summary>
        private void DrawPathArrow(Vector3 start, Vector3 end)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 center = (start + end) * 0.5f;
            
            // 화살표 머리 그리기
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            Vector3 arrowTip = center + direction * arrowSize * 0.5f;
            Vector3 arrowLeft = arrowTip - direction * arrowSize + right * arrowSize * 0.3f;
            Vector3 arrowRight = arrowTip - direction * arrowSize - right * arrowSize * 0.3f;
            
            Gizmos.DrawLine(arrowTip, arrowLeft);
            Gizmos.DrawLine(arrowTip, arrowRight);
        }
        
        /// <summary>
        /// 호버 정보 그리기
        /// </summary>
        private void DrawHoverInfo()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hoveredPoint, 0.2f);
            
            // 호버된 지점의 정보 표시
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(hoveredPoint + Vector3.up * 0.3f, 
                $"Position: {hoveredPoint}\n" +
                $"Distance: {Vector3.Distance(sceneCamera.transform.position, hoveredPoint):F2}");
            #endif
        }
        
        /// <summary>
        /// 삼각형 메시 생성
        /// </summary>
        private Mesh CreateTriangleMesh(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[] { v1, v2, v3 };
            mesh.triangles = new int[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            return mesh;
        }
        
        /// <summary>
        /// 선택된 오브젝트일 때 기즈모 그리기
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!showNavMesh || !hasValidData)
                return;
            
            // 선택된 오브젝트일 때 더 진한 색상으로 그리기
            Gizmos.color = new Color(navMeshColor.r, navMeshColor.g, navMeshColor.b, 0.8f);
            
            // 바운딩 박스 그리기
            if (vertices.Count > 0)
            {
                Bounds bounds = CalculateBounds();
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
        
        /// <summary>
        /// 바운딩 박스 계산
        /// </summary>
        private Bounds CalculateBounds()
        {
            if (vertices.Count == 0)
                return new Bounds();
            
            Bounds bounds = new Bounds(vertices[0], Vector3.zero);
            
            foreach (Vector3 vertex in vertices)
            {
                bounds.Encapsulate(vertex);
            }
            
            return bounds;
        }
        
        /// <summary>
        /// NavMesh 정보 출력
        /// </summary>
        private void OnGUI()
        {
            if (!showNavMesh || !hasValidData)
                return;
            
            // 화면 좌상단에 정보 표시
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("NavMesh 정보");
            GUILayout.Label($"정점 수: {vertices.Count}");
            GUILayout.Label($"삼각형 수: {indices.Count / 3}");
            GUILayout.Label($"폴리곤 수: {RecastNavigationWrapper.GetPolyCount()}");
            
            if (GUILayout.Button("데이터 새로고침"))
            {
                UpdateNavMeshData();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// 컴포넌트 제거 시 정리
        /// </summary>
        private void OnDestroy()
        {
            // NavMesh 디버그 드로잉 비활성화
            RecastNavigationWrapper.SetDebugDraw(false);
            
            // 메시 캐시 정리
            ClearMeshCache();
        }
        
        #region Public API
        
        /// <summary>
        /// NavMesh 표시 여부 설정
        /// </summary>
        /// <param name="show">표시 여부</param>
        public void SetShowNavMesh(bool show)
        {
            showNavMesh = show;
        }
        
        /// <summary>
        /// 와이어프레임 표시 여부 설정
        /// </summary>
        /// <param name="show">표시 여부</param>
        public void SetShowWireframe(bool show)
        {
            showWireframe = show;
        }
        
        /// <summary>
        /// 면 표시 여부 설정
        /// </summary>
        /// <param name="show">표시 여부</param>
        public void SetShowFaces(bool show)
        {
            showFaces = show;
        }
        
        /// <summary>
        /// 정점 표시 여부 설정
        /// </summary>
        /// <param name="show">표시 여부</param>
        public void SetShowVertices(bool show)
        {
            showVertices = show;
        }
        
        /// <summary>
        /// NavMesh 색상 설정
        /// </summary>
        /// <param name="color">색상</param>
        public void SetNavMeshColor(Color color)
        {
            navMeshColor = color;
        }
        
        /// <summary>
        /// 와이어프레임 색상 설정
        /// </summary>
        /// <param name="color">색상</param>
        public void SetWireframeColor(Color color)
        {
            wireframeColor = color;
        }
        
        /// <summary>
        /// 정점 색상 설정
        /// </summary>
        /// <param name="color">색상</param>
        public void SetVertexColor(Color color)
        {
            vertexColor = color;
        }
        
        /// <summary>
        /// 자동 업데이트 설정
        /// </summary>
        /// <param name="enabled">활성화 여부</param>
        public void SetAutoUpdate(bool enabled)
        {
            autoUpdate = enabled;
        }
        
        /// <summary>
        /// 업데이트 간격 설정
        /// </summary>
        /// <param name="interval">간격 (초)</param>
        public void SetUpdateInterval(float interval)
        {
            updateInterval = Mathf.Max(0.1f, interval);
        }
        
        /// <summary>
        /// 경로 설정
        /// </summary>
        /// <param name="path">경로 포인트 배열</param>
        public void SetPath(Vector3[] path)
        {
            currentPath.Clear();
            if (path != null)
            {
                currentPath.AddRange(path);
            }
            pathAnimationTime = 0f;
            animatedPathIndex = 0;
        }
        
        /// <summary>
        /// 경로 지우기
        /// </summary>
        public void ClearPath()
        {
            currentPath.Clear();
            pathAnimationTime = 0f;
            animatedPathIndex = 0;
        }
        
        /// <summary>
        /// 경로 애니메이션 설정
        /// </summary>
        /// <param name="enabled">활성화 여부</param>
        /// <param name="speed">애니메이션 속도</param>
        public void SetPathAnimation(bool enabled, float speed = 1f)
        {
            animatePath = enabled;
            pathAnimationSpeed = speed;
        }
        
        /// <summary>
        /// 경로 색상 설정
        /// </summary>
        /// <param name="startColor">시작 색상</param>
        /// <param name="endColor">끝 색상</param>
        public void SetPathColors(Color startColor, Color endColor)
        {
            pathStartColor = startColor;
            pathEndColor = endColor;
        }
        
        /// <summary>
        /// 경로 화살표 설정
        /// </summary>
        /// <param name="show">표시 여부</param>
        /// <param name="size">화살표 크기</param>
        public void SetPathArrows(bool show, float size = 0.5f)
        {
            showPathArrows = show;
            arrowSize = size;
        }
        
        /// <summary>
        /// 인터랙션 설정
        /// </summary>
        /// <param name="enabled">활성화 여부</param>
        public void SetInteraction(bool enabled)
        {
            enableInteraction = enabled;
        }
        
        /// <summary>
        /// 호버 정보 표시 설정
        /// </summary>
        /// <param name="show">표시 여부</param>
        public void SetHoverInfo(bool show)
        {
            showInfoOnHover = show;
        }
        
        /// <summary>
        /// 성능 최적화 설정
        /// </summary>
        /// <param name="useCaching">메시 캐싱 사용 여부</param>
        /// <param name="useLOD">LOD 사용 여부</param>
        /// <param name="maxTriangles">최대 표시 삼각형 수</param>
        public void SetPerformanceSettings(bool useCaching, bool useLOD, int maxTriangles)
        {
            useMeshCaching = useCaching;
            this.useLOD = useLOD;
            maxVisibleTriangles = maxTriangles;
        }
        
        /// <summary>
        /// 메시 캐시 정리
        /// </summary>
        public void ClearCache()
        {
            ClearMeshCache();
        }
        
        #endregion
        
        #region 성능 최적화
        
        /// <summary>
        /// 키보드 단축키 처리
        /// </summary>
        private void HandleKeyboardShortcuts()
        {
            if (!enableInteraction) return;
            
            // T: 토글 시각화
            if (Input.GetKeyDown(KeyCode.T))
            {
                showNavMesh = !showNavMesh;
                Debug.Log($"NavMesh 시각화: {(showNavMesh ? "활성화" : "비활성화")}");
            }
            
            // R: 새로고침
            if (Input.GetKeyDown(KeyCode.R))
            {
                UpdateNavMeshData();
                Debug.Log("NavMesh 데이터 새로고침");
            }
            
            // W: 와이어프레임 토글
            if (Input.GetKeyDown(KeyCode.W))
            {
                showWireframe = !showWireframe;
                Debug.Log($"와이어프레임: {(showWireframe ? "활성화" : "비활성화")}");
            }
            
            // F: 면 토글
            if (Input.GetKeyDown(KeyCode.F))
            {
                showFaces = !showFaces;
                Debug.Log($"면 표시: {(showFaces ? "활성화" : "비활성화")}");
            }
            
            // V: 정점 토글
            if (Input.GetKeyDown(KeyCode.V))
            {
                showVertices = !showVertices;
                Debug.Log($"정점 표시: {(showVertices ? "활성화" : "비활성화")}");
            }
            
            // A: 경로 애니메이션 토글
            if (Input.GetKeyDown(KeyCode.A))
            {
                animatePath = !animatePath;
                Debug.Log($"경로 애니메이션: {(animatePath ? "활성화" : "비활성화")}");
            }
        }
        
        /// <summary>
        /// 경로 애니메이션 업데이트
        /// </summary>
        private void UpdatePathAnimation()
        {
            pathAnimationTime += Time.deltaTime * pathAnimationSpeed;
            
            if (pathAnimationTime >= 1f)
            {
                pathAnimationTime = 0f;
                animatedPathIndex = (animatedPathIndex + 1) % currentPath.Count;
            }
        }
        
        /// <summary>
        /// 메시 캐시에서 메시 가져오기
        /// </summary>
        private Mesh GetCachedTriangleMesh(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            if (!useMeshCaching)
                return CreateTriangleMesh(v1, v2, v3);
            
            int hash = GetTriangleHash(v1, v2, v3);
            
            if (triangleMeshCache.TryGetValue(hash, out Mesh cachedMesh))
                return cachedMesh;
            
            Mesh newMesh = CreateTriangleMesh(v1, v2, v3);
            triangleMeshCache[hash] = newMesh;
            
            return newMesh;
        }
        
        /// <summary>
        /// 삼각형 해시 생성
        /// </summary>
        private int GetTriangleHash(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            return v1.GetHashCode() ^ v2.GetHashCode() ^ v3.GetHashCode();
        }
        
        /// <summary>
        /// 메시 캐시 정리
        /// </summary>
        private void ClearMeshCache()
        {
            foreach (var mesh in triangleMeshCache.Values)
            {
                if (mesh != null)
                    DestroyImmediate(mesh);
            }
            triangleMeshCache.Clear();
            
            while (meshPool.Count > 0)
            {
                var mesh = meshPool.Dequeue();
                if (mesh != null)
                    DestroyImmediate(mesh);
            }
        }
        
        #endregion
    }
} 