include_directories(../Detour/Include)
include_directories(../Recast/Include)
include_directories(../UnityWrapper/Include)

add_executable(Tests
	Detour/Tests_Detour.cpp
	Recast/Bench_rcVector.cpp
	Recast/Tests_Alloc.cpp
	Recast/Tests_Recast.cpp
	Recast/Tests_RecastFilter.cpp
	DetourCrowd/Tests_DetourPathCorridor.cpp
	Tests_NavMeshComparison.cpp
)

set_property(TARGET Tests PROPERTY CXX_STANDARD 17)

add_dependencies(Tests Recast Detour DetourCrowd RecastNavigationUnity)
target_link_libraries(Tests Recast Detour DetourCrowd RecastNavigationUnity)

find_package(Catch2 QUIET)
if (Catch2_FOUND)
	target_link_libraries(Tests Catch2::Catch2WithMain)
else()
	include_directories(./Contrib)
	target_sources(Tests PRIVATE Contrib/catch2/catch_amalgamated.cpp)
endif()

add_test(NAME Tests COMMAND Tests WORKING_DIRECTORY ${CMAKE_SOURCE_DIR})

# Copy UnityWrapper DLL to Tests directory for runtime
if(WIN32)
    add_custom_command(TARGET Tests
        POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different 
            ${CMAKE_BINARY_DIR}/Unity/$<CONFIG>/RecastNavigationUnity.dll 
            ${CMAKE_BINARY_DIR}/Tests/$<CONFIG>/
    )
endif()
