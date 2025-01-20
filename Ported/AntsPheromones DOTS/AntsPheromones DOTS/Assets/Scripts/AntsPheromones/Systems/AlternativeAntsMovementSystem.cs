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
	public partial struct AlternativeAntsMovementSystem : ISystem
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
			var colony = SystemAPI.GetSingleton<Colony>();
			Entity resourceEntity = SystemAPI.GetSingletonEntity<Resource>();
			var pheromonesBuffer = SystemAPI.GetSingletonBuffer<PheromoneBufferElement>();
			var resourcePosition = SystemAPI.GetComponentRO<Position>(resourceEntity);
			var quadTree = SystemAPI.GetSingleton<ObstacleQuadTree>();
			var movementJob = new AltAntsMovementSystemJob
			{
				Colony = colony,
				Rng = _rng,
				Obstacles = quadTree,
				ResourcePosition = resourcePosition.ValueRO.Value,
				DeltaTime = SystemAPI.Time.DeltaTime,
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

	[WithAll(typeof(AntAspect))]
	[BurstCompile]
	internal partial struct AltAntsMovementSystemJob : IJobEntity
	{
		[ReadOnly]
		public Colony Colony;
		[ReadOnly]
		public Random Rng;
		[ReadOnly]
		public float2 ResourcePosition;
		[ReadOnly]
		public ObstacleQuadTree Obstacles;
		[ReadOnly]
		public float DeltaTime;
		[ReadOnly]
		public DynamicBuffer<PheromoneBufferElement> PheromonesBuffer;
		#region Ignore

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
				if (testPosition.x < 0 || testPosition.x >= Colony.MapSize || testPosition.y < 0 || testPosition.y >= Colony.MapSize)
				{
				}
				else
				{
					int index = testPosition.x + testPosition.y * (int)Colony.MapSize;
					PheromoneBufferElement pheromoneBufferElement = PheromonesBuffer[index];
					output += math.normalizesafe( testPosition - ant.Position) * pheromoneBufferElement.Strength;
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
		///     Plot a line from start to end and add the positions to the ant
		/// </summary>
		/// <param name="ant"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		private void PlotLine(AntAspect ant,
			int2 start,
			int2 end)
		{
			int dx = math.abs(end.x - start.x);
			int dy = math.abs(end.y - start.y);
			int sx = start.x < end.x
				? 1
				: -1;
			int sy = start.y < end.y
				? 1
				: -1;
			int err = dx - dy;
			while (true)
			{
				if (start.x == end.x && start.y == end.y)
				{
					ant.AddPositionThisFrame(start);
					break;
				}

				int e2 = 2 * err;
				if (e2 > -dy)
				{
					err -= dy;
					start.x += sx;
				}

				if (e2 < dx)
				{
					err += dx;
					start.y += sy;
				}

				ant.AddPositionThisFrame(start);
			}
		}

		#endregion

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

		private float2 Normalize(float2 v)
		{
			float length = math.sqrt(v.x * v.x + v.y * v.y);
			return new float2(v.x / length,
				v.y / length);
		}

		private void Execute(AntAspect ant,
			[EntityIndexInQuery] int idx)
		{
			float targetSpeed = Colony.AntTargetSpeed;
			for (int _ = 0;
			     _ < Colony.SimulationSpeed;
			     _++)
			{
				float2 targetPosition = !ant.IsHoldingResource
					? ResourcePosition
					: new float2(Colony.MapSize * .5f,
						Colony.MapSize * .5f);
				float2 desiredVelocity = math.normalizesafe(targetPosition - ant.Position) * targetSpeed;
				float2 steering = math.normalizesafe(desiredVelocity) * Colony.GoalSteeringStrength;

				// apply random steering
				float randomSteering = Rng.NextFloat(-Colony.RandomSteering,
					Colony.RandomSteering);
				steering += AngleToDirection(ant.FacingAngle + randomSteering);
				steering += PheromoneSteering(ant) * Colony.PheromoneSteeringStrength;

				// apply wall "antenna" steering
				steering += WallSteering(ant,
					            steering) *
				            Colony.WallSteeringStrength;
				
				
				// check if there is a wall in front of the ant
				if (LineCast(ant,
					    math.normalizesafe(desiredVelocity),
					    Colony.WallSeekDistance,
					    out var hit))
				{
					float2 normal = math.normalizesafe(hit.point - ant.Position);
					steering -= normal * ( Colony.WallSteeringStrength * (1f / math.distancesq(hit.point, ant.Position)) ); //math.reflect( steering, normal ) * Colony.WallSteeringStrength;

					//steering = math.reflect(steering, normal);
				}

				// apply final steering
				steering = math.normalizesafe(steering) *
				           math.min(math.length(steering),
					           Colony.AntAcceleration);
				steering += ant.Velocity;
				steering = math.normalizesafe(steering) *
				           math.min(math.length(steering),
					           targetSpeed);
				
				
				
				float2 deltaPosition = steering * DeltaTime;
				float2 newPosition = ant.Position + deltaPosition;
				if (newPosition.x < 0 || newPosition.x >= Colony.MapSize)
					steering *= new float2(-1,
						1);
				if (newPosition.y < 0 || newPosition.y >= Colony.MapSize)
					steering *= new float2(1,
						-1);
				
				newPosition = ant.Position + steering * DeltaTime;
				ant.AddPositionThisFrame( new int2( (int)math.floor(newPosition.x), (int)math.floor(newPosition.y)));

				if (math.distancesq(ant.Position,
					    targetPosition) <
				    4f * 4f)
				{
					ant.IsHoldingResource = !ant.IsHoldingResource;
				}
				
				float excitement = ant.IsHoldingResource
					? 1f
					: .3f;
				excitement *= math.length(ant.Velocity) / targetSpeed;
				ant.Excitement = excitement;
				
				ant.Velocity = steering;
				ant.Position += ant.Velocity * DeltaTime;
				ant.FacingAngle = DirectionToAngle(ant.Velocity);
			}
		}
	}
}