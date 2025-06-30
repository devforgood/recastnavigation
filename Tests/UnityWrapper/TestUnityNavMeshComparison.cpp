#include "catch_all.hpp"
#include "UnityRecastWrapper.h"
#include "UnityNavMeshBuilder.h"
#include "UnityPathfinding.h"
#include "UnityLog.h"

// RecastDemo headers
#include "Recast.h"
#include "RecastDebugDraw.h"
#include "DetourNavMesh.h"
#include "DetourNavMeshBuilder.h"
#include "DetourNavMeshQuery.h"

#include <memory>
#include <vector>
#include <string>
#include <fstream>
#include <sstream>
#include <algorithm>
#include <iostream>

using namespace Catch;

// RecastDemo settings
struct RecastDemoSettings {
    float cellSize = 0.3f;
    float cellHeight = 0.2f;
    float agentHeight = 2.0f;
    float agentRadius = 0.6f;
    float agentMaxClimb = 0.9f;
    float agentMaxSlope = 45.0f;
    float regionMinSize = 8.0f;
    float regionMergeSize = 20.0f;
    float edgeMaxLen = 12.0f;
    float edgeMaxError = 1.3f;
    float vertsPerPoly = 6.0f;
    float detailSampleDist = 6.0f;
    float detailSampleMaxError = 1.0f;
    int partitionType = 0; // SAMPLE_PARTITION_WATERSHED
    bool autoTransformCoordinates = false;
};

// RecastDemo style NavMesh builder class
class RecastDemoNavMeshBuilder {
private:
    rcContext* m_ctx;
    rcConfig m_cfg;
    unsigned char* m_triareas;
    rcHeightfield* m_solid;
    rcCompactHeightfield* m_chf;
    rcContourSet* m_cset;
    rcPolyMesh* m_pmesh;
    rcPolyMeshDetail* m_dmesh;
    dtNavMesh* m_navMesh;
    dtNavMeshQuery* m_navQuery;
    
    RecastDemoSettings m_settings;
    
public:
    RecastDemoNavMeshBuilder() 
        : m_ctx(nullptr)
        , m_triareas(nullptr)
        , m_solid(nullptr)
        , m_chf(nullptr)
        , m_cset(nullptr)
        , m_pmesh(nullptr)
        , m_dmesh(nullptr)
        , m_navMesh(nullptr)
        , m_navQuery(nullptr) {
        
        // Initialize Recast context
        m_ctx = new rcContext();
        if (!m_ctx) {
            // Handle allocation failure
            return;
        }
    }
    
    ~RecastDemoNavMeshBuilder() {
        cleanup();
        delete m_ctx;
        m_ctx = nullptr;
    }
    
    void SetSettings(const RecastDemoSettings& settings) {
        m_settings = settings;
    }
    
