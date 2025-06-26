using UnityEngine;
using UnityEditor;
using RecastNavigation;
using System.IO;
using System.Collections.Generic;

namespace RecastNavigation.Editor
{
    /// <summary>
    /// RecastNavigation 에디터 도구
    /// </summary>
    public class RecastNavigationEditor : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool showBuildSettings = true;
        private bool showPathfindingSettings = true;
        private bool showDebugInfo = true;
        
        // NavMesh 빌드 설정
        private RecastNavigationWrapper.NavMeshBuildSettings buildSettings;
        
        // 경로 찾기 설정
        private Vector3 startPoint = Vector3.zero;
        private Vector3 endPoint = new Vector3(10f, 0f, 10f);
        private bool showPath = true;
        private Color pathColor = Color.green;
        private float pathWidth = 0.1f;
        
        // 디버그 정보
        private int polyCount = 0;
        private int vertexCount = 0;
        private bool isNavMeshLoaded = false;
        private byte[] navMeshData;
        private string statusMessage = "NavMesh가 로드되지 않았습니다.";
        
        // 경로 결과
        private Vector3[] currentPath = null;
        private float pathLength = 0f;
        
        // 옵션
        private bool autoRebuildOnSceneChange = false;
        private bool saveNavMeshToFile = false;
        private string savePath = "Assets/NavMeshData/";
        
        private bool autoTransformCoordinates = true;
        private CoordinateSystem coordinateSystem = CoordinateSystem.LeftHanded;
        private string loadPath = "";
        private bool showAdvancedSettings = false;
        
        [MenuItem("Tools/RecastNavigation/Editor")]
        public static void ShowWindow()
        {
            GetWindow<RecastNavigationEditor>("RecastNavigation Editor");
        }
        
        void OnEnable()
        {
            // 기본 설정 초기화
            buildSettings = NavMeshBuildSettingsExtensions.CreateDefault();
            
            // 씬 변경 이벤트 등록
            if (autoRebuildOnSceneChange)
            {
                EditorApplication.hierarchyChanged += OnSceneChanged;
            }
        }
        
        void OnDisable()
        {
            // 이벤트 해제
            EditorApplication.hierarchyChanged -= OnSceneChanged;
        }
        
