@echo off
setlocal enabledelayedexpansion

REM Set vcpkg environment variable
set VCPKG_ROOT=D:\projects\vcpkg

echo.
echo ==========================================
echo UnityWrapper build and test script
echo ==========================================
echo.

echo [INFO] Current directory: %CD%
echo.

REM Create build folder if not exists
if not exist build (
    echo [0/5] Creating build folder...
    mkdir build
    echo [INFO] Build folder created
)

echo [0/5] CMake configure...
echo [INFO] Moving to build directory...
cd build

echo [INFO] Running CMake command...
echo [CMD] cmake -G "Visual Studio 17 2022" -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=D:/projects/vcpkg/scripts/buildsystems/vcpkg.cmake ..
cmake -G "Visual Studio 17 2022" -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=D:/projects/vcpkg/scripts/buildsystems/vcpkg.cmake ..
if %ERRORLEVEL% neq 0 (
    echo [ERROR] CMake configure failed!
    echo [INFO] Error code: %ERRORLEVEL%
    exit /b 1
)
echo [SUCCESS] CMake configure complete
cd ..

echo [1/5] Moving to build directory...
cd build
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Cannot find build directory!
    exit /b 1
)
echo [INFO] Build directory: %CD%
echo.

echo [2/5] Building UnityWrapper tests and RecastDemo...
echo [INFO] Running build command...
echo [CMD] cmake --build . --config Release --target UnityWrapperTests RecastDemo
cmake --build . --config Release --target UnityWrapperTests RecastDemo
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Build failed!
    echo [INFO] Error code: %ERRORLEVEL%
    echo [INFO] Please check the build log.
    exit /b 1
)
echo [SUCCESS] Build succeeded!
echo.

echo [3/5] Checking DLL file...
if not exist "UnityPlugins\Windows\x64\Release\UnityWrapper.dll" (
    echo [ERROR] UnityWrapper.dll not found!
    echo [INFO] Path: UnityPlugins\Windows\x64\Release\UnityWrapper.dll
    exit /b 1
)
echo [INFO] UnityWrapper.dll found: UnityPlugins\Windows\x64\Release\UnityWrapper.dll
echo.

echo [4/5] Copying DLL...
echo [INFO] Running copy command...
echo [CMD] copy "UnityPlugins\Windows\x64\Release\UnityWrapper.dll" "Tests\Release\UnityWrapper.dll" /Y
copy "UnityPlugins\Windows\x64\Release\UnityWrapper.dll" "Tests\Release\UnityWrapper.dll" /Y
if %ERRORLEVEL% neq 0 (
    echo [ERROR] DLL copy failed!
    echo [INFO] Error code: %ERRORLEVEL%
    exit /b 1
)
echo [SUCCESS] DLL copy succeeded!
echo.

echo [5/5] Checking test executable...
if not exist "Tests\Release\UnityWrapperTests.exe" (
    echo [ERROR] UnityWrapperTests.exe not found!
    echo [INFO] Path: Tests\Release\UnityWrapperTests.exe
    exit /b 1
)
echo [INFO] UnityWrapperTests.exe found: Tests\Release\UnityWrapperTests.exe
echo.

echo ==========================================
echo Running TestUnityNavMeshComparison test...
echo ==========================================
echo [INFO] Moving to test directory...
cd Tests\Release
echo [INFO] Running test command...
echo [CMD] UnityWrapperTests.exe "[NavMeshComparison]" --success
UnityWrapperTests.exe "[NavMeshComparison]" --success

echo.
echo ==========================================
echo Test complete!
echo ==========================================
echo [INFO] Press any key to exit... 