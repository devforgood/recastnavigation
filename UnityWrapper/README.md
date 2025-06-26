# RecastNavigation Unity Wrapper

RecastNavigation을 Unity3D에서 사용할 수 있도록 DLL로 래핑한 프로젝트입니다.

## 기능

- Unity Mesh에서 NavMesh 빌드
- NavMesh 데이터 저장/로드
- A* 경로 찾기
- 경로 스무딩 및 단순화
- 다양한 품질 설정 지원

## 빌드 방법

### Windows

1. **필수 도구 설치**
   - Visual Studio 2019 이상
   - CMake 3.1 이상
   - Git

2. **프로젝트 클론**
   ```bash
   git clone https://github.com/recastnavigation/recastnavigation.git
   cd recastnavigation
   ```

3. **빌드 디렉토리 생성**
   ```bash
   mkdir build
   cd build
   ```

4. **CMake 설정**
   ```bash
   cmake .. -G "Visual Studio 16 2019" -A x64 -DRECASTNAVIGATION_UNITY=ON
   ```

5. **빌드 실행**
   ```bash
   cmake --build . --config Release
   ```

6. **결과물 확인**
   - `UnityPlugins/Windows/x64/RecastNavigationUnity.dll` 파일이 생성됩니다.

### macOS

1. **필수 도구 설치**
   ```bash
   brew install cmake
   ```

2. **빌드 실행**
   ```bash
   mkdir build && cd build
   cmake .. -DRECASTNAVIGATION_UNITY=ON
   make -j$(nproc)
   ```

3. **결과물 확인**
   - `UnityPlugins/macOS/x64/libRecastNavigationUnity.dylib` 파일이 생성됩니다.

### Linux

1. **필수 도구 설치**
   ```bash
   sudo apt-get install cmake build-essential
   ```

2. **빌드 실행**
   ```bash
   mkdir build && cd build
   cmake .. -DRECASTNAVIGATION_UNITY=ON
   make -j$(nproc)
   ```

3. **결과물 확인**
   - `UnityPlugins/Linux/x64/libRecastNavigationUnity.so` 파일이 생성됩니다.

## 테스트 실행

### C++ 테스트

프로젝트에는 포괄적인 C++ 유닛테스트가 포함되어 있습니다.

#### Windows에서 테스트 실행
```bash
cd UnityWrapper
run_tests.bat
```

#### Linux/macOS에서 테스트 실행
```bash
cd UnityWrapper
chmod +x run_tests.sh
./run_tests.sh
```

#### 수동으로 테스트 실행
```bash
# 빌드 디렉토리에서
cmake .. -DRECASTNAVIGATION_UNITY=ON -DRECASTNAVIGATION_TESTS=ON
cmake --build . --config Release
ctest --output-on-failure -C Release
```

### Unity C# 테스트

Unity 프로젝트에서 C# 테스트를 실행하려면:

1. **테스트 파일 복사**
   ```
   Assets/
     Scripts/
       RecastNavigation/
         Tests/
           RecastNavigationWrapperTests.cs
   ```

2. **Unity Test Runner 실행**
   - Unity 에디터에서 `Window > General > Test Runner` 열기
   - `EditMode` 탭에서 테스트 실행

3. **명령줄에서 테스트 실행**
   ```bash
   unity-editor -runTests -testPlatform EditMode -projectPath <UnityProjectPath>
   ```

## 테스트 커버리지

### C++ 테스트
- **UnityRecastWrapper**: 초기화, 정리, 기본 기능 테스트
- **UnityNavMeshBuilder**: NavMesh 빌드, 로드, 다양한 설정 테스트
- **UnityPathfinding**: 경로 찾기, 경로 유틸리티, 에러 처리 테스트

### C# 테스트
- **초기화 및 정리**: RecastNavigation 초기화/정리 테스트
- **NavMesh 빌드**: 다양한 메시와 설정으로 NavMesh 빌드 테스트
- **경로 찾기**: 유효한/무효한 경로 찾기 테스트
- **에러 처리**: null 입력, 잘못된 데이터 처리 테스트

## Unity에서 사용하기

### 1. DLL 파일 복사

빌드된 DLL 파일을 Unity 프로젝트의 `Plugins` 폴더에 복사합니다:

```
Assets/
  Plugins/
    Windows/
      RecastNavigationUnity.dll
    macOS/
      libRecastNavigationUnity.dylib
    Linux/
      libRecastNavigationUnity.so
```

### 2. C# 스크립트 복사

`UnityScripts` 폴더의 C# 스크립트들을 Unity 프로젝트에 복사합니다:

```
Assets/
  Scripts/
    RecastNavigation/
      RecastNavigationWrapper.cs
      RecastNavigationExample.cs
      Tests/
        RecastNavigationWrapperTests.cs
```

