using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RecastNavigationUnity
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UnityVector3
    {
        public float x, y, z;

        public UnityVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UnityVector2
    {
        public float x, y;

        public UnityVector2(Vector2 vector)
        {
            x = vector.x;
            y = vector.y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BuildSettings
    {
        public float cellSize;
        public float cellHeight;
        public float walkableSlopeAngle;
        public int walkableHeight;
        public int walkableRadius;
        public int walkableClimb;
        public int minRegionArea;
        public int mergeRegionArea;
        public int maxVertsPerPoly;
        public float detailSampleDist;
        public float detailSampleMaxError;
        public int tileSize;
        public int width;
        public int height;
        public float maxSimplificationError;
        public float maxEdgeLen;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] bmin;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] bmax;

        public BuildSettings()
        {
            cellSize = 0.3f;
            cellHeight = 0.2f;
            walkableSlopeAngle = 45.0f;
            walkableHeight = 2;
            walkableRadius = 6;
            walkableClimb = 4;
            minRegionArea = 8;
            mergeRegionArea = 20;
            maxVertsPerPoly = 6;
            detailSampleDist = 6.0f;
            detailSampleMaxError = 1.0f;
            tileSize = 0;
            width = 0;
            height = 0;
            maxSimplificationError = 1.3f;
            maxEdgeLen = 12.0f;
            bmin = new float[3];
            bmax = new float[3];
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QueryFilter
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public float[] walkableAreaCost;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public float[] walkableAreaFlags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public float[] walkableAreaWeight;
        public int includeFlags;
        public int excludeFlags;

        public QueryFilter()
        {
            walkableAreaCost = new float[64];
            walkableAreaFlags = new float[64];
            walkableAreaWeight = new float[64];
            includeFlags = 0xFFFF;
            excludeFlags = 0;

            // Set default costs
            for (int i = 0; i < 64; i++)
            {
                walkableAreaCost[i] = 1.0f;
                walkableAreaFlags[i] = 1.0f;
                walkableAreaWeight[i] = 1.0f;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PathResult
    {
        public IntPtr path;
        public int pathLength;
        public int status;

        public Vector3[] GetPath()
        {
            if (path == IntPtr.Zero || pathLength <= 0)
                return new Vector3[0];

            Vector3[] result = new Vector3[pathLength];
            IntPtr current = path;
            int structSize = Marshal.SizeOf<UnityVector3>();

            for (int i = 0; i < pathLength; i++)
            {
                UnityVector3 unityVector = Marshal.PtrToStructure<UnityVector3>(current);
                result[i] = unityVector.ToVector3();
                current = IntPtr.Add(current, structSize);
            }

            return result;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AgentParams
    {
        public float radius;
        public float height;
        public float maxAcceleration;
        public float maxSpeed;
        public float collisionQueryRange;
        public float pathOptimizationRange;
        public float separationWeight;
        public int updateFlags;
        public int obstacleAvoidanceType;
        public QueryFilter queryFilter;

        public AgentParams()
        {
            radius = 0.6f;
            height = 2.0f;
            maxAcceleration = 8.0f;
            maxSpeed = 3.5f;
            collisionQueryRange = 12.0f;
            pathOptimizationRange = 30.0f;
            separationWeight = 2.0f;
            updateFlags = 0x3F; // All flags
            obstacleAvoidanceType = 3;
            queryFilter = new QueryFilter();
        }
    }

    public class RecastNavigationUnity
    {
        private const string DLL_NAME = "RecastNavigationUnity";

        // Initialization
        [DllImport(DLL_NAME)]
        public static extern int InitializeRecastNavigation();

        [DllImport(DLL_NAME)]
        public static extern void CleanupRecastNavigation();

        // NavMesh building
        [DllImport(DLL_NAME)]
        public static extern IntPtr BuildNavMesh(
            [In] UnityVector3[] vertices, int vertexCount,
            [In] int[] indices, int indexCount,
            [In] ref BuildSettings settings);

        [DllImport(DLL_NAME)]
        public static extern IntPtr BuildNavMeshFromHeightfield(
            [In] float[] heightfield, int width, int height,
            float originX, float originY, float originZ,
            float cellSize, float cellHeight,
            [In] ref BuildSettings settings);

        [DllImport(DLL_NAME)]
        public static extern void DestroyNavMesh(IntPtr navMesh);

        // NavMeshQuery
        [DllImport(DLL_NAME)]
        public static extern IntPtr CreateNavMeshQuery(IntPtr navMesh, int maxNodes);

        [DllImport(DLL_NAME)]
        public static extern void DestroyNavMeshQuery(IntPtr query);

        [DllImport(DLL_NAME)]
        public static extern PathResult FindPath(
            IntPtr query,
            UnityVector3 startPos,
            UnityVector3 endPos,
            [In] ref QueryFilter filter);

        [DllImport(DLL_NAME)]
        public static extern UnityVector3 GetClosestPoint(
            IntPtr query,
            UnityVector3 position,
            [In] ref QueryFilter filter);

        [DllImport(DLL_NAME)]
        public static extern int Raycast(
            IntPtr query,
            UnityVector3 startPos,
            UnityVector3 endPos,
            [In] ref QueryFilter filter,
            out UnityVector3 hitPos,
            out UnityVector3 hitNormal);

        // Crowd
        [DllImport(DLL_NAME)]
        public static extern IntPtr CreateCrowd(IntPtr navMesh, int maxAgents, float maxAgentRadius);

        [DllImport(DLL_NAME)]
        public static extern void DestroyCrowd(IntPtr crowd);

        [DllImport(DLL_NAME)]
        public static extern int AddAgent(
            IntPtr crowd,
            UnityVector3 position,
            [In] ref AgentParams params_);

        [DllImport(DLL_NAME)]
        public static extern void RemoveAgent(IntPtr crowd, int agent);

        [DllImport(DLL_NAME)]
        public static extern int SetAgentTarget(
            IntPtr crowd,
            int agent,
            UnityVector3 target);

        [DllImport(DLL_NAME)]
        public static extern UnityVector3 GetAgentPosition(IntPtr crowd, int agent);

        [DllImport(DLL_NAME)]
        public static extern UnityVector3 GetAgentVelocity(IntPtr crowd, int agent);

        [DllImport(DLL_NAME)]
        public static extern void UpdateCrowd(IntPtr crowd, float deltaTime);

        // Utility
        [DllImport(DLL_NAME)]
        public static extern void FreePathResult([In] ref PathResult result);
    }

    // Unity MonoBehaviour wrapper for easy integration
    public class RecastNavigationManager : MonoBehaviour
    {
        private IntPtr navMesh = IntPtr.Zero;
        private IntPtr navMeshQuery = IntPtr.Zero;
        private IntPtr crowd = IntPtr.Zero;
        private bool isInitialized = false;

        [Header("Build Settings")]
        public BuildSettings buildSettings = new BuildSettings();

        [Header("Crowd Settings")]
        public int maxAgents = 100;
        public float maxAgentRadius = 2.0f;

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Update()
        {
            if (crowd != IntPtr.Zero)
            {
                RecastNavigationUnity.UpdateCrowd(crowd, Time.deltaTime);
            }
        }

        public bool Initialize()
        {
            if (isInitialized)
                return true;

            int result = RecastNavigationUnity.InitializeRecastNavigation();
            isInitialized = result != 0;
            return isInitialized;
        }

        public void Cleanup()
        {
            if (crowd != IntPtr.Zero)
            {
                RecastNavigationUnity.DestroyCrowd(crowd);
                crowd = IntPtr.Zero;
            }

            if (navMeshQuery != IntPtr.Zero)
            {
                RecastNavigationUnity.DestroyNavMeshQuery(navMeshQuery);
                navMeshQuery = IntPtr.Zero;
            }

            if (navMesh != IntPtr.Zero)
            {
                RecastNavigationUnity.DestroyNavMesh(navMesh);
                navMesh = IntPtr.Zero;
            }

            if (isInitialized)
            {
                RecastNavigationUnity.CleanupRecastNavigation();
                isInitialized = false;
            }
        }

        public bool BuildNavMesh(Vector3[] vertices, int[] indices)
        {
            if (!isInitialized)
                return false;

            // Calculate bounds
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            foreach (Vector3 vertex in vertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }

            buildSettings.bmin[0] = min.x;
            buildSettings.bmin[1] = min.y;
            buildSettings.bmin[2] = min.z;
            buildSettings.bmax[0] = max.x;
            buildSettings.bmax[1] = max.y;
            buildSettings.bmax[2] = max.z;

            // Convert vertices to UnityVector3 array
            UnityVector3[] unityVertices = new UnityVector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                unityVertices[i] = new UnityVector3(vertices[i]);
            }

            navMesh = RecastNavigationUnity.BuildNavMesh(unityVertices, vertices.Length, indices, indices.Length, ref buildSettings);
            
            if (navMesh != IntPtr.Zero)
            {
                navMeshQuery = RecastNavigationUnity.CreateNavMeshQuery(navMesh, 2048);
                crowd = RecastNavigationUnity.CreateCrowd(navMesh, maxAgents, maxAgentRadius);
                return true;
            }

            return false;
        }

        public Vector3[] FindPath(Vector3 start, Vector3 end, QueryFilter filter = null)
        {
            if (navMeshQuery == IntPtr.Zero)
                return new Vector3[0];

            if (filter == null)
                filter = new QueryFilter();

            UnityVector3 startPos = new UnityVector3(start);
            UnityVector3 endPos = new UnityVector3(end);

            PathResult result = RecastNavigationUnity.FindPath(navMeshQuery, startPos, endPos, ref filter);
            
            Vector3[] path = result.GetPath();
            RecastNavigationUnity.FreePathResult(ref result);
            
            return path;
        }

        public Vector3 GetClosestPoint(Vector3 position, QueryFilter filter = null)
        {
            if (navMeshQuery == IntPtr.Zero)
                return position;

            if (filter == null)
                filter = new QueryFilter();

            UnityVector3 pos = new UnityVector3(position);
            UnityVector3 result = RecastNavigationUnity.GetClosestPoint(navMeshQuery, pos, ref filter);
            
            return result.ToVector3();
        }

        public bool Raycast(Vector3 start, Vector3 end, out Vector3 hitPoint, out Vector3 hitNormal, QueryFilter filter = null)
        {
            hitPoint = Vector3.zero;
            hitNormal = Vector3.zero;

            if (navMeshQuery == IntPtr.Zero)
                return false;

            if (filter == null)
                filter = new QueryFilter();

            UnityVector3 startPos = new UnityVector3(start);
            UnityVector3 endPos = new UnityVector3(end);
            UnityVector3 hitPos, hitNorm;

            int result = RecastNavigationUnity.Raycast(navMeshQuery, startPos, endPos, ref filter, out hitPos, out hitNorm);
            
            if (result != 0)
            {
                hitPoint = hitPos.ToVector3();
                hitNormal = hitNorm.ToVector3();
                return true;
            }

            return false;
        }

        public int AddAgent(Vector3 position, AgentParams agentParams = null)
        {
            if (crowd == IntPtr.Zero)
                return -1;

            if (agentParams == null)
                agentParams = new AgentParams();

            UnityVector3 pos = new UnityVector3(position);
            return RecastNavigationUnity.AddAgent(crowd, pos, ref agentParams);
        }

        public void RemoveAgent(int agentId)
        {
            if (crowd != IntPtr.Zero)
            {
                RecastNavigationUnity.RemoveAgent(crowd, agentId);
            }
        }

        public bool SetAgentTarget(int agentId, Vector3 target)
        {
            if (crowd == IntPtr.Zero)
                return false;

            UnityVector3 targetPos = new UnityVector3(target);
            int result = RecastNavigationUnity.SetAgentTarget(crowd, agentId, targetPos);
            return result != 0;
        }

        public Vector3 GetAgentPosition(int agentId)
        {
            if (crowd == IntPtr.Zero)
                return Vector3.zero;

            UnityVector3 result = RecastNavigationUnity.GetAgentPosition(crowd, agentId);
            return result.ToVector3();
        }

        public Vector3 GetAgentVelocity(int agentId)
        {
            if (crowd == IntPtr.Zero)
                return Vector3.zero;

            UnityVector3 result = RecastNavigationUnity.GetAgentVelocity(crowd, agentId);
            return result.ToVector3();
        }
    }
} 