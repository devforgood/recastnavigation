@echo off

REM vcpkg 환경 변수 설정
set VCPKG_ROOT=D:\projects\vcpkg

echo UnityWrapper 테스트 빌드 및 실행 스크립트
echo ==========================================
echo.

REM 현재 디렉토리 확인
echo 현재 디렉토리: %CD%
echo.

REM build 폴더가 없으면 생성
if not exist build (
    echo [0/5] build 폴더가 없어 새로 생성...
    mkdir build
)

REM CMake 설정(항상 실행)
echo [0/5] CMake 설정...
cd build

echo 명령: cmake -G "Visual Studio 17 2022" -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=D:/projects/vcpkg/scripts/buildsystems/vcpkg.cmake ..
cmake -G "Visual Studio 17 2022" -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=D:/projects/vcpkg/scripts/buildsystems/vcpkg.cmake ..
if %ERRORLEVEL% neq 0 (
    echo 오류: CMake 설정 실패!
    pause
    exit /b 1
)
cd ..

REM 빌드 디렉토리로 이동
echo [1/5] 빌드 디렉토리로 이동 중...
cd build
if %ERRORLEVEL% neq 0 (
    echo 오류: build 디렉토리를 찾을 수 없습니다!
    pause
    exit /b 1
)
echo 빌드 디렉토리: %CD%
echo.

REM UnityWrapper 테스트와 RecastDemo 빌드
echo [2/5] UnityWrapper 테스트 및 RecastDemo 빌드 중...
echo 명령: cmake --build . --config Release --target UnityWrapperTests RecastDemo
cmake --build . --config Release --target UnityWrapperTests RecastDemo
if %ERRORLEVEL% neq 0 (
    echo 오류: 빌드 실패!
    echo 빌드 로그를 확인해주세요.
    pause
    exit /b 1
)
echo 빌드 성공!
echo.

REM DLL 파일 존재 확인
echo [3/5] DLL 파일 확인 중...
if not exist "UnityPlugins\Windows\x64\Release\UnityWrapper.dll" (
    echo 오류: UnityWrapper.dll을 찾을 수 없습니다!
    echo 경로: UnityPlugins\Windows\x64\Release\UnityWrapper.dll
    pause
    exit /b 1
)
echo UnityWrapper.dll 발견: UnityPlugins\Windows\x64\Release\UnityWrapper.dll
echo.

REM DLL 복사
echo [4/5] DLL 복사 중...
echo 복사: UnityPlugins\Windows\x64\Release\UnityWrapper.dll -> Tests\Release\UnityWrapper.dll
copy "UnityPlugins\Windows\x64\Release\UnityWrapper.dll" "Tests\Release\UnityWrapper.dll" /Y
if %ERRORLEVEL% neq 0 (
    echo 오류: DLL 복사 실패!
    pause
    exit /b 1
)
echo DLL 복사 성공!
echo.

REM 테스트 실행 파일 확인
echo [5/5] 테스트 실행 파일 확인 중...
if not exist "Tests\Release\UnityWrapperTests.exe" (
    echo 오류: UnityWrapperTests.exe를 찾을 수 없습니다!
    pause
    exit /b 1
)
echo UnityWrapperTests.exe 발견: Tests\Release\UnityWrapperTests.exe
echo.

REM 테스트 실행
echo ==========================================
echo TestUnityNavMeshComparison 테스트 실행 중...
echo 명령: UnityWrapperTests.exe "[NavMeshComparison]" --success
echo ==========================================
cd Tests\Release
UnityWrapperTests.exe "[NavMeshComparison]" --success

echo.
echo ==========================================
echo 테스트 완료!
echo ==========================================
pause 