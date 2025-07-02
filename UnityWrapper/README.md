# RecastNavigation Unity Wrapper

This project provides a Unity-compatible DLL wrapper for the RecastNavigation library, allowing you to use RecastNavigation's navigation mesh generation and pathfinding capabilities directly in Unity.

## Features

- **NavMesh Building**: Build navigation meshes from Unity mesh data
- **Pathfinding**: Find optimal paths between points on the navigation mesh
- **Crowd Simulation**: Manage multiple agents with collision avoidance
- **Raycasting**: Perform raycasts on the navigation mesh
- **Heightfield Support**: Build navigation meshes from heightfield data

## Building the DLL

### Prerequisites

- Visual Studio 2019 or later
- CMake 3.1 or later
- Windows 10/11

### Build Steps

1. Open Command Prompt in the UnityWrapper directory
2. Run the build script:
   ```
   build.bat
   ```

Alternatively, you can build manually:

1. Create a build directory:
   ```
   mkdir build
   cd build
   ```

2. Configure with CMake:
   ```
   cmake .. -G "Visual Studio 16 2019" -A x64 -DRECASTNAVIGATION_DEMO=OFF -DRECASTNAVIGATION_TESTS=OFF
   ```

3. Build the project:
   ```
   cmake --build . --config Release
   ```

The DLL will be generated in `build/Unity/Release/RecastNavigationUnity.dll`.

## Using in Unity

### Setup

1. Copy `RecastNavigationUnity.dll` to your Unity project's `Assets/Plugins/` folder
2. Copy the C# script `RecastNavigationUnity.cs` to your Unity project's `Assets/Scripts/` folder

### Basic Usage

```csharp
using RecastNavigationUnity;
using UnityEngine;

public class NavigationExample : MonoBehaviour
{
    private RecastNavigationManager navManager;

    void Start()
    {
        // Add the RecastNavigationManager component
        navManager = gameObject.AddComponent<RecastNavigationManager>();
        
        // Initialize the navigation system
        if (navManager.Initialize())
        {
            Debug.Log("Navigation system initialized successfully");
        }
    }

    void BuildNavMesh()
    {
        // Get mesh data from a GameObject
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;
        
        // Build navigation mesh
        bool success = navManager.BuildNavMesh(mesh.vertices, mesh.triangles);
        if (success)
        {
            Debug.Log("Navigation mesh built successfully");
        }
    }

    void FindPath()
    {
        Vector3 start = new Vector3(0, 0, 0);
        Vector3 end = new Vector3(10, 0, 10);
        
        // Find path between two points
        Vector3[] path = navManager.FindPath(start, end);
        
        if (path.Length > 0)
        {
            Debug.Log($"Path found with {path.Length} waypoints");
        }
    }

    void AddAgent()
    {
        Vector3 position = new Vector3(0, 0, 0);
        
        // Add an agent to the crowd
        int agentId = navManager.AddAgent(position);
        
        if (agentId >= 0)
        {
            // Set agent target
            Vector3 target = new Vector3(10, 0, 10);
            navManager.SetAgentTarget(agentId, target);
        }
    }
}
```

### Advanced Usage

#### Custom Build Settings

```csharp
// Configure build settings
navManager.buildSettings.cellSize = 0.3f;
navManager.buildSettings.cellHeight = 0.2f;
navManager.buildSettings.walkableSlopeAngle = 45.0f;
navManager.buildSettings.walkableHeight = 2;
navManager.buildSettings.walkableRadius = 6;
navManager.buildSettings.walkableClimb = 4;
```

#### Custom Query Filters

```csharp
QueryFilter filter = new QueryFilter();
filter.includeFlags = 0xFFFF;
filter.excludeFlags = 0;

// Set custom area costs
filter.walkableAreaCost[0] = 1.0f;  // Default area
filter.walkableAreaCost[1] = 2.0f;  // Water area (more expensive)
filter.walkableAreaCost[2] = 0.5f;  // Road area (less expensive)

// Use filter in pathfinding
Vector3[] path = navManager.FindPath(start, end, filter);
```

#### Agent Parameters

```csharp
AgentParams agentParams = new AgentParams();
agentParams.radius = 0.6f;
agentParams.height = 2.0f;
agentParams.maxSpeed = 3.5f;
agentParams.maxAcceleration = 8.0f;
agentParams.collisionQueryRange = 12.0f;
agentParams.pathOptimizationRange = 30.0f;
agentParams.separationWeight = 2.0f;

int agentId = navManager.AddAgent(position, agentParams);
```

## API Reference

### RecastNavigationManager

#### Core Methods

- `Initialize()`: Initialize the navigation system
- `Cleanup()`: Clean up resources
- `BuildNavMesh(Vector3[] vertices, int[] indices)`: Build navigation mesh from mesh data

#### Pathfinding

- `FindPath(Vector3 start, Vector3 end, QueryFilter filter = null)`: Find path between two points
- `GetClosestPoint(Vector3 position, QueryFilter filter = null)`: Get closest point on navigation mesh
- `Raycast(Vector3 start, Vector3 end, out Vector3 hitPoint, out Vector3 hitNormal, QueryFilter filter = null)`: Perform raycast on navigation mesh

#### Crowd Management

- `AddAgent(Vector3 position, AgentParams agentParams = null)`: Add agent to crowd
- `RemoveAgent(int agentId)`: Remove agent from crowd
- `SetAgentTarget(int agentId, Vector3 target)`: Set agent's target position
- `GetAgentPosition(int agentId)`: Get agent's current position
- `GetAgentVelocity(int agentId)`: Get agent's current velocity

### Data Structures

#### BuildSettings

Configuration for navigation mesh building:

- `cellSize`: Size of each cell in the navigation mesh
- `cellHeight`: Height of each cell
- `walkableSlopeAngle`: Maximum walkable slope angle
- `walkableHeight`: Minimum walkable height
- `walkableRadius`: Agent radius for walkable area calculation
- `walkableClimb`: Maximum climb height

#### QueryFilter

Filter for pathfinding queries:

- `walkableAreaCost[]`: Cost multipliers for different areas
- `includeFlags`: Areas to include in pathfinding
- `excludeFlags`: Areas to exclude from pathfinding

#### AgentParams

Parameters for crowd agents:

- `radius`: Agent radius
- `height`: Agent height
- `maxSpeed`: Maximum movement speed
- `maxAcceleration`: Maximum acceleration
- `collisionQueryRange`: Range for collision detection
- `pathOptimizationRange`: Range for path optimization
- `separationWeight`: Weight for agent separation

## Performance Considerations

- The DLL is optimized for performance and uses native C++ code
- Large navigation meshes may take time to build
- Crowd simulation performance depends on the number of agents
- Consider using appropriate build settings for your use case

## Troubleshooting

### Common Issues

1. **DLL not found**: Ensure the DLL is in the correct location (`Assets/Plugins/`)
2. **Build failures**: Check that all dependencies are properly linked
3. **Memory issues**: Always call `Cleanup()` when destroying the navigation manager

### Debug Information

Enable debug logging in Unity to see detailed information about navigation operations.

## License

This wrapper is based on the RecastNavigation library and follows the same license terms. 