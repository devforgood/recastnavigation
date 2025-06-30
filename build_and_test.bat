@echo off
setlocal enabledelayedexpansion

REM vcpkg 환경 변수 설정
set VCPKG_ROOT=D:\projects\vcpkg

echo.
echo ==========================================
echo UnityWrapper 테스트 빌드 및 실행 스크립트
echo ==========================================
echo.

REM 현재 디렉토리 확인
echo [INFO] 현재 디렉토리: %CD%
echo.

REM build 폴더가 없으면 생성
if not exist build (
    echo [0/5] build 폴더가 없어 새로 생성...
    mkdir build
    echo [INFO] build 폴더 생성 완료
)

REM CMake 설정(항상 실행)
echo [0/5] CMake 설정...
echo [INFO] build 디렉토리로 이동...
cd build

echo [INFO] CMake 명령 실행 중...
echo [CMD] cmake -G "Visual Studio 17 2022" -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=D:/projects/vcpkg/scripts/buildsystems/vcpkg.cmake ..
cmake -G "Visual Studio 17 2022" -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=D:/projects/vcpkg/scripts/buildsystems/vcpkg.cmake ..
if %ERRORLEVEL% neq 0 (
    echo [ERROR] CMake 설정 실패!
    echo [INFO] 오류 코드: %ERRORLEVEL%
    exit /b 1
)
echo [SUCCESS] CMake 설정 완료
cd ..

REM 빌드 디렉토리로 이동
echo [1/5] 빌드 디렉토리로 이동 중...
cd build
if %ERRORLEVEL% neq 0 (
    echo [ERROR] build 디렉토리를 찾을 수 없습니다!
    exit /b 1
)
echo [INFO] 빌드 디렉토리: %CD%
echo.

REM UnityWrapper 테스트와 RecastDemo 빌드
echo [2/5] UnityWrapper 테스트 및 RecastDemo 빌드 중...
echo [INFO] 빌드 명령 실행 중...
echo [CMD] cmake --build . --config Release --target UnityWrapperTests RecastDemo
cmake --build . --config Release --target UnityWrapperTests RecastDemo
if %ERRORLEVEL% neq 0 (
    echo [ERROR] 빌드 실패!
    echo [INFO] 오류 코드: %ERRORLEVEL%
    echo [INFO] 빌드 로그를 확인해주세요.
    exit /b 1
)
echo [SUCCESS] 빌드 성공!
echo.

REM DLL 파일 존재 확인
echo [3/5] DLL 파일 확인 중...
if not exist "UnityPlugins\Windows\x64\Release\UnityWrapper.dll" (
    echo [ERROR] UnityWrapper.dll을 찾을 수 없습니다!
    echo [INFO] 경로: UnityPlugins\Windows\x64\Release\UnityWrapper.dll
    exit /b 1
)
echo [INFO] UnityWrapper.dll 발견: UnityPlugins\Windows\x64\Release\UnityWrapper.dll
echo.

REM DLL 복사
echo [4/5] DLL 복사 중...
echo [INFO] 복사 명령 실행 중...
echo [CMD] copy "UnityPlugins\Windows\x64\Release\UnityWrapper.dll" "Tests\Release\UnityWrapper.dll" /Y
copy "UnityPlugins\Windows\x64\Release\UnityWrapper.dll" "Tests\Release\UnityWrapper.dll" /Y
if %ERRORLEVEL% neq 0 (
    echo [ERROR] DLL 복사 실패!
    echo [INFO] 오류 코드: %ERRORLEVEL%
    exit /b 1
)
echo [SUCCESS] DLL 복사 성공!
echo.

REM 테스트 실행 파일 확인
echo [5/5] 테스트 실행 파일 확인 중...
if not exist "Tests\Release\UnityWrapperTests.exe" (
    echo [ERROR] UnityWrapperTests.exe를 찾을 수 없습니다!
    echo [INFO] 경로: Tests\Release\UnityWrapperTests.exe
    exit /b 1
)
echo [INFO] UnityWrapperTests.exe 발견: Tests\Release\UnityWrapperTests.exe
echo.

REM 테스트 실행
echo ==========================================
echo TestUnityNavMeshComparison 테스트 실행 중...
echo ==========================================
echo [INFO] 테스트 디렉토리로 이동...
cd Tests\Release
echo [INFO] 테스트 명령 실행 중...
echo [CMD] UnityWrapperTests.exe "[NavMeshComparison]" --success
UnityWrapperTests.exe "[NavMeshComparison]" --success

echo.
echo ==========================================
echo 테스트 완료!
echo ==========================================
echo [INFO] 아무 키나 누르면 종료됩니다...
pause 