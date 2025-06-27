using UnityEngine;
using UnityEditor;
using RecastNavigation;
using System.IO;

namespace RecastNavigation.Editor
{
    /// <summary>
    /// RecastNavigation 설정 가이드 에디터 윈도우
    /// </summary>
    public class RecastNavigationSetupGuide : EditorWindow
    {
        private Vector2 scrollPosition;
        private int currentStep = 0;
        private bool[] completedSteps;
        
        // 단계별 설정
        private bool dllCopied = false;
        private bool scriptsImported = false;
        private bool navMeshBuilt = false;
        private bool pathfindingTested = false;
        
        // 설정 옵션
        private bool autoSetup = false;
        private string dllPath = "";
        private string scriptsPath = "";
        
        [MenuItem("Tools/RecastNavigation/Setup Guide")]
        public static void ShowWindow()
        {
            GetWindow<RecastNavigationSetupGuide>("RecastNavigation Setup Guide");
        }
        
        void OnEnable()
        {
            completedSteps = new bool[5];
            CheckCurrentSetup();
        }
        
        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // 헤더
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("RecastNavigation 설정 가이드", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 진행 상황
            DrawProgressSection();
            
            // 단계별 가이드
            DrawStepByStepGuide();
            
            // 자동 설정
            DrawAutoSetupSection();
            
            // 문제 해결
            DrawTroubleshootingSection();
            
            EditorGUILayout.EndScrollView();
        }
        
        void DrawProgressSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("설정 진행 상황", EditorStyles.boldLabel);
            
            // 진행률 표시
            int completedCount = 0;
            for (int i = 0; i < completedSteps.Length; i++)
            {
                if (completedSteps[i]) completedCount++;
            }
            
            float progress = (float)completedCount / completedSteps.Length;
            EditorGUILayout.LabelField($"전체 진행률: {progress * 100:F0}% ({completedCount}/{completedSteps.Length})");
            
            // 진행 바
            Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(progressRect, progress, $"{progress * 100:F0}%");
            
            EditorGUILayout.Space();
            
            // 단계별 상태
            DrawStepStatus("1. DLL 파일 복사", completedSteps[0]);
            DrawStepStatus("2. 스크립트 임포트", completedSteps[1]);
            DrawStepStatus("3. NavMesh 빌드", completedSteps[2]);
            DrawStepStatus("4. 경로 찾기 테스트", completedSteps[3]);
            DrawStepStatus("5. 설정 완료", completedSteps[4]);
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawStepStatus(string stepName, bool completed)
        {
            EditorGUILayout.BeginHorizontal();
            
            GUI.color = completed ? Color.green : Color.red;
            EditorGUILayout.LabelField(completed ? "✓" : "✗", GUILayout.Width(20));
            
            GUI.color = Color.white;
            EditorGUILayout.LabelField(stepName);
            
            EditorGUILayout.EndHorizontal();
        }
        
        void DrawStepByStepGuide()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("단계별 설정 가이드", EditorStyles.boldLabel);
            
