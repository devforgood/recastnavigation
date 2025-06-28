#pragma once

#include "UnityRecastWrapper.h"
#include "UnityLog.h"
#include "Recast.h"
#include "DetourNavMesh.h"
#include "DetourNavMeshBuilder.h"
#include <vector>
#include <memory>
#include <iostream>
#include <cstdint>

class UnityNavMeshBuilder {
public:
    UnityNavMeshBuilder();
    ~UnityNavMeshBuilder();
    
    // NavMesh 빌드
    UnityNavMeshResult BuildNavMesh(
        const UnityMeshData* meshData,
        const UnityNavMeshBuildSettings* settings
    );
    
    // NavMesh 로드
    bool LoadNavMesh(const unsigned char* data, int dataSize);
    
    // NavMesh 인스턴스 가져오기
    dtNavMesh* GetNavMesh() const { 
        return m_navMesh.get(); 
    }
    dtNavMeshQuery* GetNavMeshQuery() const { 
        return m_navMeshQuery.get(); 
    }
    
    int GetPolyCount() const;
    int GetVertexCount() const;
    
private:
    // Recast 컨텍스트
    std::unique_ptr<rcContext> m_ctx;
    
    // NavMesh 및 쿼리 객체
    std::unique_ptr<dtNavMesh> m_navMesh;
    std::unique_ptr<dtNavMeshQuery> m_navMeshQuery;
    
    // 메시 데이터 저장
    const UnityMeshData* m_meshData;
    
    // 빌드 과정에서 사용하는 데이터
    std::vector<unsigned char> m_triareas;
    std::unique_ptr<rcHeightfield> m_solid;
    std::unique_ptr<rcCompactHeightfield> m_chf;
    std::unique_ptr<rcContourSet> m_cset;
    std::unique_ptr<rcPolyMesh> m_pmesh;
    std::unique_ptr<rcPolyMeshDetail> m_dmesh;
    
    // NavMesh 빌드 과정
    bool BuildHeightfield(const UnityMeshData* meshData, const UnityNavMeshBuildSettings* settings);
    bool BuildCompactHeightfield(const UnityNavMeshBuildSettings* settings);
    bool BuildRegions(const UnityNavMeshBuildSettings* settings);
    bool BuildContourSet(const UnityNavMeshBuildSettings* settings);
    bool BuildPolyMesh(const UnityNavMeshBuildSettings* settings);
    bool BuildDetailMesh(const UnityNavMeshBuildSettings* settings);
    bool BuildDetourNavMesh(const UnityNavMeshBuildSettings* settings);
    bool CreateSimplePolyMesh(const UnityNavMeshBuildSettings* settings);
    
    // 유틸리티 함수
    void Cleanup();
    void LogBuildSettings(const UnityNavMeshBuildSettings* settings);
}; 