using AutoFarmers.Authoring;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace AutoFarmers.Systems
{

    [UpdateAfter(typeof(FarmInitializationSystem))]
    public partial struct FarmerSpawnSystem : ISystem
    {
        private Random _rng;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FarmCfg>();
            state.RequireForUpdate<Farm>();
            
            _rng = Random.CreateFromIndex(state.GlobalSystemVersion);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            
            state.Enabled = false;
            
            var farmCfg = SystemAPI.GetSingleton<FarmCfg>();
            var farm = SystemAPI.GetSingleton<Farm>();
            var rockTileBuffer = SystemAPI.GetSingletonBuffer<RockedTile>();
            
            for (int i = 0; i < 100; i++)
            {
                var spawnPosition = new int2(_rng.NextInt(0, farm.MapSize), _rng.NextInt(0, farm.MapSize));

                if (spawnPosition.x < 0 || spawnPosition.x >= farm.MapSize || spawnPosition.y < 0 || spawnPosition.y >= farm.MapSize)
                {
                    continue;
                }
					
                if (rockTileBuffer[spawnPosition.x * farm.MapSize + spawnPosition.y].IsOccupied)
                {
                    continue;
                }
					

                Entity entity = state.EntityManager.Instantiate(farmCfg.FarmerPrefab);
                state.EntityManager.SetComponentData(entity, LocalTransform.FromPosition(new float3(spawnPosition.x + 0.5f, 0f, spawnPosition.y + 0.5f)));
                state.EntityManager.SetComponentData(entity, new Farmer { IsOriginal = true });
                
                state.EntityManager.SetComponentEnabled<SmashRocksGoal>(entity, false);
                state.EntityManager.SetComponentEnabled<TillGroundGoal>(entity, false);
                state.EntityManager.SetComponentEnabled<PlantSeedsGoal>(entity, false);
                state.EntityManager.SetComponentEnabled<SellPlantsGoal>(entity, false);
                state.EntityManager.SetComponentEnabled<Pathing>(entity, false);
                state.EntityManager.SetComponentEnabled<TargetComponent>(entity, false);

             //   state.EntityManager.AddBuffer<PathBufferElement>(entity);
                
              //  state.EntityManager.SetComponentEnabled<NoneGoal>(entity, true);
                
                break;

            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
