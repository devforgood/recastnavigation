#include "UnityNavMeshBuilder.h"
#include "Recast.h"
#include "DetourNavMesh.h"
#include "DetourNavMeshBuilder.h"
#include <cstring>
#include <algorithm>

UnityNavMeshBuilder::UnityNavMeshBuilder() : m_ctx(nullptr) {
    m_ctx = new rcContext();
}

UnityNavMeshBuilder::~UnityNavMeshBuilder() {
    Cleanup();
    if (m_ctx) {
        delete m_ctx;
        m_ctx = nullptr;
    }
}

UnityNavMeshResult UnityNavMeshBuilder::BuildNavMesh(
    const UnityMeshData* meshData,
    const UnityNavMeshBuildSettings* settings
) {
    UnityNavMeshResult result = {0};
    
    if (!meshData || !settings) {
        result.success = false;
        result.errorMessage = const_cast<char*>("Invalid parameters");
        return result;
    }
    
    // 기존 데이터 정리
    Cleanup();
    
    try {
        // 빌드 과정 실행
        if (!BuildHeightfield(meshData, settings)) {
            result.success = false;
            result.errorMessage = const_cast<char*>("Failed to build heightfield");
            return result;
        }
        
        if (!BuildCompactHeightfield(settings)) {
            result.success = false;
            result.errorMessage = const_cast<char*>("Failed to build compact heightfield");
            return result;
        }
        
        if (!BuildContourSet(settings)) {
            result.success = false;
            result.errorMessage = const_cast<char*>("Failed to build contour set");
            return result;
        }
        
        if (!BuildPolyMesh(settings)) {
            result.success = false;
            result.errorMessage = const_cast<char*>("Failed to build poly mesh");
            return result;
        }
        
        if (!BuildDetailMesh(settings)) {
            result.success = false;
            result.errorMessage = const_cast<char*>("Failed to build detail mesh");
            return result;
        }
        
        if (!BuildDetourNavMesh(settings)) {
            result.success = false;
            result.errorMessage = const_cast<char*>("Failed to build detour nav mesh");
            return result;
        }
        
        // NavMesh 데이터 직렬화
        unsigned char* navData = nullptr;
        int navDataSize = 0;
        
        if (m_navMesh) {
            navDataSize = m_navMesh->getDataSize();
            navData = new unsigned char[navDataSize];
            
            if (m_navMesh->getData(navData, navDataSize) != navDataSize) {
                delete[] navData;
                result.success = false;
                result.errorMessage = const_cast<char*>("Failed to serialize nav mesh data");
                return result;
            }
        }
        
        result.navMeshData = navData;
        result.dataSize = navDataSize;
        result.success = true;
        result.errorMessage = nullptr;
        
    }
    catch (const std::exception& e) {
        result.success = false;
        result.errorMessage = const_cast<char*>(e.what());
    }
    catch (...) {
        result.success = false;
        result.errorMessage = const_cast<char*>("Unknown error during nav mesh building");
    }
    
    return result;
}

bool UnityNavMeshBuilder::LoadNavMesh(const unsigned char* data, int dataSize) {
    if (!data || dataSize <= 0) {
        return false;
    }
    
    Cleanup();
    
    try {
        m_navMesh = std::make_unique<dtNavMesh>();
        
        dtStatus status = m_navMesh->init(data, dataSize, DT_TILE_FREE_DATA);
        if (dtStatusFailed(status)) {
            return false;
        }
        
        m_navMeshQuery = std::make_unique<dtNavMeshQuery>();
        status = m_navMeshQuery->init(m_navMesh.get(), 2048);
        if (dtStatusFailed(status)) {
            return false;
        }
        
        return true;
    }
    catch (...) {
        return false;
    }
}

int UnityNavMeshBuilder::GetPolyCount() const {
    if (!m_navMesh) {
        return 0;
    }
    
    int polyCount = 0;
    for (int i = 0; i < m_navMesh->getMaxTiles(); ++i) {
        const dtMeshTile* tile = m_navMesh->getTile(i);
        if (tile && tile->header) {
            polyCount += tile->header->polyCount;
        }
    }
    
    return polyCount;
}

int UnityNavMeshBuilder::GetVertexCount() const {
    if (!m_navMesh) {
        return 0;
    }
    
    int vertexCount = 0;
    for (int i = 0; i < m_navMesh->getMaxTiles(); ++i) {
        const dtMeshTile* tile = m_navMesh->getTile(i);
        if (tile && tile->header) {
            vertexCount += tile->header->vertCount;
        }
    }
    
    return vertexCount;
}

bool UnityNavMeshBuilder::BuildHeightfield(const UnityMeshData* meshData, const UnityNavMeshBuildSettings* settings) {
    // 바운딩 박스 계산
    float bmin[3] = { FLT_MAX, FLT_MAX, FLT_MAX };
    float bmax[3] = { -FLT_MAX, -FLT_MAX, -FLT_MAX };
    
    for (int i = 0; i < meshData->vertexCount; ++i) {
        const float* v = &meshData->vertices[i * 3];
        bmin[0] = std::min(bmin[0], v[0]);
        bmin[1] = std::min(bmin[1], v[1]);
        bmin[2] = std::min(bmin[2], v[2]);
        bmax[0] = std::max(bmax[0], v[0]);
        bmax[1] = std::max(bmax[1], v[1]);
        bmax[2] = std::max(bmax[2], v[2]);
    }
    
    // Heightfield 생성
    m_solid = std::make_unique<rcHeightfield>();
    if (!rcCreateHeightfield(m_ctx, *m_solid, 
                           static_cast<int>((bmax[0] - bmin[0]) / settings->cellSize + 1),
                           static_cast<int>((bmax[2] - bmin[2]) / settings->cellSize + 1),
                           bmin, bmax, settings->cellSize, settings->cellHeight)) {
        return false;
    }
    
    // 삼각형 영역 분류
    m_triareas.resize(meshData->indexCount / 3);
    rcMarkWalkableTriangles(m_ctx, settings->walkableSlopeAngle,
                           meshData->vertices, meshData->vertexCount,
                           meshData->indices, meshData->indexCount / 3,
                           m_triareas.data());
    
    // Heightfield에 삼각형 래스터화
    if (!rcRasterizeTriangles(m_ctx, meshData->vertices, meshData->vertexCount,
                             meshData->indices, m_triareas.data(),
                             meshData->indexCount / 3, *m_solid, settings->walkableClimb)) {
        return false;
    }
    
    return true;
}

