using UnityEngine;
using RecastNavigation;
using System.Collections;

namespace RecastNavigation
{
    /// <summary>
    /// RecastNavigation 사용 예제 스크립트
    /// </summary>
    public class RecastNavigationSample : MonoBehaviour
    {
        [Header("NavMesh 설정")]
        [SerializeField] private NavMeshBuildSettings buildSettings;
        
        [Header("경로 찾기 설정")]
        [SerializeField] private Transform agent;
        [SerializeField] private Transform target;
        [SerializeField] private bool autoFindPath = true;
        [SerializeField] private float pathUpdateInterval = 0.5f;
        
        [Header("시각화")]
        [SerializeField] private bool showPath = true;
        [SerializeField] private Color pathColor = Color.green;
        [SerializeField] private float pathWidth = 0.1f;
        [SerializeField] private bool showDebugInfo = true;
        
        [Header("에이전트 이동")]
        [SerializeField] private bool moveAgent = true;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 180f;
        [SerializeField] private float arrivalDistance = 0.5f;
        
        // 내부 상태
        private RecastNavigationComponent navComponent;
        private Vector3[] currentPath;
        private int currentPathIndex = 0;
        private bool isMoving = false;
        
        // 이벤트
        public System.Action<Vector3[]> OnPathFound;
        public System.Action OnDestinationReached;
        public System.Action<string> OnError;
        
        void Start()
        {
            // RecastNavigation 컴포넌트 가져오기 또는 생성
            navComponent = FindObjectOfType<RecastNavigationComponent>();
            if (navComponent == null)
            {
                GameObject navObject = new GameObject("RecastNavigation");
                navComponent = navObject.AddComponent<RecastNavigationComponent>();
            }
            
            // 기본 설정 초기화
            if (buildSettings.Equals(default(NavMeshBuildSettings)))
            {
                buildSettings = NavMeshBuildSettingsExtensions.CreateDefault();
            }
            
            // 이벤트 구독
            navComponent.OnPathFound += OnPathFoundCallback;
            navComponent.OnError += OnErrorCallback;
            
            // NavMesh 빌드
            StartCoroutine(BuildNavMeshCoroutine());
        }
        
        void Update()
        {
            if (!navComponent.IsNavMeshLoaded) return;
            
            // 자동 경로 찾기
            if (autoFindPath && agent != null && target != null)
            {
                if (Time.frameCount % Mathf.RoundToInt(pathUpdateInterval * 60) == 0)
                {
                    FindPathToTarget();
                }
            }
            
            // 에이전트 이동
            if (moveAgent && isMoving && currentPath != null && currentPathIndex < currentPath.Length)
            {
                MoveAgentAlongPath();
            }
        }
        
        void OnDestroy()
        {
            // 이벤트 구독 해제
            if (navComponent != null)
            {
                navComponent.OnPathFound -= OnPathFoundCallback;
                navComponent.OnError -= OnErrorCallback;
            }
        }
        
        void OnDrawGizmos()
        {
            if (!showPath || currentPath == null) return;
            
            // 경로 표시
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
            
            // 현재 목표 지점 표시
            if (currentPathIndex < currentPath.Length)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(currentPath[currentPathIndex], 0.2f);
            }
        }
        
