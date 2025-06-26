cmake_minimum_required(VERSION 3.15)

# Unity용 RecastNavigation DLL 프로젝트
project(RecastNavigationUnity)

# C++ 표준 설정
set_property(GLOBAL PROPERTY CXX_STANDARD 11)

# DLL 빌드 설정
set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)

# Unity 플랫폼별 설정
if(WIN32)
    set(UNITY_PLATFORM "Windows")
    set(UNITY_ARCH "x64")
elseif(APPLE)
    set(UNITY_PLATFORM "macOS")
    set(UNITY_ARCH "x64")
elseif(UNIX)
    set(UNITY_PLATFORM "Linux")
    set(UNITY_ARCH "x64")
endif()

# Unity Wrapper 소스 파일들
set(UNITY_WRAPPER_SOURCES
    Source/UnityRecastWrapper.cpp
    Source/UnityNavMeshBuilder.cpp
    Source/UnityPathfinding.cpp
)

# Unity Wrapper 헤더 파일들
set(UNITY_WRAPPER_HEADERS
    Include/UnityRecastWrapper.h
    Include/UnityNavMeshBuilder.h
    Include/UnityPathfinding.h
)

# Unity용 DLL 생성
add_library(RecastNavigationUnity SHARED
    ${UNITY_WRAPPER_SOURCES}
    ${UNITY_WRAPPER_HEADERS}
)

# 헤더 파일 경로 설정
target_include_directories(RecastNavigationUnity PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/Include
    ${CMAKE_SOURCE_DIR}/Recast/Include
    ${CMAKE_SOURCE_DIR}/Detour/Include
    ${CMAKE_SOURCE_DIR}/DetourCrowd/Include
    ${CMAKE_SOURCE_DIR}/DetourTileCache/Include
    ${CMAKE_SOURCE_DIR}/DebugUtils/Include
)

# 라이브러리 링크 (직접 링크)
target_link_libraries(RecastNavigationUnity
    Recast
    Detour
    DetourCrowd
    DetourTileCache
    DebugUtils
)

# 컴파일러별 설정
if(MSVC)
    target_compile_options(RecastNavigationUnity PRIVATE /W4)
    # Unity 호환성을 위한 설정
    set_target_properties(RecastNavigationUnity PROPERTIES
        COMPILE_DEFINITIONS "UNITY_EXPORT"
        PREFIX ""
        SUFFIX ".dll"
    )
elseif(CMAKE_COMPILER_IS_GNUCXX)
    target_compile_options(RecastNavigationUnity PRIVATE -Wall -Wextra)
    set_target_properties(RecastNavigationUnity PROPERTIES
        COMPILE_DEFINITIONS "UNITY_EXPORT"
        PREFIX "lib"
        SUFFIX ".so"
    )
elseif(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
    target_compile_options(RecastNavigationUnity PRIVATE -Wall -Wextra)
    set_target_properties(RecastNavigationUnity PROPERTIES
        COMPILE_DEFINITIONS "UNITY_EXPORT"
        PREFIX "lib"
        SUFFIX ".dylib"
    )
endif()

# 출력 디렉토리 설정
set_target_properties(RecastNavigationUnity PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/UnityPlugins/${UNITY_PLATFORM}/${UNITY_ARCH}"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/UnityPlugins/${UNITY_PLATFORM}/${UNITY_ARCH}"
    ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/UnityPlugins/${UNITY_PLATFORM}/${UNITY_ARCH}"
)

# 설치 설정
install(TARGETS RecastNavigationUnity
    RUNTIME DESTINATION "UnityPlugins/${UNITY_PLATFORM}/${UNITY_ARCH}"
    LIBRARY DESTINATION "UnityPlugins/${UNITY_PLATFORM}/${UNITY_ARCH}"
    ARCHIVE DESTINATION "UnityPlugins/${UNITY_PLATFORM}/${UNITY_ARCH}"
)

install(FILES ${UNITY_WRAPPER_HEADERS}
    DESTINATION "UnityPlugins/Include"
)

# 테스트 프로젝트 추가
if(RECASTNAVIGATION_TESTS)
    add_subdirectory(Tests)
endif() 