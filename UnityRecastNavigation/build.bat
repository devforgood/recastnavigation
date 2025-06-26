@echo off
REM UnityRecastNavigation Build Script for Windows
REM 사용법: build.bat [target]

setlocal enabledelayedexpansion

REM 변수 정의
set PROJECT_NAME=UnityRecastNavigation
set TEST_PROJECT=Tests\UnityRecastNavigation.Tests
set SOLUTION_FILE=UnityRecastNavigation.sln
set BUILD_CONFIG=Release

REM 기본 타겟
if "%1"=="" goto build

REM 타겟별 분기
if "%1"=="build" goto build
if "%1"=="test" goto test
if "%1"=="test-verbose" goto test-verbose
if "%1"=="clean" goto clean
if "%1"=="clean-all" goto clean-all
if "%1"=="restore" goto restore
if "%1"=="rebuild" goto rebuild
if "%1"=="retest" goto retest
if "%1"=="release" goto release
if "%1"=="debug" goto debug
if "%1"=="info" goto info
if "%1"=="help" goto info
goto unknown

:build
echo Building %PROJECT_NAME%...
dotnet build %SOLUTION_FILE% --configuration %BUILD_CONFIG%
if %ERRORLEVEL% EQU 0 (
    echo Build completed successfully!
) else (
    echo Build failed!
    exit /b 1
)
goto end

:test
echo Building test project...
dotnet build %TEST_PROJECT%.csproj --configuration %BUILD_CONFIG%
if %ERRORLEVEL% NEQ 0 (
    echo Test build failed!
    exit /b 1
)
echo Running tests...
dotnet test %TEST_PROJECT%.csproj --configuration %BUILD_CONFIG% --verbosity normal
if %ERRORLEVEL% EQU 0 (
    echo Tests completed!
) else (
    echo Tests failed!
    exit /b 1
)
goto end

:test-verbose
echo Building test project...
dotnet build %TEST_PROJECT%.csproj --configuration %BUILD_CONFIG%
if %ERRORLEVEL% NEQ 0 (
    echo Test build failed!
    exit /b 1
)
echo Running tests with verbose output...
dotnet test %TEST_PROJECT%.csproj --configuration %BUILD_CONFIG% --verbosity detailed
if %ERRORLEVEL% EQU 0 (
    echo Tests completed!
) else (
    echo Tests failed!
    exit /b 1
)
goto end

:clean
echo Cleaning build artifacts...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
if exist Tests\bin rmdir /s /q Tests\bin
if exist Tests\obj rmdir /s /q Tests\obj
echo Clean completed!
goto end

:clean-all
call :clean
echo Cleaning all build artifacts...
dotnet clean %SOLUTION_FILE%
echo Full clean completed!
goto end

:restore
echo Restoring packages...
dotnet restore %SOLUTION_FILE%
if %ERRORLEVEL% EQU 0 (
    echo Package restore completed!
) else (
    echo Package restore failed!
    exit /b 1
)
goto end

:rebuild
call :clean
call :build
goto end

:retest
call :clean
call :test
goto end

:release
call :clean
echo Building release version...
dotnet build %SOLUTION_FILE% --configuration Release --no-restore
if %ERRORLEVEL% EQU 0 (
    echo Release build completed!
) else (
    echo Release build failed!
    exit /b 1
)
goto end

:debug
call :clean
echo Building debug version...
dotnet build %SOLUTION_FILE% --configuration Debug --no-restore
if %ERRORLEVEL% EQU 0 (
    echo Debug build completed!
) else (
    echo Debug build failed!
    exit /b 1
)
goto end

:info
echo === %PROJECT_NAME% Project Information ===
echo Project: %PROJECT_NAME%
echo Solution: %SOLUTION_FILE%
echo Test Project: %TEST_PROJECT%
echo Build Config: %BUILD_CONFIG%
echo.
echo Available targets:
echo   build        - Build the project
echo   test         - Run tests
echo   test-verbose - Run tests with verbose output
echo   clean        - Clean build artifacts
echo   clean-all    - Full clean
echo   restore      - Restore packages
echo   rebuild      - Clean and build
echo   retest       - Clean and test
echo   release      - Build release version
echo   debug        - Build debug version
echo   info         - Show this information
goto end

:unknown
echo Unknown target: %1
echo Use 'build.bat info' to see available targets
exit /b 1

:end
endlocal 