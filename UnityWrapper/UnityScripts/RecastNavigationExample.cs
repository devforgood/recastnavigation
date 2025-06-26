using UnityEngine;
using RecastNavigation;

/// <summary>
/// RecastNavigation 사용 예제
/// </summary>
public class RecastNavigationExample : MonoBehaviour
{
    [Header("NavMesh 빌드 설정")]
    [SerializeField] private MeshFilter targetMeshFilter;
    [SerializeField] private bool useHighQuality = false;
    [SerializeField] private bool useLowQuality = false;
    
    [Header("경로 찾기 설정")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private LineRenderer pathRenderer;
    
    [Header("디버그 정보")]
    [SerializeField] private bool showDebugInfo = true;
    
    private byte[] navMeshData;
    private bool isNavMeshLoaded = false;
    
    void Start()
    {
        // RecastNavigation 초기화
        if (!RecastNavigationWrapper.Initialize())
        {
            Debug.LogError("RecastNavigation 초기화에 실패했습니다.");
            return;
        }
        
        Debug.Log("RecastNavigation이 성공적으로 초기화되었습니다.");
        
        // NavMesh 빌드
        BuildNavMesh();
    }
    
    void OnDestroy()
    {
        // RecastNavigation 정리
        RecastNavigationWrapper.Cleanup();
    }
    
    void Update()
    {
        // 경로 찾기 테스트 (스페이스바 누를 때)
        if (Input.GetKeyDown(KeyCode.Space) && isNavMeshLoaded && startPoint && endPoint)
        {
            FindPath();
        }
        
        // NavMesh 재빌드 (R 키 누를 때)
        if (Input.GetKeyDown(KeyCode.R))
        {
            BuildNavMesh();
        }
    }
    
    /// <summary>
    /// NavMesh 빌드
    /// </summary>
    private void BuildNavMesh()
    {
        if (targetMeshFilter == null || targetMeshFilter.mesh == null)
        {
            Debug.LogError("타겟 메시가 설정되지 않았습니다.");
            return;
        }
        
        Debug.Log("NavMesh 빌드를 시작합니다...");
        
        // 빌드 설정 선택
        RecastNavigationWrapper.NavMeshBuildSettings settings;
        if (useHighQuality)
        {
            settings = NavMeshBuildSettingsExtensions.CreateHighQuality();
            Debug.Log("높은 품질 설정으로 NavMesh를 빌드합니다.");
        }
        else if (useLowQuality)
        {
            settings = NavMeshBuildSettingsExtensions.CreateLowQuality();
            Debug.Log("낮은 품질 설정으로 NavMesh를 빌드합니다.");
        }
        else
        {
            settings = NavMeshBuildSettingsExtensions.CreateDefault();
            Debug.Log("기본 설정으로 NavMesh를 빌드합니다.");
        }
        
        // NavMesh 빌드
        var result = RecastNavigationWrapper.BuildNavMesh(targetMeshFilter.mesh, settings);
        
        if (result.Success)
        {
            navMeshData = result.NavMeshData;
            Debug.Log($"NavMesh 빌드 성공! 데이터 크기: {navMeshData.Length} bytes");
            
            // NavMesh 로드
            if (RecastNavigationWrapper.LoadNavMesh(navMeshData))
            {
                isNavMeshLoaded = true;
                Debug.Log("NavMesh가 성공적으로 로드되었습니다.");
                
                // 디버그 정보 출력
                if (showDebugInfo)
                {
                    int polyCount = RecastNavigationWrapper.GetPolyCount();
                    int vertexCount = RecastNavigationWrapper.GetVertexCount();
                    Debug.Log($"NavMesh 정보 - 폴리곤: {polyCount}, 정점: {vertexCount}");
                }
            }
            else
            {
                Debug.LogError("NavMesh 로드에 실패했습니다.");
            }
        }
        else
        {
            Debug.LogError($"NavMesh 빌드 실패: {result.ErrorMessage}");
        }
    }
    
    /// <summary>
    /// 경로 찾기
    /// </summary>
    private void FindPath()
    {
        if (!isNavMeshLoaded)
        {
            Debug.LogWarning("NavMesh가 로드되지 않았습니다.");
            return;
        }
        
        Vector3 start = startPoint.position;
        Vector3 end = endPoint.position;
        
        Debug.Log($"경로 찾기를 시작합니다. 시작점: {start}, 끝점: {end}");
        
        var result = RecastNavigationWrapper.FindPath(start, end);
        
        if (result.Success)
        {
            Debug.Log($"경로 찾기 성공! 경로 포인트 수: {result.PathPoints.Length}");
            
            // 경로 시각화
            VisualizePath(result.PathPoints);
            
            // 경로 정보 출력
            if (showDebugInfo)
            {
                float pathLength = CalculatePathLength(result.PathPoints);
                Debug.Log($"경로 길이: {pathLength:F2} units");
            }
        }
        else
        {
            Debug.LogError($"경로 찾기 실패: {result.ErrorMessage}");
            
            // 경로 렌더러 초기화
            if (pathRenderer != null)
            {
                pathRenderer.positionCount = 0;
            }
        }
    }
    
    /// <summary>
    /// 경로 시각화
    /// </summary>
    private void VisualizePath(Vector3[] pathPoints)
    {
        if (pathRenderer == null)
        {
            Debug.LogWarning("LineRenderer가 설정되지 않았습니다.");
            return;
        }
        
        pathRenderer.positionCount = pathPoints.Length;
        pathRenderer.SetPositions(pathPoints);
        
        // 경로 색상 설정
        pathRenderer.startColor = Color.green;
        pathRenderer.endColor = Color.red;
        pathRenderer.startWidth = 0.1f;
        pathRenderer.endWidth = 0.1f;
    }
    
    /// <summary>
    /// 경로 길이 계산
    /// </summary>
    private float CalculatePathLength(Vector3[] pathPoints)
    {
        if (pathPoints.Length < 2)
            return 0f;
        
        float totalLength = 0f;
        for (int i = 1; i < pathPoints.Length; i++)
        {
            totalLength += Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
        }
        
        return totalLength;
    }
    
    /// <summary>
    /// NavMesh 데이터 저장
    /// </summary>
    public void SaveNavMeshData(string filePath)
    {
        if (navMeshData == null || navMeshData.Length == 0)
        {
            Debug.LogError("저장할 NavMesh 데이터가 없습니다.");
            return;
        }
        
        try
        {
            System.IO.File.WriteAllBytes(filePath, navMeshData);
            Debug.Log($"NavMesh 데이터가 저장되었습니다: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NavMesh 데이터 저장 실패: {e.Message}");
        }
    }
    
    /// <summary>
    /// NavMesh 데이터 로드
    /// </summary>
    public void LoadNavMeshData(string filePath)
    {
        try
        {
            navMeshData = System.IO.File.ReadAllBytes(filePath);
            
            if (RecastNavigationWrapper.LoadNavMesh(navMeshData))
            {
                isNavMeshLoaded = true;
                Debug.Log($"NavMesh 데이터가 로드되었습니다: {filePath}");
            }
            else
            {
                Debug.LogError("NavMesh 데이터 로드에 실패했습니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NavMesh 데이터 로드 실패: {e.Message}");
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo)
            return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("RecastNavigation 예제", GUI.skin.box);
        
        GUILayout.Label($"NavMesh 로드됨: {isNavMeshLoaded}");
        
        if (isNavMeshLoaded)
        {
            int polyCount = RecastNavigationWrapper.GetPolyCount();
            int vertexCount = RecastNavigationWrapper.GetVertexCount();
            GUILayout.Label($"폴리곤: {polyCount}");
            GUILayout.Label($"정점: {vertexCount}");
        }
        
        GUILayout.Space(10);
        GUILayout.Label("조작법:");
        GUILayout.Label("스페이스바: 경로 찾기");
        GUILayout.Label("R: NavMesh 재빌드");
        
        GUILayout.EndArea();
    }
} 