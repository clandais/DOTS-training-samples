using System.Linq;
using AutoFarmers.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace AutoFarmers.Systems
{

	struct PathNode : IBufferElementData
	{
		public int X;
		public int Y;
		
		public int2 Position => new int2(X, Y);
		
		public int Index;
		
		public int gCost;
		public int hCost;
		
		public int fCost => hCost + gCost;
		
		public bool IsWalkable;
		
		public int CameFromNodeIndex;
	}
	
	[UpdateAfter(typeof(FarmInitializationSystem))]
	public partial struct PathFindingInitialisationSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<Farm>();
			state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			state.Enabled = false;
			
			var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
			
			var farmEntity = SystemAPI.GetSingletonEntity<Farm>();
			var farm = SystemAPI.GetAspect<MapAspect>(farmEntity);
			
			//NativeArray<Entity> nodes = new NativeArray<Entity>(farm.MapSize * farm.MapSize, Allocator.Temp);
			
			var buffer = ecb.AddBuffer<GraphNode>(farmEntity);
			buffer.ResizeUninitialized( farm.MapSize * farm.MapSize);
			
			
			for (int x = 0; x < farm.MapSize * farm.MapSize; x++)
			{

				buffer[x] = new GraphNode()
				{
					Position = new int2(x % farm.MapSize, x / farm.MapSize),
					IsWalkable = farm.IsWalkable(x),
				};
			}
			
			

		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{

		}
	}
	
	
	// [BurstCompile]
	// struct FindPathJob : IJob
	// {
	//
	// 	[ReadOnly] public DynamicBuffer<GraphNode> Nodes;
	// 	[ReadOnly] public int2 Start;
	// 	[ReadOnly] public int2 Target;
	// 	[ReadOnly] public int MapSize;
	//
	// 	public NativeList<int2> Path;
	//
	// 	private NativeList<int2> FindPath(int2 start, int2 end)
	// 	{
	//
	// 		NativeArray<int2> directions = new NativeArray<int2>(4, Unity.Collections.Allocator.Temp);
	//
	// 		directions[0] = new int2(0, 1);
	// 		directions[1] = new int2(0, -1);
	// 		directions[2] = new int2(1, 0);
	// 		directions[3] = new int2(-1, 0);
	// 		
	// 		
	// 			
	// 		NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(MapSize * MapSize, Allocator.Temp);
	// 		
	// 		for (int x = 0; x < MapSize; x++)
	// 		{
	// 			for (int y = 0; y < MapSize; y++)
	// 			{
	// 				PathNode pathNode = new PathNode();
	// 				pathNode.X = x;
	// 				pathNode.Y = y;
	// 				pathNode.Index = y * MapSize + x;
	// 				
	// 				pathNode.gCost = int.MaxValue;
	// 				pathNode.hCost = CalculateHCost(new int2(x, y), end);
	// 				
	// 				pathNode.IsWalkable = Nodes[pathNode.Index].IsWalkable;
	// 				
	// 				pathNode.CameFromNodeIndex = -1;
	// 				
	// 				pathNodeArray[pathNode.Index] = pathNode;
	// 			}
	// 		}
	// 		
	// 		PathNode startNode = pathNodeArray[start.x * MapSize + start.y];
	// 		startNode.gCost = 0;
	// 		pathNodeArray[start.x * MapSize + start.y] = startNode;
	// 		
	// 		NativeList<int> openList = new NativeList<int>(Allocator.Temp);
	// 		NativeList<int> closedList = new NativeList<int>(Allocator.Temp);
	// 		
	//
	// 		openList.Add(startNode.Index);
	//
	// 		while (openList.Length > 0)
	// 		{
	// 			int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
	// 			PathNode currentNode = pathNodeArray[currentNodeIndex];
	// 			
	// 			int endNodeIndex = end.x * MapSize + end.y;
	// 			
	// 			if (currentNodeIndex == endNodeIndex)
	// 			{
	// 				break;
	// 			}
	// 			
	// 			for (int i = 0; i < openList.Length; i++)
	// 			{
	// 				if (openList[i] == currentNodeIndex)
	// 				{
	// 					openList.RemoveAtSwapBack(i);
	// 					break;
	// 				}
	// 			}
	// 			
	// 			closedList.Add(currentNodeIndex);
	//
	// 			for (int i = 0; i < directions.Length; i++)
	// 			{
	// 				int2 direction = directions[i];
	// 				int2 neighbourPosition = new int2(currentNode.X + direction.x, currentNode.Y + direction.y);
	// 				
	// 				if (neighbourPosition.x < 0 || neighbourPosition.x >= MapSize || neighbourPosition.y < 0 || neighbourPosition.y >= MapSize)
	// 				{
	// 					continue;
	// 				}
	// 				
	// 				PathNode neighbourNode = pathNodeArray[neighbourPosition.x * MapSize + neighbourPosition.y];
	// 				
	// 				int neighbourNodeIndex = neighbourPosition.x * MapSize + neighbourPosition.y;
	// 				if (closedList.Contains(neighbourNodeIndex) || !pathNodeArray[neighbourNodeIndex].IsWalkable)
	// 				{
	// 					continue;
	// 				}
	// 				
	// 				int tentativeGCost = currentNode.gCost + 
	// 				                     CalculateHCost(currentNode.Position, neighbourPosition);
	//
	// 				if (tentativeGCost < neighbourNode.gCost)
	// 				{
	// 					neighbourNode.CameFromNodeIndex = currentNodeIndex;
	// 					neighbourNode.gCost = tentativeGCost;
	// 					neighbourNode.hCost = CalculateHCost(neighbourPosition, end);
	// 					pathNodeArray[neighbourNodeIndex] = neighbourNode;
	// 					
	// 					if (!openList.Contains(neighbourNodeIndex))
	// 					{
	// 						openList.Add(neighbourNodeIndex);
	// 					}
	// 				}
	// 			}
	// 			
	// 		}
	// 		
	// 		
	// 		PathNode endNode = pathNodeArray[end.x * MapSize + end.y];
	//
	// 		NativeList<int2> path;
	// 		
	// 		if (endNode.CameFromNodeIndex == -1)
	// 		{
	// 			path = new NativeList<int2>(Allocator.Temp);
	// 		}
	// 		else
	// 		{
	// 			path = new NativeList<int2>(Allocator.Temp);
	// 			PathNode currentNode = endNode;
	// 			while (currentNode.CameFromNodeIndex != -1)
	// 			{
	// 				path.Add(currentNode.Position);
	// 				currentNode = pathNodeArray[currentNode.CameFromNodeIndex];
	// 			}
	// 		}
	// 		
	// 		
	// 		pathNodeArray.Dispose();
	// 		openList.Dispose();
	// 		closedList.Dispose();
	// 		directions.Dispose();
	// 		
	// 		return path;
	// 	}
	// 	
	// 	
	// 	private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
	// 	{
	// 		PathNode lowestCostPathNode = pathNodeArray[openList[0]];
	// 		for (int i = 1; i < openList.Length; i++)
	// 		{
	// 			PathNode testPathNode = pathNodeArray[openList[i]];
	// 			if (testPathNode.fCost < lowestCostPathNode.fCost)
	// 			{
	// 				lowestCostPathNode = testPathNode;
	// 			}
	// 		}
	//
	// 		return lowestCostPathNode.Index;
	// 	}
	// 	
	// 	private int CalculateHCost(int2 a, int2 b)
	// 	{
	// 		return math.abs(a.x - b.x) + math.abs(a.y - b.y);
	// 	}
	// 	
	// 	public void Execute()
	// 	{
	// 		 Path = FindPath(Start, Target);
	// 	}
	// }
}