        void OnGUI()
        {
            if (!showDebugInfo) return;
            
            // 화면에 디버그 정보 표시
            GUILayout.BeginArea(new Rect(10, 10, 300, 250));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("RecastNavigation Sample", EditorGUIUtility.isProSkin ? GUI.skin.label : GUI.skin.box);
            GUILayout.Label($"NavMesh 로드됨: {navComponent?.IsNavMeshLoaded}");
            GUILayout.Label($"폴리곤 수: {navComponent?.PolyCount}");
            GUILayout.Label($"정점 수: {navComponent?.VertexCount}");
            
            if (currentPath != null)
            {
                GUILayout.Label($"경로 길이: {navComponent?.PathLength:F2}");
                GUILayout.Label($"경로 포인트 수: {currentPath.Length}");
                GUILayout.Label($"현재 인덱스: {currentPathIndex}");
                GUILayout.Label($"이동 중: {isMoving}");
            }
            
            GUILayout.Space();
            
            // 컨트롤 버튼들
            if (GUILayout.Button("NavMesh 재빌드"))
            {
                StartCoroutine(BuildNavMeshCoroutine());
            }
            
            if (GUILayout.Button("경로 찾기"))
            {
                FindPathToTarget();
            }
            
            if (GUILayout.Button("에이전트 이동 시작/정지"))
            {
                ToggleAgentMovement();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// NavMesh 빌드 코루틴
        /// </summary>
        IEnumerator BuildNavMeshCoroutine()
        {
            Debug.Log("NavMesh 빌드를 시작합니다...");
            
            // NavMesh 빌드
            bool success = navComponent.BuildNavMeshFromScene();
            
            if (success)
            {
                Debug.Log("NavMesh 빌드 완료!");
                
                // 빌드 완료 후 자동으로 경로 찾기
                if (autoFindPath && agent != null && target != null)
                {
                    yield return new WaitForSeconds(0.5f);
                    FindPathToTarget();
                }
            }
            else
            {
                Debug.LogError("NavMesh 빌드 실패!");
            }
        }
        
        /// <summary>
        /// 목표 지점까지 경로 찾기
        /// </summary>
        public void FindPathToTarget()
        {
            if (agent == null || target == null)
            {
                Debug.LogWarning("에이전트 또는 목표가 설정되지 않았습니다.");
                return;
            }
            
            bool success = navComponent.FindPath(agent.position, target.position);
            
            if (success)
            {
                currentPath = navComponent.CurrentPath;
                currentPathIndex = 0;
                isMoving = moveAgent;
                
                Debug.Log($"경로 찾기 성공! 포인트 수: {currentPath.Length}, 길이: {navComponent.PathLength:F2}");
            }
            else
            {
                Debug.LogWarning("경로를 찾을 수 없습니다.");
                currentPath = null;
                isMoving = false;
            }
        }
        
        /// <summary>
        /// 에이전트를 경로를 따라 이동
        /// </summary>
        void MoveAgentAlongPath()
        {
            if (agent == null || currentPath == null || currentPathIndex >= currentPath.Length)
                return;
            
            Vector3 targetPosition = currentPath[currentPathIndex];
            Vector3 direction = (targetPosition - agent.position).normalized;
            
            // 회전
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                agent.rotation = Quaternion.RotateTowards(agent.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            // 이동
            agent.position = Vector3.MoveTowards(agent.position, targetPosition, moveSpeed * Time.deltaTime);
            
            // 목표 지점에 도달했는지 확인
            if (Vector3.Distance(agent.position, targetPosition) < arrivalDistance)
            {
                currentPathIndex++;
                
                // 경로의 끝에 도달
                if (currentPathIndex >= currentPath.Length)
                {
                    isMoving = false;
                    OnDestinationReached?.Invoke();
                    Debug.Log("목적지에 도달했습니다!");
                }
            }
        }
        
        /// <summary>
        /// 에이전트 이동 토글
        /// </summary>
        public void ToggleAgentMovement()
        {
            if (currentPath != null && currentPathIndex < currentPath.Length)
            {
                isMoving = !isMoving;
                Debug.Log($"에이전트 이동: {(isMoving ? "시작" : "정지")}");
            }
        }
        
        /// <summary>
        /// 경로 찾기 콜백
        /// </summary>
        void OnPathFoundCallback(Vector3[] path)
        {
            OnPathFound?.Invoke(path);
        }
        
        /// <summary>
        /// 에러 콜백
        /// </summary>
        void OnErrorCallback(string error)
        {
            OnError?.Invoke(error);
        }
        
        /// <summary>
        /// 에이전트 설정
        /// </summary>
        public void SetAgent(Transform agentTransform)
        {
            agent = agentTransform;
        }
        
        /// <summary>
        /// 목표 설정
        /// </summary>
        public void SetTarget(Transform targetTransform)
        {
            target = targetTransform;
        }
        
        /// <summary>
        /// 특정 지점으로 이동
        /// </summary>
        public void MoveToPosition(Vector3 position)
        {
            if (agent == null) return;
            
            bool success = navComponent.FindPath(agent.position, position);
            
            if (success)
            {
                currentPath = navComponent.CurrentPath;
                currentPathIndex = 0;
                isMoving = moveAgent;
                
                Debug.Log($"지정된 위치로 이동 시작: {position}");
            }
            else
            {
                Debug.LogWarning($"지정된 위치로 경로를 찾을 수 없습니다: {position}");
            }
        }
        
        /// <summary>
        /// 경로 초기화
        /// </summary>
        public void ClearPath()
        {
            currentPath = null;
            currentPathIndex = 0;
            isMoving = false;
        }
        
        /// <summary>
        /// NavMesh 정보 출력
        /// </summary>
        public void PrintNavMeshInfo()
        {
            if (navComponent.IsNavMeshLoaded)
            {
                Debug.Log($"NavMesh 정보:");
                Debug.Log($"- 폴리곤 수: {navComponent.PolyCount}");
                Debug.Log($"- 정점 수: {navComponent.VertexCount}");
                Debug.Log($"- 경로 길이: {navComponent.PathLength:F2}");
            }
            else
            {
                Debug.Log("NavMesh가 로드되지 않았습니다.");
            }
        }
    }
} 