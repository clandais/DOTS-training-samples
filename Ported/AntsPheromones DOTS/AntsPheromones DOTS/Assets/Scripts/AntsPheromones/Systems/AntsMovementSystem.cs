using System.Collections;
using System.Collections.Generic;
using AntsPheromones.Authoring;
using AntsPheromones.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace AntsPheromones.Systems
{
    public partial struct AntsMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Map>();
            state.RequireForUpdate<AntsConfig>();
            state.RequireForUpdate<Ant>();
            state.RequireForUpdate<ObstaclesConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rng = Random.CreateFromIndex(state.GlobalSystemVersion);
            var antsConfig = SystemAPI.GetSingleton<AntsConfig>();
            var map = SystemAPI.GetSingleton<Map>();
            var obstaclesConfig = SystemAPI.GetSingleton<ObstaclesConfig>();

            var pheromones = SystemAPI.GetSingletonBuffer<Pheromone>(true);
            var obstacles = SystemAPI.GetSingletonBuffer<ObstaclesPositions>(true);
            
            var andMovementJob = new AntsMovementSystemJob
            {
                AntsConfig = antsConfig,
                Map = map,
                Rng = rng,
                Obstacles = obstacles,
                ObstaclesConfig = obstaclesConfig,
            };
            
            state.Dependency = andMovementJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    

    [WithAll(typeof(AntAspect))]
    [BurstCompile]
    partial struct AntsMovementSystemJob : IJobEntity
    {
        [ReadOnly] public AntsConfig AntsConfig;
        [ReadOnly] public Map Map;
        [ReadOnly] public Random Rng;
        [ReadOnly] public DynamicBuffer<ObstaclesPositions> Obstacles;
        [ReadOnly] public ObstaclesConfig ObstaclesConfig;
        private void Execute(AntAspect ant)
        {
            float targetSpeed = AntsConfig.AntSpeed;
            ant.Speed += (targetSpeed - ant.Speed) * AntsConfig.AntAcceleration;
            ant.FacingAngle += Rng.NextFloat(-AntsConfig.RandomSteering, AntsConfig.RandomSteering);
            
            float wallSteering = GetWallSteering(ant, 3f) * AntsConfig.WallSteeringStrength;
            float pheromoneSteering = 0f;
            
            ant.FacingAngle += wallSteering;
            ant.FacingAngle += pheromoneSteering;
            
            targetSpeed *= 1f - (math.abs(pheromoneSteering) + math.abs(wallSteering)) / 3f;
            ant.Speed += (targetSpeed - ant.Speed) * AntsConfig.AntAcceleration;
            
            
            float2 targetPosition;
            if (!ant.IsHoldingResource)
            {
                targetPosition = Map.ResourcePosition;
            }
            else
            {
                targetPosition = Map.ColonyPosition;
            }


            if (math.lengthsq(ant.Position - targetPosition) < 4f * 4f)
            {
                ant.IsHoldingResource = ! ant.IsHoldingResource;
                ant.FacingAngle += math.PI;
            }
            
            float vx = math.cos(ant.FacingAngle) * ant.Speed;
            float vy = math.sin(ant.FacingAngle) * ant.Speed;
                
            if (ant.Position.x + vx < 0f || ant.Position.x + vx > Map.Size)
            {
                vx = -vx;
            }
                
            if (ant.Position.y + vy < 0f || ant.Position.y + vy > Map.Size)
            {
                vy = -vy;
            }
            
            targetPosition = new float2(ant.Position.x + vx, ant.Position.y + vy) ;

            ant.Position = targetPosition;
          //  ant.Rotation = quaternion.EulerXYZ(0, ant.FacingAngle, 0f); // quaternion.AxisAngle(new float3(0, 1, 0), ant.FacingAngle);
        }

        private float GetPheromoneSteering(AntAspect ant, float distance)
        {
            return 0f;
        }
        
        private int GetWallSteering(AntAspect ant, float distance)
        {
            int output = 0;

            for (int i = -1; i <= 1; i+=2) {
                float angle = ant.FacingAngle + i * math.PI*.25f;
                float testX = ant.Position.x + math.cos(angle) * distance;
                float testY = ant.Position.y + math.sin(angle) * distance;

                if (testX < 0 || testY < 0 || testX >= Map.Size || testY >= Map.Size) {
                    output += i;

                } else
                {
                    int value = SearchObstacles(new float2(testX, testY));
                    if (value > 0) {
                        output -= i;
                    }
                }
            }
            return output;
        }


        private int SearchObstacles(float2 position)
        {
            int startIdx = Obstacles.ToNativeArray(Allocator.Temp).BinarySearch(new ObstaclesPositions()
            {
                Value = position,
            }, new ObstacleAxisXComparer { });
            
            if (startIdx < 0) startIdx = ~startIdx;
            if (startIdx >= Obstacles.Length) startIdx = Obstacles.Length - 1;
            int value = 0;
            value += Search( position, startIdx+1, Obstacles.Length, +1);
            value +=  Search( position, startIdx-1, -1, -1);
            return value;
        }
        
        int Search(float2 antPos, int startIdx, int endIdx, int step)
        {

            int count = 0;
            
            for (int i = startIdx; i != endIdx; i += step)
            {
                float2 targetPos = Obstacles[i].Value;

                float distSq = math.distancesq(targetPos, antPos);

                if (distSq < ObstaclesConfig.obstacleRadius * ObstaclesConfig.obstacleRadius)
                {
                    count++;
                }
            }
            
            return count;
        }
    }
    
    struct ObstacleAxisXComparer : IComparer<ObstaclesPositions>
    {
        public int Compare(ObstaclesPositions a, ObstaclesPositions b)
        {
            return a.Value.x.CompareTo(b.Value.x);
        }
    }
}
