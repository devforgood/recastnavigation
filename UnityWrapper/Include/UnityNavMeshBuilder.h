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

// RecastDemo 상수들
enum SamplePartitionType
{
    SAMPLE_PARTITION_WATERSHED,
    SAMPLE_PARTITION_MONOTONE,
    SAMPLE_PARTITION_LAYERS,
};

enum SamplePolyAreas
{
    SAMPLE_POLYAREA_GROUND,
    SAMPLE_POLYAREA_WATER,
    SAMPLE_POLYAREA_ROAD,
    SAMPLE_POLYAREA_DOOR,
    SAMPLE_POLYAREA_GRASS,
    SAMPLE_POLYAREA_JUMP,
};

enum SamplePolyFlags
{
    SAMPLE_POLYFLAGS_WALK   = 0x01,     // Ability to walk (ground, grass, road)
    SAMPLE_POLYFLAGS_SWIM   = 0x02,     // Ability to swim (water).
    SAMPLE_POLYFLAGS_DOOR   = 0x04,     // Ability to move through doors.
    SAMPLE_POLYFLAGS_JUMP   = 0x08,     // Ability to jump.
    SAMPLE_POLYFLAGS_DISABLED = 0x10,   // Disabled polygon
    SAMPLE_POLYFLAGS_ALL = 0xffff       // All abilities.
};

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
    
    // 디버그 데이터 가져오기 (시각화용)
    bool GetDebugVertices(std::vector<float>& vertices) const;
    bool GetDebugIndices(std::vector<int>& indices) const;
    
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
    
    // 매개변수 유효성 검사 및 조정
    bool ValidateAndAdjustSettings(const UnityMeshData* meshData, UnityNavMeshBuildSettings* settings);
    void CalculateMeshBounds(const UnityMeshData* meshData, float* bmin, float* bmax);
    bool IsParameterConfigurationValid(const UnityMeshData* meshData, const UnityNavMeshBuildSettings* settings, std::string& warning);
    void AdjustParametersForMesh(const UnityMeshData* meshData, UnityNavMeshBuildSettings* settings);
    
    // NavMesh 품질 분석 및 검증
    void AnalyzeNavMeshQuality(const UnityMeshData* meshData, const UnityNavMeshBuildSettings* settings);
    void ValidateNavMeshDataConsistency();
    
    // RecastDemo 스타일 설정 관리
    void resetCommonSettings();
    void applyRecastDemoSettings(UnityNavMeshBuildSettings* settings);
    
    // 내부 상태 관리
    bool IsInitialized() const;
    void CleanupNavMeshData();
    bool SerializeNavMeshData();
    
    // NavMesh 데이터 저장
    std::vector<unsigned char> m_navMeshData;
    int m_navMeshDataSize;
    
    // RecastDemo 스타일 설정값들
    float m_cellSize;
    float m_cellHeight;
    float m_agentHeight;
    float m_agentRadius;
    float m_agentMaxClimb;
    float m_agentMaxSlope;
    float m_regionMinSize;
    float m_regionMergeSize;
    float m_edgeMaxLen;
    float m_edgeMaxError;
    float m_vertsPerPoly;
    float m_detailSampleDist;
    float m_detailSampleMaxError;
    int m_partitionType;
}; 