        void OnSceneChanged()
        {
            if (autoRebuildOnSceneChange)
            {
                BuildNavMeshFromScene();
            }
        }
        
        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // 헤더
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("RecastNavigation Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 좌표계 설정
            EditorGUILayout.LabelField("좌표계 설정", EditorStyles.boldLabel);
            coordinateSystem = (CoordinateSystem)EditorGUILayout.EnumPopup("좌표계", coordinateSystem);
            autoTransformCoordinates = EditorGUILayout.Toggle("자동 좌표 변환", autoTransformCoordinates);
            
            if (autoTransformCoordinates)
            {
                EditorGUILayout.HelpBox(
                    "Unity (왼손 좌표계)와 RecastNavigation (오른손 좌표계) 간의 좌표 변환이 자동으로 수행됩니다.\n" +
                    "Z축 방향이 반전됩니다.", 
                    MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            // 상태 표시
            DrawStatusSection();
            
            // NavMesh 빌드 설정
            DrawBuildSettingsSection();
            
            // 경로 찾기 설정
            DrawPathfindingSection();
            
            // 디버그 정보
            DrawDebugSection();
            
            // 옵션
            DrawOptionsSection();
            
            EditorGUILayout.EndScrollView();
        }
        
        void DrawStatusSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("상태", EditorStyles.boldLabel);
            
            // 상태 메시지
            EditorGUILayout.LabelField("상태:", statusMessage);
            
            // NavMesh 정보
            if (isNavMeshLoaded)
            {
                EditorGUILayout.LabelField($"폴리곤 수: {polyCount}");
                EditorGUILayout.LabelField($"정점 수: {vertexCount}");
                EditorGUILayout.LabelField($"NavMesh 데이터 크기: {navMeshData?.Length ?? 0} bytes");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawBuildSettingsSection()
        {
            showBuildSettings = EditorGUILayout.Foldout(showBuildSettings, "NavMesh 빌드 설정", true);
            if (showBuildSettings)
            {
                EditorGUILayout.BeginVertical("box");
                
                // 품질 프리셋
                EditorGUILayout.LabelField("품질 프리셋", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("기본 품질"))
                {
                    buildSettings = NavMeshBuildSettingsExtensions.CreateDefault();
                }
                if (GUILayout.Button("높은 품질"))
                {
                    buildSettings = NavMeshBuildSettingsExtensions.CreateHighQuality();
                }
                if (GUILayout.Button("낮은 품질"))
                {
                    buildSettings = NavMeshBuildSettingsExtensions.CreateLowQuality();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                // 상세 설정
                EditorGUILayout.LabelField("상세 설정", EditorStyles.boldLabel);
                
                buildSettings.cellSize = EditorGUILayout.Slider("셀 크기", buildSettings.cellSize, 0.01f, 1.0f);
                buildSettings.cellHeight = EditorGUILayout.Slider("셀 높이", buildSettings.cellHeight, 0.01f, 1.0f);
                buildSettings.walkableSlopeAngle = EditorGUILayout.Slider("이동 가능한 경사각", buildSettings.walkableSlopeAngle, 0f, 90f);
                buildSettings.walkableHeight = EditorGUILayout.Slider("이동 가능한 높이", buildSettings.walkableHeight, 0.1f, 10f);
                buildSettings.walkableRadius = EditorGUILayout.Slider("이동 가능한 반지름", buildSettings.walkableRadius, 0.1f, 5f);
                buildSettings.walkableClimb = EditorGUILayout.Slider("이동 가능한 오르기 높이", buildSettings.walkableClimb, 0.1f, 5f);
                buildSettings.minRegionArea = EditorGUILayout.Slider("최소 영역 크기", buildSettings.minRegionArea, 1f, 100f);
                buildSettings.mergeRegionArea = EditorGUILayout.Slider("병합 영역 크기", buildSettings.mergeRegionArea, 1f, 200f);
                buildSettings.maxVertsPerPoly = EditorGUILayout.IntSlider("폴리곤당 최대 정점 수", buildSettings.maxVertsPerPoly, 3, 12);
                buildSettings.detailSampleDist = EditorGUILayout.Slider("상세 샘플링 거리", buildSettings.detailSampleDist, 1f, 20f);
                buildSettings.detailSampleMaxError = EditorGUILayout.Slider("상세 샘플링 최대 오차", buildSettings.detailSampleMaxError, 0.1f, 5f);
                
                EditorGUILayout.Space();
                
                // 빌드 버튼
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("현재 씬에서 NavMesh 빌드", GUILayout.Height(30)))
                {
                    BuildNavMeshFromScene();
                }
                
                if (GUILayout.Button("선택된 오브젝트에서 빌드", GUILayout.Height(30)))
                {
                    BuildNavMeshFromSelection();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
        }
        
        void DrawPathfindingSection()
        {
            showPathfindingSettings = EditorGUILayout.Foldout(showPathfindingSettings, "경로 찾기 설정", true);
            if (showPathfindingSettings)
            {
                EditorGUILayout.BeginVertical("box");
                
                // 시작점과 끝점 설정
                EditorGUILayout.LabelField("경로 찾기", EditorStyles.boldLabel);
                
                startPoint = EditorGUILayout.Vector3Field("시작점", startPoint);
                endPoint = EditorGUILayout.Vector3Field("끝점", endPoint);
                
                EditorGUILayout.Space();
                
                // 경로 찾기 버튼
                EditorGUI.BeginDisabledGroup(!isNavMeshLoaded);
                
                if (GUILayout.Button("경로 찾기", GUILayout.Height(25)))
                {
                    FindPath();
                }
                
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.Space();
                
                // 경로 표시 설정
                EditorGUILayout.LabelField("경로 표시 설정", EditorStyles.boldLabel);
                
                showPath = EditorGUILayout.Toggle("경로 표시", showPath);
                pathColor = EditorGUILayout.ColorField("경로 색상", pathColor);
                pathWidth = EditorGUILayout.Slider("경로 두께", pathWidth, 0.01f, 1f);
                
                // 경로 정보 표시
                if (currentPath != null && currentPath.Length > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("경로 정보", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"경로 포인트 수: {currentPath.Length}");
                    EditorGUILayout.LabelField($"경로 길이: {pathLength:F2} units");
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        void DrawDebugSection()
        {
            showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "디버그 정보", true);
            if (showDebugInfo)
            {
                EditorGUILayout.BeginVertical("box");
                
                // NavMesh 정보
                EditorGUILayout.LabelField("NavMesh 정보", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"로드됨: {isNavMeshLoaded}");
                EditorGUILayout.LabelField($"폴리곤 수: {polyCount}");
                EditorGUILayout.LabelField($"정점 수: {vertexCount}");
                
                EditorGUILayout.Space();
                
                // 경로 정보
                if (currentPath != null)
                {
                    EditorGUILayout.LabelField("경로 정보", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"포인트 수: {currentPath.Length}");
                    EditorGUILayout.LabelField($"길이: {pathLength:F2}");
                    
                    // 경로 포인트 목록
                    if (currentPath.Length > 0)
                    {
                        EditorGUILayout.LabelField("경로 포인트:", EditorStyles.boldLabel);
                        for (int i = 0; i < Mathf.Min(currentPath.Length, 10); i++)
                        {
                            EditorGUILayout.LabelField($"{i}: {currentPath[i]}");
                        }
                        if (currentPath.Length > 10)
                        {
                            EditorGUILayout.LabelField($"... 및 {currentPath.Length - 10}개 더");
                        }
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        void DrawOptionsSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("옵션", EditorStyles.boldLabel);
            
            // 자동 재빌드
            autoRebuildOnSceneChange = EditorGUILayout.Toggle("씬 변경 시 자동 재빌드", autoRebuildOnSceneChange);
            
            // NavMesh 저장
            saveNavMeshToFile = EditorGUILayout.Toggle("NavMesh 파일로 저장", saveNavMeshToFile);
            
            if (saveNavMeshToFile)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("저장 경로:", GUILayout.Width(80));
                savePath = EditorGUILayout.TextField(savePath);
                if (GUILayout.Button("찾아보기", GUILayout.Width(60)))
                {
                    string newPath = EditorUtility.SaveFolderPanel("NavMesh 저장 경로 선택", savePath, "");
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        savePath = newPath;
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                if (GUILayout.Button("NavMesh 저장"))
                {
                    SaveNavMeshToFile();
                }
            }
            
            EditorGUILayout.Space();
            
            // 유틸리티 버튼
            EditorGUILayout.LabelField("유틸리티", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("NavMesh 초기화"))
            {
                InitializeRecastNavigation();
            }
            
            if (GUILayout.Button("NavMesh 정리"))
            {
                CleanupRecastNavigation();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("씬에서 모든 Mesh 수집"))
            {
                CollectAllMeshesInScene();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void OnSceneGUI()
        {
            if (!isNavMeshLoaded) return;
            
            // 시작점과 끝점을 씬에서 직접 설정할 수 있게 함
            Handles.color = Color.green;
            startPoint = Handles.PositionHandle(startPoint, Quaternion.identity);
            Handles.SphereHandleCap(0, startPoint, Quaternion.identity, 0.5f, EventType.Repaint);
            
            Handles.color = Color.red;
            endPoint = Handles.PositionHandle(endPoint, Quaternion.identity);
            Handles.SphereHandleCap(0, endPoint, Quaternion.identity, 0.5f, EventType.Repaint);
            
            // 경로 표시
            if (showPath && currentPath != null && currentPath.Length > 1)
            {
                Handles.color = pathColor;
                Handles.DrawAAPolyLine(pathWidth, currentPath);
                
                // 경로 포인트 표시
                for (int i = 0; i < currentPath.Length; i++)
                {
                    Handles.color = Color.yellow;
                    Handles.SphereHandleCap(0, currentPath[i], Quaternion.identity, 0.1f, EventType.Repaint);
                }
            }
            
            // 씬 뷰 업데이트
            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }
        
        void InitializeRecastNavigation()
        {
            if (RecastNavigationWrapper.Initialize())
            {
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
            isNavMeshLoaded = false;
            navMeshData = null;
            currentPath = null;
            statusMessage = "RecastNavigation이 정리되었습니다.";
            Debug.Log("RecastNavigation 정리 완료");
        }
        
        void BuildNavMeshFromScene()
        {
            if (!RecastNavigationWrapper.Initialize())
            {
                statusMessage = "RecastNavigation 초기화 실패";
                return;
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
                statusMessage = "씬에서 Mesh를 찾을 수 없습니다.";
                return;
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
                    statusMessage = $"NavMesh 빌드 성공! 폴리곤: {polyCount}, 정점: {vertexCount}";
                    
                    Debug.Log($"NavMesh 빌드 성공! 데이터 크기: {navMeshData.Length} bytes");
                    
                    // 파일로 저장
                    if (saveNavMeshToFile)
                    {
                        SaveNavMeshToFile();
                    }
                }
                else
                {
                    statusMessage = "NavMesh 로드 실패";
                    Debug.LogError("NavMesh 로드 실패");
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
            if (!RecastNavigationWrapper.Initialize())
            {
                statusMessage = "RecastNavigation 초기화 실패";
                return;
            }
            
            // 선택된 오브젝트들의 Mesh 수집
            GameObject[] selectedObjects = Selection.gameObjects;
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
                statusMessage = "선택된 오브젝트에서 Mesh를 찾을 수 없습니다.";
                return;
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
                    statusMessage = $"NavMesh 빌드 성공! (선택된 오브젝트) 폴리곤: {polyCount}, 정점: {vertexCount}";
                    
                    Debug.Log($"NavMesh 빌드 성공! (선택된 오브젝트) 데이터 크기: {navMeshData.Length} bytes");
                }
                else
                {
                    statusMessage = "NavMesh 로드 실패";
                    Debug.LogError("NavMesh 로드 실패");
                }
            }
            else
            {
                statusMessage = $"NavMesh 빌드 실패: {result.ErrorMessage}";
                Debug.LogError($"NavMesh 빌드 실패: {result.ErrorMessage}");
            }
        }
        
        void FindPath()
        {
            if (!isNavMeshLoaded)
            {
                statusMessage = "NavMesh가 로드되지 않았습니다.";
                return;
            }
            
            var result = RecastNavigationWrapper.FindPath(startPoint, endPoint);
            
            if (result.Success)
            {
                currentPath = result.PathPoints;
                pathLength = CalculatePathLength(currentPath);
                statusMessage = $"경로 찾기 성공! 길이: {pathLength:F2}";
                
                Debug.Log($"경로 찾기 성공! 포인트 수: {currentPath.Length}, 길이: {pathLength:F2}");
                
                // 씬 뷰 업데이트
                SceneView.RepaintAll();
            }
            else
            {
                currentPath = null;
                pathLength = 0f;
                statusMessage = $"경로 찾기 실패: {result.ErrorMessage}";
                Debug.LogError($"경로 찾기 실패: {result.ErrorMessage}");
            }
        }
        
        void SaveNavMeshToFile()
        {
            if (navMeshData == null || navMeshData.Length == 0)
            {
                Debug.LogWarning("저장할 NavMesh 데이터가 없습니다.");
                return;
            }
            
            // 디렉토리 생성
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            
            string fileName = $"NavMesh_{System.DateTime.Now:yyyyMMdd_HHmmss}.bytes";
            string fullPath = Path.Combine(savePath, fileName);
            
            try
            {
                File.WriteAllBytes(fullPath, navMeshData);
                Debug.Log($"NavMesh가 저장되었습니다: {fullPath}");
                
                // Unity 에디터에서 파일 새로고침
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NavMesh 저장 실패: {e.Message}");
            }
        }
        
        void CollectAllMeshesInScene()
        {
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
            
            Debug.Log($"씬에서 {meshes.Count}개의 Mesh를 찾았습니다.");
            
            foreach (var mesh in meshes)
            {
                Debug.Log($"- {mesh.name}: {mesh.vertexCount} 정점, {mesh.triangles.Length / 3} 삼각형");
            }
        }
        
        Mesh CombineMeshes(List<Mesh> meshes)
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
        
        float CalculatePathLength(Vector3[] path)
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
    }
} 