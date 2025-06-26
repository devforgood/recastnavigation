using UnityEngine;
using UnityEditor;

namespace RecastNavigation
{
    /// <summary>
    /// NavMeshGizmo의 에디터 전용 시각화 컴포넌트
    /// </summary>
    [CustomEditor(typeof(NavMeshGizmo))]
    public class NavMeshGizmoEditor : Editor
    {
        private NavMeshGizmo gizmo;
        private bool showSettings = true;
        private bool showColors = true;
        private bool showInfo = true;
        
        private void OnEnable()
        {
            gizmo = (NavMeshGizmo)target;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("NavMesh 기즈모 설정", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 시각화 설정
            showSettings = EditorGUILayout.Foldout(showSettings, "시각화 설정");
            if (showSettings)
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty showNavMesh = serializedObject.FindProperty("showNavMesh");
                SerializedProperty showWireframe = serializedObject.FindProperty("showWireframe");
                SerializedProperty showFaces = serializedObject.FindProperty("showFaces");
                SerializedProperty showVertices = serializedObject.FindProperty("showVertices");
                
                EditorGUILayout.PropertyField(showNavMesh, new GUIContent("NavMesh 표시"));
                EditorGUILayout.PropertyField(showWireframe, new GUIContent("와이어프레임 표시"));
                EditorGUILayout.PropertyField(showFaces, new GUIContent("면 표시"));
                EditorGUILayout.PropertyField(showVertices, new GUIContent("정점 표시"));
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 색상 설정
            showColors = EditorGUILayout.Foldout(showColors, "색상 설정");
            if (showColors)
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty navMeshColor = serializedObject.FindProperty("navMeshColor");
                SerializedProperty wireframeColor = serializedObject.FindProperty("wireframeColor");
                SerializedProperty vertexColor = serializedObject.FindProperty("vertexColor");
                
                EditorGUILayout.PropertyField(navMeshColor, new GUIContent("NavMesh 색상"));
                EditorGUILayout.PropertyField(wireframeColor, new GUIContent("와이어프레임 색상"));
                EditorGUILayout.PropertyField(vertexColor, new GUIContent("정점 색상"));
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 크기 설정
            SerializedProperty vertexSize = serializedObject.FindProperty("vertexSize");
            SerializedProperty lineWidth = serializedObject.FindProperty("lineWidth");
            
            EditorGUILayout.PropertyField(vertexSize, new GUIContent("정점 크기"));
            EditorGUILayout.PropertyField(lineWidth, new GUIContent("선 두께"));
            
            EditorGUILayout.Space();
            
            // 자동 업데이트 설정
            SerializedProperty autoUpdate = serializedObject.FindProperty("autoUpdate");
            SerializedProperty updateInterval = serializedObject.FindProperty("updateInterval");
            
            EditorGUILayout.PropertyField(autoUpdate, new GUIContent("자동 업데이트"));
            if (autoUpdate.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(updateInterval, new GUIContent("업데이트 간격"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 정보 표시
            showInfo = EditorGUILayout.Foldout(showInfo, "NavMesh 정보");
            if (showInfo)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("정점 수", RecastNavigationWrapper.GetVertexCount().ToString());
                EditorGUILayout.LabelField("폴리곤 수", RecastNavigationWrapper.GetPolyCount().ToString());
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 버튼들
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("데이터 새로고침"))
            {
                gizmo.UpdateNavMeshData();
                SceneView.RepaintAll();
            }
            
            if (GUILayout.Button("기즈모 새로고침"))
            {
                SceneView.RepaintAll();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 프리셋 버튼들
            EditorGUILayout.LabelField("빠른 설정", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("기본"))
            {
                SetDefaultSettings();
            }
            
            if (GUILayout.Button("와이어프레임만"))
            {
                SetWireframeOnlySettings();
            }
            
            if (GUILayout.Button("면만"))
            {
                SetFacesOnlySettings();
            }
            
            EditorGUILayout.EndHorizontal();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// 기본 설정 적용
        /// </summary>
        private void SetDefaultSettings()
        {
            SerializedProperty showNavMesh = serializedObject.FindProperty("showNavMesh");
            SerializedProperty showWireframe = serializedObject.FindProperty("showWireframe");
            SerializedProperty showFaces = serializedObject.FindProperty("showFaces");
            SerializedProperty showVertices = serializedObject.FindProperty("showVertices");
            
            showNavMesh.boolValue = true;
            showWireframe.boolValue = true;
            showFaces.boolValue = true;
            showVertices.boolValue = false;
            
            serializedObject.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// 와이어프레임만 표시 설정
        /// </summary>
        private void SetWireframeOnlySettings()
        {
            SerializedProperty showNavMesh = serializedObject.FindProperty("showNavMesh");
            SerializedProperty showWireframe = serializedObject.FindProperty("showWireframe");
            SerializedProperty showFaces = serializedObject.FindProperty("showFaces");
            SerializedProperty showVertices = serializedObject.FindProperty("showVertices");
            
            showNavMesh.boolValue = true;
            showWireframe.boolValue = true;
            showFaces.boolValue = false;
            showVertices.boolValue = false;
            
            serializedObject.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// 면만 표시 설정
        /// </summary>
        private void SetFacesOnlySettings()
        {
            SerializedProperty showNavMesh = serializedObject.FindProperty("showNavMesh");
            SerializedProperty showWireframe = serializedObject.FindProperty("showWireframe");
            SerializedProperty showFaces = serializedObject.FindProperty("showFaces");
            SerializedProperty showVertices = serializedObject.FindProperty("showVertices");
            
            showNavMesh.boolValue = true;
            showWireframe.boolValue = false;
            showFaces.boolValue = true;
            showVertices.boolValue = false;
            
            serializedObject.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// Scene 뷰에서 기즈모 그리기
        /// </summary>
        private void OnSceneGUI()
        {
            if (gizmo == null) return;
            
            // Scene 뷰에서 추가적인 시각화 요소들
            DrawSceneGUI();
        }
        
        /// <summary>
        /// Scene GUI 그리기
        /// </summary>
        private void DrawSceneGUI()
        {
            // 현재 선택된 오브젝트가 NavMeshGizmo인 경우에만 추가 정보 표시
            if (Selection.activeGameObject == gizmo.gameObject)
            {
                // NavMesh 정보를 Scene 뷰에 표시
                Handles.BeginGUI();
                
                GUILayout.BeginArea(new Rect(10, 10, 250, 150));
                GUILayout.BeginVertical("box");
                
                GUILayout.Label("NavMesh 기즈모", EditorGUIUtility.isProSkin ? GUI.skin.label : GUI.skin.box);
                GUILayout.Label($"정점: {RecastNavigationWrapper.GetVertexCount()}");
                GUILayout.Label($"폴리곤: {RecastNavigationWrapper.GetPolyCount()}");
                
                if (GUILayout.Button("새로고침"))
                {
                    gizmo.UpdateNavMeshData();
                    SceneView.RepaintAll();
                }
                
                GUILayout.EndVertical();
                GUILayout.EndArea();
                
                Handles.EndGUI();
            }
        }
    }
} 