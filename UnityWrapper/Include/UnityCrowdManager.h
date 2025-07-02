#pragma once

#include "RecastNavigationUnity.h"
#include <memory>

// Forward declarations
struct dtNavMesh;
struct dtCrowd;

class UnityCrowdManager {
public:
    UnityCrowdManager();
    ~UnityCrowdManager();

    std::shared_ptr<dtCrowd> CreateCrowd(NavMeshHandle navMesh, int maxAgents, float maxAgentRadius);

    AgentHandle AddAgent(
        CrowdHandle crowd,
        UnityVector3 position,
        AgentParams* params);

    void RemoveAgent(CrowdHandle crowd, AgentHandle agent);

    int SetAgentTarget(
        CrowdHandle crowd,
        AgentHandle agent,
        UnityVector3 target);

    UnityVector3 GetAgentPosition(CrowdHandle crowd, AgentHandle agent);

    UnityVector3 GetAgentVelocity(CrowdHandle crowd, AgentHandle agent);

    void UpdateCrowd(CrowdHandle crowd, float deltaTime);
}; 