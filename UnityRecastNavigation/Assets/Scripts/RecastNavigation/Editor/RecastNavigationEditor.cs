using UnityEngine;
using UnityEditor;
using RecastNavigation;
using System.IO;
using System.Collections.Generic;

namespace RecastNavigation.Editor
{
    /// <summary>
    /// RecastNavigation 에디터 윈도우
    /// </summary>
    public class RecastNavigationEditor : EditorWindow
    {
        private RecastNavigationComponent navComponent;
        private NavMeshBuildSettings buildSettings;
        private bool showAdvancedSettings = false;
        private string loadPath = "";

        [MenuItem("Window/RecastNavigation/Editor")]
        public static void ShowWindow()
        {
            GetWindow<RecastNavigationEditor>("RecastNavigation Editor");
        }

        void OnEnable()
        {
            // 기본 설정 초기화
            buildSettings = NavMeshBuildSettingsExtensions.CreateDefault();
        }

        void OnGUI()
        {
            GUILayout.Label("RecastNavigation Editor", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // 컴포넌트 찾기
            if (navComponent == null)
            {
                navComponent = FindObjectOfType<RecastNavigationComponent>();
                if (navComponent == null)
                {
                    EditorGUILayout.HelpBox("씬에서 RecastNavigationComponent를 찾을 수 없습니다.", MessageType.Warning);
                    if (GUILayout.Button("컴포넌트 추가"))
                    {
                        GameObject go = new GameObject("RecastNavigation");
                        navComponent = go.AddComponent<RecastNavigationComponent>();
                        Selection.activeGameObject = go;
                    }
                    return;
                }
            }

            // 상태 표시
            EditorGUILayout.LabelField("상태", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("초기화됨", navComponent.IsInitialized.ToString());
            EditorGUILayout.LabelField("NavMesh 로드됨", navComponent.IsNavMeshLoaded.ToString());
            EditorGUILayout.LabelField("폴리곤 수", navComponent.PolyCount.ToString());
            EditorGUILayout.LabelField("정점 수", navComponent.VertexCount.ToString());

            GUILayout.Space(10);

            // 빌드 설정
            EditorGUILayout.LabelField("빌드 설정", EditorStyles.boldLabel);
            
            buildSettings.cellSize = EditorGUILayout.FloatField("Cell Size", buildSettings.cellSize);
            buildSettings.cellHeight = EditorGUILayout.FloatField("Cell Height", buildSettings.cellHeight);
            buildSettings.walkableSlopeAngle = EditorGUILayout.FloatField("Walkable Slope Angle", buildSettings.walkableSlopeAngle);
            buildSettings.walkableHeight = EditorGUILayout.FloatField("Walkable Height", buildSettings.walkableHeight);
            buildSettings.walkableRadius = EditorGUILayout.FloatField("Walkable Radius", buildSettings.walkableRadius);
            buildSettings.walkableClimb = EditorGUILayout.FloatField("Walkable Climb", buildSettings.walkableClimb);

            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "고급 설정");
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                buildSettings.minRegionArea = EditorGUILayout.FloatField("Min Region Area", buildSettings.minRegionArea);
                buildSettings.mergeRegionArea = EditorGUILayout.FloatField("Merge Region Area", buildSettings.mergeRegionArea);
                buildSettings.maxVertsPerPoly = EditorGUILayout.IntField("Max Verts Per Poly", buildSettings.maxVertsPerPoly);
                buildSettings.detailSampleDist = EditorGUILayout.FloatField("Detail Sample Dist", buildSettings.detailSampleDist);
                buildSettings.detailSampleMaxError = EditorGUILayout.FloatField("Detail Sample Max Error", buildSettings.detailSampleMaxError);
                buildSettings.autoTransformCoordinates = EditorGUILayout.Toggle("Auto Transform Coordinates", buildSettings.autoTransformCoordinates);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);

            // 프리셋 버튼들
            EditorGUILayout.LabelField("빠른 설정", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("기본 설정"))
            {
                buildSettings = NavMeshBuildSettingsExtensions.CreateDefault();
            }
            if (GUILayout.Button("고품질"))
            {
                buildSettings = NavMeshBuildSettingsExtensions.CreateHighQuality();
            }
            if (GUILayout.Button("저품질"))
            {
                buildSettings = NavMeshBuildSettingsExtensions.CreateLowQuality();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 액션 버튼들
            EditorGUILayout.LabelField("액션", EditorStyles.boldLabel);
            
            if (GUILayout.Button("NavMesh 빌드 (씬)"))
            {
                BuildNavMeshFromScene();
            }

            if (GUILayout.Button("NavMesh 정보 출력"))
            {
                PrintNavMeshInfo();
            }

            GUILayout.Space(10);

            // 파일 로드/저장
            EditorGUILayout.LabelField("파일", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            loadPath = EditorGUILayout.TextField("파일 경로", loadPath);
            if (GUILayout.Button("찾아보기", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("NavMesh 파일 선택", "", "navmesh");
                if (!string.IsNullOrEmpty(path))
                {
                    loadPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("NavMesh 로드"))
            {
                LoadNavMeshFromFile();
            }

            if (GUILayout.Button("NavMesh 저장"))
            {
                SaveNavMeshToFile();
            }
        }

        /// <summary>
        /// 씬에서 NavMesh 빌드
        /// </summary>
        private void BuildNavMeshFromScene()
        {
            if (navComponent == null) return;

            try
            {
                EditorUtility.DisplayProgressBar("NavMesh 빌드", "빌드 중...", 0f);
                
                bool success = navComponent.BuildNavMeshFromScene();
                
                EditorUtility.ClearProgressBar();
                
                if (success)
                {
                    EditorUtility.DisplayDialog("성공", "NavMesh 빌드가 완료되었습니다.", "확인");
                    Debug.Log("NavMesh 빌드 완료!");
                }
                else
                {
                    EditorUtility.DisplayDialog("오류", "NavMesh 빌드에 실패했습니다.", "확인");
                    Debug.LogError("NavMesh 빌드 실패!");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("오류", $"NavMesh 빌드 중 오류가 발생했습니다: {e.Message}", "확인");
                Debug.LogError($"NavMesh 빌드 중 오류: {e.Message}");
            }
        }

        /// <summary>
        /// NavMesh 정보 출력
        /// </summary>
        private void PrintNavMeshInfo()
        {
            if (navComponent == null) return;

            if (navComponent.IsNavMeshLoaded)
            {
                Debug.Log("=== NavMesh 정보 ===");
                Debug.Log($"폴리곤 수: {navComponent.PolyCount}");
                Debug.Log($"정점 수: {navComponent.VertexCount}");
                Debug.Log($"경로 길이: {navComponent.PathLength}");
                Debug.Log("==================");
            }
            else
            {
                Debug.Log("NavMesh가 로드되지 않았습니다.");
            }
        }

        /// <summary>
        /// 파일에서 NavMesh 로드
        /// </summary>
        private void LoadNavMeshFromFile()
        {
            if (navComponent == null || string.IsNullOrEmpty(loadPath)) return;

            try
            {
                byte[] data = File.ReadAllBytes(loadPath);
                bool success = navComponent.LoadNavMesh(data);
                
                if (success)
                {
                    EditorUtility.DisplayDialog("성공", "NavMesh 로드가 완료되었습니다.", "확인");
                    Debug.Log($"NavMesh 로드 완료: {loadPath}");
                }
                else
                {
                    EditorUtility.DisplayDialog("오류", "NavMesh 로드에 실패했습니다.", "확인");
                    Debug.LogError("NavMesh 로드 실패!");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("오류", $"NavMesh 로드 중 오류가 발생했습니다: {e.Message}", "확인");
                Debug.LogError($"NavMesh 로드 중 오류: {e.Message}");
            }
        }

        /// <summary>
        /// 파일에 NavMesh 저장
        /// </summary>
        private void SaveNavMeshToFile()
        {
            if (navComponent == null || !navComponent.IsNavMeshLoaded) return;

            try
            {
                string path = EditorUtility.SaveFilePanel("NavMesh 저장", "", "navmesh", "navmesh");
                if (!string.IsNullOrEmpty(path))
                {
                    byte[] data = navComponent.GetNavMeshData();
                    File.WriteAllBytes(path, data);
                    
                    EditorUtility.DisplayDialog("성공", "NavMesh 저장이 완료되었습니다.", "확인");
                    Debug.Log($"NavMesh 저장 완료: {path}");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("오류", $"NavMesh 저장 중 오류가 발생했습니다: {e.Message}", "확인");
                Debug.LogError($"NavMesh 저장 중 오류: {e.Message}");
            }
        }
    }
} 