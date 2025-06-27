@echo off
echo Generating Visual Studio 2022 Solution...

REM Check if CMake is available
where cmake >nul 2>&1
if %errorlevel% neq 0 (
    echo CMake not found in PATH. Trying Visual Studio's CMake...
    set "CMAKE_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
    if not exist "%CMAKE_PATH%" (
        set "CMAKE_PATH=C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
    )
    if not exist "%CMAKE_PATH%" (
        set "CMAKE_PATH=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
    )
    if not exist "%CMAKE_PATH%" (
        echo CMake not found. Please install CMake or Visual Studio 2022 with CMake support.
        pause
        exit /b 1
    )
) else (
    set "CMAKE_PATH=cmake"
)

REM Create build directory
if not exist "build" mkdir build
cd build

REM Generate Visual Studio 2022 solution
echo Running CMake...
"%CMAKE_PATH%" .. -G "Visual Studio 17 2022" -A x64 -DRECASTNAVIGATION_DEMO=OFF -DRECASTNAVIGATION_UNITY=ON

if %errorlevel% equ 0 (
    echo.
    echo Visual Studio 2022 solution generated successfully!
    echo Solution file: build\RecastNavigation.sln
    echo.
    echo You can now open RecastNavigation.sln in Visual Studio 2022
    echo.
    echo Note: RecastDemo is excluded, but UnityWrapper is included as requested.
) else (
    echo.
    echo Failed to generate Visual Studio 2022 solution.
    echo Please check the error messages above.
)

cd ..
pause 