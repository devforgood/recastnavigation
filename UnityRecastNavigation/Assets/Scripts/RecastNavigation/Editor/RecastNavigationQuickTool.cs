using UnityEngine;
using UnityEditor;
using RecastNavigation;
using System.IO;
using System.Collections.Generic;

namespace RecastNavigation.Editor
{
    /// <summary>
    /// RecastNavigation 빠른 도구
    /// </summary>
    public class RecastNavigationQuickTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool showBasicTools = true;
        private bool showAdvancedTools = false;
        private bool showDebugTools = false;
        
        // 상태
        private bool isInitialized = false;
        private bool isNavMeshLoaded = false;
        private string statusMessage = "초기화되지 않음";
        
        // 빠른 설정
        private NavMeshBuildSettings quickSettings;
        private string quickSavePath = "Assets/NavMeshData/";
        
        // 빌드 설정
        private NavMeshBuildSettings buildSettings;
        private bool autoTransformCoordinates = true;
        private CoordinateSystem coordinateSystem = CoordinateSystem.LeftHanded;
        private YAxisRotation yAxisRotation = YAxisRotation.None;

        // 경로 찾기 설정
        private Transform startPoint;
        private Transform endPoint;
        private bool autoFindPath = false;
        private float pathUpdateInterval = 0.5f;

        // 디버그 설정
        private bool enableDebugDraw = false;
        private bool enableDebugLogging = false;
        private bool showNavMeshGizmo = true;

        // 배치 처리 설정
        private List<GameObject> selectedObjects = new List<GameObject>();
        private bool processSelectedObjects = false;
        
        [MenuItem("Tools/RecastNavigation/Quick Tool")]
        public static void ShowWindow()
        {
            GetWindow<RecastNavigationQuickTool>("RecastNavigation Quick Tool");
        }
        
        void OnEnable()
        {
            // 기본 설정 초기화
            quickSettings = NavMeshBuildSettingsExtensions.CreateDefault();
            
            // 상태 확인
            CheckStatus();

            buildSettings = NavMeshBuildSettingsExtensions.CreateDefault();
            UpdateSelectedObjects();
        }
        
        void OnSelectionChange()
        {
            UpdateSelectedObjects();
        }
        
        void UpdateSelectedObjects()
        {
            selectedObjects.Clear();
            GameObject[] selected = Selection.gameObjects;
            foreach (GameObject obj in selected)
            {
                if (obj.GetComponent<MeshFilter>() != null || obj.GetComponent<MeshRenderer>() != null)
                {
                    selectedObjects.Add(obj);
                }
            }
        }
        
        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // 헤더
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("RecastNavigation Quick Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 상태 표시
            DrawStatusSection();
            
            // 기본 도구
            DrawBasicToolsSection();
            
            // 고급 도구
            DrawAdvancedToolsSection();
            
            // 디버그 도구
            DrawDebugToolsSection();
            
            EditorGUILayout.EndScrollView();
        }
        
        void DrawStatusSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("상태", EditorStyles.boldLabel);
            
            // 상태 메시지
            EditorGUILayout.LabelField("상태:", statusMessage);
            
            // 상태 표시
            EditorGUILayout.BeginHorizontal();
            
            // 초기화 상태
            GUI.color = isInitialized ? Color.green : Color.red;
            EditorGUILayout.LabelField("초기화됨", isInitialized ? "✓" : "✗");
            
