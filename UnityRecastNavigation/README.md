# UnityRecastNavigation

Unity용 Recast Navigation 통합 패키지입니다.

## 프로젝트 구조

```
UnityRecastNavigation/
├── Assets/
│   └── Scripts/
│       └── RecastNavigation/
│           ├── RecastNavigationWrapper.cs
│           ├── RecastNavigationComponent.cs
│           ├── RecastNavigationSample.cs
│           └── Editor/
│               ├── RecastNavigationEditor.cs
│               ├── RecastNavigationQuickTool.cs
│               └── RecastNavigationSetupGuide.cs
├── Tests/
│   ├── UnityRecastNavigation.Tests.csproj
│   └── RecastNavigationWrapperTests.cs
├── UnityRecastNavigation.csproj
├── UnityRecastNavigation.sln
├── Makefile
├── build.bat
└── README.md
```

## 빌드 및 테스트

### 방법 1: Makefile 사용 (Linux/macOS)

```bash
# 기본 빌드
make

# 테스트 실행
make test

# 상세 테스트 출력
make test-verbose

# 정리
make clean

# 새로 빌드
make rebuild

# 도움말
make help
```

### 방법 2: 배치 파일 사용 (Windows)

```cmd
# 기본 빌드
build.bat

# 테스트 실행
build.bat test

# 상세 테스트 출력
build.bat test-verbose

# 정리
build.bat clean

# 새로 빌드
build.bat rebuild

# 도움말
build.bat info
```

### 방법 3: 직접 dotnet 명령어 사용

```bash
# 메인 프로젝트 빌드
dotnet build UnityRecastNavigation.csproj

# 테스트 실행
dotnet build Tests/UnityRecastNavigation.Tests.csproj
dotnet test Tests/UnityRecastNavigation.Tests.csproj
```

### Visual Studio에서 실행
1. `UnityRecastNavigation.sln` 파일을 열기
2. 솔루션 탐색기에서 테스트 프로젝트 우클릭
3. "테스트 실행" 선택

## Unity 통합

### 1. DLL 복사
UnityWrapper에서 빌드된 `RecastNavigationUnity.dll`을 Unity 프로젝트의 `Assets/Plugins/` 폴더에 복사합니다.

### 2. 스크립트 임포트
`Assets/Scripts/RecastNavigation/` 폴더의 모든 스크립트를 Unity 프로젝트에 임포트합니다.

### 3. 사용 예제
```csharp
using UnityRecastNavigation;

public class NavigationExample : MonoBehaviour
{
    private RecastNavigationWrapper navigation;

    void Start()
    {
        navigation = new RecastNavigationWrapper();
        
        // NavMesh 빌드
        Vector3[] vertices = { /* 메시 버텍스 */ };
        int[] indices = { /* 메시 인덱스 */ };
        var settings = new NavMeshBuildSettings { /* 설정 */ };
        
        var result = navigation.BuildNavMesh(vertices, indices, settings);
        
        if (result.Success)
        {
            // 경로 찾기
            Vector3 start = new Vector3(0, 0, 0);
            Vector3 end = new Vector3(10, 0, 10);
            var pathResult = navigation.FindPath(start, end);
            
            if (pathResult.Success)
            {
                // 경로 사용
                Vector3[] path = pathResult.Path;
            }
        }
    }

    void OnDestroy()
    {
        navigation?.Dispose();
    }
}
```

## 사용 가능한 빌드 타겟

### Makefile 타겟
- `make build` - 프로젝트 빌드
- `make test` - 테스트 실행
- `make test-verbose` - 상세 출력으로 테스트 실행
- `make test-coverage` - 커버리지 포함 테스트 실행
- `make clean` - 빌드 아티팩트 정리
- `make clean-all` - 완전 정리
- `make restore` - 패키지 복원
- `make rebuild` - 정리 후 빌드
- `make retest` - 정리 후 테스트
- `make release` - 릴리즈 빌드
- `make debug` - 디버그 빌드
- `make info` - 프로젝트 정보 표시

### 배치 파일 타겟
- `build.bat` - 프로젝트 빌드
- `build.bat test` - 테스트 실행
- `build.bat test-verbose` - 상세 출력으로 테스트 실행
- `build.bat clean` - 빌드 아티팩트 정리
- `build.bat clean-all` - 완전 정리
- `build.bat restore` - 패키지 복원
- `build.bat rebuild` - 정리 후 빌드
- `build.bat retest` - 정리 후 테스트
- `build.bat release` - 릴리즈 빌드
- `build.bat debug` - 디버그 빌드
- `build.bat info` - 프로젝트 정보 표시

## 주의사항

- **NUnit 충돌 방지**: 테스트 프로젝트가 별도로 분리되어 있어 Unity 엔진에서 NUnit.Framework 충돌이 발생하지 않습니다.
- **메모리 관리**: `RecastNavigationWrapper`는 `IDisposable`을 구현하므로 사용 후 반드시 `Dispose()`를 호출해야 합니다.
- **플랫폼 호환성**: 현재 Windows x64 플랫폼을 지원합니다.

## 라이선스

이 프로젝트는 Recast Navigation의 ZLib 라이선스를 따릅니다. 