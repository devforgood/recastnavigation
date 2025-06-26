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

## 파일 구조

```
Assets/Scripts/RecastNavigation/
├── RecastNavigationWrapper.cs          # DLL 래퍼 클래스
├── RecastNavigationComponent.cs        # 런타임 컴포넌트
├── RecastNavigationSample.cs           # 사용 예제
└── Editor/
    ├── RecastNavigationEditor.cs       # 메인 에디터 도구
    ├── RecastNavigationQuickTool.cs    # 빠른 도구
    └── RecastNavigationSetupGuide.cs   # 설정 가이드
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

## 예제 시나리오

### 1. 간단한 경로 찾기

```csharp
public class SimplePathfinding : MonoBehaviour
{
    void Start()
    {
        // 초기화
        RecastNavigationWrapper.Initialize();
        
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

### 디버그 도구

- **Quick Tool**: `Tools > RecastNavigation > Quick Tool`
  - 상태 확인
  - 성능 테스트
  - 디버그 정보 출력

- **Console 로그**: Unity 콘솔에서 상세한 에러 메시지 확인

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

## 변경 이력

### v1.0.0
- 초기 릴리스
- 기본 NavMesh 빌드 및 경로 찾기 기능
- Unity 에디터 도구
- 설정 가이드 