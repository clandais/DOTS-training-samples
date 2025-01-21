using AntsPheromones.Components;
using AntsPheromones.Extensions;
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
			
			state.RequireAllForUpdate<Colony, Ant, Home, Resource, ObstacleQuadTree>();
			_antsQuery = state.CreateEntityQuery<AntAspect>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			_rng = Random.CreateFromIndex(state.GlobalSystemVersion);
			var colony = SystemAPI.GetSingleton<Colony>();
			Entity resourceEntity = SystemAPI.GetSingletonEntity<Resource>();
			var pheromonesBuffer = SystemAPI.GetSingletonBuffer<PheromoneBufferElement>();
			var resourcePosition = SystemAPI.GetComponentRO<Position>(resourceEntity);
			var quadTree = SystemAPI.GetSingleton<ObstacleQuadTree>();
			var movementJob = new AntsMovementSystemJob
			{
				Colony = colony,
				Rng = _rng,
				Obstacles = quadTree,
				ResourcePosition = resourcePosition.ValueRO.Value,
				DeltaTime = SystemAPI.Time.fixedDeltaTime,
				PheromonesBuffer = pheromonesBuffer
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
				var testPosition = (int2)(ant.Position + direction * Colony.PheromoneSeekDistance);
				bool outOfBounds = (testPosition.x < 0 || testPosition.x >= Colony.MapSize || testPosition.y < 0 || testPosition.y >= Colony.MapSize);
				
				if (outOfBounds) continue;
				
				int index = testPosition.x + testPosition.y * (int)Colony.MapSize;
				PheromoneBufferElement pheromoneBufferElement = PheromonesBuffer[index];
				output += math.normalizesafe(testPosition - ant.Position) * pheromoneBufferElement.Strength;
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
					    out var hit,
					    Colony.WallSeekDistance))
				{
					// get the hit normal from the hit.point and the ant position
					float2 normal = math.normalizesafe(hit.point - ant.Position);
					outSteering -= normal;
				}
			}

			return outSteering;
		}

		private static float2 AngleToDirection(float angle)
		{
			return new float2(math.cos(angle),
				math.sin(angle));
		}

		private static float DirectionToAngle(float2 direction)
		{
			return math.atan2(direction.y,
				direction.x);
		}


		private void Execute(AntAspect ant)
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
				    out var hit))
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


			float2 deltaPosition = steering * (DeltaTime * Colony.SimulationSpeed);
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

			// Create a new NativeParallelHashSet to store nearby obstacles, with an initial capacity of 16, using temporary memory allocation.
			var nearbyObstacles = new NativeParallelHashSet<float2>(16, Allocator.Temp);

			// Check if there are obstacles nearby within the specified AABB (Axis-Aligned Bounding Box).
			Obstacles.Value.RangeAABBUnique(
				new AABB2D(
					newPosition - new float2(-math.length(steering), -math.length(steering)),
					newPosition + new float2(math.length(steering), math.length(steering))
					),
				nearbyObstacles
				);

			// If there are any nearby obstacles, process each one.
			if (!nearbyObstacles.IsEmpty)
			{
				foreach (float2 obstacle in nearbyObstacles)
				{
					// Calculate the vector from the ant's position to the obstacle.
					float2 delta = ant.Position - obstacle;
					// Calculate the squared distance between the ant and the obstacle.
					float sqrDist = math.lengthsq(delta);

					// If the squared distance is less than the squared obstacle radius,
					// adjust the ant's position and steering.
					if (sqrDist < Colony.ObstacleRadius * Colony.ObstacleRadius)
					{
					
						// Normalize the delta vector.
						delta = math.normalizesafe(delta);
						
						// Adjust the ant's position to avoid the obstacle.
						newPosition = obstacle + delta * Colony.ObstacleRadius;

						// Adjust the ant's steering to avoid the obstacle.
						// '* 1.5f' is a magic number to make the steering more aggressive.
						steering.x -= delta.x * (delta.x * steering.x + delta.y * steering.y) * 1.5f;
						steering.y -= delta.y * (delta.x * steering.x + delta.y * steering.y) * 1.5f;
					}
				}

				// Normalize the steering vector and limit its length to the target speed. Again.
				steering = math.normalizesafe(steering) * math.min(math.length(steering), targetSpeed);
			}

			// Dispose of the NativeParallelHashSet to free the allocated memory.
			nearbyObstacles.Dispose();


			// add the new position to the ant for the pheromone trail
			ant.AddPositionThisFrame(new int2((int)math.floor(newPosition.x),
				(int)math.floor(newPosition.y)));

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
			ant.Position = newPosition; // += ant.Velocity * (DeltaTime * Colony.SimulationSpeed);
			ant.FacingAngle = DirectionToAngle(ant.Velocity);
		}
	}

	#endregion
}