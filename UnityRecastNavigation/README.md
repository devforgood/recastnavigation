# Unity RecastNavigation

Unity3D에서 RecastNavigation을 사용할 수 있는 통합 솔루션입니다.

## 개요

이 프로젝트는 RecastNavigation 라이브러리를 Unity3D에서 쉽게 사용할 수 있도록 래핑한 C# 스크립트들과 에디터 도구들을 제공합니다.

## 주요 기능

- **Mesh에서 NavMesh 생성**: Unity의 Mesh 데이터를 사용하여 RecastNavigation NavMesh 생성
- **경로 찾기**: A* 알고리즘을 사용한 효율적인 경로 찾기
- **에디터 도구**: Unity 에디터에서 직접 NavMesh 빌드 및 테스트
- **런타임 컴포넌트**: 게임 실행 중 NavMesh 사용 및 경로 찾기
- **설정 가이드**: 단계별 설정 가이드 및 자동 설정 도구
- **좌표계 변환**: Unity와 RecastNavigation 간의 자동 좌표 변환
- **Y축 회전**: 메시 방향성 조정을 위한 Y축 회전 지원
- **NavMesh 시각화**: Unity Scene에서 NavMesh를 기즈모로 시각화

## 파일 구조

```
Assets/Scripts/RecastNavigation/
├── RecastNavigationWrapper.cs          # DLL 래퍼 클래스
├── RecastNavigationComponent.cs        # 런타임 컴포넌트
├── RecastNavigationSample.cs           # 사용 예제
├── NavMeshGizmo.cs                     # NavMesh 시각화 컴포넌트
└── Editor/
    ├── RecastNavigationEditor.cs       # 메인 에디터 도구
    ├── RecastNavigationQuickTool.cs    # 빠른 도구
    ├── RecastNavigationSetupGuide.cs   # 설정 가이드
    └── NavMeshGizmoEditor.cs           # NavMesh 기즈모 에디터
└── Tests/
    └── RecastNavigationWrapperTests.cs # 유닛 테스트
```

## 설치 방법

### 1. DLL 빌드

먼저 RecastNavigation DLL을 빌드해야 합니다:

```bash
# 프로젝트 루트에서
mkdir build
cd build
cmake ..
cmake --build . --config Release
```

### 2. Unity 프로젝트 설정

1. **DLL 복사**: 빌드된 `RecastNavigationUnity.dll`을 `Assets/Plugins/` 폴더에 복사
2. **스크립트 임포트**: 이 폴더의 모든 스크립트를 `Assets/Scripts/RecastNavigation/` 폴더에 복사
3. **Unity 재시작**: Unity 에디터를 재시작하여 DLL 로드

### 3. 자동 설정 (권장)

Unity 에디터에서 `Tools > RecastNavigation > Setup Guide`를 열고 자동 설정을 사용하세요.

## 사용 방법

### 기본 사용법

```csharp
using RecastNavigation;

// 초기화
RecastNavigationWrapper.Initialize();

// Mesh에서 NavMesh 빌드
Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
var settings = NavMeshBuildSettingsExtensions.CreateDefault();
var result = RecastNavigationWrapper.BuildNavMesh(mesh, settings);

if (result.Success)
{
    // NavMesh 로드
    RecastNavigationWrapper.LoadNavMesh(result.NavMeshData);
    
    // 경로 찾기
    Vector3 start = Vector3.zero;
    Vector3 end = new Vector3(10f, 0f, 10f);
    var pathResult = RecastNavigationWrapper.FindPath(start, end);
    
    if (pathResult.Success)
    {
        Vector3[] path = pathResult.PathPoints;
        // 경로 사용
    }
}

// 정리
RecastNavigationWrapper.Cleanup();
```

### 컴포넌트 사용법

```csharp
// RecastNavigationComponent 추가
var navComponent = gameObject.AddComponent<RecastNavigationComponent>();

// 씬에서 NavMesh 빌드
navComponent.BuildNavMeshFromScene();

// 경로 찾기
navComponent.FindPath(startPosition, endPosition);
```

