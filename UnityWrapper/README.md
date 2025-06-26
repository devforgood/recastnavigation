# Unity RecastNavigation Wrapper

Unity3D에서 RecastNavigation을 사용할 수 있는 C# 래퍼입니다. 이 프로젝트는 RecastNavigation의 C++ 라이브러리를 Unity에서 DLL로 임포트하여 사용할 수 있게 해줍니다.

## 기능

- **NavMesh 빌드**: Unity 씬의 Mesh 데이터를 기반으로 NavMesh 생성
- **경로 찾기**: A* 알고리즘을 사용한 효율적인 경로 찾기
- **에디터 도구**: Unity 에디터에서 NavMesh 빌드 및 테스트
- **런타임 컴포넌트**: 게임 실행 중 NavMesh 사용
- **시각화**: 경로 및 NavMesh 정보 시각화
- **설정 옵션**: 다양한 NavMesh 빌드 설정 지원

## 빌드 방법

### Windows

```bash
# 빌드 디렉토리 생성
mkdir build
cd build

# CMake로 프로젝트 생성
cmake .. -G "Visual Studio 16 2019" -A x64

# 빌드 실행
cmake --build . --config Release

# DLL 파일은 build/UnityWrapper/Release/ 디렉토리에 생성됩니다
```

### Linux/macOS

```bash
# 빌드 디렉토리 생성
mkdir build
cd build

# CMake로 프로젝트 생성
cmake ..

# 빌드 실행
make -j$(nproc)

# DLL 파일은 build/UnityWrapper/ 디렉토리에 생성됩니다
```

## Unity 프로젝트 설정

1. **DLL 파일 복사**: 빌드된 DLL 파일을 Unity 프로젝트의 `Plugins` 폴더에 복사
2. **스크립트 복사**: `UnityScripts` 폴더의 모든 C# 스크립트를 Unity 프로젝트에 복사
3. **플랫폼 설정**: DLL 파일의 플랫폼 설정을 올바르게 구성

## 사용법

### 1. 에디터 도구 사용

#### 빠른 도구 (Quick Tool)
```
Tools > RecastNavigation > 빠른 도구
```
- 한 번의 클릭으로 NavMesh 빌드
- 간단한 경로 찾기 테스트
- 샘플 씬 생성

#### 상세 에디터 (Detailed Editor)
```
Tools > RecastNavigation > Editor
```
- 모든 NavMesh 빌드 설정 조정
- 실시간 경로 찾기 테스트
- NavMesh 데이터 저장/로드
- 디버그 정보 표시

### 2. 런타임 컴포넌트 사용

#### RecastNavigationComponent
```csharp
// 컴포넌트 추가
RecastNavigationComponent navComponent = gameObject.AddComponent<RecastNavigationComponent>();

// NavMesh 빌드
bool success = navComponent.BuildNavMeshFromScene();

// 경로 찾기
bool pathFound = navComponent.FindPath(startPoint, endPoint);
Vector3[] path = navComponent.CurrentPath;
```

#### RecastNavigationSample
```csharp
// 샘플 스크립트 사용
RecastNavigationSample sample = gameObject.AddComponent<RecastNavigationSample>();

// 에이전트와 목표 설정
sample.SetAgent(agentTransform);
sample.SetTarget(targetTransform);

// 자동 경로 찾기 및 이동
sample.autoFindPath = true;
sample.moveAgent = true;
```

### 3. 프로그래밍 방식 사용

#### 기본 사용법
```csharp
// 초기화
RecastNavigationWrapper.Initialize();

// NavMesh 빌드 설정
var buildSettings = NavMeshBuildSettingsExtensions.CreateDefault();
buildSettings.cellSize = 0.3f;
buildSettings.cellHeight = 0.2f;

// Mesh에서 NavMesh 빌드
Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
var result = RecastNavigationWrapper.BuildNavMesh(mesh, buildSettings);

if (result.Success)
{
    // NavMesh 로드
    RecastNavigationWrapper.LoadNavMesh(result.NavMeshData);
    
    // 경로 찾기
    var pathResult = RecastNavigationWrapper.FindPath(Vector3.zero, new Vector3(10, 0, 10));
    if (pathResult.Success)
    {
        Vector3[] path = pathResult.PathPoints;
        Debug.Log($"경로 찾기 성공! 길이: {path.Length}");
    }
}

// 정리
RecastNavigationWrapper.Cleanup();
```

