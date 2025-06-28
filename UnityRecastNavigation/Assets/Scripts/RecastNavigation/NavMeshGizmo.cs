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
                Debug.Log("🔍 NavMeshGizmo: UpdateNavMeshData 시작");
                
                debugData = RecastNavigationWrapper.GetDebugMeshData();
                
                Debug.Log($"🔍 GetDebugMeshData 결과:");
                Debug.Log($"  - Vertices: {(debugData.Vertices != null ? debugData.Vertices.Length.ToString() : "null")}");
                Debug.Log($"  - Indices: {(debugData.Indices != null ? debugData.Indices.Length.ToString() : "null")}");
                Debug.Log($"  - TriangleCount: {debugData.TriangleCount}");
                
                if (debugData.Vertices != null && debugData.Indices != null && debugData.Vertices.Length > 0)
                {
                    vertices.Clear();
                    indices.Clear();
                    triangleCenters.Clear();
                    triangleNormals.Clear();
                    
                    // 정점 데이터 복사
                    vertices.AddRange(debugData.Vertices);
                    indices.AddRange(debugData.Indices);
                    
                    Debug.Log($"✅ NavMesh 데이터 로드됨: {vertices.Count}개 정점, {indices.Count/3}개 삼각형");
                    
                    // NavMesh 품질 분석
                    AnalyzeNavMeshQuality();
                    
                    // === Unity side data bounding box calculation ===
                    if (vertices.Count > 0)
                    {
                        Vector3 min = vertices[0];
                        Vector3 max = vertices[0];
                        
                        foreach (Vector3 vertex in vertices)
                        {
                            if (vertex.x < min.x) min.x = vertex.x;
                            if (vertex.y < min.y) min.y = vertex.y;
                            if (vertex.z < min.z) min.z = vertex.z;
                            
                            if (vertex.x > max.x) max.x = vertex.x;
                            if (vertex.y > max.y) max.y = vertex.y;
                            if (vertex.z > max.z) max.z = vertex.z;
                        }
                        
                        Debug.Log($"📊 Unity side bounding box: Min({min.x:F2}, {min.y:F2}, {min.z:F2}), Max({max.x:F2}, {max.y:F2}, {max.z:F2})");
                        Vector3 size = max - min;
                        Debug.Log($"📊 Unity side NavMesh size: ({size.x:F2} x {size.y:F2} x {size.z:F2})");
                    }
                    
                    // 삼각형 중심점과 노멀 계산
                    CalculateTriangleData();
                    
                    hasValidData = true;
                    Debug.Log("✅ NavMesh 데이터 업데이트 완료");
                }
                else
                {
                    hasValidData = false;
                    Debug.LogWarning("⚠️ NavMesh 데이터가 비어있음");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ NavMesh 데이터 업데이트 실패: {ex.Message}");
                hasValidData = false;
            }
        }
        
        /// <summary>
        /// NavMesh 품질 분석
        /// </summary>
        private void AnalyzeNavMeshQuality()
        {
            if (vertices.Count == 0 || indices.Count == 0)
            {
                Debug.LogWarning("⚠️ NavMesh 품질 분석: 데이터가 비어있음");
                return;
            }
            
            Debug.Log("🔬 === NavMesh 품질 분석 ===");
            
            // 기본 통계
            int triangleCount = indices.Count / 3;
            Debug.Log($"📊 기본 통계:");
            Debug.Log($"  - 정점 수: {vertices.Count}");
            Debug.Log($"  - 삼각형 수: {triangleCount}");
            
            // 경계 상자 및 면적 계산
            Bounds bounds = CalculateBounds();
            float area = bounds.size.x * bounds.size.z; // Y축 제외한 2D 면적
            Debug.Log($"📊 경계 상자:");
            Debug.Log($"  - 중심: ({bounds.center.x:F2}, {bounds.center.y:F2}, {bounds.center.z:F2})");
            Debug.Log($"  - 크기: ({bounds.size.x:F2} x {bounds.size.y:F2} x {bounds.size.z:F2})");
            Debug.Log($"  - 2D 면적: {area:F2} 제곱미터");
            
            // 삼각형 밀도 분석
            if (triangleCount > 0)
            {
                float avgAreaPerTriangle = area / triangleCount;
                float avgEdgeLength = Mathf.Sqrt(avgAreaPerTriangle);
                
                Debug.Log($"📊 삼각형 밀도:");
                Debug.Log($"  - 삼각형당 평균 면적: {avgAreaPerTriangle:F2} 제곱미터");
                Debug.Log($"  - 예상 평균 변 길이: {avgEdgeLength:F2} 미터");
                
                // 품질 평가
                if (avgAreaPerTriangle > 10.0f)
                {
                    Debug.LogWarning($"⚠️ 품질 경고: 삼각형이 너무 큼 (평균 {avgAreaPerTriangle:F2}㎡)");
                    Debug.LogWarning("  💡 제안: cellSize를 줄이거나 detailSampleDist를 줄여보세요");
                }
                else if (avgAreaPerTriangle < 0.1f)
                {
                    Debug.LogWarning($"⚠️ 품질 경고: 삼각형이 너무 작음 (평균 {avgAreaPerTriangle:F2}㎡)");
                    Debug.LogWarning("  💡 제안: cellSize를 늘리거나 detailSampleDist를 늘려보세요");
                }
                else
                {
                    Debug.Log($"✅ 삼각형 크기가 적절함 (평균 {avgAreaPerTriangle:F2}㎡)");
                }
            }
            
            // 삼각형 품질 분석
            AnalyzeTriangleShapes();
            
            Debug.Log("🔬 === NavMesh 품질 분석 완료 ===");
        }
        
        /// <summary>
        /// 삼각형 모양 품질 분석
        /// </summary>
        private void AnalyzeTriangleShapes()
        {
            if (indices.Count < 3)
                return;
            
            Debug.Log($"🔺 삼각형 모양 분석:");
            
            float minArea = float.MaxValue;
            float maxArea = 0f;
            float totalArea = 0f;
            int degenerateTriangles = 0;
            int skinnyTriangles = 0;
            
            for (int i = 0; i < indices.Count; i += 3)
            {
                if (i + 2 < indices.Count)
                {
                    Vector3 v1 = vertices[indices[i]];
                    Vector3 v2 = vertices[indices[i + 1]];
                    Vector3 v3 = vertices[indices[i + 2]];
                    
                    // 삼각형 면적 계산
                    float area = Vector3.Cross(v2 - v1, v3 - v1).magnitude * 0.5f;
                    totalArea += area;
                    minArea = Mathf.Min(minArea, area);
                    maxArea = Mathf.Max(maxArea, area);
                    
                    // 퇴화된 삼각형 체크 (면적이 매우 작음)
                    if (area < 0.001f)
                    {
                        degenerateTriangles++;
                    }
                    
                    // 가늘고 긴 삼각형 체크 (aspect ratio)
                    float[] edgeLengths = new float[3]
                    {
                        Vector3.Distance(v1, v2),
                        Vector3.Distance(v2, v3),
                        Vector3.Distance(v3, v1)
                    };
                    
                    System.Array.Sort(edgeLengths);
                    float aspectRatio = edgeLengths[2] / edgeLengths[0]; // 최장변/최단변
                    
                    if (aspectRatio > 10.0f) // 10:1 비율 이상이면 가늘고 긴 삼각형
                    {
                        skinnyTriangles++;
                    }
                }
            }
            
            int triangleCount = indices.Count / 3;
            Debug.Log($"  - 최소 면적: {minArea:F4} ㎡");
            Debug.Log($"  - 최대 면적: {maxArea:F4} ㎡");
            Debug.Log($"  - 평균 면적: {totalArea/triangleCount:F4} ㎡");
            Debug.Log($"  - 퇴화된 삼각형: {degenerateTriangles}개 ({(float)degenerateTriangles/triangleCount*100:F1}%)");
            Debug.Log($"  - 가늘고 긴 삼각형: {skinnyTriangles}개 ({(float)skinnyTriangles/triangleCount*100:F1}%)");
            
            if (degenerateTriangles > 0)
            {
                Debug.LogWarning($"⚠️ {degenerateTriangles}개의 퇴화된 삼각형 발견");
            }
            
            if (skinnyTriangles > triangleCount * 0.1f) // 10% 이상이 가늘고 긴 삼각형
            {
                Debug.LogWarning($"⚠️ 가늘고 긴 삼각형이 너무 많음 ({skinnyTriangles}개)");
                Debug.LogWarning("  💡 제안: maxSimplificationError 값을 조정해보세요");
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
            // 디버그 로깅 (너무 많이 출력되지 않도록 조건부)
            if (Time.frameCount % 120 == 0) // 2초마다 한 번
            {
                Debug.Log($"🎨 OnDrawGizmos 호출됨 - showNavMesh:{showNavMesh}, hasValidData:{hasValidData}, vertices:{vertices.Count}");
            }
            
            if (!showNavMesh)
            {
                if (Time.frameCount % 120 == 0) Debug.Log("❌ showNavMesh = false");
                return;
            }
            
            if (!hasValidData)
            {
                if (Time.frameCount % 120 == 0) Debug.Log("❌ hasValidData = false");
                return;
            }
            
            if (vertices.Count == 0)
            {
                if (Time.frameCount % 120 == 0) Debug.Log("❌ vertices.Count = 0");
                return;
            }
            
            // 실제 그리기 시작
            if (Time.frameCount % 120 == 0) 
            {
                Debug.Log($"✅ Gizmos 그리기 시작! 삼각형 수: {indices.Count/3}");
            }
            
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
            
            int triangleCount = 0;
            
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
                        
                        // Unity Gizmos는 직접 삼각형을 그릴 수 없으므로 
                        // 대신 작은 사각형들로 면적을 채우는 방식 사용
                        DrawTriangleFilled(v1, v2, v3);
                        
                        triangleCount++;
                    }
                }
            }
            
            // 삼각형 수 로깅 (디버깅용)
            if (Time.frameCount % 120 == 0 && triangleCount > 0)
            {
                Debug.Log($"✅ {triangleCount}개 삼각형을 그렸습니다!");
            }
        }
        
        /// <summary>
        /// 삼각형을 채워서 그리기 (Gizmos.DrawMesh 대체)
        /// </summary>
        private void DrawTriangleFilled(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            // 삼각형의 중심점과 면적 계산
            Vector3 center = (v1 + v2 + v3) / 3f;
            
            // 삼각형 면적 계산
            float area = Vector3.Cross(v2 - v1, v3 - v1).magnitude * 0.5f;
            
            // 면적이 너무 작으면 점으로 표시
            if (area < 0.01f)
            {
                Gizmos.DrawSphere(center, 0.05f);
                return;
            }
            
            // 삼각형 면적에 비례하여 세분화 수준 결정
            int subdivisions = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(area) * 2), 1, 8);
            
            // 삼각형을 작은 점들로 채우기
            for (int i = 0; i <= subdivisions; i++)
            {
                for (int j = 0; j <= subdivisions - i; j++)
                {
                    if (i + j <= subdivisions)
                    {
                        float u = (float)i / subdivisions;
                        float v = (float)j / subdivisions;
                        float w = 1.0f - u - v;
                        
                        if (u >= 0 && v >= 0 && w >= 0)
                        {
                            Vector3 point = u * v1 + v * v2 + w * v3;
                            float pointSize = 0.02f + area * 0.1f;
                            Gizmos.DrawSphere(point, Mathf.Min(pointSize, 0.1f));
                        }
                    }
                }
            }
            
            // 삼각형 가장자리도 그리기
            Gizmos.color = new Color(navMeshColor.r * 0.7f, navMeshColor.g * 0.7f, navMeshColor.b * 0.7f, navMeshColor.a);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v3);
            Gizmos.DrawLine(v3, v1);
            
            // 원래 색상 복원
            Gizmos.color = navMeshColor;
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