### 에디터 도구 사용법

1. **메인 에디터**: `Tools > RecastNavigation > Editor`
   - NavMesh 빌드 설정
   - 경로 찾기 테스트
   - 시각화 옵션

2. **빠른 도구**: `Tools > RecastNavigation > Quick Tool`
   - 빠른 NavMesh 빌드
   - 성능 테스트
   - 디버그 도구

3. **설정 가이드**: `Tools > RecastNavigation > Setup Guide`
   - 단계별 설정
   - 자동 설정
   - 문제 해결

## API 참조

### RecastNavigationWrapper

#### 초기화
- `Initialize()`: RecastNavigation 초기화
- `Cleanup()`: RecastNavigation 정리

#### NavMesh 빌드
- `BuildNavMesh(Mesh mesh, NavMeshBuildSettings settings)`: Mesh에서 NavMesh 빌드
- `LoadNavMesh(byte[] data)`: NavMesh 데이터 로드

#### 경로 찾기
- `FindPath(Vector3 start, Vector3 end)`: 두 지점 간 경로 찾기

#### 정보 조회
- `GetPolyCount()`: NavMesh 폴리곤 수
- `GetVertexCount()`: NavMesh 정점 수

### NavMeshBuildSettings

```csharp
public struct NavMeshBuildSettings
{
    public float cellSize;              // 셀 크기
    public float cellHeight;            // 셀 높이
    public float walkableSlopeAngle;    // 이동 가능한 경사각
    public float walkableHeight;        // 이동 가능한 높이
    public float walkableRadius;        // 이동 가능한 반지름
    public float walkableClimb;         // 이동 가능한 오르기 높이
    public float minRegionArea;         // 최소 영역 크기
    public float mergeRegionArea;       // 병합 영역 크기
    public int maxVertsPerPoly;         // 폴리곤당 최대 정점 수
    public float detailSampleDist;      // 상세 샘플링 거리
    public float detailSampleMaxError;  // 상세 샘플링 최대 오차
}
```

### 프리셋 설정

```csharp
// 기본 설정
var defaultSettings = NavMeshBuildSettingsExtensions.CreateDefault();

// 높은 품질 (느림)
var highQualitySettings = NavMeshBuildSettingsExtensions.CreateHighQuality();

// 낮은 품질 (빠름)
var lowQualitySettings = NavMeshBuildSettingsExtensions.CreateLowQuality();
```

## 좌표계 변환

Unity와 RecastNavigation은 서로 다른 좌표계를 사용하며, 이로 인해 메시 데이터와 경로 찾기 결과에 차이가 발생할 수 있습니다. 이 패키지는 이러한 차이를 자동으로 보정하는 기능을 제공합니다.

### 좌표계 차이점

#### Unity 좌표계 (왼손 좌표계)
```
Y축: 위쪽 (Up)     → +Y
X축: 오른쪽 (Right) → +X  
Z축: 앞쪽 (Forward) → +Z
```

#### RecastNavigation 좌표계 (오른손 좌표계)
```
Y축: 위쪽 (Up)     → +Y
X축: 오른쪽 (Right) → +X
Z축: 앞쪽 (Forward) → +Z
```

### 좌표계 변환의 필요성

1. **메시 데이터 방향성**: Unity에서 내보낸 메시의 정점 순서와 면의 방향이 RecastNavigation에서 다르게 해석될 수 있습니다.

2. **경로 찾기 결과**: 경로 찾기 결과의 좌표가 Unity 월드 좌표계와 맞지 않을 수 있습니다.

3. **메시 UV 매핑**: 메시의 UV 좌표와 노멀 방향이 좌표계 차이로 인해 잘못 해석될 수 있습니다.

### 자동 좌표 변환 기능

#### 1. 좌표계 설정

```csharp
// 좌표계 타입 설정
public enum CoordinateSystem
{
    LeftHanded = 0,   // Unity (왼손 좌표계)
    RightHanded = 1   // RecastNavigation (오른손 좌표계)
}

// 좌표계 설정
RecastNavigationWrapper.UnityRecast_SetCoordinateSystem(CoordinateSystem.LeftHanded);
```

