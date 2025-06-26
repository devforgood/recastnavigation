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
        
        private NavMeshDebugData debugData;
        private List<Vector3> vertices = new List<Vector3>();
        private List<int> indices = new List<int>();
        private List<Vector3> triangleCenters = new List<Vector3>();
        private List<Vector3> triangleNormals = new List<Vector3>();
        
        private float lastUpdateTime;
        private bool hasValidData = false;
        
        private void Start()
        {
            // 초기화
            InitializeGizmo();
        }
        
        private void Update()
        {
            if (autoUpdate && Time.time - lastUpdateTime > updateInterval)
            {
                UpdateNavMeshData();
                lastUpdateTime = Time.time;
            }
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
                        Gizmos.DrawMesh(CreateTriangleMesh(v1, v2, v3));
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
        
        #endregion
    }
} 