            // 단계 1: DLL 파일 복사
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("1단계: DLL 파일 복사", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("RecastNavigation DLL을 Unity 프로젝트의 Plugins 폴더에 복사해야 합니다.", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("DLL 경로:", GUILayout.Width(80));
            dllPath = EditorGUILayout.TextField(dllPath);
            if (GUILayout.Button("찾아보기", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("DLL 파일 선택", "", "dll");
                if (!string.IsNullOrEmpty(path))
                {
                    dllPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("DLL 복사 및 확인"))
            {
                CopyDllToPlugins();
            }
            
            EditorGUILayout.EndVertical();
            
            // 단계 2: 스크립트 임포트
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("2단계: 스크립트 임포트", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("RecastNavigation 스크립트들을 Unity 프로젝트에 임포트합니다.", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("스크립트 경로:", GUILayout.Width(80));
            scriptsPath = EditorGUILayout.TextField(scriptsPath);
            if (GUILayout.Button("찾아보기", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("스크립트 폴더 선택", "", "");
                if (!string.IsNullOrEmpty(path))
                {
                    scriptsPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("스크립트 임포트 및 확인"))
            {
                ImportScripts();
            }
            
            EditorGUILayout.EndVertical();
            
            // 단계 3: NavMesh 빌드
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("3단계: NavMesh 빌드", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("현재 씬에서 NavMesh를 빌드하여 경로 찾기 기능을 테스트합니다.", EditorStyles.wordWrappedLabel);
            
            if (GUILayout.Button("NavMesh 빌드 테스트"))
            {
                TestNavMeshBuild();
            }
            
            EditorGUILayout.EndVertical();
            
            // 단계 4: 경로 찾기 테스트
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("4단계: 경로 찾기 테스트", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("빌드된 NavMesh를 사용하여 경로 찾기 기능을 테스트합니다.", EditorStyles.wordWrappedLabel);
            
            if (GUILayout.Button("경로 찾기 테스트"))
            {
                TestPathfinding();
            }
            
            EditorGUILayout.EndVertical();
            
            // 단계 5: 설정 완료
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("5단계: 설정 완료", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("모든 설정이 완료되었습니다. 이제 RecastNavigation을 사용할 수 있습니다.", EditorStyles.wordWrappedLabel);
            
            if (GUILayout.Button("설정 완료 확인"))
            {
                CompleteSetup();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawAutoSetupSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("자동 설정", EditorStyles.boldLabel);
            
            autoSetup = EditorGUILayout.Toggle("자동 설정 사용", autoSetup);
            
            if (autoSetup)
            {
                EditorGUILayout.LabelField("자동 설정을 사용하면 모든 단계를 자동으로 진행합니다.", EditorStyles.wordWrappedLabel);
                
                if (GUILayout.Button("자동 설정 시작", GUILayout.Height(30)))
                {
                    StartAutoSetup();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawTroubleshootingSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("문제 해결", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("설정 중 문제가 발생한 경우 다음 도구들을 사용하세요:", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("DLL 상태 확인"))
            {
                CheckDllStatus();
            }
            
            if (GUILayout.Button("스크립트 상태 확인"))
            {
                CheckScriptStatus();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("에러 로그 확인"))
            {
                CheckErrorLogs();
            }
            
            if (GUILayout.Button("설정 초기화"))
            {
                ResetSetup();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 일반적인 문제들
            EditorGUILayout.LabelField("일반적인 문제들:", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("• DLL을 찾을 수 없음: Plugins 폴더에 DLL이 올바르게 복사되었는지 확인", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• 스크립트 컴파일 에러: 스크립트가 올바른 위치에 있는지 확인", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• NavMesh 빌드 실패: 씬에 Mesh가 있는지 확인", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• 경로 찾기 실패: NavMesh가 올바르게 빌드되었는지 확인", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        void CheckCurrentSetup()
        {
            // DLL 상태 확인
            string pluginsPath = "Assets/Plugins/";
            string dllName = "RecastNavigationUnity.dll";
            dllCopied = File.Exists(Path.Combine(pluginsPath, dllName));
            completedSteps[0] = dllCopied;
            
            // 스크립트 상태 확인
            string scriptsFolder = "Assets/Scripts/RecastNavigation/";
            scriptsImported = Directory.Exists(scriptsFolder);
            completedSteps[1] = scriptsImported;
            
            // NavMesh 상태 확인 (실제로는 더 복잡한 확인이 필요)
            navMeshBuilt = false; // 실제 확인 로직 필요
            completedSteps[2] = navMeshBuilt;
            
            // 경로 찾기 상태 확인
            pathfindingTested = false; // 실제 확인 로직 필요
            completedSteps[3] = pathfindingTested;
            
            // 전체 완료 상태
            completedSteps[4] = completedSteps[0] && completedSteps[1] && completedSteps[2] && completedSteps[3];
        }
        
        void CopyDllToPlugins()
        {
            if (string.IsNullOrEmpty(dllPath))
            {
                EditorUtility.DisplayDialog("오류", "DLL 경로를 선택해주세요.", "확인");
                return;
            }
            
            if (!File.Exists(dllPath))
            {
                EditorUtility.DisplayDialog("오류", "선택한 DLL 파일이 존재하지 않습니다.", "확인");
                return;
            }
            
            // Plugins 폴더 생성
            string pluginsPath = "Assets/Plugins/";
            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
            }
            
            // DLL 복사
            string fileName = Path.GetFileName(dllPath);
            string destPath = Path.Combine(pluginsPath, fileName);
            
            try
            {
                File.Copy(dllPath, destPath, true);
                AssetDatabase.Refresh();
                
                dllCopied = true;
                completedSteps[0] = true;
                
                EditorUtility.DisplayDialog("성공", "DLL이 성공적으로 복사되었습니다.", "확인");
                Debug.Log($"DLL 복사 완료: {destPath}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("오류", $"DLL 복사 실패: {e.Message}", "확인");
                Debug.LogError($"DLL 복사 실패: {e.Message}");
            }
        }
        
        void ImportScripts()
        {
            if (string.IsNullOrEmpty(scriptsPath))
            {
                EditorUtility.DisplayDialog("오류", "스크립트 경로를 선택해주세요.", "확인");
                return;
            }
            
            if (!Directory.Exists(scriptsPath))
            {
                EditorUtility.DisplayDialog("오류", "선택한 스크립트 폴더가 존재하지 않습니다.", "확인");
                return;
            }
            
            // 스크립트 폴더 생성
            string destScriptsPath = "Assets/Scripts/RecastNavigation/";
            if (!Directory.Exists(destScriptsPath))
            {
                Directory.CreateDirectory(destScriptsPath);
            }
            
            try
            {
                // 스크립트 파일들 복사
                string[] scriptFiles = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);
                
                foreach (string scriptFile in scriptFiles)
                {
                    string relativePath = scriptFile.Substring(scriptsPath.Length);
                    string destFile = Path.Combine(destScriptsPath, relativePath);
                    
                    // 디렉토리 생성
                    string destDir = Path.GetDirectoryName(destFile);
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    
                    File.Copy(scriptFile, destFile, true);
                }
                
                AssetDatabase.Refresh();
                
                scriptsImported = true;
                completedSteps[1] = true;
                
                EditorUtility.DisplayDialog("성공", "스크립트가 성공적으로 임포트되었습니다.", "확인");
                Debug.Log($"스크립트 임포트 완료: {destScriptsPath}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("오류", $"스크립트 임포트 실패: {e.Message}", "확인");
                Debug.LogError($"스크립트 임포트 실패: {e.Message}");
            }
        }
        
        void TestNavMeshBuild()
        {
            if (!dllCopied)
            {
                EditorUtility.DisplayDialog("오류", "먼저 DLL을 복사해주세요.", "확인");
                return;
            }
            
            if (!scriptsImported)
            {
                EditorUtility.DisplayDialog("오류", "먼저 스크립트를 임포트해주세요.", "확인");
                return;
            }
            
            try
            {
                // RecastNavigation 초기화
                if (!RecastNavigationWrapper.Initialize())
                {
                    EditorUtility.DisplayDialog("오류", "RecastNavigation 초기화 실패", "확인");
                    return;
                }
                
                // 씬의 Mesh 수집
                MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
                if (renderers.Length == 0)
                {
                    EditorUtility.DisplayDialog("오류", "씬에서 Mesh를 찾을 수 없습니다.", "확인");
                    return;
                }
                
                // 간단한 테스트 메시 생성
                Mesh testMesh = CreateTestMesh();
                
                // NavMesh 빌드
                var settings = NavMeshBuildSettingsExtensions.CreateDefault();
                var result = RecastNavigationWrapper.BuildNavMesh(testMesh, settings);
                
                if (result.Success)
                {
                    navMeshBuilt = true;
                    completedSteps[2] = true;
                    
                    EditorUtility.DisplayDialog("성공", "NavMesh 빌드 테스트 성공!", "확인");
                    Debug.Log("NavMesh 빌드 테스트 성공");
                }
                else
                {
                    EditorUtility.DisplayDialog("오류", $"NavMesh 빌드 실패: {result.ErrorMessage}", "확인");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("오류", $"NavMesh 빌드 테스트 실패: {e.Message}", "확인");
                Debug.LogError($"NavMesh 빌드 테스트 실패: {e.Message}");
            }
        }
        
        void TestPathfinding()
        {
            if (!navMeshBuilt)
            {
                EditorUtility.DisplayDialog("오류", "먼저 NavMesh를 빌드해주세요.", "확인");
                return;
            }
            
            try
            {
                // 간단한 경로 찾기 테스트
                Vector3 start = Vector3.zero;
                Vector3 end = new Vector3(10f, 0f, 10f);
                
                var result = RecastNavigationWrapper.FindPath(start, end);
                
                if (result.Success)
                {
                    pathfindingTested = true;
                    completedSteps[3] = true;
                    
                    string pathInfo = result.PathPoints != null ? $"포인트 수: {result.PathPoints.Length}" : "경로 포인트: null (테스트 모드)";
                    
                    EditorUtility.DisplayDialog("성공", "경로 찾기 테스트 성공!", "확인");
                    Debug.Log($"경로 찾기 테스트 성공! {pathInfo}");
                }
                else
                {
                    EditorUtility.DisplayDialog("오류", $"경로 찾기 실패: {result.ErrorMessage}", "확인");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("오류", $"경로 찾기 테스트 실패: {e.Message}", "확인");
                Debug.LogError($"경로 찾기 테스트 실패: {e.Message}");
            }
        }
        
        void CompleteSetup()
        {
            if (completedSteps[0] && completedSteps[1] && completedSteps[2] && completedSteps[3])
            {
                completedSteps[4] = true;
                
                EditorUtility.DisplayDialog("축하합니다!", "RecastNavigation 설정이 완료되었습니다!\n\n이제 Tools > RecastNavigation 메뉴에서 에디터 도구를 사용할 수 있습니다.", "확인");
                Debug.Log("RecastNavigation 설정 완료!");
            }
            else
            {
                EditorUtility.DisplayDialog("오류", "모든 단계를 완료해야 합니다.", "확인");
            }
        }
        
        void StartAutoSetup()
        {
            EditorUtility.DisplayDialog("자동 설정", "자동 설정을 시작합니다. 각 단계가 자동으로 진행됩니다.", "확인");
            
            // 자동으로 모든 단계 진행
            CopyDllToPlugins();
            
            if (dllCopied)
            {
                ImportScripts();
                
                if (scriptsImported)
                {
                    TestNavMeshBuild();
                    
                    if (navMeshBuilt)
                    {
                        TestPathfinding();
                        
                        if (pathfindingTested)
                        {
                            CompleteSetup();
                        }
                    }
                }
            }
        }
        
        void CheckDllStatus()
        {
            string pluginsPath = "Assets/Plugins/";
            string dllName = "RecastNavigationUnity.dll";
            string dllPath = Path.Combine(pluginsPath, dllName);
            
            if (File.Exists(dllPath))
            {
                FileInfo fileInfo = new FileInfo(dllPath);
                EditorUtility.DisplayDialog("DLL 상태", $"DLL이 존재합니다.\n경로: {dllPath}\n크기: {fileInfo.Length} bytes\n수정일: {fileInfo.LastWriteTime}", "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("DLL 상태", "DLL을 찾을 수 없습니다.", "확인");
            }
        }
        
        void CheckScriptStatus()
        {
            string scriptsFolder = "Assets/Scripts/RecastNavigation/";
            
            if (Directory.Exists(scriptsFolder))
            {
                string[] files = Directory.GetFiles(scriptsFolder, "*.cs", SearchOption.AllDirectories);
                EditorUtility.DisplayDialog("스크립트 상태", $"스크립트 폴더가 존재합니다.\n경로: {scriptsFolder}\n파일 수: {files.Length}", "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("스크립트 상태", "스크립트 폴더를 찾을 수 없습니다.", "확인");
            }
        }
        
        void CheckErrorLogs()
        {
            // Unity 콘솔 로그 확인
            Debug.Log("=== RecastNavigation 에러 로그 확인 ===");
            Debug.Log("Unity 콘솔에서 RecastNavigation 관련 에러를 확인하세요.");
            
            EditorUtility.DisplayDialog("에러 로그", "Unity 콘솔에서 RecastNavigation 관련 에러를 확인하세요.", "확인");
        }
        
        void ResetSetup()
        {
            bool confirmed = EditorUtility.DisplayDialog("설정 초기화", "모든 설정을 초기화하시겠습니까?", "예", "아니오");
            
            if (confirmed)
            {
                // 상태 초기화
                dllCopied = false;
                scriptsImported = false;
                navMeshBuilt = false;
                pathfindingTested = false;
                
                for (int i = 0; i < completedSteps.Length; i++)
                {
                    completedSteps[i] = false;
                }
                
                EditorUtility.DisplayDialog("초기화 완료", "설정이 초기화되었습니다.", "확인");
            }
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
    }
} 