using AutoFarmers.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace AutoFarmers.Systems
{
    [WithAll(typeof(Ground))]
    [BurstCompile]
    internal partial struct SpawnGroundJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public int MapSize;


        [BurstCompile]
        private void Execute([EntityIndexInQuery] int index, Entity entity, ref LocalTransform localTransform)
        {

            int x = index / MapSize;
            int y = index % MapSize;

            ecb.SetComponent(index, entity, LocalTransform.FromPosition(new float3(x, 0f, y)));
        }
    }

    [BurstCompile]
    internal struct SpawnStoresJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [NativeDisableParallelForRestriction] public NativeArray<bool> StoreTileBuffer;
        [ReadOnly] public Entity StorePrefab;
        [ReadOnly] public int MapSize;
        [ReadOnly] public int MaxStores;
        private int StoreCount;
        [ReadOnly] public Random Rng;

        public void Execute(int index)
        {
            if (index >= MapSize * MapSize)
            {
                return;
            }


            int x = Rng.NextInt(0, MapSize);
            int y = Rng.NextInt(0, MapSize);
            int idx = x * MapSize + y;

            if (StoreTileBuffer[idx] || StoreCount >= MaxStores)
            {
                StoreTileBuffer[idx] = false;
            }
            else
            {
                StoreTileBuffer[idx] = true;

                ECB.Instantiate(index, StorePrefab);
                ECB.SetComponent(index, StorePrefab, LocalTransform.FromPosition(new float3(x, 0f, y)));
                StoreCount++;
            }


        }
    }

    public partial struct FarmInitializationSystem : ISystem
    {
        private Random rng;
        private bool _groundInitialized;
        private bool _storeInitialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FarmCfg>();
            state.RequireForUpdate<Farm>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            rng = Random.CreateFromIndex(state.GlobalSystemVersion);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            state.Enabled = false;

            BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            FarmCfg farmCfg = SystemAPI.GetSingleton<FarmCfg>();
            Farm farm = SystemAPI.GetSingleton<Farm>();
            Entity farmEntity = SystemAPI.GetSingletonEntity<Farm>();

            DynamicBuffer<StoreTile> storeTileBuffer = state.EntityManager.AddBuffer<StoreTile>(farmEntity);
            storeTileBuffer.ResizeUninitialized(farm.MapSize * farm.MapSize);


            // spawn ground
            {
                state.EntityManager.Instantiate(farmCfg.GroundPrefab, farm.MapSize * farm.MapSize, Allocator.Temp);

                SpawnGroundJob groundSpawnJob = new SpawnGroundJob
                {
                    ecb = ecb.AsParallelWriter(),
                    MapSize = farm.MapSize,
                };

                JobHandle spawnGroundHandle = groundSpawnJob.ScheduleParallel(state.Dependency);
                spawnGroundHandle.Complete();
            }


            // spawn stores
            {
                var storeTilesArray = new NativeArray<bool>(farm.MapSize * farm.MapSize, Allocator.TempJob);

                SpawnStoresJob storeSpawnJob = new SpawnStoresJob
                {
                    ECB = ecb.AsParallelWriter(),
                    StoreTileBuffer = storeTilesArray,
                    StorePrefab = farmCfg.StorePrefab,
                    MapSize = farm.MapSize,
                    MaxStores = farm.StoreCount,
                    Rng = rng,
                };
                JobHandle spawnStoreHandle = storeSpawnJob.Schedule(farm.MapSize * farm.MapSize, 64, state.Dependency);
                spawnStoreHandle.Complete();

                for (var i = 0; i < farm.MapSize * farm.MapSize; i++)
                {
                    storeTileBuffer[i] = new StoreTile { IsOccupied = storeTilesArray[i] };
                }

                storeTilesArray.Dispose();
            }


            // spawn farmer
            {
                for (var i = 0; i < 100; i++)
                {
                    int2 spawnPosition = new int2(rng.NextInt(0, farm.MapSize), rng.NextInt(0, farm.MapSize));

                    if (spawnPosition.x < 0 || spawnPosition.x >= farm.MapSize || spawnPosition.y < 0 || spawnPosition.y >= farm.MapSize)
                    {

                    }
                    else
                    {
                        Entity entity = state.EntityManager.Instantiate(farmCfg.FarmerPrefab);
                        state.EntityManager.SetComponentData(entity, LocalTransform.FromPosition(new float3(spawnPosition.x + 0.5f, 0f, spawnPosition.y + 0.5f)));
                        state.EntityManager.SetComponentData(entity, new Farmer { IsOriginal = true });
                        break;
                    }
                }
            }

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
