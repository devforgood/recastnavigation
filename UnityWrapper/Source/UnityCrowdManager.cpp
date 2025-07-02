#include "UnityCrowdManager.h"
#include "RecastNavigationUnity.h"
#include "DetourNavMesh.h"
#include "DetourCrowd.h"
#include "DetourNavMeshQuery.h"
#include <memory>

UnityCrowdManager::UnityCrowdManager() {
}

UnityCrowdManager::~UnityCrowdManager() {
}

std::shared_ptr<dtCrowd> UnityCrowdManager::CreateCrowd(NavMeshHandle navMesh, int maxAgents, float maxAgentRadius) {
    if (!navMesh) {
        return nullptr;
    }

    dtCrowd* crowd = dtAllocCrowd();
    if (!crowd) {
        return nullptr;
    }

    if (dtStatusFailed(crowd->init(maxAgents, maxAgentRadius, static_cast<dtNavMesh*>(navMesh)))) {
        dtFreeCrowd(crowd);
        return nullptr;
    }

    return std::shared_ptr<dtCrowd>(crowd, [](dtCrowd* c) {
        if (c) dtFreeCrowd(c);
    });
}

AgentHandle UnityCrowdManager::AddAgent(
    CrowdHandle crowd,
    UnityVector3 position,
    AgentParams* params) {
    
    if (!crowd || !params) {
        return -1;
    }

    dtCrowd* crowd2 = static_cast<dtCrowd*>(crowd);
    
    // Convert Unity coordinates to Detour coordinates
    float pos[3] = { position.x, position.y, position.z };
    
    // Create agent parameters
    dtCrowdAgentParams agentParams;
    memset(&agentParams, 0, sizeof(agentParams));
    agentParams.radius = params->radius;
    agentParams.height = params->height;
    agentParams.maxAcceleration = params->maxAcceleration;
    agentParams.maxSpeed = params->maxSpeed;
    agentParams.collisionQueryRange = params->collisionQueryRange;
    agentParams.pathOptimizationRange = params->pathOptimizationRange;
    agentParams.separationWeight = params->separationWeight;
    agentParams.updateFlags = params->updateFlags;
    agentParams.obstacleAvoidanceType = params->obstacleAvoidanceType;
    
    // Set query filter type
    agentParams.queryFilterType = params->queryFilterType;
    
    // Add agent
    int agentIndex = crowd2->addAgent(pos, &agentParams);
    
    return agentIndex;
}

void UnityCrowdManager::RemoveAgent(CrowdHandle crowd, AgentHandle agent) {
    if (!crowd) {
        return;
    }

    dtCrowd* crowd2 = static_cast<dtCrowd*>(crowd);
	crowd2->removeAgent(agent);
}

int UnityCrowdManager::SetAgentTarget(
    CrowdHandle crowd,
    AgentHandle agent,
    UnityVector3 target) {
    
    if (!crowd) {
        return 0;
    }

    dtCrowd* crowd2 = static_cast<dtCrowd*>(crowd);
    
    // Convert Unity coordinates to Detour coordinates
    float targetPos[3] = { target.x, target.y, target.z };
    
    // Set agent target
    dtStatus status = crowd2->requestMoveTarget(agent, 0, targetPos);
    
    return dtStatusSucceed(status) ? 1 : 0;
}

UnityVector3 UnityCrowdManager::GetAgentPosition(CrowdHandle crowd, AgentHandle agent) {
    UnityVector3 result = { 0, 0, 0 };
    
    if (!crowd) {
        return result;
    }

    dtCrowd* crowd2 = static_cast<dtCrowd*>(crowd);
    
    const dtCrowdAgent* agentData = crowd2->getAgent(agent);
    if (agentData) {
        result.x = agentData->npos[0];
        result.y = agentData->npos[1];
        result.z = agentData->npos[2];
    }
    
    return result;
}

UnityVector3 UnityCrowdManager::GetAgentVelocity(CrowdHandle crowd, AgentHandle agent) {
    UnityVector3 result = { 0, 0, 0 };
    
    if (!crowd) {
        return result;
    }

    dtCrowd* crowd2 = static_cast<dtCrowd*>(crowd);
    
    const dtCrowdAgent* agentData = crowd2->getAgent(agent);
    if (agentData) {
        result.x = agentData->nvel[0];
        result.y = agentData->nvel[1];
        result.z = agentData->nvel[2];
    }
    
    return result;
}

void UnityCrowdManager::UpdateCrowd(CrowdHandle crowd, float deltaTime) {
    if (!crowd) {
        return;
    }

    dtCrowd* crowd2 = static_cast<dtCrowd*>(crowd);
	crowd2->update(deltaTime, nullptr);
} 