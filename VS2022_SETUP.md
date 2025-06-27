# Visual Studio 2022 설정 가이드

## 방법 1: Visual Studio 2022에서 직접 CMake 프로젝트 열기 (권장)

1. **Visual Studio 2022 실행**
2. **"Open a local folder"** 클릭
3. **recastnavigation 폴더 선택**
4. Visual Studio가 자동으로 CMake 프로젝트로 인식합니다

## 방법 2: CMakePresets.json 사용

1. **Visual Studio 2022 실행**
2. **"Open a local folder"** 클릭
3. **recastnavigation 폴더 선택**
4. **CMake 설정**에서 다음 중 하나 선택:
   - `windows-debug` (디버그 빌드)
   - `windows-release` (릴리즈 빌드)

## 포함된 프로젝트

다음 프로젝트들이 포함됩니다:
- **Recast** - 메시 생성 및 처리
- **Detour** - 네비게이션 메시 쿼리
- **DetourCrowd** - 군중 시뮬레이션
- **DetourTileCache** - 타일 캐싱 시스템
- **DebugUtils** - 디버깅 및 시각화 도구
- **Tests** - 테스트 실행 파일
- **UnityWrapper** - Unity용 래퍼 라이브러리
- **UnityWrapperTests** - UnityWrapper 테스트

## 제외된 프로젝트

다음 프로젝트들은 제외됩니다:
- **RecastDemo** - 데모 애플리케이션

## 빌드 설정

### CMake 옵션
- `RECASTNAVIGATION_DEMO=OFF` - RecastDemo 제외
- `RECASTNAVIGATION_UNITY=ON` - UnityWrapper 포함
- `RECASTNAVIGATION_TESTS=ON` - 테스트 포함

### 플랫폼
- **아키텍처**: x64
- **생성기**: Visual Studio 17 2022

## 문제 해결

### CMake가 설치되지 않은 경우
1. **Visual Studio Installer** 실행
2. **Visual Studio 2022 수정**
3. **개별 구성 요소** 탭에서 **CMake 도구** 체크
4. **수정** 클릭

### 빌드 오류가 발생하는 경우
1. **빌드 > 솔루션 정리** 실행
2. **빌드 > 솔루션 다시 빌드** 실행

### UnityWrapper.lib 오류가 발생하는 경우
1. **UnityWrapper 프로젝트가 먼저 빌드되었는지 확인**
2. **빌드 순서**: UnityWrapper → UnityWrapperTests
3. **전체 솔루션 다시 빌드** 실행

## 추가 정보

- 프로젝트 구조는 `CMakePresets.json`에서 관리됩니다
- 빌드 출력은 `build/` 폴더에 생성됩니다
- 설치 파일은 `install/` 폴더에 생성됩니다 