#### 2. 자동 변환 활성화

```csharp
// 컴포넌트에서 자동 좌표 변환 활성화
var component = GetComponent<RecastNavigationComponent>();
component.SetAutoTransformCoordinates(true);

// 또는 개별 설정
RecastNavigationWrapper.UnityRecast_SetAutoTransformCoordinates(true);
```

#### 3. 수동 좌표 변환

```csharp
// Unity 좌표를 RecastNavigation 좌표로 변환
Vector3 unityPosition = new Vector3(1, 2, 3);
Vector3 recastPosition = RecastNavigationWrapper.TransformPosition(unityPosition);

// RecastNavigation 좌표를 Unity 좌표로 변환
Vector3 unityPos = RecastNavigationWrapper.InverseTransformPosition(recastPosition);
```

### Y축 회전 보정

메시의 방향성이 Unity와 RecastNavigation에서 다르게 해석될 때 Y축 회전을 사용하여 보정할 수 있습니다.

#### Y축 회전 타입

```csharp
public enum YAxisRotation
{
    None = 0,      // 회전 없음
    Rotate90 = 1,  // Y축 기준 90도 시계방향 회전
    Rotate180 = 2, // Y축 기준 180도 회전
    Rotate270 = 3  // Y축 기준 270도 시계방향 회전 (90도 반시계방향)
}
```

#### Y축 회전 사용법

```csharp
// Y축 회전 설정
RecastNavigationWrapper.UnityRecast_SetYAxisRotation(YAxisRotation.Rotate90);

// 컴포넌트에서 설정
component.SetYAxisRotation(YAxisRotation.Rotate90);

// 개별 좌표에 Y축 회전 적용
Vector3 originalPosition = new Vector3(1, 2, 3);
Vector3 rotatedPosition = RecastNavigationWrapper.ApplyYAxisRotation(originalPosition, YAxisRotation.Rotate90);
```

### Y축 회전이 필요한 경우

1. **메시의 정면 방향이 다를 때**: 
   - Unity에서는 Z축이 앞쪽이지만, 일부 3D 모델링 도구나 메시 데이터에서는 다른 방향을 정면으로 사용
   - 예: Blender에서 내보낸 메시가 Unity와 방향이 다를 때

2. **메시의 UV 매핑이나 노멀 방향이 다를 때**:
   - 메시의 표면 방향이 Unity와 RecastNavigation에서 다르게 해석
   - 텍스처 매핑이나 라이팅이 잘못 적용될 때

3. **특정 게임 엔진이나 도구에서 내보낸 메시**:
   - 다른 게임 엔진에서 내보낸 메시 데이터의 경우 좌표계가 다를 수 있음
   - 예: Unreal Engine, 3ds Max, Maya 등에서 내보낸 메시

### 좌표계 변환 설정 예제

```csharp
public class CoordinateSystemExample : MonoBehaviour
{
    void Start()
    {
        // 1. 좌표계 설정
        RecastNavigationWrapper.UnityRecast_SetCoordinateSystem(CoordinateSystem.LeftHanded);
        
        // 2. Y축 회전 설정 (필요한 경우)
        RecastNavigationWrapper.UnityRecast_SetYAxisRotation(YAxisRotation.Rotate90);
        
        // 3. 자동 좌표 변환 활성화
        RecastNavigationWrapper.UnityRecast_SetAutoTransformCoordinates(true);
        
        // 4. NavMesh 빌드
        var component = GetComponent<RecastNavigationComponent>();
        component.BuildNavMeshFromScene();
    }
    
    void TestCoordinateTransform()
    {
        Vector3 unityPos = new Vector3(10, 5, 15);
        
        // Unity → RecastNavigation 변환
        Vector3 recastPos = RecastNavigationWrapper.TransformPosition(unityPos);
        Debug.Log($"Unity: {unityPos} → Recast: {recastPos}");
        
        // RecastNavigation → Unity 변환
        Vector3 backToUnity = RecastNavigationWrapper.InverseTransformPosition(recastPos);
        Debug.Log($"Recast: {recastPos} → Unity: {backToUnity}");
    }
}
```

