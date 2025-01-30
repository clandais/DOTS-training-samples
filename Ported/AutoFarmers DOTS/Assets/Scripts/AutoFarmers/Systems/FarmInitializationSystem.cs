using System;
using AutoFarmers.Authoring;
using AutoFarmers.Systems.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace AutoFarmers.Systems
{


	
	public partial struct FarmInitializationSystem : ISystem
	{
		private Random _rng;

		
		// [BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<FarmCfg>();
			state.RequireForUpdate<Farm>();
			state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
			_rng = Random.CreateFromIndex((uint)DateTime.Now.Millisecond);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{

			state.Enabled = false;

			var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
			EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
			var farmCfg = SystemAPI.GetSingleton<FarmCfg>();
			var farm = SystemAPI.GetSingleton<Farm>();
			Entity farmEntity = SystemAPI.GetSingletonEntity<Farm>();

			var storeTileBuffer = state.EntityManager.AddBuffer<StoreTile>(farmEntity);
			storeTileBuffer.ResizeUninitialized(farm.MapSize * farm.MapSize);


			// spawn ground
			{
				state.EntityManager.Instantiate(farmCfg.GroundPrefab, farm.MapSize * farm.MapSize, Allocator.Temp);

				var groundSpawnJob = new SpawnGroundJob
				{
					ecb = ecb.AsParallelWriter(),
					MapSize = farm.MapSize
				};

				JobHandle spawnGroundHandle = groundSpawnJob.ScheduleParallel(state.Dependency);
				spawnGroundHandle.Complete();
			}


			// spawn stores
			{
				var storeTilesArray = new NativeArray<bool>(farm.MapSize * farm.MapSize, Allocator.TempJob);

				var storeSpawnJob = new SpawnStoresJob
				{
					ECB = ecb.AsParallelWriter(),
					StoreTileBuffer = storeTilesArray,
					StorePrefab = farmCfg.StorePrefab,
					MapSize = farm.MapSize,
					MaxStores = farm.StoreCount,
					Rng = _rng
				};
				JobHandle spawnStoreHandle = storeSpawnJob.Schedule(farm.MapSize * farm.MapSize, 64, state.Dependency);
				spawnStoreHandle.Complete();

				for (int i = 0; i < farm.MapSize * farm.MapSize; i++)
				{
					storeTileBuffer[i] = new StoreTile { IsOccupied = storeTilesArray[i] };
				}

				storeTilesArray.Dispose();
			}
			
			var rockTileBuffer = state.EntityManager.AddBuffer<RockedTile>(farmEntity);

			// spawn rocks
			{

				
				rockTileBuffer.ResizeUninitialized(farm.MapSize * farm.MapSize);

				storeTileBuffer = state.EntityManager.GetBuffer<StoreTile>(farmEntity);

				var rockTileArray = new NativeArray<bool>(farm.MapSize * farm.MapSize, Allocator.TempJob);
				var rockedTileList = new NativeList<RockedTile>(Allocator.TempJob);

				var job = new SpawnRocksJob
				{
					FarmCfg = farmCfg,
					Farm = farm,
					Rng = _rng,
					StoreTileBuffer = storeTileBuffer,
					ECB = ecb,
					RockTileBuffer = rockTileArray,
					RockBuffer = rockedTileList,
				};

				JobHandle jobHandle = job.Schedule(state.Dependency);
				jobHandle.Complete();


				for (int i = 0; i < farm.MapSize * farm.MapSize; i++)
				{
					rockTileBuffer[i] = new RockedTile { IsOccupied = rockTileArray[i] };
				}
				
				foreach (var rockTle in rockedTileList)
				{
					var rock = rockTle.Rock;
					var rect = rock.Rect;
					
					for (int x =rect.x; x <= rect.x + rect.width; x++)
					{
						for (int y = rect.y; y <= rect.y + rect.height; y++)
						{
							
							rockTileBuffer[x * farm.MapSize + y] = rockTle;		
						}
					}
				}
				
				


				rockTileArray.Dispose();
				rockedTileList.Dispose();
			}
			
			// spawn farmer
			{


				// for (int i = 0; i < 100; i++)
				// {
				// 	var spawnPosition = new int2(_rng.NextInt(0, farm.MapSize), _rng.NextInt(0, farm.MapSize));
				//
				// 	if (spawnPosition.x < 0 || spawnPosition.x >= farm.MapSize || spawnPosition.y < 0 || spawnPosition.y >= farm.MapSize)
				// 	{
				// 		continue;
				// 	}
				// 	
				// 	if (rockTileBuffer[spawnPosition.x * farm.MapSize + spawnPosition.y].IsOccupied)
				// 	{
				// 		continue;
				// 	}
				// 	
				//
				// 	Entity entity = state.EntityManager.Instantiate(farmCfg.FarmerPrefab);
				// 	state.EntityManager.SetComponentData(entity, LocalTransform.FromPosition(new float3(spawnPosition.x + 0.5f, 0f, spawnPosition.y + 0.5f)));
				// 	state.EntityManager.SetComponentData(entity, new Farmer { IsOriginal = true });
				// 	break;
				//
				// }
			}

		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{

		}
	}
}