### 3. 사용 예제

```csharp
using RecastNavigation;
using UnityEngine;

public class NavMeshExample : MonoBehaviour
{
    void Start()
    {
        // RecastNavigation 초기화
        if (!RecastNavigationWrapper.Initialize())
        {
            Debug.LogError("초기화 실패");
            return;
        }
        
        // Mesh에서 NavMesh 빌드
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        var settings = NavMeshBuildSettingsExtensions.CreateDefault();
        var result = RecastNavigationWrapper.BuildNavMesh(mesh, settings);
        
        if (result.Success)
        {
            // NavMesh 로드
            RecastNavigationWrapper.LoadNavMesh(result.NavMeshData);
            
            // 경로 찾기
            Vector3 start = new Vector3(0, 0, 0);
            Vector3 end = new Vector3(10, 0, 10);
            var pathResult = RecastNavigationWrapper.FindPath(start, end);
            
            if (pathResult.Success)
            {
                Debug.Log($"경로 찾기 성공! 포인트 수: {pathResult.PathPoints.Length}");
            }
        }
    }
    
    void OnDestroy()
    {
        RecastNavigationWrapper.Cleanup();
    }
}
```

## 설정 옵션

### NavMesh 빌드 설정

```csharp
// 기본 설정
var defaultSettings = NavMeshBuildSettingsExtensions.CreateDefault();

// 높은 품질 (느리지만 정확)
var highQualitySettings = NavMeshBuildSettingsExtensions.CreateHighQuality();

// 낮은 품질 (빠르지만 부정확)
var lowQualitySettings = NavMeshBuildSettingsExtensions.CreateLowQuality();

// 커스텀 설정
var customSettings = new RecastNavigationWrapper.NavMeshBuildSettings
{
    cellSize = 0.3f,           // 셀 크기
    cellHeight = 0.2f,         // 셀 높이
    walkableSlopeAngle = 45.0f, // 이동 가능한 경사각
    walkableHeight = 2.0f,     // 이동 가능한 높이
    walkableRadius = 0.6f,     // 이동 가능한 반지름
    walkableClimb = 0.9f,      // 이동 가능한 오르기 높이
    minRegionArea = 8.0f,      // 최소 영역 크기
    mergeRegionArea = 20.0f,   // 병합 영역 크기
    maxVertsPerPoly = 6,       // 폴리곤당 최대 정점 수
    detailSampleDist = 6.0f,   // 상세 샘플링 거리
    detailSampleMaxError = 1.0f // 상세 샘플링 최대 오차
};
```

## 주의사항

1. **메모리 관리**: NavMesh 데이터는 자동으로 해제되지만, 경로 결과는 수동으로 해제해야 할 수 있습니다.

2. **스레드 안전성**: 현재 구현은 스레드 안전하지 않습니다. 멀티스레드 환경에서 사용할 때는 적절한 동기화가 필요합니다.

3. **플랫폼 호환성**: 각 플랫폼별로 적절한 DLL 파일을 사용해야 합니다.

4. **성능**: 큰 메시의 경우 NavMesh 빌드에 시간이 걸릴 수 있습니다. 런타임에 빌드하는 것보다는 에디터에서 미리 빌드하는 것을 권장합니다.

## 문제 해결

### DLL 로드 실패
- DLL 파일이 올바른 위치에 있는지 확인
- 플랫폼별 DLL 파일이 맞는지 확인
- Unity 프로젝트 설정에서 플러그인 설정 확인

### NavMesh 빌드 실패
- Mesh 데이터가 유효한지 확인
- 빌드 설정이 적절한지 확인
- 메모리 부족 문제인지 확인

### 경로 찾기 실패
- NavMesh가 제대로 로드되었는지 확인
- 시작점과 끝점이 NavMesh 내부에 있는지 확인
- 경로가 존재하는지 확인

### 테스트 실패
- Catch2 라이브러리가 설치되어 있는지 확인
- 빌드 설정에서 테스트가 활성화되어 있는지 확인
- 메모리 누수나 크래시가 없는지 확인

## 라이선스

이 프로젝트는 RecastNavigation과 동일한 라이선스를 따릅니다. 자세한 내용은 LICENSE.txt 파일을 참조하세요.

## 기여

버그 리포트나 기능 요청은 GitHub Issues를 통해 제출해 주세요. Pull Request도 환영합니다.

### 테스트 기여

새로운 기능을 추가할 때는 다음을 확인해 주세요:

1. **C++ 테스트 추가**: 새로운 기능에 대한 유닛테스트 작성
2. **C# 테스트 추가**: Unity 래퍼에 대한 테스트 작성
3. **테스트 실행**: 모든 테스트가 통과하는지 확인
4. **문서 업데이트**: README와 주석 업데이트 