### 문제 해결 가이드

#### 좌표계 문제 진단

1. **경로가 잘못된 방향으로 생성되는 경우**:
   - Y축 회전 설정 확인
   - `YAxisRotation.Rotate90` 또는 `YAxisRotation.Rotate270` 시도

2. **메시가 거꾸로 렌더링되는 경우**:
   - 좌표계 설정 확인
   - `CoordinateSystem.RightHanded`로 변경 시도

3. **NavMesh가 메시와 맞지 않는 경우**:
   - 자동 좌표 변환 활성화 확인
   - 수동 좌표 변환 함수 사용

#### 디버그 도구

```csharp
// 좌표계 변환 디버그 정보 출력
RecastNavigationWrapper.UnityRecast_EnableDebugLogging(true);

// 현재 설정 확인
var coordSystem = RecastNavigationWrapper.UnityRecast_GetCoordinateSystem();
var yRotation = RecastNavigationWrapper.UnityRecast_GetYAxisRotation();
var autoTransform = RecastNavigationWrapper.UnityRecast_GetAutoTransformCoordinates();

Debug.Log($"Coordinate System: {coordSystem}");
Debug.Log($"Y-Axis Rotation: {yRotation}");
Debug.Log($"Auto Transform: {autoTransform}");
```

## 예제 시나리오

### 1. 간단한 경로 찾기

```csharp
public class SimplePathfinding : MonoBehaviour
{
    void Start()
    {
        // 초기화
        RecastNavigationWrapper.Initialize();
        
        // 좌표계 설정
        RecastNavigationWrapper.UnityRecast_SetCoordinateSystem(CoordinateSystem.LeftHanded);
        RecastNavigationWrapper.UnityRecast_SetAutoTransformCoordinates(true);
        
        // 현재 씬의 모든 Mesh로 NavMesh 빌드
        var navComponent = gameObject.AddComponent<RecastNavigationComponent>();
        navComponent.BuildNavMeshFromScene();
    }
    
    void Update()
    {
        // 마우스 클릭으로 경로 찾기
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var result = RecastNavigationWrapper.FindPath(transform.position, hit.point);
                if (result.Success)
                {
                    // 경로 시각화
                    DrawPath(result.PathPoints);
                }
            }
        }
    }
    
    void DrawPath(Vector3[] path)
    {
        // 경로를 라인으로 그리기
        for (int i = 1; i < path.Length; i++)
        {
            Debug.DrawLine(path[i-1], path[i], Color.green, 5f);
        }
    }
}
```

### 2. 에이전트 이동

```csharp
public class AgentMovement : MonoBehaviour
{
    private RecastNavigationComponent navComponent;
    private Vector3[] currentPath;
    private int pathIndex = 0;
    
    void Start()
    {
        navComponent = FindObjectOfType<RecastNavigationComponent>();
    }
    
    public void MoveTo(Vector3 target)
    {
        var result = navComponent.FindPath(transform.position, target);
        if (result.Success)
        {
            currentPath = result.PathPoints;
            pathIndex = 0;
        }
    }
    
    void Update()
    {
        if (currentPath != null && pathIndex < currentPath.Length)
        {
            Vector3 target = currentPath[pathIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, 5f * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, target) < 0.1f)
            {
                pathIndex++;
            }
        }
    }
}
```

### 3. 좌표계 변환 테스트

