@echo off
echo UnityWrapper 테스트 실행 스크립트
echo =================================

REM 빌드 디렉토리 생성
if not exist "build" mkdir build
cd build

REM CMake 설정 (테스트 포함)
echo CMake 설정 중...
cmake .. -G "Visual Studio 17 2022" -A x64 -DRECASTNAVIGATION_UNITY=ON -DRECASTNAVIGATION_TESTS=ON
if %ERRORLEVEL% neq 0 (
    echo CMake 설정 실패!
    pause
    exit /b 1
)

REM 빌드 실행
echo 빌드 중...
cmake --build . --config Release
if %ERRORLEVEL% neq 0 (
    echo 빌드 실패!
    pause
    exit /b 1
)

REM 테스트 실행
echo 테스트 실행 중...
ctest --output-on-failure -C Release
if %ERRORLEVEL% neq 0 (
    echo 일부 테스트가 실패했습니다!
    pause
    exit /b 1
)

echo 모든 테스트가 성공적으로 완료되었습니다!
pause 