    bool BuildNavMesh(const float* verts, int nverts, const int* tris, int ntris) {
        // 기존 데이터만 정리 (m_ctx는 유지)
        delete[] m_triareas;
        m_triareas = nullptr;
        rcFreeHeightField(m_solid);
        m_solid = nullptr;
        rcFreeCompactHeightfield(m_chf);
        m_chf = nullptr;
        rcFreeContourSet(m_cset);
        m_cset = nullptr;
        rcFreePolyMesh(m_pmesh);
        m_pmesh = nullptr;
        rcFreePolyMeshDetail(m_dmesh);
        m_dmesh = nullptr;
        dtFreeNavMesh(m_navMesh);
        m_navMesh = nullptr;
        dtFreeNavMeshQuery(m_navQuery);
        m_navQuery = nullptr;
        
        // Input data validation
        if (!verts || !tris || nverts <= 0 || ntris <= 0) {
            std::cout << "RecastDemo: Invalid input data - verts=" << (verts ? "valid" : "null") 
                     << ", tris=" << (tris ? "valid" : "null") 
                     << ", nverts=" << nverts << ", ntris=" << ntris << std::endl;
            return false;
        }
        
        // rcContext validation and initialization
        if (!m_ctx) {
            m_ctx = new rcContext();
            if (!m_ctx) {
                std::cout << "RecastDemo: Failed to create rcContext" << std::endl;
                return false;
            }
        }
        
        std::cout << "RecastDemo: Starting NavMesh build with " << nverts << " vertices, " << ntris << " triangles" << std::endl;
        
        // Step 1. Initialize build config
        memset(&m_cfg, 0, sizeof(m_cfg));
        m_cfg.cs = m_settings.cellSize;
        m_cfg.ch = m_settings.cellHeight;
        m_cfg.walkableSlopeAngle = m_settings.agentMaxSlope;
        m_cfg.walkableHeight = (int)ceilf(m_settings.agentHeight / m_cfg.ch);
        m_cfg.walkableClimb = (int)floorf(m_settings.agentMaxClimb / m_cfg.ch);
        m_cfg.walkableRadius = (int)ceilf(m_settings.agentRadius / m_cfg.cs);
        m_cfg.maxEdgeLen = (int)(m_settings.edgeMaxLen / m_settings.cellSize);
        m_cfg.maxSimplificationError = m_settings.edgeMaxError;
        m_cfg.minRegionArea = (int)rcSqr(m_settings.regionMinSize);
        m_cfg.mergeRegionArea = (int)rcSqr(m_settings.regionMergeSize);
        m_cfg.maxVertsPerPoly = (int)m_settings.vertsPerPoly;
        m_cfg.detailSampleDist = m_settings.detailSampleDist < 0.9f ? 0 : m_settings.cellSize * m_settings.detailSampleDist;
        m_cfg.detailSampleMaxError = m_settings.cellHeight * m_settings.detailSampleMaxError;
        
        // Calculate bounds
        float bmin[3], bmax[3];
        rcCalcBounds(verts, nverts, bmin, bmax);
        rcVcopy(m_cfg.bmin, bmin);
        rcVcopy(m_cfg.bmax, bmax);
        rcCalcGridSize(m_cfg.bmin, m_cfg.bmax, m_cfg.cs, &m_cfg.width, &m_cfg.height);
        
        std::cout << "RecastDemo: Grid size = " << m_cfg.width << "x" << m_cfg.height << std::endl;
        std::cout << "RecastDemo: Bounds: min(" << bmin[0] << "," << bmin[1] << "," << bmin[2] 
                 << ") max(" << bmax[0] << "," << bmax[1] << "," << bmax[2] << ")" << std::endl;
        
        // Step 2. Rasterize input polygon soup
        m_solid = rcAllocHeightfield();
        if (!m_solid) {
            std::cout << "RecastDemo: Failed to allocate heightfield" << std::endl;
            return false;
        }
        
        if (!rcCreateHeightfield(m_ctx, *m_solid, m_cfg.width, m_cfg.height, m_cfg.bmin, m_cfg.bmax, m_cfg.cs, m_cfg.ch)) {
            std::cout << "RecastDemo: Failed to create heightfield" << std::endl;
            return false;
        }
        
        m_triareas = new unsigned char[ntris];
        if (!m_triareas) {
            std::cout << "RecastDemo: Failed to allocate triangle areas" << std::endl;
            return false;
        }
        
        memset(m_triareas, 0, ntris * sizeof(unsigned char));
        rcMarkWalkableTriangles(m_ctx, m_cfg.walkableSlopeAngle, verts, nverts, tris, ntris, m_triareas);
        
        if (!rcRasterizeTriangles(m_ctx, verts, nverts, tris, m_triareas, ntris, *m_solid, m_cfg.walkableClimb)) {
            std::cout << "RecastDemo: Failed to rasterize triangles" << std::endl;
            return false;
        }
        
        std::cout << "RecastDemo: Rasterization completed successfully" << std::endl;
        
        // Step 3. Filter walkable surfaces
        rcFilterLowHangingWalkableObstacles(m_ctx, m_cfg.walkableClimb, *m_solid);
        rcFilterLedgeSpans(m_ctx, m_cfg.walkableHeight, m_cfg.walkableClimb, *m_solid);
        rcFilterWalkableLowHeightSpans(m_ctx, m_cfg.walkableHeight, *m_solid);
        
        std::cout << "RecastDemo: Filtering completed successfully" << std::endl;
        
        // Step 4. Partition walkable surface to simple regions
        m_chf = rcAllocCompactHeightfield();
        if (!m_chf) {
            std::cout << "RecastDemo: Failed to allocate compact heightfield" << std::endl;
            return false;
        }
        
        if (!rcBuildCompactHeightfield(m_ctx, m_cfg.walkableHeight, m_cfg.walkableClimb, *m_solid, *m_chf)) {
            std::cout << "RecastDemo: Failed to build compact heightfield" << std::endl;
            return false;
        }
        
        if (!rcErodeWalkableArea(m_ctx, m_cfg.walkableRadius, *m_chf)) {
            std::cout << "RecastDemo: Failed to erode walkable area" << std::endl;
            return false;
        }
        
        std::cout << "RecastDemo: Compact heightfield built successfully" << std::endl;
        
        // Step 5. Partition the heightfield
        if (!rcBuildDistanceField(m_ctx, *m_chf)) {
            std::cout << "RecastDemo: Failed to build distance field" << std::endl;
            return false;
        }
        
        if (!rcBuildRegions(m_ctx, *m_chf, 0, m_cfg.minRegionArea, m_cfg.mergeRegionArea)) {
            std::cout << "RecastDemo: Failed to build regions" << std::endl;
            return false;
        }
        
        std::cout << "RecastDemo: Regions built successfully" << std::endl;
        
        // Step 6. Build contours
        m_cset = rcAllocContourSet();
        if (!m_cset) {
            std::cout << "RecastDemo: Failed to allocate contour set" << std::endl;
            return false;
        }
        
        if (!rcBuildContours(m_ctx, *m_chf, m_cfg.maxSimplificationError, m_cfg.maxEdgeLen, *m_cset)) {
            std::cout << "RecastDemo: Failed to build contours" << std::endl;
            return false;
        }
        
        std::cout << "RecastDemo: Contours built successfully" << std::endl;
        
        // Step 7. Build polygons mesh from contours
        m_pmesh = rcAllocPolyMesh();
        if (!m_pmesh) {
            std::cout << "RecastDemo: Failed to allocate poly mesh" << std::endl;
            return false;
        }
        
        if (!rcBuildPolyMesh(m_ctx, *m_cset, m_cfg.maxVertsPerPoly, *m_pmesh)) {
            std::cout << "RecastDemo: Failed to build poly mesh" << std::endl;
            return false;
        }
        
        std::cout << "RecastDemo: Built poly mesh with " << m_pmesh->npolys << " polygons, " << m_pmesh->nverts << " vertices" << std::endl;
        
        // Step 8. Create detail mesh which allows to access approximate height on each polygon
        m_dmesh = rcAllocPolyMeshDetail();
        if (!m_dmesh) {
            std::cout << "RecastDemo: Failed to allocate detail mesh" << std::endl;
            return false;
        }
        
        if (!rcBuildPolyMeshDetail(m_ctx, *m_pmesh, *m_chf, m_cfg.detailSampleDist, m_cfg.detailSampleMaxError, *m_dmesh)) {
            std::cout << "RecastDemo: Failed to build detail mesh" << std::endl;
            return false;
        }
        
        std::cout << "RecastDemo: Detail mesh built successfully" << std::endl;
        
        // Step 9. Create Detour data from Recast poly mesh
        unsigned char* navData = nullptr;
        int navDataSize = 0;
        
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
        params.walkableHeight = m_settings.agentHeight;
        params.walkableRadius = m_settings.agentRadius;
        params.walkableClimb = m_settings.agentMaxClimb;
        rcVcopy(params.bmin, m_pmesh->bmin);
        rcVcopy(params.bmax, m_pmesh->bmax);
        params.cs = m_pmesh->cs;
        params.ch = m_pmesh->ch;
        params.buildBvTree = true;
        
        if (!dtCreateNavMeshData(&params, &navData, &navDataSize)) {
            std::cout << "RecastDemo: Failed to create NavMesh data" << std::endl;
            return false;
        }
        
        std::cout << "RecastDemo: NavMesh data created successfully, size: " << navDataSize << std::endl;
        
        m_navMesh = dtAllocNavMesh();
        if (!m_navMesh) {
            std::cout << "RecastDemo: Failed to allocate NavMesh" << std::endl;
            dtFree(navData);
            return false;
        }
        
        dtStatus status = m_navMesh->init(navData, navDataSize, DT_TILE_FREE_DATA);
        if (dtStatusFailed(status)) {
            std::cout << "RecastDemo: Failed to initialize NavMesh, status=" << status << std::endl;
            dtFreeNavMesh(m_navMesh);
            m_navMesh = nullptr;
            return false;
        }
        
        m_navQuery = dtAllocNavMeshQuery();
        if (!m_navQuery) {
            std::cout << "RecastDemo: Failed to allocate NavMeshQuery" << std::endl;
            return false;
        }
        
        status = m_navQuery->init(m_navMesh, 2048);
        if (dtStatusFailed(status)) {
            std::cout << "RecastDemo: Failed to initialize NavMeshQuery, status=" << status << std::endl;
            dtFreeNavMeshQuery(m_navQuery);
            m_navQuery = nullptr;
            return false;
        }
        
        std::cout << "RecastDemo: NavMesh build completed successfully" << std::endl;
        return true;
    }
    
