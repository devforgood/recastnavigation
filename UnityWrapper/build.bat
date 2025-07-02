@echo off
echo Building RecastNavigation Unity DLL...

REM Create build directory
if not exist "build" mkdir build
cd build

REM Configure with CMake
cmake .. -G "Visual Studio 17 2022" -A x64 -DRECASTNAVIGATION_DEMO=OFF -DRECASTNAVIGATION_TESTS=OFF

REM Build the project
cmake --build . --config Release

REM Copy DLL to Unity project (if exists)
if exist "Unity\Release\RecastNavigationUnity.dll" (
    if exist "..\..\UnityProject\Assets\Plugins" (
        copy "Unity\Release\RecastNavigationUnity.dll" "..\..\UnityProject\Assets\Plugins\"
        echo DLL copied to Unity project
    ) else (
        echo Unity project not found, DLL is in build\Unity\Release\
    )
)

echo Build completed!