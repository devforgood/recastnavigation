using UnityEngine;
using UnityEditor;
using RecastNavigation;
using System.IO;
using System.Security.Cryptography;
using System.Linq;

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
        
        // EditorPrefs 키
        private const string PREF_DLL_PATH = "RecastNavigation_DllPath";
        private const string PREF_SCRIPTS_PATH = "RecastNavigation_ScriptsPath";
        private const string PREF_AUTO_SETUP = "RecastNavigation_AutoSetup";
        private const string PREF_COMPLETED_STEPS = "RecastNavigation_CompletedSteps";
        private const string PREF_CURRENT_STEP = "RecastNavigation_CurrentStep";
        
        [MenuItem("Tools/RecastNavigation/Setup Guide")]
        public static void ShowWindow()
        {
            GetWindow<RecastNavigationSetupGuide>("RecastNavigation Setup Guide");
        }
        
        void OnEnable()
        {
            completedSteps = new bool[5];
            LoadSettings();
            CheckCurrentSetup();
        }
        
        void OnDisable()
        {
            SaveSettings();
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
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("단계별 설정 가이드", EditorStyles.boldLabel);
            
            // 설정 저장/로드 버튼
            if (GUILayout.Button("설정 저장", GUILayout.Width(80)))
            {
                SaveSettings();
                ShowNotification(new GUIContent("설정이 저장되었습니다."));
            }
            
            if (GUILayout.Button("설정 초기화", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("설정 초기화", "모든 설정을 초기화하시겠습니까?", "확인", "취소"))
                {
                    ResetAllSettings();
                    ShowNotification(new GUIContent("설정이 초기화되었습니다."));
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 단계 1: DLL 파일 복사
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("1단계: DLL 파일 복사", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("RecastNavigation DLL을 Unity 프로젝트의 Plugins 폴더에 복사해야 합니다.", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("DLL 경로:", GUILayout.Width(80));
            
            // 툴팁과 함께 텍스트 필드 표시
            GUIContent dllPathContent = new GUIContent(dllPath, dllPath);
            dllPath = EditorGUILayout.TextField(dllPathContent, dllPath);
            if (GUILayout.Button("찾아보기", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("DLL 파일 선택", string.IsNullOrEmpty(dllPath) ? "" : Path.GetDirectoryName(dllPath), "dll");
                if (!string.IsNullOrEmpty(path))
                {
                    dllPath = path;
                    SaveSettings(); // 즉시 저장
                    Repaint(); // UI 강제 업데이트
                    Debug.Log($"DLL 경로 선택됨: {dllPath}");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 선택된 경로 표시 및 유효성 확인
            if (!string.IsNullOrEmpty(dllPath))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("선택된 파일:", GUILayout.Width(80));
                
                string displayPath = dllPath.Length > 50 ? "..." + dllPath.Substring(dllPath.Length - 47) : dllPath;
                GUIContent pathContent = new GUIContent(displayPath, dllPath);
                
                // 파일 존재 여부에 따라 색상 변경
                bool fileExists = File.Exists(dllPath);
                GUI.color = fileExists ? Color.white : Color.red;
                EditorGUILayout.SelectableLabel(pathContent.text, EditorStyles.textField, GUILayout.Height(16));
                GUI.color = Color.white;
                
                // 상태 아이콘
                string statusIcon = fileExists ? "✓" : "✗";
                GUI.color = fileExists ? Color.green : Color.red;
                EditorGUILayout.LabelField(statusIcon, GUILayout.Width(20));
                GUI.color = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            
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
            
            // 툴팁과 함께 텍스트 필드 표시
            GUIContent scriptsPathContent = new GUIContent(scriptsPath, scriptsPath);
            scriptsPath = EditorGUILayout.TextField(scriptsPathContent, scriptsPath);
            if (GUILayout.Button("찾아보기", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("스크립트 폴더 선택", string.IsNullOrEmpty(scriptsPath) ? "" : scriptsPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    scriptsPath = path;
                    SaveSettings(); // 즉시 저장
                    Repaint(); // UI 강제 업데이트
                    Debug.Log($"스크립트 경로 선택됨: {scriptsPath}");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 선택된 경로 표시 및 유효성 확인
            if (!string.IsNullOrEmpty(scriptsPath))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("선택된 폴더:", GUILayout.Width(80));
                
                string displayPath = scriptsPath.Length > 50 ? "..." + scriptsPath.Substring(scriptsPath.Length - 47) : scriptsPath;
                GUIContent pathContent = new GUIContent(displayPath, scriptsPath);
                
                // 폴더 존재 여부에 따라 색상 변경
                bool folderExists = Directory.Exists(scriptsPath);
                GUI.color = folderExists ? Color.white : Color.red;
                EditorGUILayout.SelectableLabel(pathContent.text, EditorStyles.textField, GUILayout.Height(16));
                GUI.color = Color.white;
                
                // 상태 아이콘
                string statusIcon = folderExists ? "✓" : "✗";
                GUI.color = folderExists ? Color.green : Color.red;
                EditorGUILayout.LabelField(statusIcon, GUILayout.Width(20));
                GUI.color = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            
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
            
            if (GUILayout.Button("설정 정보"))
            {
                ShowSettingsInfo();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
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
            Debug.Log("[RecastNavigation] ===========================================");
            Debug.Log("[RecastNavigation] DLL Copy Process Started");
            Debug.Log("[RecastNavigation] ===========================================");
            
            if (string.IsNullOrEmpty(dllPath))
            {
                Debug.LogError("[RecastNavigation] DLL path is not set");
                EditorUtility.DisplayDialog("오류", "DLL 경로를 선택해주세요.", "확인");
                return;
            }
            
            Debug.Log($"[RecastNavigation] Source DLL path: {dllPath}");
            
            if (!File.Exists(dllPath))
            {
                Debug.LogError($"[RecastNavigation] Source DLL file does not exist: {dllPath}");
                EditorUtility.DisplayDialog("오류", "선택한 DLL 파일이 존재하지 않습니다.", "확인");
                return;
            }
            
            // Log source file information
            try
            {
                var fileInfo = new FileInfo(dllPath);
                Debug.Log($"[RecastNavigation] Source file size: {fileInfo.Length} bytes");
                Debug.Log($"[RecastNavigation] Source file last write time: {fileInfo.LastWriteTime}");
                Debug.Log($"[RecastNavigation] Source file creation time: {fileInfo.CreationTime}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RecastNavigation] Failed to get source file info: {ex.Message}");
            }
            
            // Create Plugins folder
            string pluginsPath = "Assets/Plugins/";
            Debug.Log($"[RecastNavigation] Target plugins folder: {pluginsPath}");
            
            if (!Directory.Exists(pluginsPath))
            {
                Debug.Log("[RecastNavigation] Plugins folder does not exist. Creating...");
                Directory.CreateDirectory(pluginsPath);
                Debug.Log("[RecastNavigation] Plugins folder created successfully");
            }
            else
            {
                Debug.Log("[RecastNavigation] Plugins folder already exists");
            }
            
            // DLL copy
            string fileName = Path.GetFileName(dllPath);
            string destPath = Path.Combine(pluginsPath, fileName);
            Debug.Log($"[RecastNavigation] Target DLL path: {destPath}");
            
                          try
              {
                 // Step 1: Try to copy directly without any comparison (fastest approach)
                 Debug.Log("[RecastNavigation] Attempting direct file copy without comparison...");
                Debug.Log($"[RecastNavigation] File.Copy({dllPath}, {destPath}, true)");
                
                File.Copy(dllPath, destPath, true);
                Debug.Log("[RecastNavigation] File copy completed. Refreshing AssetDatabase...");
                
                AssetDatabase.Refresh();
                Debug.Log("[RecastNavigation] AssetDatabase refresh completed");
                
                dllCopied = true;
                completedSteps[0] = true;
                SaveSettings(); // 설정 자동 저장
                Debug.Log("[RecastNavigation] Settings saved");
                
                EditorUtility.DisplayDialog("성공", "DLL이 성공적으로 복사되었습니다.", "확인");
                Debug.Log($"[RecastNavigation] ✓ DLL copy successful: {destPath}");
                Debug.Log("[RecastNavigation] ===========================================");
            }
            catch (System.UnauthorizedAccessException e)
            {
                Debug.LogError($"[RecastNavigation] ✗ DLL copy permission error: {e.Message}");
                Debug.LogError($"[RecastNavigation] Stack trace:\n{e.StackTrace}");
                Debug.LogError("[RecastNavigation] ===========================================");
                
                EditorUtility.DisplayDialog("권한 오류", $"DLL 복사 권한이 없습니다: {e.Message}\n\nUnity Editor를 관리자 권한으로 실행해보세요.", "확인");
            }
            catch (System.IO.IOException e)
            {
                Debug.LogError($"[RecastNavigation] ✗ DLL copy I/O error: {e.Message}");
                Debug.LogError($"[RecastNavigation] Error type: {e.GetType().Name}");
                Debug.LogError($"[RecastNavigation] Stack trace:\n{e.StackTrace}");
                
                if (e.Message.Contains("being used by another process"))
                {
                    Debug.LogWarning("[RecastNavigation] DLL is being used by another process");
                    
                    // Step 2: Check if files are actually different using MD5
                    bool filesAreDifferent = false;
                    string comparisonResult = "";
                    
                    Debug.Log("[RecastNavigation] Checking if files are actually different using MD5...");
                    try
                    {
                        if (File.Exists(destPath))
                        {
                            string sourceMD5 = GetFileMD5Hash(dllPath);
                            string destMD5 = GetFileMD5Hash(destPath);
                            
                            filesAreDifferent = (sourceMD5 != destMD5);
                            comparisonResult = $"Source MD5: {sourceMD5}\nTarget MD5: {destMD5}";
                            
                            Debug.Log($"[RecastNavigation] MD5 Comparison Result: {(filesAreDifferent ? "Different" : "Identical")}");
                            Debug.Log($"[RecastNavigation] {comparisonResult}");
                        }
                        else
                        {
                            filesAreDifferent = true;
                            comparisonResult = "Target file does not exist";
                            Debug.Log("[RecastNavigation] Target file does not exist - update needed");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[RecastNavigation] Could not compare files using MD5: {ex.Message}");
                        filesAreDifferent = true; // Assume different if cannot compare
                        comparisonResult = $"MD5 comparison failed: {ex.Message}";
                    }
                    
                    if (filesAreDifferent)
                    {
                        Debug.Log("[RecastNavigation] Files are different - Unity restart required");
                        
                        // Files are different, Unity restart is necessary
                        int choice = EditorUtility.DisplayDialogComplex(
                            "DLL 업데이트 필요", 
                            "새로운 버전의 DLL이 있지만 Unity가 현재 DLL을 사용 중입니다.\n" +
                            "업데이트하려면 DLL 언로드가 필요합니다.\n\n" +
                            $"파일 비교 결과:\n{comparisonResult}\n\n" +
                            "해결 방법을 선택하세요:",
                            "강력한 DLL 언로드 시도", 
                            "취소",
                            "Unity Editor 재시작 안내"
                        );
                        
                        Debug.Log($"[RecastNavigation] User choice: {choice} (0=DomainReload, 1=Cancel, 2=RestartGuide)");
                        
                        if (choice == 0) // 강력한 DLL 언로드 시도
                        {
                            Debug.Log("[RecastNavigation] Will attempt powerful DLL unload strategies");
                            AttemptPowerfulDllUnload();
                            return;
                        }
                        else if (choice == 2) // Unity Editor 재시작 안내
                        {
                            Debug.Log("[RecastNavigation] Providing Unity Editor restart guide");
                            EditorUtility.DisplayDialog("Unity Editor 재시작", 
                                "Unity Editor를 완전히 종료한 후 다시 시작해주세요.\n\n" +
                                "재시작 후:\n" +
                                "1. Tools > RecastNavigation > Setup Guide 열기\n" +
                                "2. DLL 복사 재시도\n\n" +
                                "이 방법이 가장 확실합니다.", 
                                "확인");
                            return;
                        }
                        else // 취소
                        {
                            Debug.Log("[RecastNavigation] User cancelled DLL copy operation");
                            return;
                        }
                    }
                    else
                    {
                        Debug.Log("[RecastNavigation] Files are identical - no update needed");
                        
                        // Files are identical, no need to restart Unity
                        dllCopied = true;
                        completedSteps[0] = true;
                        SaveSettings();
                        
                        EditorUtility.DisplayDialog("정보", 
                            "DLL 파일이 이미 최신 버전입니다.\n" +
                            "Unity 재시작이 필요하지 않습니다.\n\n" +
                            $"파일 비교 결과:\n{comparisonResult}", 
                            "확인");
                        
                        Debug.Log("[RecastNavigation] ✓ DLL is already up to date, marked as completed");
                        Debug.Log("[RecastNavigation] ===========================================");
                        return;
                    }
                }
                else
                {
                    Debug.LogError("[RecastNavigation] General I/O error occurred");
                    EditorUtility.DisplayDialog("파일 I/O 오류", $"DLL 복사 중 I/O 오류: {e.Message}", "확인");
                }
                
                Debug.LogError("[RecastNavigation] ===========================================");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RecastNavigation] ✗ DLL copy unexpected error: {e.GetType().Name} - {e.Message}");
                Debug.LogError($"[RecastNavigation] Stack trace:\n{e.StackTrace}");
                Debug.LogError("[RecastNavigation] ===========================================");
                
                EditorUtility.DisplayDialog("오류", $"DLL 복사 실패: {e.Message}", "확인");
            }
        }
        

        
        /// <summary>
        /// Force DLL unload using multiple strategies and schedule copy
        /// </summary>
        void ScheduleDllCopyAfterDomainReload()
        {
            Debug.Log("[RecastNavigation] Attempting to unload DLL using multiple strategies...");
            
            // Strategy 1: Force garbage collection first
            Debug.Log("[RecastNavigation] Step 1: Force garbage collection");
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            // Strategy 2: Request script reload (Domain Reload)
            Debug.Log("[RecastNavigation] Step 2: Requesting script reload (Domain Reload)");
            UnityEditor.EditorUtility.RequestScriptReload();
            
                         // Strategy 3: Request compilation (alternative approach)
             Debug.Log("[RecastNavigation] Step 3: Requesting script compilation");
             try 
             {
                 UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                 
                 // Also try to compile assemblies
                 var assemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies();
                 Debug.Log($"[RecastNavigation] Found {assemblies.Length} assemblies in compilation pipeline");
             }
             catch (System.Exception ex)
             {
                 Debug.LogWarning($"[RecastNavigation] CompilationPipeline request failed: {ex.Message}");
             }
             
             // Strategy 4: Resource cleanup
             Debug.Log("[RecastNavigation] Step 4: Additional resource cleanup");
             try
             {
                 UnityEngine.Resources.UnloadUnusedAssets();
                 System.GC.Collect();
             }
             catch (System.Exception ex)
             {
                 Debug.LogWarning($"[RecastNavigation] Resource cleanup failed: {ex.Message}");
             }
            
            // EditorPrefs에 복사 예약 정보 저장
            EditorPrefs.SetString("RecastNavigation_PendingDllCopy_Source", dllPath);
            EditorPrefs.SetString("RecastNavigation_PendingDllCopy_Dest", Path.Combine("Assets/Plugins/", Path.GetFileName(dllPath)));
            EditorPrefs.SetBool("RecastNavigation_PendingDllCopy", true);
            
            // Domain Reload 이벤트 구독
            EditorApplication.delayCall += () =>
            {
                Debug.Log("[RecastNavigation] Starting Domain Reload...");
                
                // 현재 창 닫기
                Close();
                
                // Domain Reload 실행
                EditorUtility.RequestScriptReload();
            };
        }
        
        /// <summary>
        /// Domain Reload 후 예약된 DLL 복사를 실행합니다.
        /// </summary>
        [InitializeOnLoadMethod]
        static void CheckPendingDllCopy()
        {
            if (EditorPrefs.GetBool("RecastNavigation_PendingDllCopy", false))
            {
                string sourcePath = EditorPrefs.GetString("RecastNavigation_PendingDllCopy_Source", "");
                string destPath = EditorPrefs.GetString("RecastNavigation_PendingDllCopy_Dest", "");
                
                // 예약 정보 클리어
                EditorPrefs.DeleteKey("RecastNavigation_PendingDllCopy");
                EditorPrefs.DeleteKey("RecastNavigation_PendingDllCopy_Source");
                EditorPrefs.DeleteKey("RecastNavigation_PendingDllCopy_Dest");
                
                if (!string.IsNullOrEmpty(sourcePath) && !string.IsNullOrEmpty(destPath))
                {
                    EditorApplication.delayCall += () => ExecutePendingDllCopy(sourcePath, destPath);
                }
            }
        }
        
        /// <summary>
        /// 예약된 DLL 복사를 실행합니다.
        /// </summary>
        static void ExecutePendingDllCopy(string sourcePath, string destPath)
        {
            Debug.Log("[RecastNavigation] ===========================================");
            Debug.Log("[RecastNavigation] Executing DLL Copy After Domain Reload");
            Debug.Log("[RecastNavigation] ===========================================");
            
            try
            {
                Debug.Log($"[RecastNavigation] Starting DLL copy after Domain Reload:");
                Debug.Log($"[RecastNavigation] - Source: {sourcePath}");
                Debug.Log($"[RecastNavigation] - Target: {destPath}");
                
                if (!File.Exists(sourcePath))
                {
                    Debug.LogError($"[RecastNavigation] ✗ Source DLL file not found: {sourcePath}");
                    EditorUtility.DisplayDialog("오류", $"소스 DLL 파일을 찾을 수 없습니다:\n{sourcePath}", "확인");
                    return;
                }
                
                // Log source file information
                try
                {
                    var sourceInfo = new FileInfo(sourcePath);
                    Debug.Log($"[RecastNavigation] Source file size: {sourceInfo.Length} bytes");
                    Debug.Log($"[RecastNavigation] Source file last write time: {sourceInfo.LastWriteTime}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[RecastNavigation] Failed to get source file info: {ex.Message}");
                }
                
                // Create Plugins folder
                string pluginsDir = Path.GetDirectoryName(destPath);
                Debug.Log($"[RecastNavigation] Target directory: {pluginsDir}");
                
                if (!Directory.Exists(pluginsDir))
                {
                    Debug.Log("[RecastNavigation] Target directory does not exist. Creating...");
                    Directory.CreateDirectory(pluginsDir);
                    Debug.Log("[RecastNavigation] Target directory created successfully");
                }
                else
                {
                    Debug.Log("[RecastNavigation] Target directory already exists");
                }
                
                // Check if existing file exists
                if (File.Exists(destPath))
                {
                    Debug.Log("[RecastNavigation] Existing target file found. Proceeding with overwrite...");
                    try
                    {
                        var destInfo = new FileInfo(destPath);
                        Debug.Log($"[RecastNavigation] Existing file size: {destInfo.Length} bytes");
                        Debug.Log($"[RecastNavigation] Existing file last write time: {destInfo.LastWriteTime}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[RecastNavigation] Failed to get existing file info: {ex.Message}");
                    }
                }
                else
                {
                    Debug.Log("[RecastNavigation] No existing file at target location. Creating new copy...");
                }
                
                // DLL copy
                Debug.Log("[RecastNavigation] Starting file copy...");
                File.Copy(sourcePath, destPath, true);
                Debug.Log("[RecastNavigation] File copy completed");
                
                Debug.Log("[RecastNavigation] Starting AssetDatabase refresh...");
                AssetDatabase.Refresh();
                Debug.Log("[RecastNavigation] AssetDatabase refresh completed");
                
                Debug.Log($"[RecastNavigation] ✓ DLL copy after Domain Reload completed: {destPath}");
                
                // Success notification and reopen Setup Guide
                EditorUtility.DisplayDialog("성공", 
                    "Domain Reload 후 DLL이 성공적으로 복사되었습니다!\n\n" +
                    "Setup Guide를 다시 열어서 다음 단계를 진행하세요.", 
                    "Setup Guide 열기");
                
                Debug.Log("[RecastNavigation] Reopening Setup Guide...");
                // Reopen Setup Guide
                ShowWindow();
                
                Debug.Log("[RecastNavigation] ===========================================");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RecastNavigation] ✗ DLL copy after Domain Reload failed: {e.GetType().Name} - {e.Message}");
                Debug.LogError($"[RecastNavigation] Stack trace:\n{e.StackTrace}");
                Debug.LogError("[RecastNavigation] ===========================================");
                
                EditorUtility.DisplayDialog("오류", 
                    $"Domain Reload 후에도 DLL 복사에 실패했습니다:\n{e.Message}\n\n" +
                    "Unity Editor를 완전히 재시작해보세요.", 
                    "확인");
            }
        }
        
        /// <summary>
        /// Attempt powerful DLL unload using multiple advanced strategies
        /// </summary>
        void AttemptPowerfulDllUnload()
        {
            Debug.Log("[RecastNavigation] Starting powerful DLL unload sequence...");
            
            // Strategy 1: Try specific DLL unload using PluginImporter (most efficient)
            Debug.Log("[RecastNavigation] Strategy 1: Specific DLL unload using PluginImporter");
            if (TryUnloadSpecificDll())
            {
                Debug.Log("[RecastNavigation] ✓ Specific DLL unload successful, skipping other strategies");
                return;
            }
            
            // Strategy 2: Assembly-specific unload attempt
            Debug.Log("[RecastNavigation] Strategy 2: Assembly-specific unload attempt");
            TryUnloadSpecificAssembly("UnityWrapper");
            
            // Strategy 3: Aggressive garbage collection
            Debug.Log("[RecastNavigation] Strategy 3: Aggressive garbage collection");
            for (int i = 0; i < 3; i++)
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
            }
            
            // Strategy 4: Unload unused assets
            Debug.Log("[RecastNavigation] Strategy 4: Unload unused assets");
            UnityEngine.Resources.UnloadUnusedAssets();
            
            // Strategy 5: Playmode cycling (fallback method)
            Debug.Log("[RecastNavigation] Strategy 5: Playmode cycling for DLL unload");
            StartPlaymodeCycleForDllUnload();
        }
        
        /// <summary>
        /// Try to unload specific DLL using PluginImporter settings
        /// </summary>
        bool TryUnloadSpecificDll()
        {
            try
            {
                string destPath = Path.Combine(Path.Combine(Application.dataPath, "Plugins"), Path.GetFileName(dllPath));
                string relativePath = "Assets/Plugins/" + Path.GetFileName(dllPath);
                
                Debug.Log($"[RecastNavigation] Attempting specific DLL unload: {relativePath}");
                
                // Get PluginImporter for the DLL
                PluginImporter pluginImporter = AssetImporter.GetAtPath(relativePath) as PluginImporter;
                
                if (pluginImporter != null)
                {
                    Debug.Log("[RecastNavigation] Found PluginImporter, temporarily disabling DLL...");
                    
                    // Store current settings
                    bool wasEditorCompatible = pluginImporter.GetCompatibleWithEditor();
                    bool wasAnyPlatformCompatible = pluginImporter.GetCompatibleWithAnyPlatform();
                    
                    // Temporarily disable DLL
                    pluginImporter.SetCompatibleWithEditor(false);
                    pluginImporter.SetCompatibleWithAnyPlatform(false);
                    
                    // Apply changes
                    pluginImporter.SaveAndReimport();
                    
                    Debug.Log("[RecastNavigation] DLL disabled, waiting for unload...");
                    
                    // Small delay to ensure unload
                    System.Threading.Thread.Sleep(500);
                    
                    try
                    {
                        Debug.Log("[RecastNavigation] Attempting file copy while DLL is disabled...");
                        File.Copy(dllPath, destPath, true);
                        Debug.Log("[RecastNavigation] ✓ File copy successful during DLL disable");
                        
                        // Re-enable DLL with original settings
                        pluginImporter.SetCompatibleWithEditor(wasEditorCompatible);
                        pluginImporter.SetCompatibleWithAnyPlatform(wasAnyPlatformCompatible);
                        pluginImporter.SaveAndReimport();
                        
                        Debug.Log("[RecastNavigation] DLL re-enabled with new content");
                        
                        // Success! Update UI
                        dllCopied = true;
                        completedSteps[0] = true;
                        SaveSettings();
                        
                        AssetDatabase.Refresh();
                        
                                                 // Create Assembly Definition file for better DLL control
                         CreateAssemblyDefinitionFile();
                         
                         EditorUtility.DisplayDialog("성공", 
                             "특정 DLL 언로드를 통해 성공적으로 복사되었습니다!\n" +
                             "전체 Domain Reload 없이 완료되었으며,\n" +
                             "Assembly Definition 파일도 생성되었습니다.", 
                             "확인");
                        
                        Debug.Log("[RecastNavigation] ✓ Specific DLL unload and replace completed successfully");
                        return true;
                    }
                    catch (System.Exception copyEx)
                    {
                        Debug.LogWarning($"[RecastNavigation] File copy failed even with DLL disabled: {copyEx.Message}");
                        
                        // Restore original settings
                        pluginImporter.SetCompatibleWithEditor(wasEditorCompatible);
                        pluginImporter.SetCompatibleWithAnyPlatform(wasAnyPlatformCompatible);
                        pluginImporter.SaveAndReimport();
                        
                        return false;
                    }
                }
                else
                {
                    Debug.LogWarning("[RecastNavigation] Could not find PluginImporter for target DLL");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RecastNavigation] Specific DLL unload failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Try to unload specific assembly by name
        /// </summary>
        void TryUnloadSpecificAssembly(string assemblyName)
        {
            try
            {
                Debug.Log($"[RecastNavigation] Attempting to unload assembly: {assemblyName}");
                
                // Get all loaded assemblies
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                var targetAssembly = assemblies.FirstOrDefault(a => a.GetName().Name.Contains(assemblyName));
                
                if (targetAssembly != null)
                {
                    Debug.Log($"[RecastNavigation] Found target assembly: {targetAssembly.FullName}");
                    
                    // Get all types from assembly (this can help release references)
                    var types = targetAssembly.GetTypes();
                    Debug.Log($"[RecastNavigation] Assembly contains {types.Length} types");
                    
                    // Clear references and force GC
                    targetAssembly = null;
                    types = null;
                    
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                    System.GC.Collect();
                    
                    Debug.Log("[RecastNavigation] Assembly reference cleared and GC executed");
                }
                else
                {
                    Debug.LogWarning($"[RecastNavigation] Assembly containing '{assemblyName}' not found");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RecastNavigation] Assembly unload attempt failed: {ex.Message}");
            }
                 }
         
         /// <summary>
         /// Create Assembly Definition file for better DLL control
         /// </summary>
         void CreateAssemblyDefinitionFile()
         {
             try
             {
                 string pluginsPath = Path.Combine(Application.dataPath, "Plugins");
                 string asmdefPath = Path.Combine(pluginsPath, "RecastNavigation.Runtime.asmdef");
                 
                 if (File.Exists(asmdefPath))
                 {
                     Debug.Log("[RecastNavigation] Assembly Definition file already exists, skipping creation");
                     return;
                 }
                 
                 Debug.Log("[RecastNavigation] Creating Assembly Definition file for better DLL control...");
                 
                 // Assembly Definition JSON content
                 string asmdefContent = @"{
    ""name"": ""RecastNavigation.Runtime"",
    ""rootNamespace"": ""RecastNavigation"",
    ""references"": [],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": true,
    ""precompiledReferences"": [
        ""UnityWrapper.dll""
    ],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}";
                 
                 File.WriteAllText(asmdefPath, asmdefContent);
                 Debug.Log($"[RecastNavigation] ✓ Assembly Definition file created: {asmdefPath}");
                 
                 // Also create meta file to avoid import issues
                 string metaPath = asmdefPath + ".meta";
                 if (!File.Exists(metaPath))
                 {
                     string metaContent = @"fileFormatVersion: 2
guid: " + System.Guid.NewGuid().ToString("N") + @"
AssemblyDefinitionImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
";
                     File.WriteAllText(metaPath, metaContent);
                     Debug.Log("[RecastNavigation] ✓ Assembly Definition meta file created");
                 }
             }
             catch (System.Exception ex)
             {
                 Debug.LogWarning($"[RecastNavigation] Failed to create Assembly Definition file: {ex.Message}");
             }
         }
         
         /// <summary>
         /// Use playmode cycling to force DLL unload (most effective method)
         /// </summary>
        void StartPlaymodeCycleForDllUnload()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                Debug.Log("[RecastNavigation] Already in play mode, skipping playmode cycle");
                ScheduleDllCopyAfterDomainReload();
                return;
            }
            
            Debug.Log("[RecastNavigation] Starting playmode cycle to force DLL unload...");
            
            // Store the DLL copy request for after playmode cycle
            string destPath = Path.Combine(Path.Combine(Application.dataPath, "Plugins"), Path.GetFileName(dllPath));
            EditorPrefs.SetString("RecastNavigation_PendingDllCopy_Source", dllPath);
            EditorPrefs.SetString("RecastNavigation_PendingDllCopy_Dest", destPath);
            EditorPrefs.SetBool("RecastNavigation_PlaymodeCycleInProgress", true);
            
            // Hook into playmode state changes
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChangedForDllUnload;
            
            // Enter play mode
            Debug.Log("[RecastNavigation] Entering play mode to unload DLL...");
            UnityEditor.EditorApplication.isPlaying = true;
        }
        
        /// <summary>
        /// Handle playmode state changes for DLL unloading
        /// </summary>
        static void OnPlayModeStateChangedForDllUnload(UnityEditor.PlayModeStateChange state)
        {
            Debug.Log($"[RecastNavigation] Playmode state changed: {state}");
            
            if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode)
            {
                Debug.Log("[RecastNavigation] Entered play mode, will exit shortly to unload DLL...");
                // Exit play mode after a short delay
                UnityEditor.EditorApplication.delayCall += () => {
                    if (UnityEditor.EditorApplication.isPlaying)
                    {
                        Debug.Log("[RecastNavigation] Exiting play mode to complete DLL unload...");
                        UnityEditor.EditorApplication.isPlaying = false;
                    }
                };
            }
            else if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                Debug.Log("[RecastNavigation] Exited play mode, DLL should be unloaded now");
                
                // Unhook the event
                UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChangedForDllUnload;
                
                // Check if we have a pending DLL copy
                if (EditorPrefs.GetBool("RecastNavigation_PlaymodeCycleInProgress", false))
                {
                    EditorPrefs.DeleteKey("RecastNavigation_PlaymodeCycleInProgress");
                    
                    string sourcePath = EditorPrefs.GetString("RecastNavigation_PendingDllCopy_Source", "");
                    string destPath = EditorPrefs.GetString("RecastNavigation_PendingDllCopy_Dest", "");
                    
                    if (!string.IsNullOrEmpty(sourcePath) && !string.IsNullOrEmpty(destPath))
                    {
                        Debug.Log("[RecastNavigation] Executing pending DLL copy after playmode cycle...");
                        
                        // Small delay to ensure everything is settled
                        UnityEditor.EditorApplication.delayCall += () => {
                            ExecutePendingDllCopy(sourcePath, destPath);
                        };
                    }
                }
            }
        }

        /// <summary>
        /// 파일을 사용하고 있는 프로세스 목록을 가져옵니다. (개선된 버전)
        /// </summary>
        /// <param name="filePath">확인할 파일 경로</param>
        /// <returns>프로세스 정보 문자열</returns>
        string GetProcessesUsingFile(string filePath)
        {
            Debug.Log($"[RecastNavigation] Starting process check for file usage: {filePath}");
            
            try
            {
                // Check processes using file with handle.exe tool
                // (if Windows Sysinternals tool is installed)
                Debug.Log("[RecastNavigation] Attempting process check using handle.exe tool...");
                
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "handle.exe",
                    Arguments = $"\"{filePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        
                        Debug.Log($"[RecastNavigation] handle.exe execution result - ExitCode: {process.ExitCode}");
                        
                        if (!string.IsNullOrEmpty(error))
                        {
                            Debug.LogWarning($"[RecastNavigation] handle.exe error: {error}");
                        }
                        
                        if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                        {
                            Debug.Log($"[RecastNavigation] handle.exe output:\n{output}");
                            string parsedResult = ParseHandleOutput(output);
                            if (!string.IsNullOrEmpty(parsedResult))
                            {
                                Debug.Log($"[RecastNavigation] Parsed process information:\n{parsedResult}");
                                return parsedResult;
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[RecastNavigation] handle.exe execution failed or no output");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[RecastNavigation] Failed to start handle.exe process");
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Debug.LogWarning($"[RecastNavigation] Cannot find handle.exe: {ex.Message}");
                Debug.Log("[RecastNavigation] Windows Sysinternals handle.exe is not installed or not in PATH");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RecastNavigation] Error executing handle.exe: {ex.Message}");
                Debug.LogWarning($"[RecastNavigation] Error type: {ex.GetType().Name}");
            }

            // Fall back to general process check if handle.exe is unavailable or fails
            Debug.Log("[RecastNavigation] Falling back to general process check...");
            return GetCommonProcessesInfo(filePath);
        }

        /// <summary>
        /// handle.exe 출력을 파싱합니다.
        /// </summary>
        string ParseHandleOutput(string output)
        {
            var lines = output.Split('\n');
            var processes = new System.Collections.Generic.List<string>();
            
            foreach (var line in lines)
            {
                if (line.Contains(".exe") && line.Contains("pid:"))
                {
                    processes.Add(line.Trim());
                }
            }
            
            return processes.Count > 0 ? string.Join("\n", processes) : "";
        }

        /// <summary>
        /// 일반적으로 DLL을 사용할 수 있는 프로세스들을 확인합니다. (개선된 버전)
        /// </summary>
        string GetCommonProcessesInfo(string filePath)
        {
            var runningProcesses = new System.Collections.Generic.List<string>();
            string fileName = Path.GetFileName(filePath);
            
            Debug.Log($"[RecastNavigation] Starting general process check - Target file: {fileName}");
            
            try
            {
                var processes = System.Diagnostics.Process.GetProcesses();
                Debug.Log($"[RecastNavigation] Total process count: {processes.Length}");
                
                int checkedCount = 0;
                int suspiciousCount = 0;
                
                foreach (var process in processes)
                {
                    try
                    {
                        checkedCount++;
                        string processName = process.ProcessName.ToLower();
                        
                        if (processName.Contains("unity") ||
                            processName.Contains("devenv") ||
                            processName.Contains("code") ||
                            processName.Contains("rider") ||
                            processName.Contains("msbuild") ||
                            processName.Contains("dotnet"))
                        {
                            suspiciousCount++;
                            string processInfo = $"{process.ProcessName} (PID: {process.Id})";
                            runningProcesses.Add(processInfo);
                            Debug.Log($"[RecastNavigation] Suspicious process found: {processInfo}");
                            
                            // Also log process start time
                            try
                            {
                                Debug.Log($"[RecastNavigation] - Start time: {process.StartTime}");
                                Debug.Log($"[RecastNavigation] - Working directory: {process.StartInfo.WorkingDirectory}");
                            }
                            catch (System.Exception innerEx)
                            {
                                Debug.LogWarning($"[RecastNavigation] - Process info access restricted: {innerEx.Message}");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // Some processes may have restricted access
                        Debug.LogWarning($"[RecastNavigation] Process access restricted (PID {process.Id}): {ex.Message}");
                    }
                }
                
                Debug.Log($"[RecastNavigation] Process check completed - Checked: {checkedCount}, Suspicious: {suspiciousCount}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RecastNavigation] Error checking process list: {ex.Message}");
                Debug.LogError($"[RecastNavigation] Stack trace:\n{ex.StackTrace}");
            }

            if (runningProcesses.Count > 0)
            {
                string result = "의심되는 프로세스들:\n" + string.Join("\n", runningProcesses);
                Debug.Log($"[RecastNavigation] Final result:\n{result}");
                return result;
            }
            
            string fallbackMsg = "Unity, Visual Studio, VS Code, Rider 등이 실행 중인지 확인해보세요.";
            Debug.Log($"[RecastNavigation] No suspicious processes found: {fallbackMsg}");
            return fallbackMsg;
        }

        /// <summary>
        /// 파일을 다른 이름으로 복사합니다. (개선된 버전)
        /// </summary>
        bool CopyWithAlternateName(string sourcePath, string destPath)
        {
            Debug.Log("[RecastNavigation] -------------------------------------------");
            Debug.Log("[RecastNavigation] Starting Backup Method DLL Copy");
            Debug.Log("[RecastNavigation] -------------------------------------------");
            
            try
            {
                string directory = Path.GetDirectoryName(destPath);
                string fileName = Path.GetFileNameWithoutExtension(destPath);
                string extension = Path.GetExtension(destPath);
                
                Debug.Log($"[RecastNavigation] Backup path composition:");
                Debug.Log($"[RecastNavigation] - Directory: {directory}");
                Debug.Log($"[RecastNavigation] - File name: {fileName}");
                Debug.Log($"[RecastNavigation] - Extension: {extension}");
                
                // Rename existing file to backup
                string backupPath = Path.Combine(directory, $"{fileName}_backup_{System.DateTime.Now:yyyyMMdd_HHmmss}{extension}");
                Debug.Log($"[RecastNavigation] Backup file path: {backupPath}");
                
                if (File.Exists(destPath))
                {
                    Debug.Log("[RecastNavigation] Existing file found. Moving to backup...");
                    
                    // Log existing file information
                    try
                    {
                        var destInfo = new FileInfo(destPath);
                        Debug.Log($"[RecastNavigation] Existing file size: {destInfo.Length} bytes");
                        Debug.Log($"[RecastNavigation] Existing file last write time: {destInfo.LastWriteTime}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[RecastNavigation] Failed to get existing file info: {ex.Message}");
                    }
                    
                    File.Move(destPath, backupPath);
                    Debug.Log($"[RecastNavigation] ✓ Existing file moved to backup completed: {backupPath}");
                }
                else
                {
                    Debug.Log("[RecastNavigation] No existing file found. Skipping backup step");
                }
                
                // Copy new file
                Debug.Log("[RecastNavigation] Starting new file copy...");
                File.Copy(sourcePath, destPath, false);
                Debug.Log("[RecastNavigation] New file copy completed");
                
                Debug.Log("[RecastNavigation] Starting AssetDatabase refresh...");
                AssetDatabase.Refresh();
                Debug.Log("[RecastNavigation] AssetDatabase refresh completed");
                
                dllCopied = true;
                completedSteps[0] = true;
                SaveSettings();
                Debug.Log("[RecastNavigation] Settings saved");
                
                EditorUtility.DisplayDialog("성공", 
                    $"DLL이 성공적으로 복사되었습니다.\n기존 파일은 백업되었습니다:\n{backupPath}", 
                    "확인");
                Debug.Log($"[RecastNavigation] ✓ DLL backup copy completed: {destPath}");
                Debug.Log("[RecastNavigation] -------------------------------------------");
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RecastNavigation] ✗ Backup copy failed: {ex.GetType().Name} - {ex.Message}");
                Debug.LogError($"[RecastNavigation] Stack trace:\n{ex.StackTrace}");
                Debug.LogError("[RecastNavigation] -------------------------------------------");
                
                EditorUtility.DisplayDialog("오류", $"백업 복사 실패: {ex.Message}", "확인");
                return false;
            }
        }

        /// <summary>
        /// 파일의 MD5 해시 값을 계산합니다. (개선된 버전)
        /// </summary>
        /// <param name="filePath">파일 경로</param>
        /// <returns>MD5 해시 문자열</returns>
        /// <exception cref="System.IO.IOException">파일이 사용 중이거나 접근할 수 없는 경우</exception>
        /// <exception cref="System.UnauthorizedAccessException">파일 접근 권한이 없는 경우</exception>
        string GetFileMD5Hash(string filePath)
        {
            const int maxRetries = 3;
            const int retryDelayMs = 200;
            
            Debug.Log($"[RecastNavigation] Starting MD5 hash calculation: {filePath}");
            
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    // Use using statements to ensure automatic resource disposal
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var md5 = MD5.Create())
                    {
                        Debug.Log($"[RecastNavigation] File stream opened successfully (attempt {retry + 1}/{maxRetries}): {filePath}");
                        
                        // Check file size
                        Debug.Log($"[RecastNavigation] File size: {stream.Length} bytes");
                        
                        byte[] hash = md5.ComputeHash(stream);
                        string hashString = System.BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        
                        Debug.Log($"[RecastNavigation] MD5 hash calculation completed: {hashString}");
                        return hashString;
                    }
                }
                catch (System.IO.IOException ex) when (retry < maxRetries - 1)
                {
                    // Retry only if not the last attempt
                    Debug.LogWarning($"[RecastNavigation] MD5 calculation retry {retry + 1}/{maxRetries} - IOException: {ex.Message}");
                    Debug.LogWarning($"[RecastNavigation] Retrying after {retryDelayMs}ms...");
                    System.Threading.Thread.Sleep(retryDelayMs);
                    continue;
                }
                catch (System.UnauthorizedAccessException ex)
                {
                    // Permission errors are not worth retrying
                    Debug.LogError($"[RecastNavigation] MD5 calculation permission error: {ex.Message}");
                    Debug.LogError($"[RecastNavigation] File path: {filePath}");
                    throw;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[RecastNavigation] MD5 calculation unexpected error (attempt {retry + 1}/{maxRetries}): {ex.GetType().Name} - {ex.Message}");
                    Debug.LogError($"[RecastNavigation] Stack trace:\n{ex.StackTrace}");
                    
                    if (retry == maxRetries - 1) throw;
                    System.Threading.Thread.Sleep(retryDelayMs);
                }
            }
            
            // Throw exception if all retries failed
            string errorMsg = $"Cannot calculate MD5 hash for file '{filePath}'. File is in use or inaccessible.";
            Debug.LogError($"[RecastNavigation] {errorMsg}");
            throw new System.IO.IOException(errorMsg);
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
                SaveSettings(); // 설정 자동 저장
                
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
                    SaveSettings(); // 설정 자동 저장
                    
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
                    SaveSettings(); // 설정 자동 저장
                    
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
                SaveSettings(); // 설정 자동 저장
                
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
        
        #region 설정 저장/로드
        
        /// <summary>
        /// 설정을 저장합니다
        /// </summary>
        void SaveSettings()
        {
            try
            {
                // 경로 설정 저장
                EditorPrefs.SetString(PREF_DLL_PATH, dllPath ?? "");
                EditorPrefs.SetString(PREF_SCRIPTS_PATH, scriptsPath ?? "");
                EditorPrefs.SetBool(PREF_AUTO_SETUP, autoSetup);
                EditorPrefs.SetInt(PREF_CURRENT_STEP, currentStep);
                
                // 완료된 단계들 저장 (비트마스크로 저장)
                int completedStepsMask = 0;
                for (int i = 0; i < completedSteps.Length; i++)
                {
                    if (completedSteps[i])
                    {
                        completedStepsMask |= (1 << i);
                    }
                }
                EditorPrefs.SetInt(PREF_COMPLETED_STEPS, completedStepsMask);
                
                Debug.Log("RecastNavigation 설정이 저장되었습니다.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"설정 저장 실패: {e.Message}");
            }
        }
        
        /// <summary>
        /// 설정을 로드합니다
        /// </summary>
        void LoadSettings()
        {
            try
            {
                // 경로 설정 로드
                dllPath = EditorPrefs.GetString(PREF_DLL_PATH, "");
                scriptsPath = EditorPrefs.GetString(PREF_SCRIPTS_PATH, "");
                autoSetup = EditorPrefs.GetBool(PREF_AUTO_SETUP, false);
                currentStep = EditorPrefs.GetInt(PREF_CURRENT_STEP, 0);
                
                // 완료된 단계들 로드 (비트마스크에서 복원)
                int completedStepsMask = EditorPrefs.GetInt(PREF_COMPLETED_STEPS, 0);
                for (int i = 0; i < completedSteps.Length; i++)
                {
                    completedSteps[i] = (completedStepsMask & (1 << i)) != 0;
                }
                
                // 상태 변수들 업데이트
                dllCopied = completedSteps[0];
                scriptsImported = completedSteps[1];
                navMeshBuilt = completedSteps[2];
                pathfindingTested = completedSteps[3];
                
                Debug.Log("RecastNavigation 설정이 로드되었습니다.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"설정 로드 실패: {e.Message}");
                // 실패 시 기본값으로 초기화
                ResetAllSettings();
            }
        }
        
        /// <summary>
        /// 모든 설정을 초기화합니다
        /// </summary>
        void ResetAllSettings()
        {
            try
            {
                // EditorPrefs에서 설정 삭제
                EditorPrefs.DeleteKey(PREF_DLL_PATH);
                EditorPrefs.DeleteKey(PREF_SCRIPTS_PATH);
                EditorPrefs.DeleteKey(PREF_AUTO_SETUP);
                EditorPrefs.DeleteKey(PREF_COMPLETED_STEPS);
                EditorPrefs.DeleteKey(PREF_CURRENT_STEP);
                
                // 변수들 초기화
                dllPath = "";
                scriptsPath = "";
                autoSetup = false;
                currentStep = 0;
                
                dllCopied = false;
                scriptsImported = false;
                navMeshBuilt = false;
                pathfindingTested = false;
                
                for (int i = 0; i < completedSteps.Length; i++)
                {
                    completedSteps[i] = false;
                }
                
                Debug.Log("RecastNavigation 설정이 초기화되었습니다.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"설정 초기화 실패: {e.Message}");
            }
        }
        
        /// <summary>
        /// 설정 정보를 표시합니다
        /// </summary>
        void ShowSettingsInfo()
        {
            string info = $"=== RecastNavigation 설정 정보 ===\n";
            info += $"DLL 경로: {dllPath}\n";
            info += $"스크립트 경로: {scriptsPath}\n";
            info += $"자동 설정: {autoSetup}\n";
            info += $"현재 단계: {currentStep}\n";
            info += $"완료된 단계: ";
            
            for (int i = 0; i < completedSteps.Length; i++)
            {
                if (completedSteps[i])
                {
                    info += $"{i + 1} ";
                }
            }
            
            Debug.Log(info);
            EditorUtility.DisplayDialog("설정 정보", info, "확인");
        }
        
        #endregion
    }
} 