    int GetPolyCount() const {
        if (m_pmesh) return m_pmesh->npolys;
        return 0;
    }
    
    int GetVertexCount() const {
        if (m_pmesh) return m_pmesh->nverts;
        return 0;
    }
    
    int GetDetailTriCount() const {
        if (m_dmesh) return m_dmesh->ntris;
        return 0;
    }
    
    int GetDetailVertexCount() const {
        if (m_dmesh) return m_dmesh->nverts;
        return 0;
    }
    
    dtNavMesh* GetNavMesh() const { return m_navMesh; }
    dtNavMeshQuery* GetNavMeshQuery() const { return m_navQuery; }
    
private:
    void cleanup() {
        delete[] m_triareas;
        m_triareas = nullptr;
        rcFreeHeightField(m_solid);
        m_solid = nullptr;
        rcFreeCompactHeightfield(m_chf);
        m_chf = nullptr;
        rcFreeContourSet(m_cset);
        m_cset = nullptr;
        rcFreePolyMesh(m_pmesh);
        m_pmesh = nullptr;
        rcFreePolyMeshDetail(m_dmesh);
        m_dmesh = nullptr;
        dtFreeNavMesh(m_navMesh);
        m_navMesh = nullptr;
        dtFreeNavMeshQuery(m_navQuery);
        m_navQuery = nullptr;
        // m_ctx는 삭제하지 않음 (재사용을 위해)
    }
};

