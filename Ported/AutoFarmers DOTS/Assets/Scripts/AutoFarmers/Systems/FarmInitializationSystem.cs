using System;
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
    partial struct SpawnGroundJob : IJobEntity
    {
        
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public int MapSize;
        
        
        [BurstCompile]
        void Execute([EntityIndexInQuery]int index, Entity entity, ref LocalTransform localTransform)
        {
            
            var x = index / MapSize;
            var y = index % MapSize;
            
            ecb.SetComponent(index, entity, LocalTransform.FromPosition( new float3(x, 0f, y)));
        }
    }

    [BurstCompile]
    struct SpawnStoresJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [NativeDisableParallelForRestriction]
        public NativeArray<bool> StoreTileBuffer;
        [ReadOnly] public Entity StorePrefab;
        [ReadOnly] public int MapSize;
        [ReadOnly] public int MaxStores;
        public int StoreCount;
        [ReadOnly] public Random Rng;
        
        public void Execute(int index)
        {
            if (index >= MapSize * MapSize)
            {
                return;
            }
            
            
            var x = Rng.NextInt(0, MapSize);
            var y = Rng.NextInt(0, MapSize);
            
            var idx = x * MapSize + y;
            
            // var x = index / MapSize;
           // var y = index % MapSize;
            
            if (StoreTileBuffer[idx] || StoreCount >= MaxStores)
            {
                
            }
            else
            {
                StoreTileBuffer[idx] = true;
            
                ECB.Instantiate(index, StorePrefab);
                ECB.SetComponent(index, StorePrefab, LocalTransform.FromPosition( new float3(x, 0f, y)));
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

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var farmCfg = SystemAPI.GetSingleton<FarmCfg>();
            var farm = SystemAPI.GetSingleton<Farm>();
            var farmEntity = SystemAPI.GetSingletonEntity<Farm>();

            var storeTileBuffer = state.EntityManager.AddBuffer<StoreTile>(farmEntity);
            storeTileBuffer.ResizeUninitialized( farm.MapSize * farm.MapSize );
            
            
            state.EntityManager.Instantiate(farmCfg.GroundPrefab, farm.MapSize * farm.MapSize, Allocator.Temp);
            
            var groundSpawnJob = new SpawnGroundJob
            {
                ecb = ecb.AsParallelWriter(),
                MapSize = farm.MapSize,
            };
            
           var spawnGroundHandle  = groundSpawnJob.ScheduleParallel(state.Dependency);
           spawnGroundHandle.Complete();
           
           //ecb.Playback(state.EntityManager);
           // state.Dependency.Complete();
           
            NativeArray<bool> storeTilesArray = new NativeArray<bool>(farm.MapSize * farm.MapSize, Allocator.TempJob);
            
            var storeSpawnJob = new SpawnStoresJob
            {
                ECB = ecb.AsParallelWriter(),
                StoreTileBuffer = storeTilesArray,
                StorePrefab = farmCfg.StorePrefab,
                MapSize = farm.MapSize,
                MaxStores = farm.StoreCount,
                Rng = rng,
            };
            var spawnStoreHandle = storeSpawnJob.Schedule( farm.MapSize * farm.MapSize, 64, state.Dependency);
            spawnStoreHandle.Complete();
            
            for (int i = 0; i < farm.MapSize*farm.MapSize; i++)
            {
                storeTileBuffer[i] = new StoreTile {IsOccupied = storeTilesArray[i]};
            }
            
            storeTilesArray.Dispose();
            
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
