using UnityEngine;
using UnityEditor;

namespace RecastNavigation.Editor
{
    /// <summary>
    /// NavMeshGizmo 커스텀 에디터
    /// </summary>
    [CustomEditor(typeof(NavMeshGizmo))]
    public class NavMeshGizmoEditor : UnityEditor.Editor
    {
        private SerializedProperty showNavMeshProp;
        private SerializedProperty showWireframeProp;
        private SerializedProperty showFacesProp;
        private SerializedProperty showVerticesProp;
        private SerializedProperty navMeshColorProp;
        private SerializedProperty wireframeColorProp;
        private SerializedProperty faceColorProp;
        private SerializedProperty vertexColorProp;
        private SerializedProperty lineWidthProp;
        private SerializedProperty lodDistanceProp;
        private SerializedProperty enableClickToEditProp;
        private SerializedProperty hoveredPointProp;

        void OnEnable()
        {
            showNavMeshProp = serializedObject.FindProperty("showNavMesh");
            showWireframeProp = serializedObject.FindProperty("showWireframe");
            showFacesProp = serializedObject.FindProperty("showFaces");
            showVerticesProp = serializedObject.FindProperty("showVertices");
            navMeshColorProp = serializedObject.FindProperty("navMeshColor");
            wireframeColorProp = serializedObject.FindProperty("wireframeColor");
            faceColorProp = serializedObject.FindProperty("faceColor");
            vertexColorProp = serializedObject.FindProperty("vertexColor");
            lineWidthProp = serializedObject.FindProperty("lineWidth");
            lodDistanceProp = serializedObject.FindProperty("lodDistance");
            enableClickToEditProp = serializedObject.FindProperty("enableClickToEdit");
            hoveredPointProp = serializedObject.FindProperty("hoveredPoint");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("NavMesh Gizmo 설정", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 표시 옵션
            EditorGUILayout.LabelField("표시 옵션", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showNavMeshProp, new GUIContent("NavMesh 표시"));
            EditorGUILayout.PropertyField(showWireframeProp, new GUIContent("와이어프레임 표시"));
            EditorGUILayout.PropertyField(showFacesProp, new GUIContent("면 표시"));
            EditorGUILayout.PropertyField(showVerticesProp, new GUIContent("정점 표시"));

            EditorGUILayout.Space();

            // 색상 설정
            EditorGUILayout.LabelField("색상 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(navMeshColorProp, new GUIContent("NavMesh 색상"));
            EditorGUILayout.PropertyField(wireframeColorProp, new GUIContent("와이어프레임 색상"));
            EditorGUILayout.PropertyField(faceColorProp, new GUIContent("면 색상"));
            EditorGUILayout.PropertyField(vertexColorProp, new GUIContent("정점 색상"));

            EditorGUILayout.Space();

            // 기타 설정
            EditorGUILayout.LabelField("기타 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(lineWidthProp, new GUIContent("선 두께"));
            EditorGUILayout.PropertyField(lodDistanceProp, new GUIContent("LOD 거리"));
            EditorGUILayout.PropertyField(enableClickToEditProp, new GUIContent("클릭 편집 활성화"));

            EditorGUILayout.Space();

            // 액션 버튼들
            EditorGUILayout.LabelField("액션", EditorStyles.boldLabel);
            
            NavMeshGizmo gizmo = (NavMeshGizmo)target;
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("NavMesh 업데이트"))
            {
                gizmo.UpdateNavMeshData();
            }
            if (GUILayout.Button("경로 지우기"))
            {
                gizmo.ClearPath();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("모든 표시"))
            {
                gizmo.SetShowNavMesh(true);
                gizmo.SetShowWireframe(true);
                gizmo.SetShowFaces(true);
                gizmo.SetShowVertices(true);
            }
            if (GUILayout.Button("모든 숨기기"))
            {
                gizmo.SetShowNavMesh(false);
                gizmo.SetShowWireframe(false);
                gizmo.SetShowFaces(false);
                gizmo.SetShowVertices(false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 정보 표시
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("런타임 정보", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("NavMesh 로드됨", gizmo.IsNavMeshLoaded ? "예" : "아니오");
                EditorGUILayout.LabelField("폴리곤 수", gizmo.PolyCount.ToString());
                EditorGUILayout.LabelField("정점 수", gizmo.VertexCount.ToString());
                EditorGUILayout.LabelField("경로 포인트 수", gizmo.PathPointCount.ToString());
            }

            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            NavMeshGizmo gizmo = (NavMeshGizmo)target;
            
            if (!gizmo.enableClickToEdit) return;

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit))
                {
                    gizmo.hoveredPoint = hit.point;
                    e.Use();
                    SceneView.RepaintAll();
                }
            }
        }
    }
} 