bool UnityNavMeshBuilder::BuildCompactHeightfield(const UnityNavMeshBuildSettings* settings) {
    m_chf = std::make_unique<rcCompactHeightfield>();
    
    if (!rcBuildCompactHeightfield(m_ctx, settings->walkableHeight, settings->walkableClimb,
                                  *m_solid, *m_chf)) {
        return false;
    }
    
    if (!rcErodeWalkableArea(m_ctx, settings->walkableRadius, *m_chf)) {
        return false;
    }
    
    if (!rcBuildDistanceField(m_ctx, *m_chf)) {
        return false;
    }
    
    if (!rcBuildRegions(m_ctx, *m_chf, 0, settings->minRegionArea, settings->mergeRegionArea)) {
        return false;
    }
    
    return true;
}

bool UnityNavMeshBuilder::BuildContourSet(const UnityNavMeshBuildSettings* settings) {
    m_cset = std::make_unique<rcContourSet>();
    
    if (!rcBuildContours(m_ctx, *m_chf, settings->maxSimplificationError,
                        settings->maxEdgeLen, *m_cset)) {
        return false;
    }
    
    return true;
}

bool UnityNavMeshBuilder::BuildPolyMesh(const UnityNavMeshBuildSettings* settings) {
    m_pmesh = std::make_unique<rcPolyMesh>();
    
    if (!rcBuildPolyMesh(m_ctx, *m_cset, settings->maxVertsPerPoly, *m_pmesh)) {
        return false;
    }
    
    return true;
}

bool UnityNavMeshBuilder::BuildDetailMesh(const UnityNavMeshBuildSettings* settings) {
    m_dmesh = std::make_unique<rcPolyMeshDetail>();
    
    if (!rcBuildPolyMeshDetail(m_ctx, *m_pmesh, *m_chf, settings->detailSampleDist,
                              settings->detailSampleMaxError, *m_dmesh)) {
        return false;
    }
    
    return true;
}

bool UnityNavMeshBuilder::BuildDetourNavMesh(const UnityNavMeshBuildSettings* settings) {
    // NavMesh 생성
    m_navMesh = std::make_unique<dtNavMesh>();
    
    dtNavMeshCreateParams params = {};
    params.verts = m_pmesh->verts;
    params.vertCount = m_pmesh->nverts;
    params.polys = m_pmesh->polys;
    params.polyAreas = m_pmesh->areas;
    params.polyFlags = m_pmesh->flags;
    params.polyCount = m_pmesh->npolys;
    params.nvp = m_pmesh->nvp;
    params.detailMeshes = m_dmesh->meshes;
    params.detailVerts = m_dmesh->verts;
    params.detailVertsCount = m_dmesh->nverts;
    params.detailTris = m_dmesh->tris;
    params.detailTriCount = m_dmesh->ntris;
    params.offMeshConVerts = nullptr;
    params.offMeshConRad = nullptr;
    params.offMeshConDir = nullptr;
    params.offMeshConAreas = nullptr;
    params.offMeshConFlags = nullptr;
    params.offMeshConUserID = nullptr;
    params.offMeshConCount = 0;
    params.walkableHeight = settings->walkableHeight;
    params.walkableRadius = settings->walkableRadius;
    params.walkableClimb = settings->walkableClimb;
    params.tileX = 0;
    params.tileY = 0;
    params.tileLayer = 0;
    params.bmin[0] = m_pmesh->bmin[0];
    params.bmin[1] = m_pmesh->bmin[1];
    params.bmin[2] = m_pmesh->bmin[2];
    params.bmax[0] = m_pmesh->bmax[0];
    params.bmax[1] = m_pmesh->bmax[1];
    params.bmax[2] = m_pmesh->bmax[2];
    params.cs = settings->cellSize;
    params.ch = settings->cellHeight;
    params.buildBvTree = true;
    
    unsigned char* navData = nullptr;
    int navDataSize = 0;
    
    if (!dtCreateNavMeshData(&params, &navData, &navDataSize)) {
        return false;
    }
    
    dtStatus status = m_navMesh->init(navData, navDataSize, DT_TILE_FREE_DATA);
    dtFree(navData);
    
    if (dtStatusFailed(status)) {
        return false;
    }
    
    // NavMesh 쿼리 생성
    m_navMeshQuery = std::make_unique<dtNavMeshQuery>();
    status = m_navMeshQuery->init(m_navMesh.get(), 2048);
    
    return !dtStatusFailed(status);
}

void UnityNavMeshBuilder::Cleanup() {
    m_triareas.clear();
    m_solid.reset();
    m_chf.reset();
    m_cset.reset();
    m_pmesh.reset();
    m_dmesh.reset();
    m_navMesh.reset();
    m_navMeshQuery.reset();
}

void UnityNavMeshBuilder::LogBuildSettings(const UnityNavMeshBuildSettings* settings) {
    // 디버그용 로그 출력 (필요시 구현)
} 