```csharp
public class CoordinateTransformTest : MonoBehaviour
{
    void Start()
    {
        // 좌표계 변환 테스트
        TestCoordinateTransformation();
    }
    
    void TestCoordinateTransformation()
    {
        Vector3 originalPos = new Vector3(10, 5, 15);
        
        // Unity → RecastNavigation 변환
        Vector3 recastPos = RecastNavigationWrapper.TransformPosition(originalPos);
        Debug.Log($"Original Unity Position: {originalPos}");
        Debug.Log($"Transformed to Recast: {recastPos}");
        
        // RecastNavigation → Unity 변환
        Vector3 backToUnity = RecastNavigationWrapper.InverseTransformPosition(recastPos);
        Debug.Log($"Back to Unity: {backToUnity}");
        
        // 변환 정확도 확인
        float error = Vector3.Distance(originalPos, backToUnity);
        Debug.Log($"Transformation Error: {error}");
        
        if (error < 0.001f)
        {
            Debug.Log("✅ 좌표계 변환이 정확합니다!");
        }
        else
        {
            Debug.LogWarning("⚠️ 좌표계 변환에 오차가 있습니다.");
        }
    }
}
```

### 4. NavMesh 시각화

```csharp
public class NavMeshVisualizationExample : MonoBehaviour
{
    private RecastNavigationComponent navComponent;
    private NavMeshGizmo navMeshGizmo;
    
    void Start()
    {
        // RecastNavigation 컴포넌트 추가
        navComponent = gameObject.AddComponent<RecastNavigationComponent>();
        
        // NavMesh 빌드
        navComponent.BuildNavMeshFromScene();
        
        // NavMeshGizmo 자동 추가 (기본적으로 활성화됨)
        // navComponent.AddNavMeshGizmo();
        
        // 또는 수동으로 NavMeshGizmo 설정
        navComponent.ConfigureNavMeshGizmo(
            showNavMesh: true,    // NavMesh 표시
            showWireframe: true,  // 와이어프레임 표시
            showFaces: true,      // 면 표시
            showVertices: false   // 정점 표시
        );
    }
    
    void Update()
    {
        // NavMesh 정보 실시간 업데이트
        if (navMeshGizmo == null)
        {
            navMeshGizmo = GetComponent<NavMeshGizmo>();
        }
        
        // 마우스 클릭으로 경로 찾기 및 시각화
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var result = navComponent.FindPath(transform.position, hit.point);
                if (result.Success)
                {
                    // 경로가 자동으로 기즈모로 표시됨
                    Debug.Log($"경로 찾기 성공: {result.PathPoints.Length}개 포인트");
                }
            }
        }
    }
}
```

## NavMesh 시각화

Unity Scene에서 NavMesh를 실시간으로 시각화할 수 있는 기능을 제공합니다.

### NavMeshGizmo 컴포넌트

NavMeshGizmo는 Unity Scene에서 NavMesh를 기즈모로 표시하는 컴포넌트입니다.

#### 주요 기능

- **실시간 시각화**: NavMesh가 빌드되거나 로드될 때 자동으로 업데이트
- **다양한 표시 모드**: 면, 와이어프레임, 정점 선택적 표시
- **커스터마이징**: 색상, 크기, 투명도 등 완전히 커스터마이징 가능
- **성능 최적화**: 자동 업데이트 간격 조절로 성능 최적화

#### 사용법

```csharp
// 1. 자동 추가 (권장)
var navComponent = gameObject.AddComponent<RecastNavigationComponent>();
navComponent.BuildNavMeshFromScene(); // NavMeshGizmo가 자동으로 추가됨

// 2. 수동 추가
var gizmo = gameObject.AddComponent<NavMeshGizmo>();
gizmo.UpdateNavMeshData();

// 3. 설정 커스터마이징
gizmo.SetShowNavMesh(true);
gizmo.SetShowWireframe(true);
gizmo.SetShowFaces(true);
gizmo.SetShowVertices(false);

gizmo.SetNavMeshColor(new Color(0.2f, 0.8f, 0.2f, 0.6f));
gizmo.SetWireframeColor(new Color(0.1f, 0.1f, 0.1f, 1.0f));
```

#### Inspector 설정

- **시각화 설정**: NavMesh, 와이어프레임, 면, 정점 표시 여부
- **색상 설정**: 각 요소별 색상 및 투명도
- **크기 설정**: 정점 크기, 선 두께 등
- **자동 업데이트**: 업데이트 간격 및 자동 업데이트 여부

