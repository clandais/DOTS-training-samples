using System;
using AntsPheromones.Components;
using NativeTrees;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace AntsPheromones.Systems
{
	public partial struct SpawnSystem : ISystem
	{
		private Random _random;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<Colony>();
			_random = Random.CreateFromIndex(state.GlobalSystemVersion);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			state.Enabled = false;
			
			var colony = SystemAPI.GetSingleton<Colony>();
			SpawnHome(ref state, colony);
			SpawnResource(ref state, colony);
			SpawnObstacles(ref state, colony);
			SpawnAnts(ref state, colony);
			SpawnPheromones(ref state, colony);
		}

		private void SpawnPheromones(ref SystemState state, Colony colony)
		{
			float mapSize = colony.MapSize;
			int pheromoneCount = (int)mapSize * (int)mapSize;
			Entity pheromoneEntity = state.EntityManager.CreateEntity();
			var pheromoneBuffer = state.EntityManager.AddBuffer<PheromoneBufferElement>(pheromoneEntity);
			pheromoneBuffer.ResizeUninitialized(pheromoneCount);


			for (int i = 0; i < pheromoneCount; i++) 
				pheromoneBuffer[i] = new PheromoneBufferElement { Strength = 0f };
			
		}

		private void SpawnAnts(ref SystemState state, Colony colony)
		{
			
			Random random = Random.CreateFromIndex(state.GlobalSystemVersion);
			
			state.EntityManager.Instantiate(colony.AntPrefab, colony.AntCount, Allocator.Temp);
			float mapSize = colony.MapSize;

			var ecb = new EntityCommandBuffer(Allocator.Temp);
			
			
			foreach (var (position, direction, localTransform, speed, entity)
			         in SystemAPI.Query<RefRW<Position>, RefRW<Direction>, RefRW<LocalTransform>, RefRW<Speed>>()
				         .WithAll<Ant>().WithEntityAccess())
			{
				float2 pos = new float2(random.NextFloat(-5f, 5f) + mapSize * .5f, random.NextFloat(-5f, 5f) + mapSize * .5f);
			
				ecb.SetComponent(entity, new Position { Value = pos });
				ecb.SetComponent(entity, new Direction { Value = new float2(random.NextFloat(-1f, 1f), random.NextFloat(-1f, 1f)) });
				ecb.SetComponent(entity, new Speed { Value = colony.AntTargetSpeed });
				ecb.SetComponent(entity, new LocalTransform { Position = new float3(pos.x, 0f, pos.y), Scale = colony.AntScale });
				ecb.AddBuffer<PositionsThisFrame>(entity);
			}
			
			ecb.Playback(state.EntityManager);
			ecb.Dispose();

		}

		private void SpawnObstacles(ref SystemState state, Colony colony)
		{
			float mapSize = colony.MapSize;
			int ringCount = colony.RingCount;
			float obstacleRadius = colony.ObstacleRadius;
			float maxFillRatio = colony.ObstacleFill;

			var obstaclePositions = new NativeList<float2>(Allocator.Temp);

			for (int i = 1; i <= ringCount; ++i)
			{
				float ringRadius = i / (ringCount + 1f) * (mapSize * .5f);
				float circumference = 2f * math.PI * ringRadius;
				int maxCount = (int)(circumference / (obstacleRadius * 2f) * 2f);
				int offset = _random.NextInt(0, maxCount);
				int holeCount = _random.NextInt(1, 6);

				for (int j = 0; j < maxCount; ++j)
				{
					float fillRatio = (float)j / maxCount;

					if (fillRatio * holeCount % 1f < maxFillRatio)
					{
						float angle = (j + offset) / (float)maxCount * math.PI * 2f;
						Entity obstacle = state.EntityManager.Instantiate(colony.ObstaclePrefab);

						var obstaclePosition = new float2(mapSize * .5f + math.cos(angle) * ringRadius, mapSize * .5f + math.sin(angle) * ringRadius);
						var localTransform = SystemAPI.GetComponentRW<LocalTransform>(obstacle);
						localTransform.ValueRW.Position = new float3(obstaclePosition.x, 0f, obstaclePosition.y);

						obstaclePositions.Add(obstaclePosition);
					}
				}
			}

			int objectsPerNodes = colony.ObjectsPerNode;
			
			if (objectsPerNodes < 1)
				objectsPerNodes = 1;

			var quadTree = new NativeQuadtree<float2>(
				new AABB2D(new float2(0f, 0f), new float2(mapSize, mapSize)),
				objectsPerNodes,
				colony.MaxDepth,
				Allocator.Persistent);

			foreach (float2 position in obstaclePositions)
			{
				quadTree.Insert(position,
					new AABB2D(
						position + new float2(-obstacleRadius, -obstacleRadius),
						position + new float2(obstacleRadius, obstacleRadius)
						));
			}

			var obstacleQuadTree = new ObstacleQuadTree { Value = quadTree };

			state.EntityManager.CreateSingleton<ObstacleQuadTree>();
			var qt = SystemAPI.GetSingletonRW<ObstacleQuadTree>();
			qt.ValueRW = obstacleQuadTree;

			obstaclePositions.Dispose();
		}

		private void SpawnResource(ref SystemState state, Colony colony)
		{
			Entity resource = state.EntityManager.Instantiate(colony.ResourcePrefab);
			float mapSize = colony.MapSize;
			float resourceAngle = _random.NextFloat(0f, math.PI * 2f);

			float2 position = new float2(1, 1) * colony.MapSize * .5f +
			                  new float2(math.cos(resourceAngle), math.sin(resourceAngle))
			                  * mapSize * 0.475f;
			state.EntityManager.SetComponentData(resource, LocalTransform.FromPosition(new float3(position.x, 0f, position.y)));
			state.EntityManager.SetComponentData(resource, new Position { Value = position });
			// state.EntityManager.AddComponent<Resource>(resource);


		}

		private void SpawnHome(ref SystemState state, Colony colony)
		{
			Entity home = state.EntityManager.Instantiate(colony.HomePrefab);
			var localTransform = SystemAPI.GetComponentRW<LocalTransform>(home);
			localTransform.ValueRW.Position = new float3(colony.MapSize * .5f, 0f, colony.MapSize * .5f);

			state.EntityManager.AddComponent<Home>(home);
		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{
			var quadTree = SystemAPI.GetSingleton<ObstacleQuadTree>();
			quadTree.Value.Dispose();
		}
	}
}