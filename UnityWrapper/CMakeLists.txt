cmake_minimum_required(VERSION 3.5)

project(RecastNavigationUnity)

# Set C++ standard
set_property(GLOBAL PROPERTY CXX_STANDARD 98)

# Unity DLL specific options
option(RECASTNAVIGATION_DT_POLYREF64 "Use 64bit polyrefs instead of 32bit for Detour" OFF)
option(RECASTNAVIGATION_DT_VIRTUAL_QUERYFILTER "Use dynamic dispatch for dtQueryFilter in Detour to allow for custom filters" OFF)
option(RECASTNAVIGATION_ENABLE_ASSERTS "Enable custom recastnavigation asserts" "$<IF:$<CONFIG:Debug>,ON,OFF>")

# Enable Windows export all symbols for DLL
if(MSVC)
    set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
endif()

# Include directories (RecastDemo 스타일로 수정)
include_directories(SYSTEM ${CMAKE_SOURCE_DIR}/DebugUtils/Include)
include_directories(SYSTEM ${CMAKE_SOURCE_DIR}/Detour/Include)
include_directories(SYSTEM ${CMAKE_SOURCE_DIR}/DetourCrowd/Include)
include_directories(SYSTEM ${CMAKE_SOURCE_DIR}/DetourTileCache/Include)
include_directories(SYSTEM ${CMAKE_SOURCE_DIR}/Recast/Include)
include_directories(Include)

# Unity wrapper source files
set(UNITY_WRAPPER_SOURCES
    Source/RecastNavigationUnity.cpp
    Source/UnityNavMeshBuilder.cpp
    Source/UnityNavMeshQuery.cpp
    Source/UnityCrowdManager.cpp
)

# Create shared library for Unity
add_library(RecastNavigationUnity SHARED ${UNITY_WRAPPER_SOURCES})

# Define RECASTNAVIGATIONUNITY_EXPORTS for DLL export
target_compile_definitions(RecastNavigationUnity PRIVATE RECASTNAVIGATIONUNITY_EXPORTS)

# Add dependencies (RecastDemo 스타일로 수정)
add_dependencies(RecastNavigationUnity DebugUtils Detour DetourCrowd DetourTileCache Recast)

# Link with RecastNavigation libraries
target_link_libraries(RecastNavigationUnity 
    DebugUtils 
    Detour 
    DetourCrowd 
    DetourTileCache 
    Recast
)

# Set output directory for Unity
set_target_properties(RecastNavigationUnity PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/Unity
    LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/Unity
    ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/Unity
)

# Copy DLL to Unity project directory after build (optional)
if(WIN32)
    add_custom_command(TARGET RecastNavigationUnity
        POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different 
            ${CMAKE_BINARY_DIR}/Unity/$<CONFIG>/RecastNavigationUnity.dll 
            ${CMAKE_SOURCE_DIR}/UnityProject/Assets/Plugins/Editor/
    )
endif()

# Install target
install(TARGETS RecastNavigationUnity
    RUNTIME DESTINATION lib
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
) 