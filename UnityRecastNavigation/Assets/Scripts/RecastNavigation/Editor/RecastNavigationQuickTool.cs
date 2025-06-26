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
                
                EditorGUILayout.Space();
                
                // 선택된 오브젝트에서 빌드
                EditorGUILayout.LabelField("선택된 오브젝트", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("선택된 오브젝트에서 빌드", GUILayout.Height(30)))
                {
                    BuildNavMeshFromSelection();
                }
                
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
            // 상태 확인 (실제로는 DLL 호출이 필요하지만 여기서는 간단히 표시)
            isInitialized = false;
            isNavMeshLoaded = false;
            statusMessage = "상태 확인 중...";
            
            // 실제 상태 확인은 DLL이 로드된 후에 가능
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
            if (!RecastNavigationWrapper.Initialize())
            {
                statusMessage = "RecastNavigation 초기화 실패";
                return;
            }
            
            // 씬의 모든 Mesh 수집
            MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
            if (renderers.Length == 0)
            {
                statusMessage = "씬에서 Mesh를 찾을 수 없습니다.";
                return;
            }
            
            // 메시 합치기
            Mesh combinedMesh = CombineAllMeshes(renderers);
            
            // NavMesh 빌드
            var result = RecastNavigationWrapper.BuildNavMesh(combinedMesh, settings);
            
            if (result.Success)
            {
                if (RecastNavigationWrapper.LoadNavMesh(result.NavMeshData))
                {
                    isNavMeshLoaded = true;
                    statusMessage = $"NavMesh 빌드 성공! (프리셋 사용)";
                    Debug.Log($"NavMesh 빌드 성공! 폴리곤: {RecastNavigationWrapper.GetPolyCount()}, 정점: {RecastNavigationWrapper.GetVertexCount()}");
                }
                else
                {
                    statusMessage = "NavMesh 로드 실패";
                }
            }
            else
            {
                statusMessage = $"NavMesh 빌드 실패: {result.ErrorMessage}";
            }
        }
        
        void BuildNavMeshFromSelection()
        {
            if (selectedObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("오류", "처리할 메시 오브젝트가 선택되지 않았습니다.", "확인");
                return;
            }

            RecastNavigationComponent navComponent = FindObjectOfType<RecastNavigationComponent>();
            if (navComponent == null)
            {
                navComponent = CreateRecastNavigationComponent();
            }

            try
            {
                EditorUtility.DisplayProgressBar("NavMesh 빌드", "선택된 오브젝트에서 NavMesh 빌드 중...", 0f);
                
                // 선택된 오브젝트들의 메시를 합치기
                List<Vector3> allVertices = new List<Vector3>();
                List<int> allIndices = new List<int>();

                for (int i = 0; i < selectedObjects.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("NavMesh 빌드", $"오브젝트 처리 중... ({i + 1}/{selectedObjects.Count})", (float)i / selectedObjects.Count);
                    
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
                    EditorUtility.DisplayDialog("오류", "유효한 메시 데이터가 없습니다.", "확인");
                    return;
                }

                bool success = navComponent.BuildNavMesh(allVertices.ToArray(), allIndices.ToArray());
                
                EditorUtility.ClearProgressBar();
                
                if (success)
                {
                    EditorUtility.DisplayDialog("성공", "선택된 오브젝트에서 NavMesh 빌드가 완료되었습니다.", "확인");
                    Debug.Log("선택된 오브젝트에서 NavMesh 빌드 완료!");
                }
                else
                {
                    EditorUtility.DisplayDialog("오류", "선택된 오브젝트에서 NavMesh 빌드에 실패했습니다.", "확인");
                    Debug.LogError("선택된 오브젝트에서 NavMesh 빌드 실패!");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("오류", $"NavMesh 빌드 중 오류가 발생했습니다: {e.Message}", "확인");
                Debug.LogError($"NavMesh 빌드 중 오류: {e.Message}");
            }
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
            string filePath = EditorUtility.OpenFilePanel("NavMesh 파일 선택", quickSavePath, "bytes");
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    byte[] data = File.ReadAllBytes(filePath);
                    if (RecastNavigationWrapper.LoadNavMesh(data))
                    {
                        isNavMeshLoaded = true;
                        statusMessage = $"NavMesh 로드 성공: {Path.GetFileName(filePath)}";
                        Debug.Log($"NavMesh 로드 성공: {filePath}");
                    }
                    else
                    {
                        statusMessage = "NavMesh 로드 실패";
                        Debug.LogError("NavMesh 로드 실패");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"NavMesh 로드 실패: {e.Message}");
                }
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