cmake_minimum_required(VERSION 3.15)

# UnityWrapper 테스트 프로젝트
project(UnityWrapperTests)

# C++ 표준 설정
set_property(GLOBAL PROPERTY CXX_STANDARD 11)

# 테스트 실행 파일 생성
add_executable(UnityWrapperTests
    TestUnityRecastWrapper.cpp
    TestUnityNavMeshBuilder.cpp
    TestUnityPathfinding.cpp
    ${CMAKE_SOURCE_DIR}/Tests/Contrib/catch2/catch_amalgamated.cpp
)

# 헤더 파일 경로 설정
target_include_directories(UnityWrapperTests PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/../Include
    ${CMAKE_SOURCE_DIR}/Recast/Include
    ${CMAKE_SOURCE_DIR}/Detour/Include
    ${CMAKE_SOURCE_DIR}/DetourCrowd/Include
    ${CMAKE_SOURCE_DIR}/DetourTileCache/Include
    ${CMAKE_SOURCE_DIR}/DebugUtils/Include
    ${CMAKE_SOURCE_DIR}/Tests/Contrib/catch2
)

# 라이브러리 링크 (직접 링크)
target_link_libraries(UnityWrapperTests
    RecastNavigationUnity
    Recast
    Detour
    DetourCrowd
    DetourTileCache
    DebugUtils
)

# DLL import 라이브러리 경로 추가
if(WIN32)
    target_link_directories(UnityWrapperTests PRIVATE
        ${CMAKE_BINARY_DIR}/UnityPlugins/Windows/x64
    )
endif()

# 컴파일러별 설정
if(MSVC)
    target_compile_options(UnityWrapperTests PRIVATE /W4)
elseif(CMAKE_COMPILER_IS_GNUCXX)
    target_compile_options(UnityWrapperTests PRIVATE -Wall -Wextra)
elseif(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
    target_compile_options(UnityWrapperTests PRIVATE -Wall -Wextra)
endif()

# 출력 디렉토리 설정
set_target_properties(UnityWrapperTests PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/Tests"
)

# DLL을 실행 폴더로 복사하는 post-build 명령 추가
if(WIN32)
    add_custom_command(TARGET UnityWrapperTests POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "${CMAKE_BINARY_DIR}/UnityPlugins/Windows/x64/Debug/RecastNavigationUnity.dll"
        "${CMAKE_BINARY_DIR}/Tests/Debug/RecastNavigationUnity.dll"
        COMMENT "Copying RecastNavigationUnity.dll to test output directory"
    )
endif() 