#### 에디터 전용 기능

- **Scene 뷰 정보**: NavMesh 정보를 Scene 뷰에 실시간 표시
- **빠른 설정**: 기본, 와이어프레임만, 면만 등 프리셋 설정
- **실시간 업데이트**: NavMesh 변경 시 자동으로 기즈모 업데이트

### 시각화 모드

#### 1. 기본 모드
- NavMesh 면과 와이어프레임 모두 표시
- 가장 일반적인 사용 모드

#### 2. 와이어프레임 모드
- NavMesh 구조만 선으로 표시
- 성능이 좋고 구조 파악에 유용

#### 3. 면 모드
- NavMesh 면만 표시
- 이동 가능한 영역을 명확하게 확인

#### 4. 정점 모드
- NavMesh 정점을 구체로 표시
- 디버깅 및 정밀한 분석에 유용

### 성능 고려사항

- **큰 NavMesh**: 정점 수가 많은 경우 자동 업데이트 간격을 늘려서 성능 최적화
- **실시간 업데이트**: 필요하지 않은 경우 자동 업데이트를 비활성화
- **선택적 표시**: 필요한 요소만 표시하여 렌더링 성능 향상

### 고급 기능

#### 1. 성능 최적화
- **메시 캐싱**: 동일한 삼각형 메시를 재사용하여 메모리 효율성 향상
- **LOD (Level of Detail)**: 거리에 따른 세부 수준 조절
- **최대 표시 삼각형 수 제한**: 성능에 맞춰 표시할 삼각형 수 제한

#### 2. 경로 시각화
- **애니메이션 경로**: 경로를 순차적으로 애니메이션으로 표시
- **그라데이션 색상**: 시작점에서 끝점까지 색상 변화
- **경로 화살표**: 이동 방향을 나타내는 화살표 표시
- **시작/끝점 강조**: 경로의 시작점과 끝점을 특별히 표시

#### 3. 인터랙션 기능
- **키보드 단축키**: 
  - `T`: 시각화 토글
  - `R`: 새로고침
  - `W`: 와이어프레임 토글
  - `F`: 면 토글
  - `V`: 정점 토글
  - `A`: 경로 애니메이션 토글
- **호버 정보**: 마우스 호버 시 위치 정보 표시
- **실시간 설정 변경**: 런타임 중에도 설정 변경 가능

#### 4. 고급 설정 예제

```csharp
// 성능 최적화 설정
gizmo.SetPerformanceSettings(
    useCaching: true,      // 메시 캐싱 사용
    useLOD: true,          // LOD 사용
    maxTriangles: 1000     // 최대 1000개 삼각형 표시
);

// 경로 시각화 설정
gizmo.SetPathAnimation(true, 2f);  // 애니메이션 활성화, 속도 2배
gizmo.SetPathColors(Color.green, Color.red);  // 녹색에서 빨간색으로
gizmo.SetPathArrows(true, 0.8f);  // 화살표 표시, 크기 0.8

// 인터랙션 설정
gizmo.SetInteraction(true);  // 인터랙션 활성화
gizmo.SetHoverInfo(true);    // 호버 정보 표시
```

#### 5. 성능 모니터링

```csharp
public class NavMeshPerformanceMonitor : MonoBehaviour
{
    private NavMeshGizmo gizmo;
    private float lastFrameTime;
    private int frameCount;
    
    void Start()
    {
        gizmo = GetComponent<NavMeshGizmo>();
    }
    
    void Update()
    {
        frameCount++;
        if (Time.time - lastFrameTime >= 1f)
        {
            float fps = frameCount / (Time.time - lastFrameTime);
            Debug.Log($"NavMesh Gizmo FPS: {fps:F1}");
            
            frameCount = 0;
            lastFrameTime = Time.time;
        }
    }
}
```

## 테스트

### Unity 테스트 실행

Unity 프로젝트에서 NUnit 테스트를 실행할 수 있습니다:

