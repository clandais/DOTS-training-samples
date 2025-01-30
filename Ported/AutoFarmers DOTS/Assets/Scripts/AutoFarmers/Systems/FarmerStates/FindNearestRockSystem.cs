using AutoFarmers.Authoring;
using NativeTrees;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AutoFarmers.Systems.FarmerStates
{
	public partial struct FindNearestRockSystem : ISystem
	{
		
		private EntityQuery _farmerQuery;
		
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
			state.RequireForUpdate<Farm>();

			_farmerQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAspect<FarmerAspect>()
				.WithAll<SmashRocksGoal>()
				.WithNone<Pathing>()
				.WithNone<TargetComponent>()
				.Build(ref state);

			state.RequireForUpdate(_farmerQuery);

		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{

			var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
			
			var famEntity = SystemAPI.GetSingletonEntity<Farm>();
			var map = SystemAPI.GetAspect<MapAspect>(famEntity);

			var tiles = map.GetWalkableArray();
			var mapSize = map.MapSize;

			var quadTree = new NativeQuadtree<int2>(new AABB2D(
				new float2(0, 0),
				new float2(mapSize, mapSize)
				), Allocator.TempJob);


			for (int i = 0; i < mapSize*mapSize; i++)
			{
				var (walkable, rect) = tiles[i];
				
				if (!walkable)
				{
				
					int x = i % mapSize;
					int y = i / mapSize;
					quadTree.Insert( new int2(x, y), new AABB2D(new float2(x, y), new float2(x+1, y+1)));
				}
			}

			var job = new FindNearestRockJob()
			{
				WalkableTiles = tiles,
				MapSize = mapSize,
				Ecb = ecb.AsParallelWriter(),
				QuadTree = quadTree,
			};

			state.Dependency = job.ScheduleParallel(
				_farmerQuery,
				state.Dependency);

			quadTree.Dispose(state.Dependency);
			tiles.Dispose(state.Dependency);

		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{

		}
	}
	
	
	[WithAll(typeof(FarmerAspect))]
	[WithAll(typeof(SmashRocksGoal))]
	[WithNone(typeof(TargetComponent))]
	partial struct FindNearestRockJob : IJobEntity
	{
		public EntityCommandBuffer.ParallelWriter Ecb;
		
		[ReadOnly] public NativeQuadtree<int2> QuadTree;
		[ReadOnly] public NativeArray<(bool, RectInt)> WalkableTiles;
		[ReadOnly] public int MapSize;

		void Execute(FarmerAspect farmer, [EntityIndexInQuery] int index)
		{
			if (QuadTree.TryGetNearestAABB( farmer.Position, float.MaxValue, out int2 nearest))
			{
		
				// up, down, left, right
				var directions = new NativeArray<int2>(4, Allocator.Temp);

				directions[0] = new int2(-1, 0);
				directions[1] = new int2(1, 0);
				directions[2] = new int2(0, -1);
				directions[3] = new int2(0, 1);
				
				// find the nearest walkable tile
				foreach (int2 direction in directions)
				{
					int2 next = nearest + direction;
					if (next.x < 0 || next.x >= WalkableTiles.Length || next.y < 0 || next.y >= WalkableTiles.Length)
					{
						continue;
					}

					if (WalkableTiles[next.x + MapSize * next.y].Item1)
					{
						nearest = next;
						break;
					}
				}
				
				// set the target
				Ecb.SetComponentEnabled<TargetComponent>( index, farmer.GetEntity(), true);
				Ecb.SetComponent(index, farmer.GetEntity(), new TargetComponent()
				{
					Value = new int2(nearest.x, nearest.y),
				});
			}
		}
	}
}