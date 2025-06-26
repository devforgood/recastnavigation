using UnityEngine;
using RecastNavigation;
using System.Collections.Generic;

namespace RecastNavigation
{
    /// <summary>
    /// 런타임에서 RecastNavigation을 사용할 수 있는 컴포넌트
    /// </summary>
    public class RecastNavigationComponent : MonoBehaviour
    {
        [Header("NavMesh 설정")]
        [SerializeField] private NavMeshBuildSettings buildSettings;
        
        [Header("경로 찾기 설정")]
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField] private bool autoFindPath = false;
        [SerializeField] private float pathUpdateInterval = 0.1f;
        
        [Header("시각화 설정")]
        [SerializeField] private bool showPath = true;
        [SerializeField] private Color pathColor = Color.green;
        [SerializeField] private float pathWidth = 0.1f;
        [SerializeField] private bool showNavMesh = false;
        [SerializeField] private Color navMeshColor = Color.blue;
        
        [Header("디버그 정보")]
        [SerializeField] private bool showDebugInfo = true;
        
        // 내부 상태
        private bool isInitialized = false;
        private bool isNavMeshLoaded = false;
        private byte[] navMeshData;
        private Vector3[] currentPath;
        private float pathLength;
        private float lastPathUpdateTime;
        
        // 이벤트
        public System.Action<Vector3[]> OnPathFound;
        public System.Action<string> OnError;
        
        // 프로퍼티
        public bool IsInitialized => isInitialized;
        public bool IsNavMeshLoaded => isNavMeshLoaded;
        public Vector3[] CurrentPath => currentPath;
        public float PathLength => pathLength;
        public int PolyCount => isNavMeshLoaded ? RecastNavigationWrapper.GetPolyCount() : 0;
        public int VertexCount => isNavMeshLoaded ? RecastNavigationWrapper.GetVertexCount() : 0;
        
        void Awake()
        {
            // 기본 설정 초기화
            if (buildSettings.Equals(default(NavMeshBuildSettings)))
            {
                buildSettings = NavMeshBuildSettingsExtensions.CreateDefault();
            }
        }
        
        void Start()
        {
            InitializeRecastNavigation();
        }
        
        void Update()
        {
            if (!isInitialized || !isNavMeshLoaded) return;
            
            // 자동 경로 찾기
            if (autoFindPath && startPoint != null && endPoint != null)
            {
                if (Time.time - lastPathUpdateTime >= pathUpdateInterval)
                {
                    FindPath(startPoint.position, endPoint.position);
                    lastPathUpdateTime = Time.time;
                }
            }
        }
        
        void OnDestroy()
        {
            CleanupRecastNavigation();
        }
        
        void OnDrawGizmos()
        {
            if (!showPath && !showNavMesh) return;
            
            // 경로 표시
            if (showPath && currentPath != null && currentPath.Length > 1)
            {
                Gizmos.color = pathColor;
                for (int i = 1; i < currentPath.Length; i++)
                {
                    Gizmos.DrawLine(currentPath[i - 1], currentPath[i]);
                }
                
                // 경로 포인트 표시
                Gizmos.color = Color.yellow;
                for (int i = 0; i < currentPath.Length; i++)
                {
                    Gizmos.DrawWireSphere(currentPath[i], 0.1f);
                }
            }
            
            // NavMesh 정보 표시
            if (showNavMesh && isNavMeshLoaded)
            {
                Gizmos.color = navMeshColor;
                // 여기에 NavMesh 시각화 코드 추가 가능
            }
        }
        
