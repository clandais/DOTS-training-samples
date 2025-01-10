using AntsPheromones.Authoring;
using AntsPheromones.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace AntsPheromones.Systems
{
    public partial struct ObstaclesSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Map>();
            state.RequireForUpdate<ObstaclesConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var rng = Random.CreateFromIndex(state.GlobalSystemVersion); 
            var map = SystemAPI.GetSingleton<Map>();
            var obstaclesConfig = SystemAPI.GetSingleton<ObstaclesConfig>();


            var bufferEntity = state.EntityManager.CreateEntity();
            var obstacleBuffer = state.EntityManager.AddBuffer<ObstaclesPositions>(bufferEntity);

            for (int i = 1; i <= obstaclesConfig.obstacleRingCount; i++)
            {
                float ringRadius = (i/(float)obstaclesConfig.obstacleRingCount) * map.Size / 2f;
                float circumference = 2 * math.PI * ringRadius;
                int maxCount = (int)(circumference / (2f *obstaclesConfig.obstacleRadius) * 2f);
                int offset = rng.NextInt(0, maxCount);
                int holeCount = rng.NextInt(1, 3);

                for (int j = 0; j < maxCount; j++)
                {
                    float t = (float)j / maxCount;

                    if ((t * holeCount) % 1f < obstaclesConfig.obstaclesPerRing)
                    {
                        float angle = (j + offset) / (float)(maxCount) * (2f * math.PI);
                        
                        var entity = state.EntityManager.Instantiate(obstaclesConfig.obstaclePrefab);
                        var position = new float2(map.Size * .5f + math.cos(angle) * ringRadius, map.Size * .5f + math.sin(angle) * ringRadius);
                        var obstacle = new Obstacle
                        {
                            Position = position,
                            Radius = obstaclesConfig.obstacleRadius,
                        };
                        
                        
                        
                        state.EntityManager.SetComponentData(entity, obstacle);
                        state.EntityManager.SetComponentData(entity, LocalTransform.FromPosition(
                            new float3(position.x, 0, position.y)));

                        obstacleBuffer.Add(new ObstaclesPositions { Value = position });
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
