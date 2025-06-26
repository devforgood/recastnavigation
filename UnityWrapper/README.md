# Unity RecastNavigation Wrapper

RecastNavigation 라이브러리를 Unity3D에서 사용할 수 있도록 래핑한 C++ DLL 프로젝트입니다.

## 개요

이 프로젝트는 RecastNavigation의 핵심 기능들을 Unity3D에서 사용할 수 있도록 C 스타일 인터페이스로 래핑한 DLL을 제공합니다.

## 주요 기능

- **Mesh에서 NavMesh 생성**: Unity의 Mesh 데이터를 사용하여 RecastNavigation NavMesh 생성
- **경로 찾기**: A* 알고리즘을 사용한 효율적인 경로 찾기
- **NavMesh 데이터 관리**: NavMesh 데이터의 저장 및 로드
- **크로스 플랫폼 지원**: Windows, macOS, Linux 지원

## 빌드 방법

### Windows (Visual Studio)

```bash
mkdir build
cd build
cmake .. -G "Visual Studio 16 2019" -A x64
cmake --build . --config Release
```

### Linux/macOS

```bash
mkdir build
cd build
cmake ..
make -j$(nproc)
```

## API 인터페이스

### 헤더 파일

```cpp
// UnityNavMeshBuilder.h
bool InitializeRecastNavigation();
void CleanupRecastNavigation();

bool BuildNavMeshFromMesh(
    const Vector3* vertices, int vertexCount,
    const int* indices, int indexCount,
    const NavMeshBuildSettings* settings,
    unsigned char** navMeshData, int* dataSize,
    char** errorMessage);

bool LoadNavMeshFromData(const unsigned char* data, int dataSize);

bool FindPathBetweenPoints(
    const Vector3 start, const Vector3 end,
    Vector3** pathPoints, int* pointCount,
    char** errorMessage);

int GetNavMeshPolyCount();
int GetNavMeshVertexCount();

void FreeMemory(void* ptr);
```

### 데이터 구조

```cpp
struct Vector3
{
    float x, y, z;
};

struct NavMeshBuildSettings
{
    float cellSize;
    float cellHeight;
    float walkableSlopeAngle;
    float walkableHeight;
    float walkableRadius;
    float walkableClimb;
    float minRegionArea;
    float mergeRegionArea;
    int maxVertsPerPoly;
    float detailSampleDist;
    float detailSampleMaxError;
};
```

## Unity 통합

### Unity 스크립트

Unity에서 사용할 수 있는 C# 스크립트들은 `../UnityRecastNavigation/` 폴더에 있습니다.

### 기본 사용법

```csharp
using RecastNavigation;

// 초기화
RecastNavigationWrapper.Initialize();

// NavMesh 빌드 설정
var buildSettings = NavMeshBuildSettingsExtensions.CreateDefault();

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

### 컴포넌트 사용법

```csharp
// RecastNavigationComponent 추가
var navComponent = gameObject.AddComponent<RecastNavigationComponent>();

// 씬에서 NavMesh 빌드
navComponent.BuildNavMeshFromScene();

// 경로 찾기
navComponent.FindPath(startPosition, endPosition);
```

### 에디터 도구

Unity 에디터에서 다음 도구들을 사용할 수 있습니다:

1. **메인 에디터**: `Tools > RecastNavigation > Editor`
2. **빠른 도구**: `Tools > RecastNavigation > Quick Tool`
3. **설정 가이드**: `Tools > RecastNavigation > Setup Guide`

## 파일 구조

```
UnityWrapper/
├── Include/
│   ├── UnityNavMeshBuilder.h
│   ├── UnityPathfinding.h
│   └── UnityRecastWrapper.h
├── Source/
│   ├── UnityNavMeshBuilder.cpp
│   ├── UnityPathfinding.cpp
│   └── UnityRecastWrapper.cpp
├── Tests/
│   ├── CMakeLists.txt
│   ├── TestUnityNavMeshBuilder.cpp
│   ├── TestUnityPathfinding.cpp
│   └── TestUnityRecastWrapper.cpp
├── CMakeLists.txt
├── README.md
├── run_tests.bat
└── run_tests.sh
```

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

Unity 프로젝트에서 `../UnityRecastNavigation/Assets/Scripts/RecastNavigation/Tests/` 폴더의 테스트 스크립트를 실행하세요.

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

Unity 에디터 도구의 디버그 섹션에서 다음 정보를 확인할 수 있습니다:
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

## Unity 스크립트 사용

Unity에서 사용할 수 있는 완전한 스크립트 패키지는 `../UnityRecastNavigation/` 폴더에 있습니다. 이 폴더에는 다음이 포함됩니다:

- **RecastNavigationWrapper.cs**: DLL 래퍼 클래스
- **RecastNavigationComponent.cs**: 런타임 컴포넌트
- **RecastNavigationSample.cs**: 사용 예제
- **Editor/**: Unity 에디터 도구들
  - RecastNavigationEditor.cs
  - RecastNavigationQuickTool.cs
  - RecastNavigationSetupGuide.cs

자세한 사용법은 `../UnityRecastNavigation/README.md`를 참조하세요.

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
- C++ DLL 래퍼
- Unity C# 인터페이스
- 테스트 스위트 