1. **Test Runner 열기**: `Window > General > Test Runner`
2. **EditMode 탭 선택**: 에디터 모드에서 테스트 실행
3. **테스트 실행**: `RecastNavigationWrapperTests` 클래스의 모든 테스트 실행

### 테스트 커버리지

현재 테스트는 다음 기능들을 커버합니다:

- **초기화 및 정리**: `Initialize()`, `Cleanup()`
- **NavMesh 빌드**: 다양한 설정으로 NavMesh 빌드
- **NavMesh 로드**: 유효한/무효한 데이터로 로드 테스트
- **경로 찾기**: 다양한 시나리오에서 경로 찾기
- **정보 조회**: 폴리곤 수, 정점 수 조회
- **성능 테스트**: 빌드 및 경로 찾기 성능
- **좌표계 변환**: 좌표 변환 함수 테스트
- **Y축 회전**: Y축 회전 기능 테스트

### 테스트 실행 방법

```csharp
// 개별 테스트 실행
[Test]
public void MyTest()
{
    // 테스트 코드
}

// 성능 테스트
[UnityTest]
public IEnumerator PerformanceTest()
{
    // 성능 테스트 코드
    yield return null;
}
```

## 성능 최적화

### 1. NavMesh 빌드 최적화

- **낮은 품질 설정 사용**: 빠른 빌드를 위해 `CreateLowQuality()` 사용
- **선택적 빌드**: 필요한 오브젝트만 선택하여 NavMesh 빌드
- **비동기 빌드**: 큰 맵의 경우 코루틴을 사용한 비동기 빌드

### 2. 경로 찾기 최적화

- **경로 캐싱**: 자주 사용되는 경로를 캐시
- **경로 업데이트 제한**: 필요할 때만 경로 재계산
- **거리 기반 최적화**: 가까운 목표는 간단한 경로 사용

### 3. 메모리 관리

```csharp
void OnDestroy()
{
    // 컴포넌트 제거 시 정리
    RecastNavigationWrapper.Cleanup();
}
```

## 문제 해결

### 일반적인 문제들

1. **DLL을 찾을 수 없음**
   - `Assets/Plugins/` 폴더에 DLL이 올바르게 복사되었는지 확인
   - Unity 에디터 재시작

2. **스크립트 컴파일 에러**
   - 스크립트가 올바른 위치에 있는지 확인
   - 네임스페이스 충돌 확인

3. **NavMesh 빌드 실패**
   - 씬에 Mesh가 있는지 확인
   - Mesh가 올바른 형식인지 확인

4. **경로 찾기 실패**
   - NavMesh가 올바르게 빌드되었는지 확인
   - 시작점과 끝점이 NavMesh 내부에 있는지 확인

5. **좌표계 문제**
   - 좌표계 설정 확인
   - Y축 회전 설정 확인
   - 자동 좌표 변환 활성화 확인

### 디버그 도구

- **Quick Tool**: `Tools > RecastNavigation > Quick Tool`
  - 상태 확인
  - 성능 테스트
  - 디버그 정보 출력

- **Console 로그**: Unity 콘솔에서 상세한 에러 메시지 확인

- **테스트 실행**: Test Runner에서 테스트를 실행하여 기능 확인

- **좌표계 디버그**: 좌표계 변환 디버그 정보 출력

## 라이선스

이 프로젝트는 RecastNavigation 라이브러리를 기반으로 하며, 원본 라이브러리의 라이선스를 따릅니다.

## 기여하기

1. 이슈 리포트
2. 기능 요청
3. 코드 기여
4. 문서 개선

## 지원

- **GitHub Issues**: 버그 리포트 및 기능 요청
- **Documentation**: 이 README 및 코드 주석
- **Examples**: 제공된 예제 스크립트들
- **Tests**: 테스트 스크립트로 기능 검증

## 변경 이력

### v1.0.0
- 초기 릴리스
- 기본 NavMesh 빌드 및 경로 찾기 기능
- Unity 에디터 도구
- 설정 가이드
- 유닛 테스트 