// OBJ 파일 로드 함수
struct ObjMeshData {
    std::vector<float> vertices;
    std::vector<int> indices;
    bool loaded;
    ObjMeshData() : loaded(false) {}
};

ObjMeshData LoadObjFile(const std::string& filename) {
    ObjMeshData mesh;
    std::ifstream file(filename);
    if (!file.is_open()) return mesh;
    std::string line;
    while (std::getline(file, line)) {
        if (line.substr(0, 2) == "v ") {
            std::istringstream iss(line.substr(2));
            float x, y, z;
            iss >> x >> y >> z;
            mesh.vertices.push_back(x);
            mesh.vertices.push_back(y);
            mesh.vertices.push_back(z);
        } else if (line.substr(0, 2) == "f ") {
            std::istringstream iss(line.substr(2));
            int i1, i2, i3;
            char slash;
            iss >> i1 >> i2 >> i3;
            mesh.indices.push_back(i1 - 1);
            mesh.indices.push_back(i2 - 1);
            mesh.indices.push_back(i3 - 1);
        }
    }
    mesh.loaded = !mesh.vertices.empty() && !mesh.indices.empty();
    return mesh;
}

struct NavMeshComparisonResult {
    bool polyCountMatch;
    bool vertexCountMatch;
    bool detailTriCountMatch;
    bool detailVertexCountMatch;
    bool navMeshValid;
    bool navQueryValid;
    std::string differences;
    bool IsIdentical() const {
        return polyCountMatch && vertexCountMatch && detailTriCountMatch && 
               detailVertexCountMatch && navMeshValid && navQueryValid;
    }
};

// Forward declarations for builder classes (assume implemented elsewhere)
// class UnityNavMeshBuilder {
// public:
//     int GetPolyCount() const;
//     int GetVertexCount() const;
//     int GetPolyMeshPolyCount() const;
//     int GetPolyMeshVertexCount() const;
//     int GetDetailMeshTriCount() const;
//     int GetDetailMeshVertexCount() const;
//     const dtNavMesh* GetNavMesh() const;
//     const dtNavMeshQuery* GetNavMeshQuery() const;
//     UnityNavMeshResult BuildNavMesh(const UnityMeshData*, const UnityNavMeshBuildSettings*);
// };
// class RecastDemoNavMeshBuilder {
// public:
//     int GetPolyCount() const;
//     int GetVertexCount() const;
//     int GetDetailTriCount() const;
//     int GetDetailVertexCount() const;
//     const dtNavMesh* GetNavMesh() const;
//     const dtNavMeshQuery* GetNavMeshQuery() const;
//     void SetSettings(const struct RecastDemoSettings&);
//     bool BuildNavMesh(const float*, int, const int*, int);
// };

NavMeshComparisonResult CompareNavMeshResults(
    const UnityNavMeshBuilder& unityBuilder,
    const RecastDemoNavMeshBuilder& recastBuilder) {
    NavMeshComparisonResult result;
    result.polyCountMatch = unityBuilder.GetPolyCount() == recastBuilder.GetPolyCount();
    result.vertexCountMatch = unityBuilder.GetVertexCount() == recastBuilder.GetVertexCount();
    result.detailTriCountMatch = unityBuilder.GetDetailMeshTriCount() == recastBuilder.GetDetailTriCount();
    result.detailVertexCountMatch = unityBuilder.GetDetailMeshVertexCount() == recastBuilder.GetDetailVertexCount();
    result.navMeshValid = unityBuilder.GetNavMesh() && recastBuilder.GetNavMesh();
    result.navQueryValid = unityBuilder.GetNavMeshQuery() && recastBuilder.GetNavMeshQuery();
    if (!result.polyCountMatch)
        result.differences += "PolyCount mismatch\n";
    if (!result.vertexCountMatch)
        result.differences += "VertexCount mismatch\n";
    if (!result.detailTriCountMatch)
        result.differences += "DetailTriCount mismatch\n";
    if (!result.detailVertexCountMatch)
        result.differences += "DetailVertexCount mismatch\n";
    if (!result.navMeshValid)
        result.differences += "NavMesh invalid\n";
    if (!result.navQueryValid)
        result.differences += "NavQuery invalid\n";
    return result;
}