            // NavMesh 상태
            GUI.color = isNavMeshLoaded ? Color.green : Color.red;
            EditorGUILayout.LabelField("NavMesh 로드됨", isNavMeshLoaded ? "✓" : "✗");
            
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawBasicToolsSection()
        {
            showBasicTools = EditorGUILayout.Foldout(showBasicTools, "기본 도구", true);
            if (showBasicTools)
            {
                EditorGUILayout.BeginVertical("box");
                
                // 초기화/정리
                EditorGUILayout.LabelField("초기화", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("초기화", GUILayout.Height(25)))
                {
                    InitializeRecastNavigation();
                }
                
                if (GUILayout.Button("정리", GUILayout.Height(25)))
                {
                    CleanupRecastNavigation();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                // 빠른 NavMesh 빌드
                EditorGUILayout.LabelField("빠른 NavMesh 빌드", EditorStyles.boldLabel);
                
                // 품질 프리셋
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("빠른 빌드 (낮은 품질)"))
                {
                    BuildNavMeshWithPreset(NavMeshBuildSettingsExtensions.CreateLowQuality());
                }
                
                if (GUILayout.Button("기본 빌드"))
                {
                    BuildNavMeshWithPreset(NavMeshBuildSettingsExtensions.CreateDefault());
                }
                
                if (GUILayout.Button("고품질 빌드"))
                {
                    BuildNavMeshWithPreset(NavMeshBuildSettingsExtensions.CreateHighQuality());
                }
                
                EditorGUILayout.EndHorizontal();
                
                // RecastDemo 검증된 프리셋들 추가
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("RecastDemo 검증된 설정", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("🎯 RecastDemo 검증된 설정", GUILayout.Height(30)))
                {
                    BuildNavMeshWithPreset(NavMeshBuildSettingsExtensions.CreateRecastDemoVerified());
                }
                GUI.backgroundColor = Color.white;
                
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("🛡️ RecastDemo 보수적 설정", GUILayout.Height(30)))
                {
                    BuildNavMeshWithPreset(NavMeshBuildSettingsExtensions.CreateRecastDemoConservative());
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                // 선택된 오브젝트에서 빌드
                EditorGUILayout.LabelField("선택된 오브젝트", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("선택된 오브젝트에서 빌드", GUILayout.Height(30)))
                {
                    BuildNavMeshFromSelection();
                }
                
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("🔧 RecastDemo 설정으로 빌드 (권장)", GUILayout.Height(30)))
                {
                    BuildNavMeshFromRecastDemoSettings();
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.Space(3);
                
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("🧪 초간단 테스트 메시로 진단", GUILayout.Height(25)))
                {
                    TestWithSimpleTriangle();
                }
                GUI.backgroundColor = Color.white;
                
                if (GUILayout.Button("선택된 오브젝트 정보"))
                {
                    ShowSelectionInfo();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
        }
        
        void DrawAdvancedToolsSection()
        {
            showAdvancedTools = EditorGUILayout.Foldout(showAdvancedTools, "고급 도구", true);
            if (showAdvancedTools)
            {
                EditorGUILayout.BeginVertical("box");
                
                // NavMesh 저장/로드
                EditorGUILayout.LabelField("NavMesh 저장/로드", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("저장 경로:", GUILayout.Width(80));
                quickSavePath = EditorGUILayout.TextField(quickSavePath);
                if (GUILayout.Button("찾아보기", GUILayout.Width(60)))
                {
                    string newPath = EditorUtility.SaveFolderPanel("NavMesh 저장 경로 선택", quickSavePath, "");
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        quickSavePath = newPath;
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("NavMesh 저장"))
                {
                    SaveNavMeshToFile();
                }
                
                if (GUILayout.Button("NavMesh 로드"))
                {
                    LoadNavMeshFromFile();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                // 경로 찾기 테스트
                EditorGUILayout.LabelField("경로 찾기 테스트", EditorStyles.boldLabel);
                
                if (GUILayout.Button("간단한 경로 찾기 테스트"))
                {
                    RunPathfindingTest();
                }
                
                if (GUILayout.Button("랜덤 경로 찾기 테스트"))
                {
                    RunRandomPathfindingTest();
                }
                
                EditorGUILayout.Space();
                
                // 성능 테스트
                EditorGUILayout.LabelField("성능 테스트", EditorStyles.boldLabel);
                
                if (GUILayout.Button("NavMesh 빌드 성능 테스트"))
                {
                    RunBuildPerformanceTest();
                }
                
                if (GUILayout.Button("경로 찾기 성능 테스트"))
                {
                    RunPathfindingPerformanceTest();
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        void DrawDebugToolsSection()
        {
            showDebugTools = EditorGUILayout.Foldout(showDebugTools, "디버그 도구", true);
            if (showDebugTools)
            {
                EditorGUILayout.BeginVertical("box");
                
                // 씬 분석
                EditorGUILayout.LabelField("씬 분석", EditorStyles.boldLabel);
                
                if (GUILayout.Button("씬에서 모든 Mesh 분석"))
                {
                    AnalyzeSceneMeshes();
                }
                
                if (GUILayout.Button("씬에서 모든 Collider 분석"))
                {
                    AnalyzeSceneColliders();
                }
                
                if (GUILayout.Button("씬에서 모든 Terrain 분석"))
                {
                    AnalyzeSceneTerrains();
                }
                
                EditorGUILayout.Space();
                
                // NavMesh 정보
                EditorGUILayout.LabelField("NavMesh 정보", EditorStyles.boldLabel);
                
                if (GUILayout.Button("NavMesh 상세 정보 출력"))
                {
                    PrintNavMeshInfo();
                }
                
                if (GUILayout.Button("NavMesh 통계 출력"))
                {
                    PrintNavMeshStats();
                }
                
                EditorGUILayout.Space();
                
                // 디버그 옵션
                EditorGUILayout.LabelField("디버그 옵션", EditorStyles.boldLabel);
                
                if (GUILayout.Button("모든 디버그 정보 출력"))
                {
                    PrintAllDebugInfo();
                }
                
                if (GUILayout.Button("메모리 사용량 확인"))
                {
                    CheckMemoryUsage();
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        void CheckStatus()
        {
            Debug.Log("=== CheckStatus 시작 ===");
            
            statusMessage = "상태 확인 중...";
            
            try
            {
                // 1. RecastNavigation 초기화 상태 확인
                Debug.Log("1. RecastNavigation 초기화 상태 확인...");
                isInitialized = RecastNavigationWrapper.Initialize();
                Debug.Log($"초기화 상태: {isInitialized}");
                
                if (isInitialized)
                {
                    // 2. NavMesh 로드 상태 확인
                    Debug.Log("2. NavMesh 로드 상태 확인...");
                    
                    try
                    {
                        int polyCount = RecastNavigationWrapper.GetPolyCount();
                        int vertCount = RecastNavigationWrapper.GetVertexCount();
                        
                        isNavMeshLoaded = (polyCount > 0 || vertCount > 0);
                        Debug.Log($"NavMesh 로드 상태: {isNavMeshLoaded}");
                        Debug.Log($"  - 폴리곤 수: {polyCount}");
                        Debug.Log($"  - 정점 수: {vertCount}");
                        
                        if (isNavMeshLoaded)
                        {
                            statusMessage = $"준비됨 (폴리곤: {polyCount}, 정점: {vertCount})";
                        }
                        else
                        {
                            statusMessage = "초기화됨 (NavMesh 없음)";
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"NavMesh 상태 확인 중 오류: {e.Message}");
                        isNavMeshLoaded = false;
                        statusMessage = "초기화됨 (NavMesh 상태 불명)";
                    }
                }
                else
                {
                    isNavMeshLoaded = false;
                    statusMessage = "초기화되지 않음";
                    Debug.LogWarning("RecastNavigation이 초기화되지 않았습니다.");
                }
                
                Debug.Log($"최종 상태: 초기화={isInitialized}, NavMesh로드={isNavMeshLoaded}");
                Debug.Log("=== CheckStatus 완료 ===");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"상태 확인 중 오류: {e.Message}");
                Debug.LogError($"스택 트레이스: {e.StackTrace}");
                
                isInitialized = false;
                isNavMeshLoaded = false;
                statusMessage = $"상태 확인 오류: {e.Message}";
            }
        }
        
        void InitializeRecastNavigation()
        {
            if (RecastNavigationWrapper.Initialize())
            {
                isInitialized = true;
                statusMessage = "RecastNavigation이 초기화되었습니다.";
                Debug.Log("RecastNavigation 초기화 성공");
            }
            else
            {
                statusMessage = "RecastNavigation 초기화 실패";
                Debug.LogError("RecastNavigation 초기화 실패");
            }
        }
        
        void CleanupRecastNavigation()
        {
            RecastNavigationWrapper.Cleanup();
            isInitialized = false;
            isNavMeshLoaded = false;
            statusMessage = "RecastNavigation이 정리되었습니다.";
            Debug.Log("RecastNavigation 정리 완료");
        }
        
        void BuildNavMeshWithPreset(NavMeshBuildSettings settings)
        {
            Debug.Log("=== BuildNavMeshWithPreset 시작 ===");
            
            // 1. RecastNavigation 초기화 확인 및 시도
            Debug.Log("1. RecastNavigation 초기화 확인...");
            if (!isInitialized)
            {
                Debug.Log("RecastNavigation이 초기화되지 않았습니다. 자동 초기화를 시도합니다.");
                InitializeRecastNavigation();
                
                if (!isInitialized)
                {
                    string errorMessage = "RecastNavigation 초기화에 실패했습니다.\n\n" +
                                        "가능한 원인:\n" +
                                        "1. UnityWrapper.dll이 Assets/Plugins 폴더에 없음\n" +
                                        "2. DLL이 현재 플랫폼과 호환되지 않음\n" +
                                        "3. Visual C++ Redistributable 미설치\n\n" +
                                        "Setup Guide를 사용하여 DLL을 먼저 설치해주세요.";
                    
                    EditorUtility.DisplayDialog("초기화 실패", errorMessage, "확인");
                    statusMessage = "RecastNavigation 초기화 실패";
                    Debug.LogError("RecastNavigation 자동 초기화 실패!");
                    return;
                }
                else
                {
                    Debug.Log("RecastNavigation 자동 초기화 성공!");
                }
            }
            else
            {
                Debug.Log("RecastNavigation이 이미 초기화되어 있습니다.");
            }
            
            // 2. 씬의 모든 Mesh 수집
            Debug.Log("2. 씬의 Mesh 수집 중...");
            MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
            Debug.Log($"발견된 MeshRenderer 수: {renderers.Length}");
            
            if (renderers.Length == 0)
            {
                statusMessage = "씬에서 Mesh를 찾을 수 없습니다.";
                Debug.LogWarning("씬에서 MeshRenderer를 찾을 수 없습니다. Mesh가 있는 오브젝트가 있는지 확인하세요.");
                return;
            }
            
            // 각 렌더러 정보 출력
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                var meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    var mesh = meshFilter.sharedMesh;
                    Debug.Log($"  - {renderer.name}: {mesh.vertexCount} 정점, {mesh.triangles.Length/3} 삼각형");
                }
                else
                {
                    Debug.LogWarning($"  - {renderer.name}: MeshFilter 또는 Mesh가 없음");
                }
            }
            
            // 3. 메시 합치기
            Debug.Log("3. 메시 합치는 중...");
            Mesh combinedMesh = CombineAllMeshes(renderers);
            
            if (combinedMesh == null)
            {
                statusMessage = "메시 합치기 실패";
                Debug.LogError("메시 합치기 실패!");
                return;
            }
            
            Debug.Log($"합쳐진 메시: {combinedMesh.vertexCount} 정점, {combinedMesh.triangles.Length/3} 삼각형");
            
            // 4. NavMesh 빌드
            Debug.Log("4. NavMesh 빌드 시작...");
            Debug.Log($"빌드 설정: cellSize={settings.cellSize}, cellHeight={settings.cellHeight}");
            Debug.Log($"빌드 설정: walkableHeight={settings.walkableHeight}, walkableRadius={settings.walkableRadius}");
            
            var result = RecastNavigationWrapper.BuildNavMesh(combinedMesh, settings);
            
            if (result.Success)
            {
                Debug.Log($"NavMesh 빌드 성공! 데이터 크기: {result.NavMeshData?.Length ?? 0} 바이트");
                
                // 5. NavMesh 로드
                Debug.Log("5. NavMesh 로드 시도...");
                if (result.NavMeshData != null && result.NavMeshData.Length > 0)
                {
                    if (RecastNavigationWrapper.LoadNavMesh(result.NavMeshData))
                    {
                        isNavMeshLoaded = true;
                        int polyCount = RecastNavigationWrapper.GetPolyCount();
                        int vertCount = RecastNavigationWrapper.GetVertexCount();
                        
                        statusMessage = $"NavMesh 빌드 성공! (프리셋 사용)";
                        Debug.Log($"NavMesh 로드 성공! 폴리곤: {polyCount}, 정점: {vertCount}");
                        Debug.Log("=== BuildNavMeshWithPreset 완료 ===");
                    }
                    else
                    {
                        statusMessage = "NavMesh 로드 실패";
                        Debug.LogError("NavMesh 로드 실패! LoadNavMesh 함수가 false를 반환했습니다.");
                    }
                }
                else
                {
                    statusMessage = "NavMesh 데이터가 비어있음";
                    Debug.LogError("NavMesh 빌드는 성공했지만 데이터가 비어있습니다.");
                }
            }
            else
            {
                statusMessage = $"NavMesh 빌드 실패: {result.ErrorMessage}";
                Debug.LogError($"NavMesh 빌드 실패: {result.ErrorMessage}");
            }
        }
        
        void BuildNavMeshFromSelection()
        {
            Debug.Log("=== BuildNavMeshFromSelection 시작 ===");
            
            // 0. RecastNavigation 초기화 확인 및 시도
            Debug.Log("0. RecastNavigation 초기화 확인...");
            if (!isInitialized)
            {
                Debug.Log("RecastNavigation이 초기화되지 않았습니다. 자동 초기화를 시도합니다.");
                InitializeRecastNavigation();
                
                if (!isInitialized)
                {
                    string errorMessage = "RecastNavigation 초기화에 실패했습니다.\n\n" +
                                        "가능한 원인:\n" +
                                        "1. UnityWrapper.dll이 Assets/Plugins 폴더에 없음\n" +
                                        "2. DLL이 현재 플랫폼과 호환되지 않음\n" +
                                        "3. Visual C++ Redistributable 미설치\n\n" +
                                        "Setup Guide를 사용하여 DLL을 먼저 설치해주세요.";
                    
                    EditorUtility.DisplayDialog("초기화 실패", errorMessage, "확인");
                    Debug.LogError("RecastNavigation 자동 초기화 실패!");
                    return;
                }
                else
                {
                    Debug.Log("RecastNavigation 자동 초기화 성공!");
                }
            }
            else
            {
                Debug.Log("RecastNavigation이 이미 초기화되어 있습니다.");
            }
            
            // 1. 선택된 오브젝트 확인
            Debug.Log("1. 선택된 오브젝트 확인...");
            Debug.Log($"selectedObjects.Count: {selectedObjects.Count}");
            Debug.Log($"Selection.gameObjects.Length: {Selection.gameObjects.Length}");
            
            if (selectedObjects.Count == 0)
            {
                Debug.LogWarning("처리할 메시 오브젝트가 선택되지 않았습니다.");
                Debug.Log("선택 조건: MeshFilter 또는 MeshRenderer 컴포넌트가 있는 오브젝트");
                
                // 현재 선택된 모든 오브젝트 정보 출력
                var allSelected = Selection.gameObjects;
                Debug.Log($"현재 선택된 모든 오브젝트 ({allSelected.Length}개):");
                foreach (var obj in allSelected)
                {
                    var meshFilter = obj.GetComponent<MeshFilter>();
                    var meshRenderer = obj.GetComponent<MeshRenderer>();
                    Debug.Log($"  - {obj.name}: MeshFilter={meshFilter != null}, MeshRenderer={meshRenderer != null}");
                }
                
                EditorUtility.DisplayDialog("오류", "처리할 메시 오브젝트가 선택되지 않았습니다.\n\nMeshFilter 또는 MeshRenderer 컴포넌트가 있는 오브젝트를 선택해주세요.", "확인");
                return;
            }

            // 2. RecastNavigationComponent 확인/생성
            Debug.Log("2. RecastNavigationComponent 확인...");
            RecastNavigationComponent navComponent = FindObjectOfType<RecastNavigationComponent>();
            if (navComponent == null)
            {
                Debug.Log("RecastNavigationComponent가 없어서 새로 생성합니다.");
                navComponent = CreateRecastNavigationComponent();
                if (navComponent == null)
                {
                    Debug.LogError("RecastNavigationComponent 생성 실패!");
                    EditorUtility.DisplayDialog("오류", "RecastNavigationComponent 생성에 실패했습니다.", "확인");
                    return;
                }
            }
            else
            {
                Debug.Log($"기존 RecastNavigationComponent 발견: {navComponent.gameObject.name}");
            }

            try
            {
                EditorUtility.DisplayProgressBar("NavMesh 빌드", "선택된 오브젝트에서 NavMesh 빌드 중...", 0f);
                
                // 3. 선택된 오브젝트들의 메시를 합치기
                Debug.Log("3. 선택된 오브젝트들의 메시 합치는 중...");
                List<Vector3> allVertices = new List<Vector3>();
                List<int> allIndices = new List<int>();

                for (int i = 0; i < selectedObjects.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("NavMesh 빌드", $"오브젝트 처리 중... ({i + 1}/{selectedObjects.Count})", (float)i / selectedObjects.Count);
                    
                    GameObject obj = selectedObjects[i];
                    MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                    
                    Debug.Log($"  처리 중: {obj.name}");
                    
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        Mesh mesh = meshFilter.sharedMesh;
                        Vector3[] vertices = mesh.vertices;
                        int[] indices = mesh.triangles;
                        
                        Debug.Log($"    - 메시 정보: {vertices.Length} 정점, {indices.Length/3} 삼각형");

                        // 월드 좌표로 변환
                        Transform transform = obj.transform;
                        for (int j = 0; j < vertices.Length; j++)
                        {
                            vertices[j] = transform.TransformPoint(vertices[j]);
                        }

                        // 인덱스 조정
                        int vertexOffset = allVertices.Count;
                        for (int j = 0; j < indices.Length; j++)
                        {
                            indices[j] += vertexOffset;
                        }

                        allVertices.AddRange(vertices);
                        allIndices.AddRange(indices);
                        
                        Debug.Log($"    - 누적: {allVertices.Count} 정점, {allIndices.Count/3} 삼각형");
                    }
                    else
                    {
                        Debug.LogWarning($"    - {obj.name}: MeshFilter 또는 Mesh가 없음");
                    }
                }

                Debug.Log($"최종 합쳐진 메시: {allVertices.Count} 정점, {allIndices.Count/3} 삼각형");

                if (allVertices.Count == 0 || allIndices.Count == 0)
                {
                    EditorUtility.ClearProgressBar();
                    Debug.LogError("유효한 메시 데이터가 없습니다!");
                    EditorUtility.DisplayDialog("오류", "유효한 메시 데이터가 없습니다.\n\n선택된 오브젝트에 유효한 Mesh가 있는지 확인해주세요.", "확인");
                    return;
                }

                // 4. NavMesh 빌드
                Debug.Log("4. NavMesh 빌드 시작...");
                
                Vector3[] vertexArray = allVertices.ToArray();
                int[] indexArray = allIndices.ToArray();
                
                // 먼저 일반 빌드 시도
                bool success = navComponent.BuildNavMesh(vertexArray, indexArray);
                
                // 실패시 권장 설정으로 재시도
                if (!success)
                {
                    Debug.LogWarning("일반 설정으로 빌드 실패. 권장 설정으로 재시도합니다...");
                    success = navComponent.BuildNavMeshWithRecommendedSettings(vertexArray, indexArray);
                    
                    if (success)
                    {
                        Debug.Log("✓ 권장 설정으로 NavMesh 빌드 성공!");
                    }
                }
                
                EditorUtility.ClearProgressBar();
                
                if (success)
                {
                    isNavMeshLoaded = true;
                    statusMessage = "선택된 오브젝트에서 NavMesh 빌드 성공";
                    
                    EditorUtility.DisplayDialog("성공", "선택된 오브젝트에서 NavMesh 빌드가 완료되었습니다.", "확인");
                    Debug.Log("선택된 오브젝트에서 NavMesh 빌드 완료!");
                    Debug.Log("=== BuildNavMeshFromSelection 완료 ===");
                }
                else
                {
                    statusMessage = "선택된 오브젝트에서 NavMesh 빌드 실패";
                    
                    EditorUtility.DisplayDialog("오류", "선택된 오브젝트에서 NavMesh 빌드에 실패했습니다.\n\nConsole 로그를 확인해주세요.", "확인");
                    Debug.LogError("선택된 오브젝트에서 NavMesh 빌드 실패!");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                statusMessage = $"NavMesh 빌드 중 오류: {e.Message}";
                
                EditorUtility.DisplayDialog("오류", $"NavMesh 빌드 중 오류가 발생했습니다:\n\n{e.Message}", "확인");
                Debug.LogError($"NavMesh 빌드 중 오류: {e.Message}");
                Debug.LogError($"스택 트레이스: {e.StackTrace}");
            }
        }
        
        void BuildNavMeshFromRecastDemoSettings()
        {
            Debug.Log("=== RecastDemo 검증된 설정으로 NavMesh 빌드 시작 ===");
            
            // 0. RecastNavigation 초기화 확인
            if (!isInitialized)
            {
                Debug.Log("RecastNavigation이 초기화되지 않았습니다. 자동 초기화를 시도합니다.");
                InitializeRecastNavigation();
                
                if (!isInitialized)
                {
                    EditorUtility.DisplayDialog("초기화 실패", "RecastNavigation 초기화에 실패했습니다.\nSetup Guide를 사용하여 DLL을 먼저 설치해주세요.", "확인");
                    Debug.LogError("RecastNavigation 자동 초기화 실패!");
                    return;
                }
            }
            
            // 1. 선택된 오브젝트 확인
            if (selectedObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("오류", "처리할 메시 오브젝트가 선택되지 않았습니다.\n\nMeshFilter 또는 MeshRenderer 컴포넌트가 있는 오브젝트를 선택해주세요.", "확인");
                return;
            }

            // 2. RecastNavigationComponent 확인/생성
            RecastNavigationComponent navComponent = FindObjectOfType<RecastNavigationComponent>();
            if (navComponent == null)
            {
                navComponent = CreateRecastNavigationComponent();
                if (navComponent == null)
                {
                    EditorUtility.DisplayDialog("오류", "RecastNavigationComponent 생성에 실패했습니다.", "확인");
                    return;
                }
            }

            try
            {
                EditorUtility.DisplayProgressBar("RecastDemo 설정 NavMesh 빌드", "메시 데이터 수집 중...", 0f);
                
                // 3. 메시 데이터 수집
                List<Vector3> allVertices = new List<Vector3>();
                List<int> allIndices = new List<int>();

                for (int i = 0; i < selectedObjects.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("RecastDemo 설정 NavMesh 빌드", $"오브젝트 처리 중... ({i + 1}/{selectedObjects.Count})", (float)i / selectedObjects.Count);
                    
                    GameObject obj = selectedObjects[i];
                    MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                    
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        Mesh mesh = meshFilter.sharedMesh;
                        Vector3[] vertices = mesh.vertices;
                        int[] indices = mesh.triangles;

                        // 월드 좌표로 변환
                        Transform transform = obj.transform;
                        for (int j = 0; j < vertices.Length; j++)
                        {
                            vertices[j] = transform.TransformPoint(vertices[j]);
                        }

                        // 인덱스 조정
                        int vertexOffset = allVertices.Count;
                        for (int j = 0; j < indices.Length; j++)
                        {
                            indices[j] += vertexOffset;
                        }

                        allVertices.AddRange(vertices);
                        allIndices.AddRange(indices);
                    }
                }

                if (allVertices.Count == 0 || allIndices.Count == 0)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("오류", "유효한 메시 데이터가 없습니다.\n\n선택된 오브젝트에 유효한 Mesh가 있는지 확인해주세요.", "확인");
                    return;
                }

                // 4. RecastDemo 검증된 설정으로 NavMesh 빌드
                EditorUtility.DisplayProgressBar("RecastDemo 설정 NavMesh 빌드", "RecastDemo 검증된 설정으로 NavMesh 빌드 중...", 0.8f);
                
                Vector3[] vertexArray = allVertices.ToArray();
                int[] indexArray = allIndices.ToArray();
                
                // RecastDemo 검증된 설정 사용
                NavMeshBuildSettings recastDemoSettings = NavMeshBuildSettingsExtensions.CreateRecastDemoVerified();
                bool success = navComponent.BuildNavMesh(vertexArray, indexArray, recastDemoSettings);
                
                EditorUtility.ClearProgressBar();
                
                if (success)
                {
                    isNavMeshLoaded = true;
                    statusMessage = "RecastDemo 설정으로 NavMesh 빌드 성공";
                    
                    EditorUtility.DisplayDialog("성공", "RecastDemo 검증된 설정으로 NavMesh 빌드가 완료되었습니다!\n\n✓ RecastDemo와 동일한 매개변수 사용\n✓ 검증된 안정적인 설정\n✓ 최적화된 품질과 성능", "확인");
                    Debug.Log("🎯 RecastDemo 검증된 설정으로 NavMesh 빌드 완료!");
                    Debug.Log("사용된 설정:");
                    Debug.Log($"  - cellSize: {recastDemoSettings.cellSize}");
                    Debug.Log($"  - cellHeight: {recastDemoSettings.cellHeight}");
                    Debug.Log($"  - walkableRadius: {recastDemoSettings.walkableRadius}");
                    Debug.Log($"  - minRegionArea: {recastDemoSettings.minRegionArea}");
                    Debug.Log($"  - mergeRegionArea: {recastDemoSettings.mergeRegionArea}");
                }
                else
                {
                    statusMessage = "RecastDemo 설정으로도 NavMesh 빌드 실패";
                    
                    // 실패 시 보수적 설정 제안
                    bool tryConservative = EditorUtility.DisplayDialog("빌드 실패", 
                        "RecastDemo 검증된 설정으로도 NavMesh 빌드에 실패했습니다.\n\n보수적 설정으로 재시도하시겠습니까?\n(더 작은 메시에 적합한 설정)", 
                        "보수적 설정으로 재시도", 
                        "취소");
                    
                    if (tryConservative)
                    {
                        EditorUtility.DisplayProgressBar("보수적 설정 빌드", "보수적 설정으로 재시도 중...", 0.9f);
                        
                        NavMeshBuildSettings conservativeSettings = NavMeshBuildSettingsExtensions.CreateRecastDemoConservative();
                        bool conservativeSuccess = navComponent.BuildNavMesh(vertexArray, indexArray, conservativeSettings);
                        
                        EditorUtility.ClearProgressBar();
                        
                        if (conservativeSuccess)
                        {
                            isNavMeshLoaded = true;
                            statusMessage = "보수적 설정으로 NavMesh 빌드 성공";
                            
                            EditorUtility.DisplayDialog("성공", "보수적 설정으로 NavMesh 빌드가 완료되었습니다!\n\n✓ 작은 메시에 최적화된 설정\n✓ erosion 문제 해결\n✓ 더 세밀한 NavMesh", "확인");
                            Debug.Log("🛡️ 보수적 설정으로 NavMesh 빌드 완료!");
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("오류", "보수적 설정으로도 NavMesh 빌드에 실패했습니다.\n\n메시가 너무 작거나 복잡할 수 있습니다.\nConsole 로그를 확인해주세요.", "확인");
                            Debug.LogError("보수적 설정으로도 NavMesh 빌드 실패!");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                statusMessage = $"RecastDemo 설정 빌드 중 오류: {e.Message}";
                
                EditorUtility.DisplayDialog("오류", $"RecastDemo 설정 빌드 중 오류가 발생했습니다:\n\n{e.Message}", "확인");
                Debug.LogError($"RecastDemo 설정 빌드 중 오류: {e.Message}");
            }
            
            Debug.Log("=== RecastDemo 검증된 설정으로 NavMesh 빌드 완료 ===");
        }
        
        void TestWithSimpleTriangle()
        {
            Debug.Log("=== 🧪 초간단 테스트 메시 진단 시작 ===");
            
            // 초기화 확인
            if (!isInitialized)
            {
                InitializeRecastNavigation();
                if (!isInitialized)
                {
                    EditorUtility.DisplayDialog("오류", "RecastNavigation 초기화 실패", "확인");
                    return;
                }
            }
            
            // RecastNavigationComponent 확인/생성
            RecastNavigationComponent navComponent = FindObjectOfType<RecastNavigationComponent>();
            if (navComponent == null)
            {
                navComponent = CreateRecastNavigationComponent();
                if (navComponent == null)
                {
                    EditorUtility.DisplayDialog("오류", "RecastNavigationComponent 생성 실패", "확인");
                    return;
                }
            }
            
            // 초간단 테스트 메시 생성: 거대한 삼각형 1개
            Vector3[] testVertices = new Vector3[]
            {
                new Vector3(-50f, 0f, -50f),  // 좌하
                new Vector3(50f, 0f, -50f),   // 우하  
                new Vector3(0f, 0f, 50f)      // 상중앙
            };
            
            int[] testIndices = new int[] { 0, 1, 2 };
            
            Debug.Log("테스트 메시 정보:");
            Debug.Log($"  정점 3개: {testVertices[0]}, {testVertices[1]}, {testVertices[2]}");
            Debug.Log($"  삼각형 1개: 인덱스 {testIndices[0]}-{testIndices[1]}-{testIndices[2]}");
            Debug.Log($"  면적: 약 {100 * 100 / 2}m²");
            
            try
            {
                EditorUtility.DisplayProgressBar("초간단 테스트", "테스트 메시로 NavMesh 빌드 중...", 0.5f);
                
                // 1차: 권장 설정으로 빌드
                Debug.Log("=== 1차 테스트: 권장 설정 ===");
                bool success1 = navComponent.BuildNavMeshWithRecommendedSettings(testVertices, testIndices);
                
                if (!success1)
                {
                    Debug.LogError("1차 테스트 실패: 권장 설정으로도 실패");
                    
                    // 2차: 수동 최적 설정으로 빌드  
                    Debug.Log("=== 2차 테스트: 수동 최적 설정 ===");
                    
                    var manualSettings = new NavMeshBuildSettings
                    {
                        cellSize = 1.0f,           // 큰 cellSize
                        cellHeight = 0.2f,
                        walkableSlopeAngle = 45.0f,
                        walkableHeight = 2.0f,
                        walkableRadius = 0.6f,
                        walkableClimb = 0.9f,
                        minRegionArea = 0.1f,      // 매우 작은 영역도 허용
                        mergeRegionArea = 0.5f,
                        maxVertsPerPoly = 6,
                        detailSampleDist = 6.0f,
                        detailSampleMaxError = 1.0f,
                        autoTransformCoordinates = false  // 좌표 변환 끄기
                    };
                    
                    navComponent.UpdateBuildSettings(manualSettings);
                    bool success2 = navComponent.BuildNavMesh(testVertices, testIndices);
                    
                    if (success2)
                    {
                        Debug.Log("✓ 2차 테스트 성공: 수동 설정으로 해결됨!");
                        statusMessage = "초간단 테스트 성공 (수동 설정)";
                    }
                    else
                    {
                        Debug.LogError("❌ 2차 테스트도 실패: 심각한 C++ DLL 문제 의심");
                        statusMessage = "초간단 테스트 실패 - DLL 문제";
                    }
                }
                else
                {
                    Debug.Log("✓ 1차 테스트 성공: 권장 설정으로 해결됨!");
                    statusMessage = "초간단 테스트 성공 (권장 설정)";
                }
                
                EditorUtility.ClearProgressBar();
                
                // 결과 대화상자
                string result = success1 ? "권장 설정으로 성공" : "권장 설정 실패";
                EditorUtility.DisplayDialog("테스트 완료", $"초간단 테스트 메시 결과:\n{result}\n\nConsole 로그를 확인하세요.", "확인");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"초간단 테스트 중 오류: {e.Message}");
                EditorUtility.DisplayDialog("오류", $"테스트 중 오류:\n{e.Message}", "확인");
            }
            
            Debug.Log("=== 🧪 초간단 테스트 메시 진단 완료 ===");
        }
        
        void ShowSelectionInfo()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            Debug.Log($"선택된 오브젝트 수: {selectedObjects.Length}");
            
            foreach (var obj in selectedObjects)
            {
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    Mesh mesh = meshFilter.sharedMesh;
                    Debug.Log($"- {obj.name}: {mesh.vertexCount} 정점, {mesh.triangles.Length / 3} 삼각형");
                }
                else
                {
                    Debug.Log($"- {obj.name}: Mesh 없음");
                }
            }
        }
        
        void SaveNavMeshToFile()
        {
            if (!isNavMeshLoaded)
            {
                Debug.LogWarning("저장할 NavMesh가 없습니다.");
                return;
            }
            
            // 디렉토리 생성
            if (!Directory.Exists(quickSavePath))
            {
                Directory.CreateDirectory(quickSavePath);
            }
            
            string fileName = $"NavMesh_Quick_{System.DateTime.Now:yyyyMMdd_HHmmss}.bytes";
            string fullPath = Path.Combine(quickSavePath, fileName);
            
            // 실제로는 현재 로드된 NavMesh 데이터를 저장해야 함
            Debug.Log($"NavMesh 저장: {fullPath}");
        }
        
        void LoadNavMeshFromFile()
        {
            Debug.Log("=== LoadNavMeshFromFile 시작 ===");
            
            string filePath = EditorUtility.OpenFilePanel("NavMesh 파일 선택", quickSavePath, "bytes");
            if (!string.IsNullOrEmpty(filePath))
            {
                Debug.Log($"선택된 파일: {filePath}");
                
                try
                {
                    // 1. 파일 존재 확인
                    if (!File.Exists(filePath))
                    {
                        statusMessage = "선택된 파일이 존재하지 않음";
                        Debug.LogError($"파일이 존재하지 않습니다: {filePath}");
                        return;
                    }
                    
                    // 2. 파일 크기 확인
                    FileInfo fileInfo = new FileInfo(filePath);
                    Debug.Log($"파일 크기: {fileInfo.Length} 바이트");
                    
                    if (fileInfo.Length == 0)
                    {
                        statusMessage = "선택된 파일이 비어있음";
                        Debug.LogError("선택된 파일이 비어있습니다.");
                        return;
                    }
                    
                    // 3. 파일 읽기
                    Debug.Log("파일 데이터 읽는 중...");
                    byte[] data = File.ReadAllBytes(filePath);
                    Debug.Log($"읽은 데이터 크기: {data.Length} 바이트");
                    
                    // 4. RecastNavigation 초기화 확인
                    if (!RecastNavigationWrapper.Initialize())
                    {
                        statusMessage = "RecastNavigation 초기화 실패";
                        Debug.LogError("RecastNavigation 초기화 실패!");
                        return;
                    }
                    
                    // 5. NavMesh 로드 시도
                    Debug.Log("NavMesh 로드 시도...");
                    if (RecastNavigationWrapper.LoadNavMesh(data))
                    {
                        isNavMeshLoaded = true;
                        
                        // 로드된 NavMesh 정보 확인
                        int polyCount = RecastNavigationWrapper.GetPolyCount();
                        int vertCount = RecastNavigationWrapper.GetVertexCount();
                        
                        statusMessage = $"NavMesh 로드 성공: {Path.GetFileName(filePath)}";
                        Debug.Log($"NavMesh 로드 성공!");
                        Debug.Log($"  - 파일: {filePath}");
                        Debug.Log($"  - 폴리곤 수: {polyCount}");
                        Debug.Log($"  - 정점 수: {vertCount}");
                        Debug.Log("=== LoadNavMeshFromFile 완료 ===");
                    }
                    else
                    {
                        statusMessage = "NavMesh 로드 실패";
                        Debug.LogError("NavMesh 로드 실패!");
                        Debug.LogError("LoadNavMesh 함수가 false를 반환했습니다.");
                        Debug.LogError("파일이 유효한 NavMesh 데이터인지 확인해주세요.");
                    }
                }
                catch (System.Exception e)
                {
                    statusMessage = $"NavMesh 로드 중 오류: {e.Message}";
                    Debug.LogError($"NavMesh 로드 실패: {e.Message}");
                    Debug.LogError($"스택 트레이스: {e.StackTrace}");
                }
            }
            else
            {
                Debug.Log("파일 선택이 취소되었습니다.");
            }
        }
        
        void RunPathfindingTest()
        {
            if (!isNavMeshLoaded)
            {
                Debug.LogWarning("NavMesh가 로드되지 않았습니다.");
                return;
            }
            
            Vector3 start = Vector3.zero;
            Vector3 end = new Vector3(10f, 0f, 10f);
            
            var result = RecastNavigationWrapper.FindPath(start, end);
            
            if (result.Success)
            {
                Debug.Log($"경로 찾기 테스트 성공! 포인트 수: {result.PathPoints.Length}");
            }
            else
            {
                Debug.LogWarning($"경로 찾기 테스트 실패: {result.ErrorMessage}");
            }
        }
        
        void RunRandomPathfindingTest()
        {
            if (!isNavMeshLoaded)
            {
                Debug.LogWarning("NavMesh가 로드되지 않았습니다.");
                return;
            }
            
            // 랜덤한 시작점과 끝점 생성
            Vector3 start = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
            Vector3 end = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
            
            var result = RecastNavigationWrapper.FindPath(start, end);
            
            if (result.Success)
            {
                Debug.Log($"랜덤 경로 찾기 테스트 성공! {start} -> {end}, 포인트 수: {result.PathPoints.Length}");
            }
            else
            {
                Debug.LogWarning($"랜덤 경로 찾기 테스트 실패: {result.ErrorMessage}");
            }
        }
        
        void RunBuildPerformanceTest()
        {
            Debug.Log("NavMesh 빌드 성능 테스트 시작...");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // 테스트용 간단한 메시 생성
            Mesh testMesh = CreateTestMesh();
            
            var result = RecastNavigationWrapper.BuildNavMesh(testMesh, quickSettings);
            
            stopwatch.Stop();
            
            if (result.Success)
            {
                Debug.Log($"NavMesh 빌드 성능 테스트 완료: {stopwatch.ElapsedMilliseconds}ms");
            }
            else
            {
                Debug.LogError($"NavMesh 빌드 성능 테스트 실패: {result.ErrorMessage}");
            }
        }
        
        void RunPathfindingPerformanceTest()
        {
            if (!isNavMeshLoaded)
            {
                Debug.LogWarning("NavMesh가 로드되지 않았습니다.");
                return;
            }
            
            Debug.Log("경로 찾기 성능 테스트 시작...");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            int testCount = 100;
            int successCount = 0;
            
            for (int i = 0; i < testCount; i++)
            {
                Vector3 start = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
                Vector3 end = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
                
                var result = RecastNavigationWrapper.FindPath(start, end);
                if (result.Success)
                {
                    successCount++;
                }
            }
            
            stopwatch.Stop();
            
            Debug.Log($"경로 찾기 성능 테스트 완료: {testCount}회 중 {successCount}회 성공, {stopwatch.ElapsedMilliseconds}ms");
        }
        
        void AnalyzeSceneMeshes()
        {
            MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
            Debug.Log($"씬 분석 - MeshRenderer: {renderers.Length}개");
            
            int totalVertices = 0;
            int totalTriangles = 0;
            
            foreach (var renderer in renderers)
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    Mesh mesh = meshFilter.sharedMesh;
                    totalVertices += mesh.vertexCount;
                    totalTriangles += mesh.triangles.Length / 3;
                    
                    Debug.Log($"- {renderer.name}: {mesh.vertexCount} 정점, {mesh.triangles.Length / 3} 삼각형");
                }
            }
            
            Debug.Log($"총계: {totalVertices} 정점, {totalTriangles} 삼각형");
        }
        
        void AnalyzeSceneColliders()
        {
            Collider[] colliders = FindObjectsOfType<Collider>();
            Debug.Log($"씬 분석 - Collider: {colliders.Length}개");
            
            foreach (var collider in colliders)
            {
                Debug.Log($"- {collider.name}: {collider.GetType().Name}");
            }
        }
        
        void AnalyzeSceneTerrains()
        {
            Terrain[] terrains = FindObjectsOfType<Terrain>();
            Debug.Log($"씬 분석 - Terrain: {terrains.Length}개");
            
            foreach (var terrain in terrains)
            {
                TerrainData terrainData = terrain.terrainData;
                Debug.Log($"- {terrain.name}: {terrainData.heightmapResolution}x{terrainData.heightmapResolution} 높이맵");
            }
        }
        
        void PrintNavMeshInfo()
        {
            RecastNavigationComponent navComponent = FindObjectOfType<RecastNavigationComponent>();
            if (navComponent == null)
            {
                Debug.Log("RecastNavigationComponent를 찾을 수 없습니다.");
                return;
            }

            Debug.Log("=== NavMesh 정보 ===");
            Debug.Log($"초기화됨: {navComponent.IsInitialized}");
            Debug.Log($"NavMesh 로드됨: {navComponent.IsNavMeshLoaded}");
            Debug.Log($"폴리곤 수: {navComponent.PolyCount}");
            Debug.Log($"정점 수: {navComponent.VertexCount}");
            Debug.Log($"경로 길이: {navComponent.PathLength}");
            Debug.Log("==================");
        }
        
        void PrintNavMeshStats()
        {
            if (isNavMeshLoaded)
            {
                Debug.Log("NavMesh 통계:");
                Debug.Log($"- 폴리곤 수: {RecastNavigationWrapper.GetPolyCount()}");
                Debug.Log($"- 정점 수: {RecastNavigationWrapper.GetVertexCount()}");
                // 추가 통계 정보
            }
            else
            {
                Debug.Log("NavMesh가 로드되지 않았습니다.");
            }
        }
        
        void PrintAllDebugInfo()
        {
            Debug.Log("=== RecastNavigation 디버그 정보 ===");
            Debug.Log($"초기화됨: {isInitialized}");
            Debug.Log($"NavMesh 로드됨: {isNavMeshLoaded}");
            Debug.Log($"상태 메시지: {statusMessage}");
            
            if (isNavMeshLoaded)
            {
                Debug.Log($"폴리곤 수: {RecastNavigationWrapper.GetPolyCount()}");
                Debug.Log($"정점 수: {RecastNavigationWrapper.GetVertexCount()}");
            }
            
            Debug.Log("=== 씬 정보 ===");
            AnalyzeSceneMeshes();
            AnalyzeSceneColliders();
            AnalyzeSceneTerrains();
        }
        
        void CheckMemoryUsage()
        {
            Debug.Log("메모리 사용량 확인:");
            Debug.Log($"- 총 메모리: {System.GC.GetTotalMemory(false) / 1024 / 1024} MB");
            Debug.Log($"- 할당된 메모리: {System.GC.GetTotalMemory(true) / 1024 / 1024} MB");
        }
        
        Mesh CombineAllMeshes(MeshRenderer[] renderers)
        {
            List<Mesh> meshes = new List<Mesh>();
            
            foreach (var renderer in renderers)
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    meshes.Add(meshFilter.sharedMesh);
                }
            }
            
            return CombineMeshes(meshes);
        }
        
        Mesh CombineMeshes(List<Mesh> meshes)
        {
            if (meshes.Count == 1)
            {
                return meshes[0];
            }
            
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
        
        Mesh CreateTestMesh()
        {
            // 간단한 평면 메시 생성
            Mesh mesh = new Mesh();
            
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-5f, 0f, -5f),
                new Vector3(5f, 0f, -5f),
                new Vector3(5f, 0f, 5f),
                new Vector3(-5f, 0f, 5f)
            };
            
            int[] triangles = new int[]
            {
                0, 1, 2,
                0, 2, 3
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }

        RecastNavigationComponent CreateRecastNavigationComponent()
        {
            GameObject go = new GameObject("RecastNavigation");
            RecastNavigationComponent component = go.AddComponent<RecastNavigationComponent>();
            Debug.Log("RecastNavigationComponent가 생성되었습니다.");
            return component;
        }
    }
} 