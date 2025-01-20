using AntsPheromones.Components;
using NativeTrees;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace AntsPheromones.Systems
{
    [UpdateAfter(typeof(PheromonesRenderingSystem))]
    public partial struct AntsMovementSystem : ISystem
    {
        private Random _rng;
        private EntityQuery _antsQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Colony>();
            state.RequireForUpdate<Ant>();
            state.RequireForUpdate<Home>();
            state.RequireForUpdate<Resource>();
            state.RequireForUpdate<ObstacleQuadTree>();
            _antsQuery = new EntityQueryBuilder(Allocator.Temp).WithAspect<AntAspect>()
                .Build(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _rng = Random.CreateFromIndex(state.GlobalSystemVersion);
            Colony colony = SystemAPI.GetSingleton<Colony>();
            Entity resourceEntity = SystemAPI.GetSingletonEntity<Resource>();
            DynamicBuffer<PheromoneBufferElement> pheromonesBuffer = SystemAPI.GetSingletonBuffer<PheromoneBufferElement>();
            RefRO<Position> resourcePosition = SystemAPI.GetComponentRO<Position>(resourceEntity);
            ObstacleQuadTree quadTree = SystemAPI.GetSingleton<ObstacleQuadTree>();
            AntsMovementSystemJob movementJob = new AntsMovementSystemJob
            {
                Colony = colony,
                Rng = _rng,
                Obstacles = quadTree,
                ResourcePosition = resourcePosition.ValueRO.Value,
                DeltaTime = SystemAPI.Time.DeltaTime,
                PheromonesBuffer = pheromonesBuffer,
            };
            state.Dependency = movementJob.ScheduleParallel(_antsQuery,
                state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }

    #region Job

    [WithAll(typeof(AntAspect))]
    [BurstCompile]
    internal partial struct AntsMovementSystemJob : IJobEntity
    {
        [ReadOnly] public Colony Colony;
        [ReadOnly] public Random Rng;
        [ReadOnly] public float2 ResourcePosition;
        [ReadOnly] public ObstacleQuadTree Obstacles;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public DynamicBuffer<PheromoneBufferElement> PheromonesBuffer;


        /// <summary>
        ///     Look on each side of the ant to see if there is a pheromone trail
        /// </summary>
        /// <param name="ant"></param>
        /// <returns></returns>
        private float2 PheromoneSteering(AntAspect ant)
        {
            float2 output = 0f;
            for (int i = -1;
                 i <= 1;
                 i += 2)
            {
                float a = ant.FacingAngle + i * math.PI * .25f;
                float2 direction = AngleToDirection(a);
                int2 testPosition = (int2) (ant.Position + direction * Colony.PheromoneSeekDistance);
                if (testPosition.x < 0 || testPosition.x >= Colony.MapSize || testPosition.y < 0 || testPosition.y >= Colony.MapSize)
                {
                }
                else
                {
                    int index = testPosition.x + testPosition.y * (int) Colony.MapSize;
                    PheromoneBufferElement pheromoneBufferElement = PheromonesBuffer[index];
                    output += math.normalizesafe(testPosition - ant.Position) * pheromoneBufferElement.Strength;
                }
            }

            return math.normalizesafe(output);
        }

        private bool LineCast(AntAspect ant,
            float2 direction,
            float distance,
            out QuadtreeRaycastHit<float2> hit)
        {
            return Obstacles.Value.RaycastAABB(new Ray2D(ant.Position,
                    direction),
                out hit,
                distance);
        }

        /// <summary>
        ///     Check if there is a wall on each side of the ant
        /// </summary>
        /// <param name="ant"></param>
        /// <param name="steering"></param>
        /// <returns></returns>
        private float2 WallSteering(AntAspect ant,
            float2 steering)
        {
            float2 outSteering = float2.zero;
            // check if there is a wall on each side of the ant
            for (int i = -1;
                 i <= 1;
                 i += 2)
            {
                float angle = DirectionToAngle(steering) + i * Mathf.PI * .25f;
                float2 side = AngleToDirection(angle);
                if (Obstacles.Value.RaycastAABB(new Ray2D(ant.Position,
                            side),
                        out QuadtreeRaycastHit<float2> hit,
                        Colony.WallSeekDistance))
                {
                    // get the hit normal from the hit.point and the ant position
                    float2 normal = math.normalizesafe(hit.point - ant.Position);
                    outSteering -= normal;
                }
            }

            return outSteering;
        }

        private float2 AngleToDirection(float angle)
        {
            return new float2(math.cos(angle),
                math.sin(angle));
        }

        private float DirectionToAngle(float2 direction)
        {
            return math.atan2(direction.y,
                direction.x);
        }
        

        private void Execute(AntAspect ant,
            [EntityIndexInQuery] int idx)
        {
            float targetSpeed = Colony.AntTargetSpeed;

            float2 targetPosition = !ant.IsHoldingResource
                ? ResourcePosition
                : new float2(Colony.MapSize * .5f,
                    Colony.MapSize * .5f);
            float2 desiredVelocity = math.normalizesafe(targetPosition - ant.Position) * targetSpeed;
            float2 steering = math.normalizesafe(desiredVelocity) * Colony.GoalSteeringStrength;

            // get random steering
            float randomSteering = Rng.NextFloat(-Colony.RandomSteering,
                Colony.RandomSteering);
            // apply random steering
            steering += AngleToDirection(ant.FacingAngle + randomSteering);


            // apply pheromone steering
            steering += PheromoneSteering(ant) * Colony.PheromoneSteeringStrength;

            // apply wall "antenna" steering
            steering += WallSteering(ant,
                            steering) *
                        Colony.WallSteeringStrength;


            // apply final steering
            steering = math.normalizesafe(steering) *
                       math.min(math.length(steering),
                           Colony.AntAcceleration * Colony.SimulationSpeed);
            steering += ant.Velocity;
            steering = math.normalizesafe(steering) *
                       math.min(math.length(steering),
                           targetSpeed);


            // check if there is a wall in front of the ant
            if (LineCast(ant,
                    math.normalizesafe(steering),
                    math.length(steering),
                    out QuadtreeRaycastHit<float2> hit))
            {
                float2 normal = math.normalizesafe(ant.Position - hit.point);
                steering = math.normalizesafe(math.reflect(steering,
                    normal));
                // re-apply final steering
                steering *=
                    math.min(math.length(steering),
                        Colony.AntAcceleration * Colony.SimulationSpeed);
                steering += ant.Velocity;
                steering = math.normalizesafe(steering) *
                           math.min(math.length(steering),
                               targetSpeed);
            }

            float2 deltaPosition = steering * DeltaTime * Colony.SimulationSpeed;
            // check if the ant is going out of bounds
            float2 newPosition = ant.Position + deltaPosition;
            if (newPosition.x < 0 || newPosition.x >= Colony.MapSize)
            {
                steering *= new float2(-1,
                    1);
            }
            if (newPosition.y < 0 || newPosition.y >= Colony.MapSize)
            {
                steering *= new float2(1,
                    -1);
            }

            // add the new position to the ant for the pheromone trail
            ant.AddPositionThisFrame(new int2((int) math.floor(newPosition.x),
                (int) math.floor(newPosition.y)));

            // check if the ant is close enough to the target (home or resource)
            if (math.distancesq(newPosition,
                    targetPosition) <
                4f * 4f)
            {
                ant.IsHoldingResource = !ant.IsHoldingResource;
            }

            // calculate excitement
            float excitement = ant.IsHoldingResource
                ? 1f
                : .3f;
            excitement *= math.length(ant.Velocity) / targetSpeed;


            ant.Excitement = excitement;
            ant.Velocity = steering;
            ant.Position += ant.Velocity * (DeltaTime * Colony.SimulationSpeed);
            ant.FacingAngle = DirectionToAngle(ant.Velocity);
        }
    }
    #endregion
}