TEST_CASE("Obj NavMesh Comparison Only", "[NavMeshComparison]") {
    std::string objPath = "../../../RecastDemo/Bin/Meshes/nav_test.obj";
    ObjMeshData objMesh = LoadObjFile(objPath);
    REQUIRE(objMesh.loaded);

    UnityNavMeshBuildSettings unitySettings = {};
    unitySettings.cellSize = 0.3f;
    unitySettings.cellHeight = 0.2f;
    unitySettings.walkableSlopeAngle = 45.0f;
    unitySettings.walkableHeight = 2.0f;
    unitySettings.walkableRadius = 0.6f;
    unitySettings.walkableClimb = 0.9f;
    unitySettings.minRegionArea = 8.0f;
    unitySettings.mergeRegionArea = 20.0f;
    unitySettings.maxVertsPerPoly = 6;
    unitySettings.detailSampleDist = 6.0f;
    unitySettings.detailSampleMaxError = 1.0f;
    unitySettings.autoTransformCoordinates = false;
    unitySettings.partitionType = 0;

    RecastDemoSettings recastSettings;
    recastSettings.cellSize = 0.3f;
    recastSettings.cellHeight = 0.2f;
    recastSettings.agentHeight = 2.0f;
    recastSettings.agentRadius = 0.6f;
    recastSettings.agentMaxClimb = 0.9f;
    recastSettings.agentMaxSlope = 45.0f;
    recastSettings.regionMinSize = 8.0f;
    recastSettings.regionMergeSize = 20.0f;
    recastSettings.edgeMaxLen = 12.0f;
    recastSettings.edgeMaxError = 1.3f;
    recastSettings.vertsPerPoly = 6.0f;
    recastSettings.detailSampleDist = 6.0f;
    recastSettings.detailSampleMaxError = 1.0f;
    recastSettings.autoTransformCoordinates = false;

    UnityNavMeshBuilder unityBuilder;
    UnityMeshData unityMeshData;
    unityMeshData.vertices = objMesh.vertices.data();
    unityMeshData.indices = objMesh.indices.data();
    unityMeshData.vertexCount = static_cast<int>(objMesh.vertices.size()) / 3;
    unityMeshData.indexCount = static_cast<int>(objMesh.indices.size());
    UnityNavMeshResult unityResult = unityBuilder.BuildNavMesh(&unityMeshData, &unitySettings);
    REQUIRE(unityResult.success == true);

    RecastDemoNavMeshBuilder recastBuilder;
    recastBuilder.SetSettings(recastSettings);
    bool recastSuccess = recastBuilder.BuildNavMesh(
        objMesh.vertices.data(), static_cast<int>(objMesh.vertices.size()) / 3,
        objMesh.indices.data(), static_cast<int>(objMesh.indices.size()) / 3
    );
    REQUIRE(recastSuccess == true);

    NavMeshComparisonResult comparison = CompareNavMeshResults(unityBuilder, recastBuilder);
    INFO("Unity PolyCount: " << unityBuilder.GetPolyCount());
    INFO("Recast PolyCount: " << recastBuilder.GetPolyCount());
    INFO("Unity VertexCount: " << unityBuilder.GetVertexCount());
    INFO("Recast VertexCount: " << recastBuilder.GetVertexCount());
    INFO("Unity DetailTriCount: " << unityBuilder.GetDetailMeshTriCount());
    INFO("Recast DetailTriCount: " << recastBuilder.GetDetailTriCount());
    INFO("Unity DetailVertexCount: " << unityBuilder.GetDetailMeshVertexCount());
    INFO("Recast DetailVertexCount: " << recastBuilder.GetDetailVertexCount());
    INFO("Comparison: " << (comparison.IsIdentical() ? "IDENTICAL" : "DIFFERENT"));
    if (!comparison.IsIdentical()) {
        INFO("Differences:\n" << comparison.differences);
    }
    CHECK(comparison.IsIdentical());
} 