        void OnGUI()
        {
            if (!showDebugInfo) return;
            
            // 화면에 디버그 정보 표시
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("RecastNavigation Debug Info", EditorGUIUtility.isProSkin ? GUI.skin.label : GUI.skin.box);
            GUILayout.Label($"초기화됨: {isInitialized}");
            GUILayout.Label($"NavMesh 로드됨: {isNavMeshLoaded}");
            
            if (isNavMeshLoaded)
            {
                GUILayout.Label($"폴리곤 수: {PolyCount}");
                GUILayout.Label($"정점 수: {VertexCount}");
            }
            
            if (currentPath != null)
            {
                GUILayout.Label($"경로 길이: {pathLength:F2}");
                GUILayout.Label($"경로 포인트 수: {currentPath.Length}");
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// RecastNavigation 초기화
        /// </summary>
        public bool InitializeRecastNavigation()
        {
            if (isInitialized) return true;
            
            if (RecastNavigationWrapper.Initialize())
            {
                isInitialized = true;
                Debug.Log("RecastNavigation 초기화 성공");
                return true;
            }
            else
            {
                Debug.LogError("RecastNavigation 초기화 실패");
                OnError?.Invoke("RecastNavigation 초기화 실패");
                return false;
            }
        }
        
        /// <summary>
        /// RecastNavigation 정리
        /// </summary>
        public void CleanupRecastNavigation()
        {
            if (isInitialized)
            {
                RecastNavigationWrapper.Cleanup();
                isInitialized = false;
                isNavMeshLoaded = false;
                navMeshData = null;
                currentPath = null;
                Debug.Log("RecastNavigation 정리 완료");
            }
        }
        
        /// <summary>
        /// 현재 씬에서 NavMesh 빌드
        /// </summary>
        public bool BuildNavMeshFromScene()
        {
            if (!InitializeRecastNavigation())
            {
                return false;
            }
            
            // 씬의 모든 MeshRenderer 수집
            MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
            List<Mesh> meshes = new List<Mesh>();
            
            foreach (var renderer in renderers)
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    meshes.Add(meshFilter.sharedMesh);
                }
            }
            
            if (meshes.Count == 0)
            {
                Debug.LogWarning("씬에서 Mesh를 찾을 수 없습니다.");
                OnError?.Invoke("씬에서 Mesh를 찾을 수 없습니다.");
                return false;
            }
            
            // 모든 메시를 하나로 합치기
            Mesh combinedMesh = CombineMeshes(meshes);
            
            // NavMesh 빌드
            var result = RecastNavigationWrapper.BuildNavMesh(combinedMesh, buildSettings);
            
            if (result.Success)
            {
                navMeshData = result.NavMeshData;
                
                // NavMesh 로드
                if (RecastNavigationWrapper.LoadNavMesh(navMeshData))
                {
                    isNavMeshLoaded = true;
                    polyCount = RecastNavigationWrapper.GetPolyCount();
                    vertexCount = RecastNavigationWrapper.GetVertexCount();
                    Debug.Log($"NavMesh 빌드 성공! 폴리곤: {polyCount}, 정점: {vertexCount}");
                    return true;
                }
                else
                {
                    Debug.LogError("NavMesh 로드 실패");
                    OnError?.Invoke("NavMesh 로드 실패");
                    return false;
                }
            }
            else
            {
                Debug.LogError($"NavMesh 빌드 실패: {result.ErrorMessage}");
                OnError?.Invoke($"NavMesh 빌드 실패: {result.ErrorMessage}");
                return false;
            }
        }
        
        /// <summary>
        /// 선택된 오브젝트들에서 NavMesh 빌드
        /// </summary>
        public bool BuildNavMeshFromSelection(GameObject[] selectedObjects)
        {
            if (!InitializeRecastNavigation())
            {
                return false;
            }
            
            List<Mesh> meshes = new List<Mesh>();
            
            foreach (var obj in selectedObjects)
            {
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    meshes.Add(meshFilter.sharedMesh);
                }
            }
            
            if (meshes.Count == 0)
            {
                Debug.LogWarning("선택된 오브젝트에서 Mesh를 찾을 수 없습니다.");
                OnError?.Invoke("선택된 오브젝트에서 Mesh를 찾을 수 없습니다.");
                return false;
            }
            
            // 모든 메시를 하나로 합치기
            Mesh combinedMesh = CombineMeshes(meshes);
            
            // NavMesh 빌드
            var result = RecastNavigationWrapper.BuildNavMesh(combinedMesh, buildSettings);
            