#### 고급 설정
```csharp
// 높은 품질 설정
var highQualitySettings = NavMeshBuildSettingsExtensions.CreateHighQuality();

// 낮은 품질 설정 (빠른 빌드)
var lowQualitySettings = NavMeshBuildSettingsExtensions.CreateLowQuality();

// 커스텀 설정
var customSettings = new NavMeshBuildSettings
{
    cellSize = 0.2f,
    cellHeight = 0.1f,
    walkableSlopeAngle = 45f,
    walkableHeight = 2.0f,
    walkableRadius = 0.6f,
    walkableClimb = 0.9f,
    minRegionArea = 8f,
    mergeRegionArea = 20f,
    maxVertsPerPoly = 6,
    detailSampleDist = 6f,
    detailSampleMaxError = 1f
};
```

## 샘플 씬 생성

에디터 도구에서 "예제 씬 생성" 버튼을 클릭하면 다음과 같은 샘플 씬이 생성됩니다:

- **Ground**: 기본 바닥 (Plane)
- **Obstacle_0~4**: 랜덤하게 배치된 장애물들 (Cube)
- **StartPoint**: 시작점 마커 (Sphere)
- **EndPoint**: 끝점 마커 (Sphere)
- **RecastNavigation**: RecastNavigation 컴포넌트가 추가된 오브젝트

## 설정 옵션

### NavMesh 빌드 설정

| 설정 | 설명 | 기본값 | 범위 |
|------|------|--------|------|
| cellSize | 셀 크기 | 0.3f | 0.01f ~ 1.0f |
| cellHeight | 셀 높이 | 0.2f | 0.01f ~ 1.0f |
| walkableSlopeAngle | 이동 가능한 경사각 | 45f | 0f ~ 90f |
| walkableHeight | 이동 가능한 높이 | 2.0f | 0.1f ~ 10f |
| walkableRadius | 이동 가능한 반지름 | 0.6f | 0.1f ~ 5f |
| walkableClimb | 이동 가능한 오르기 높이 | 0.9f | 0.1f ~ 5f |
| minRegionArea | 최소 영역 크기 | 8f | 1f ~ 100f |
| mergeRegionArea | 병합 영역 크기 | 20f | 1f ~ 200f |
| maxVertsPerPoly | 폴리곤당 최대 정점 수 | 6 | 3 ~ 12 |
| detailSampleDist | 상세 샘플링 거리 | 6f | 1f ~ 20f |
| detailSampleMaxError | 상세 샘플링 최대 오차 | 1f | 0.1f ~ 5f |

### 품질 프리셋

- **기본 품질**: 균형잡힌 성능과 품질
- **높은 품질**: 더 정확한 NavMesh, 느린 빌드
- **낮은 품질**: 빠른 빌드, 간단한 NavMesh

## 테스트

### C++ 테스트 실행

#### Windows
```bash
cd UnityWrapper
run_tests.bat
```

#### Linux/macOS
```bash
cd UnityWrapper
./run_tests.sh
```

### Unity C# 테스트

Unity 프로젝트에서 `UnityScripts/Tests/` 폴더의 테스트 스크립트를 실행하세요.

## 문제 해결

### 일반적인 문제

1. **DLL 로드 실패**
   - DLL 파일이 올바른 위치에 있는지 확인
   - 플랫폼 설정이 올바른지 확인
   - Visual C++ Redistributable 설치 필요

2. **NavMesh 빌드 실패**
   - 씬에 Mesh가 있는지 확인
   - Mesh가 올바른 형식인지 확인
   - 빌드 설정이 적절한지 확인

3. **경로 찾기 실패**
   - NavMesh가 로드되었는지 확인
   - 시작점과 끝점이 NavMesh 내부에 있는지 확인
   - 경로가 존재하는지 확인

### 디버그 정보

에디터 도구의 디버그 섹션에서 다음 정보를 확인할 수 있습니다:
- NavMesh 폴리곤 수
- NavMesh 정점 수
- 경로 길이
- 경로 포인트 수
- 에러 메시지

## 성능 최적화

1. **적절한 셀 크기 설정**: 씬 크기에 맞는 셀 크기 선택
2. **Mesh 최적화**: 불필요한 정점 제거
3. **영역 크기 조정**: minRegionArea와 mergeRegionArea 조정
4. **폴리곤 복잡도 제한**: maxVertsPerPoly 값 조정

## 라이선스

이 프로젝트는 RecastNavigation과 동일한 라이선스를 따릅니다. 자세한 내용은 LICENSE 파일을 참조하세요.

## 기여

버그 리포트, 기능 요청, 풀 리퀘스트를 환영합니다. 기여하기 전에 CONTRIBUTING.md 파일을 확인해주세요.

## 지원

문제가 발생하면 다음을 확인해주세요:
1. 이 README의 문제 해결 섹션
2. 테스트 실행 결과
3. Unity 콘솔의 에러 메시지
4. GitHub Issues

## 변경 사항

### v1.0.0
- 초기 릴리스
- 기본 NavMesh 빌드 및 경로 찾기 기능
- Unity 에디터 도구
- 런타임 컴포넌트
- 샘플 스크립트
- 테스트 스위트 