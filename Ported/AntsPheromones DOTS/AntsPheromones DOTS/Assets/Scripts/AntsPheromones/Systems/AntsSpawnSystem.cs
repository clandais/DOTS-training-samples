using AntsPheromones.Authoring;
using AntsPheromones.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace AntsPheromones.Systems
{
    public partial struct AntsSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Map>();
            state.RequireForUpdate<AntsConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var rng = Random.CreateFromIndex(state.GlobalSystemVersion);
            var map = SystemAPI.GetSingleton<Map>();
            var antsConfig = SystemAPI.GetSingleton<AntsConfig>();
            
            int antCount = antsConfig.AntCount;

            // spawn Pheromones
            {
                var entity = state.EntityManager.CreateEntity();
                var pheromoneBuffer = state.EntityManager.AddBuffer<Pheromone>(entity);
                pheromoneBuffer.Length = map.Size * map.Size;

                for (int i = 0; i < pheromoneBuffer.Length; i++)
                {
                    pheromoneBuffer[i] = new Pheromone
                    {
                        Value = new float3{},
                    };
                }
            }
            

            for (int i = 0; i < antCount; i++)
            {
                
            
                var entity = state.EntityManager.Instantiate(antsConfig.AntPrefab);
                var localTransform = LocalTransform.FromPosition(rng.NextFloat(-5, 5f) + map.Size * .5f, 0f, rng.NextFloat(-5, 5f) + map.Size * .5f);
                state.EntityManager.SetComponentData(entity, localTransform);
                state.EntityManager.SetComponentData(entity, new Ant
                {
                    FacingAngle = rng.NextFloat() * math.PI * 2f,
                    Speed = antsConfig.AntSpeed,
                    HoldingResource = false,
                    Position = localTransform.Position.xz,
                });
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
