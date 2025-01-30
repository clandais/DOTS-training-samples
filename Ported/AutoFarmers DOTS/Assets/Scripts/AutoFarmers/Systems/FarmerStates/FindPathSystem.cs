using AutoFarmers.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AutoFarmers.Systems.FarmerStates
{
	 [UpdateAfter(typeof(PathFindingInitialisationSystem))]
	public partial struct FindPathSystem : ISystem
	{

		private EntityQuery _farmerQuery;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
			state.RequireForUpdate<Farm>();
			state.RequireForUpdate<GraphNode>();
			_farmerQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAspect<FarmerAspect>()
				.WithAll<TargetComponent>()
				.WithAll<PathBufferElement>()
				.WithNone<Pathing>()
				.Build(ref state);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			Entity farmEntity = SystemAPI.GetSingletonEntity<Farm>();
			var graphNode = SystemAPI.GetBuffer<GraphNode>(farmEntity);

						
			var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

			
			var job = new FindPathToTargetJob
			{
				Nodes = graphNode,
				MapSize = SystemAPI.GetAspect<MapAspect>(farmEntity).MapSize,
				Ecb = ecb.AsParallelWriter(),
			};

			state.Dependency = job.ScheduleParallel(_farmerQuery, state.Dependency);
			
			

		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{

		}
	}


	[WithAll(typeof(FarmerAspect))]
	[WithAll(typeof(TargetComponent))]
	internal partial struct FindPathToTargetJob : IJobEntity
	{
		[ReadOnly] public DynamicBuffer<GraphNode> Nodes;
		[ReadOnly] public int MapSize;
		public EntityCommandBuffer.ParallelWriter Ecb;

		private void Execute(FarmerAspect farmer, TargetComponent target,  [EntityIndexInQuery] int index)
		{
			var start = new int2((int)math.floor(farmer.Position.x), (int)math.floor( farmer.Position.y));
			int2 end = target.Value;

			var path = FindPath(start, end);
			
			Debug.Log($" Path Length: {path.Length}");
			
			Ecb.SetBuffer<PathBufferElement>(index, farmer.GetEntity());

			for (int i = path.Length -1; i >= 0; i--)
			{
				Ecb.AppendToBuffer(index,farmer.GetEntity(), new PathBufferElement()
				{
					Value = path[i]
				});
			}
			
			
			
			Ecb.SetComponent(index, farmer.GetEntity(), new Pathing()
			{
				CurrentIndex = 0,
			});


			Ecb.SetComponentEnabled<Pathing>(index, farmer.GetEntity(), true);
			path.Dispose();
		}


		private NativeList<int2> FindPath(int2 start, int2 end)
		{

			var directions = new NativeArray<int2>(4, Allocator.Temp);

			directions[0] = new int2(-1, 0);
			directions[1] = new int2(1, 0);
			directions[2] = new int2(0, -1);
			directions[3] = new int2(0, 1);


			var pathNodeArray = new NativeArray<PathNode>(
				MapSize * MapSize, 
				Allocator.Temp);

			for (int x = 0; x < MapSize; x++)
			{
				for (int y = 0; y < MapSize; y++)
				{
					var pathNode = new PathNode
					{
						X = x,
						Y = y,
						Index = y * MapSize + x,
						gCost = int.MaxValue,
						hCost = CalculateHCost(new int2(x, y), end)
					};

					pathNode.IsWalkable = Nodes[pathNode.Index].IsWalkable;
					pathNode.CameFromNodeIndex = -1;
					pathNodeArray[pathNode.Index] = pathNode;
				}
			}

			
			
			PathNode startNode = pathNodeArray[start.x + MapSize * start.y];
			startNode.gCost = 0;
			
			pathNodeArray[start.x + MapSize * start.y] = startNode;

			var openList = new NativeList<int>(Allocator.Temp);
			var closedList = new NativeList<int>(Allocator.Temp);
			int endNodeIndex = end.x + MapSize * end.y;

			openList.Add(startNode.Index);

			while (openList.Length > 0)
			{
				int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
				PathNode currentNode = pathNodeArray[currentNodeIndex];



				if (currentNodeIndex == endNodeIndex)
				{
					break;
				}

				for (int i = 0; i < openList.Length; i++)
				{
					if (openList[i] == currentNodeIndex)
					{
						openList.RemoveAtSwapBack(i);
						break;
					}
				}

				closedList.Add(currentNodeIndex);

				foreach (int2 direction in directions)
				{
					var neighbourPosition = new int2(
						currentNode.X + direction.x,
						currentNode.Y + direction.y);

					if (neighbourPosition.x < 0 || neighbourPosition.x >= MapSize || neighbourPosition.y < 0 || neighbourPosition.y >= MapSize)
					{
						continue;
					}

					PathNode neighbourNode = pathNodeArray[neighbourPosition.x + MapSize * neighbourPosition.y];
					int neighbourNodeIndex = neighbourPosition.x + MapSize * neighbourPosition.y;
					
					if (closedList.Contains(neighbourNodeIndex) || !pathNodeArray[neighbourNodeIndex].IsWalkable)
					{
						continue;
					}

					int tentativeGCost = currentNode.gCost + CalculateHCost(currentNode.Position, neighbourPosition);

					if (tentativeGCost < neighbourNode.gCost)
					{
						neighbourNode.CameFromNodeIndex = currentNodeIndex;
						neighbourNode.gCost = tentativeGCost;
						neighbourNode.hCost = CalculateHCost(neighbourPosition, end);
						pathNodeArray[neighbourNodeIndex] = neighbourNode;

						if (!openList.Contains(neighbourNodeIndex))
						{
							openList.Add(neighbourNode.Index);
						}
					}
				}
			}


			PathNode endNode = pathNodeArray[endNodeIndex];
			NativeList<int2> path;

			if (endNode.CameFromNodeIndex == -1)
			{
				
				Debug.Log("No Path Found");
				path = new NativeList<int2>(Allocator.Temp);
			}
			else
			{
				Debug.Log("Path Found");
				
				path = new NativeList<int2>(Allocator.Temp);
				PathNode currentNode = endNode;
				while (currentNode.CameFromNodeIndex != -1)
				{
					path.Add(currentNode.Position);
					currentNode = pathNodeArray[currentNode.CameFromNodeIndex];
				}
			}


			pathNodeArray.Dispose();
			openList.Dispose();
			closedList.Dispose();
			directions.Dispose();

			return path;
		}

		private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
		{
			PathNode lowestCostPathNode = pathNodeArray[openList[0]];
			for (int i = 1; i < openList.Length; i++)
			{
				PathNode testPathNode = pathNodeArray[openList[i]];
				if (testPathNode.fCost < lowestCostPathNode.fCost)
				{
					lowestCostPathNode = testPathNode;
				}
			}

			return lowestCostPathNode.Index;
		}

		private int CalculateHCost(int2 a, int2 b)
		{
			return math.abs(  math.abs(a.x - b.x) - math.abs(a.y - b.y));
		}
	}
}