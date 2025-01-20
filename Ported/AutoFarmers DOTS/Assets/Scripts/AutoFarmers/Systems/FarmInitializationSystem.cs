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
        public EntityCommandBuffer.ParallelWriter ecb;
        public NativeArray<bool> storeTileBuffer;
        [ReadOnly] public Entity storePrefab;
        [ReadOnly] public int MapSize;
        [ReadOnly] public int MaxStores;
        public int storeCount;
        
        public void Execute(int index)
        {
            if (index >= MapSize * MapSize)
            {
                return;
            }
            
            var x = index / MapSize;
            var y = index % MapSize;
            
            if (storeTileBuffer[index])
            {
                return;
            }
            
            if (storeCount >= MaxStores)
            {
                return;
            }
            
            storeTileBuffer[index] = true;
            
            ecb.Instantiate(index, storePrefab);
            ecb.SetComponent(index, storePrefab, LocalTransform.FromPosition( new float3(x, 0f, y)));
        }
    }
    
    public partial struct FarmInitializationSystem : ISystem
    {
        private Random rng;
        private bool _groundInitialized;
        private bool _storeInitialized;
        
      //  [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FarmCfg>();
            state.RequireForUpdate<Farm>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            rng = Random.CreateFromIndex((uint)DateTime.Now.Millisecond);
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
            
           var groundSpawnHandle = groundSpawnJob.ScheduleParallel( state.Dependency);
           //ecb.Playback(state.EntityManager);
           // state.Dependency.Complete();
           
           // groundSpawnHandle.Complete();
            //
            
            NativeArray<bool> storeTilesArray = new NativeArray<bool>(farm.MapSize * farm.MapSize, Allocator.TempJob);
            
            var storeSpawnJob = new SpawnStoresJob
            {
                ecb = ecb.AsParallelWriter(),
                storeTileBuffer = storeTilesArray,
                storePrefab = farmCfg.StorePrefab,
                MapSize = farm.MapSize,
            };
            var storeSpawnHandle = storeSpawnJob.Schedule( farm.MapSize * farm.MapSize, 64, state.Dependency);
            //state.Dependency.Complete();

            var combined = JobHandle.CombineDependencies(groundSpawnHandle, storeSpawnHandle);
            
            combined.Complete();
            
            for (int i = 0; i < farm.MapSize*farm.MapSize; i++)
            {
                storeTileBuffer[i] = new StoreTile {IsOccupied = storeTilesArray[i]};
            }
            
            storeTilesArray.Dispose();
            
            // storeSpawnHandle.Complete();
            
            // ecb.Playback(state.EntityManager);
            // ecb.Dispose();
                
                
            // initialize bool buffer
            

            // var storeTileBuffer = state.EntityManager.AddBuffer<StoreTile>(farmEntity);
            //
            // storeTileBuffer.ResizeUninitialized( farm.MapSize * farm.MapSize );
            //
            // for (int x = 0; x < farm.MapSize; x++)
            // {
            //     for (int y = 0; y < farm.MapSize; y++)
            //     {
            //         var entity = state.EntityManager.Instantiate(farmCfg.GroundPrefab);
            //         state.EntityManager.SetComponentData(entity, LocalTransform.FromPosition( new float3(x, 0f, y)));
            //     }
            // }
            //
            //
            // if (!_storeInitialized)
            // {
            //     _storeInitialized = true;
            //     
            //     
            //     int spawnCount = 0;
            //     while (spawnCount < farm.StoreCount)
            //     {
            //         int x = rng.NextInt(0, farm.MapSize);
            //         int y = rng.NextInt(0, farm.MapSize);
            //         
            //         int idx = x * farm.MapSize + y;
            //         
            //         if (storeTileBuffer[idx].IsOccupied)
            //         {
            //             continue;
            //         }
            //         
            //         storeTileBuffer[idx] = new StoreTile {IsOccupied = true};
            //         
            //         var entity = state.EntityManager.Instantiate(farmCfg.StorePrefab);
            //         state.EntityManager.SetComponentData(entity, LocalTransform.FromPosition( new float3(x, 0f, y)));
            //         spawnCount++;
            //     }
            // }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