            if (result.Success)
            {
                navMeshData = result.NavMeshData;
                
                // NavMesh 로드
                if (RecastNavigationWrapper.LoadNavMesh(navMeshData))
                {
                    isNavMeshLoaded = true;
                    polyCount = RecastNavigationWrapper.GetPolyCount();
                    vertexCount = RecastNavigationWrapper.GetVertexCount();
                    Debug.Log($"NavMesh 빌드 성공! (선택된 오브젝트) 폴리곤: {polyCount}, 정점: {vertexCount}");
                    return true;
                }
                else
                {
                    Debug.LogError("NavMesh 로드 실패");
                    OnError?.Invoke("NavMesh 로드 실패");
                    return false;
                }
            }
            else
            {
                Debug.LogError($"NavMesh 빌드 실패: {result.ErrorMessage}");
                OnError?.Invoke($"NavMesh 빌드 실패: {result.ErrorMessage}");
                return false;
            }
        }
        
        /// <summary>
        /// 경로 찾기
        /// </summary>
        public bool FindPath(Vector3 start, Vector3 end)
        {
            if (!isNavMeshLoaded)
            {
                Debug.LogWarning("NavMesh가 로드되지 않았습니다.");
                OnError?.Invoke("NavMesh가 로드되지 않았습니다.");
                return false;
            }
            
            var result = RecastNavigationWrapper.FindPath(start, end);
            
            if (result.Success)
            {
                currentPath = result.PathPoints;
                pathLength = CalculatePathLength(currentPath);
                Debug.Log($"경로 찾기 성공! 포인트 수: {currentPath.Length}, 길이: {pathLength:F2}");
                OnPathFound?.Invoke(currentPath);
                return true;
            }
            else
            {
                currentPath = null;
                pathLength = 0f;
                Debug.LogError($"경로 찾기 실패: {result.ErrorMessage}");
                OnError?.Invoke($"경로 찾기 실패: {result.ErrorMessage}");
                return false;
            }
        }
        
        /// <summary>
        /// NavMesh 데이터 저장
        /// </summary>
        public bool SaveNavMeshData(string filePath)
        {
            if (navMeshData == null || navMeshData.Length == 0)
            {
                Debug.LogWarning("저장할 NavMesh 데이터가 없습니다.");
                return false;
            }
            
            try
            {
                System.IO.File.WriteAllBytes(filePath, navMeshData);
                Debug.Log($"NavMesh가 저장되었습니다: {filePath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NavMesh 저장 실패: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// NavMesh 데이터 로드
        /// </summary>
        public bool LoadNavMeshData(string filePath)
        {
            if (!InitializeRecastNavigation())
            {
                return false;
            }
            
            try
            {
                byte[] data = System.IO.File.ReadAllBytes(filePath);
                
                if (RecastNavigationWrapper.LoadNavMesh(data))
                {
                    navMeshData = data;
                    isNavMeshLoaded = true;
                    Debug.Log($"NavMesh 로드 성공: {filePath}");
                    return true;
                }
                else
                {
                    Debug.LogError("NavMesh 로드 실패");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NavMesh 로드 실패: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 메시들을 하나로 합치기
        /// </summary>
        private Mesh CombineMeshes(List<Mesh> meshes)
        {
            if (meshes.Count == 1)
            {
                return meshes[0];
            }
            
            // 메시 합치기
            CombineInstance[] combine = new CombineInstance[meshes.Count];
            
            for (int i = 0; i < meshes.Count; i++)
            {
                combine[i].mesh = meshes[i];
                combine[i].transform = Matrix4x4.identity;
            }
            
            Mesh combinedMesh = new Mesh();
            combinedMesh.CombineMeshes(combine, true, true);
            combinedMesh.name = "CombinedMesh";
            
            return combinedMesh;
        }
        
        /// <summary>
        /// 경로 길이 계산
        /// </summary>
        private float CalculatePathLength(Vector3[] path)
        {
            if (path == null || path.Length < 2)
                return 0f;
            
            float length = 0f;
            for (int i = 1; i < path.Length; i++)
            {
                length += Vector3.Distance(path[i - 1], path[i]);
            }
            
            return length;
        }
        
        // 에디터 전용 코드
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // 설정 유효성 검사
            if (pathUpdateInterval < 0.01f)
                pathUpdateInterval = 0.01f;
            
            if (pathWidth < 0.01f)
                pathWidth = 0.01f;
        }
        #endif
    }
} 