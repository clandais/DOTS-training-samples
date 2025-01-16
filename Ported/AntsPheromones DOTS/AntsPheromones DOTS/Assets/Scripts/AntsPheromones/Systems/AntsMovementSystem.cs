using System;
using AntsV2.Components;
using NativeTrees;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using AntAspect = AntsPheromones.Components.AntAspect;
using float2 = Unity.Mathematics.float2;
using Random = Unity.Mathematics.Random;

namespace AntsV2.Systems
{
	[UpdateAfter(typeof(AntsPheromones.Systems.PheromonesRenderingSystem))]
	//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
	public partial struct AntsMovementSystem : ISystem
	{
		private Random rng;
		private EntityQuery _antsQuery;

		//[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<Colony>();
			state.RequireForUpdate<Ant>();
			state.RequireForUpdate<Home>();
			state.RequireForUpdate<Resource>();
			state.RequireForUpdate<ObstacleQuadTree>();
			//		rng = Random.CreateFromIndex((uint)DateTime.Now.Millisecond);

			_antsQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAspect<AntAspect>().Build(ref state);

		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{

			rng = Random.CreateFromIndex(state.GlobalSystemVersion);
			var colony = SystemAPI.GetSingleton<Colony>();
			Entity resourceEntity = SystemAPI.GetSingletonEntity<Resource>();
			var pheromonesBuffer = SystemAPI.GetSingletonBuffer<PheromoneBufferElement>();
			var resourcePosition = SystemAPI.GetComponentRO<Position>(resourceEntity);
			var quadTree = SystemAPI.GetSingleton<ObstacleQuadTree>();

			var movementJob = new AntsMovementSystemJob
			{
				Colony = colony,
				Rng = rng,
				Obstacles = quadTree,
				ResourcePosition = resourcePosition.ValueRO.Value,
				DeltaTime = SystemAPI.Time.DeltaTime,
				PheromonesBuffer = pheromonesBuffer
			};

			state.Dependency = movementJob.ScheduleParallel(_antsQuery, state.Dependency);


		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{

		}
	}


	[WithAll(typeof(AntAspect))]
	// [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
	public partial struct AntsMovementSystemJob : IJobEntity
	{

		[ReadOnly] public Colony Colony;
		[ReadOnly] public Random Rng;
		[ReadOnly] public float2 ResourcePosition;
		[ReadOnly] public ObstacleQuadTree Obstacles;
		[ReadOnly] public float DeltaTime;
		[ReadOnly] public DynamicBuffer<PheromoneBufferElement> PheromonesBuffer;

		private float2 steering;
		private float targetSpeed;
		private float angle;


		private float PheromoneSteering(AntAspect ant)
		{
			float output = 0f;

			for (int i = -1; i <= 1; i += 2)
			{
				float a = angle + i * math.PI * .25f;
				int testX = (int)(ant.Position.x + math.cos(a) * Colony.PheromoneSeekDistance);
				int testY = (int)(ant.Position.y + math.sin(a) * Colony.PheromoneSeekDistance);


				if (testX < 0 || testX >= Colony.MapSize || testY < 0 || testY >= Colony.MapSize)
				{

				}
				else
				{
					int index = testX + testY * (int)Colony.MapSize;
					PheromoneBufferElement pheromoneBufferElement = PheromonesBuffer[index];

					output += pheromoneBufferElement.Strength * i;
				}
			}

			return math.sign(output);
		}


		private int WallSteering(AntAspect ant)
		{
			int output = 0;

			// cast rays in on the two sides of the ant
			for (int i = -1; i <= 1; i += 2)
			{
				float a = angle + i * math.PI * .25f;

				float2 testPosition = ant.Position + new float2(math.cos(a), math.sin(a));

				var seekDirection = new float2(math.cos(a), math.sin(a));

				var obstacles = new NativeList<float2>(Allocator.Temp);

				Obstacles.Value.RangeAABB(new AABB2D(
						testPosition + new float2(-Colony.WallSeekDistance, -Colony.WallSeekDistance),
						testPosition + new float2(Colony.WallSeekDistance, Colony.WallSeekDistance)),
					obstacles);

				int value = obstacles.Length;

				if (value > 0)
				{
					output -= i;
				}
			}


			return output;
		}

		private void Execute(AntAspect ant, [EntityIndexInQuery] int idx)
		{

			//if (ant.Position.x < 0 || ant.Position.x >= Colony.MapSize || ant.Position.y < 0 || ant.Position.y >= Colony.MapSize) return;

			steering = float2.zero;
			targetSpeed = Colony.AntTargetSpeed;
			angle = ant.FacingAngle + Rng.NextFloat(-Colony.RandomSteering, Colony.RandomSteering);

			float pheromoneSteering = PheromoneSteering(ant);
			int wallSteering = WallSteering(ant);

			angle += wallSteering * Colony.WallSteeringStrength;
			angle += pheromoneSteering * Colony.PheromoneSteeringStrength;

			targetSpeed *= 1f - (math.abs(pheromoneSteering) + math.abs(wallSteering)) / 3f;

			ant.Speed += (targetSpeed - ant.Speed) * Colony.AntAcceleration;

			float2 targetPosition = !ant.IsHoldingResource ? ResourcePosition : new float2(Colony.MapSize * .5f, Colony.MapSize * .5f);

			float2 direction = math.normalize(targetPosition - ant.Position);

			if (!Obstacles.Value.Raycast<RayAABBIntersecter<float2>>(new Ray2D(
					    ant.Position,
					    direction),
				    out _,
				    maxDistance: math.length(targetPosition - ant.Position)))
			{
				float targetAngle = math.atan2(targetPosition.y - ant.Position.y, targetPosition.x - ant.Position.x);

				if (targetAngle - angle > math.PI)
				{
					angle += math.PI * 2f;
				}

				else if (targetAngle - angle < -math.PI)
				{
					angle -= math.PI * 2f;
				}
				else
				{
					if (math.abs(targetAngle - angle) < math.PI * .5f)
						angle += (targetAngle - angle) * Colony.GoalSteeringStrength;
				}
			}


			if (math.lengthsq(ant.Position - targetPosition) < 4f * 4f)
			{
				ant.IsHoldingResource = !ant.IsHoldingResource;
				angle += math.PI;
			}

			float vx = math.cos(angle) * ant.Speed;
			float vy = math.sin(angle) * ant.Speed;
			float ovx = vx;
			float ovy = vy;


			if (ant.Position.x + vx < 0 || ant.Position.x + vx >= Colony.MapSize) vx *= -1;
			if (ant.Position.y + vy < 0 || ant.Position.y + vy >= Colony.MapSize) vy *= -1;

			float2 newPosition = ant.Position + new float2(vx, vy);

			float dx, dy, dist;

			var nearbyObstacles = new NativeList<float2>(Allocator.Temp);

			Obstacles.Value.RangeAABB(
				new AABB2D(
					newPosition + new float2(-Colony.WallNearbySeekRadius, -Colony.WallNearbySeekRadius),
					newPosition + new float2(Colony.WallNearbySeekRadius, Colony.WallNearbySeekRadius)),
				nearbyObstacles
				);

			if (!nearbyObstacles.IsEmpty)
			{
				foreach (float2 obstacle in nearbyObstacles)
				{
					dx = newPosition.x - obstacle.x;
					dy = newPosition.y - obstacle.y;
					float sqrDist = dx * dx + dy * dy;

					if (sqrDist < Colony.ObstacleRadius * Colony.ObstacleRadius)
					{
						dist = math.sqrt(sqrDist);
						dx /= dist;
						dy /= dist;
						newPosition.x = obstacle.x + dx * Colony.ObstacleRadius;
						newPosition.y = obstacle.y + dy * Colony.ObstacleRadius;

						vx -= dx * (dx * vx + dy * vy) * 1.5f;
						vy -= dy * (dx * vx + dy * vy) * 1.5f;
					}
				}
			}

			float inwardOrOutward = -Colony.OutwardStrength;
			float pushRadius = Colony.MapSize * .4f;

			if (ant.IsHoldingResource)
			{
				inwardOrOutward = Colony.InWardStrength;
				pushRadius = Colony.MapSize;
			}

			var colonyCenter = new float2(Colony.MapSize * .5f, Colony.MapSize * .5f);
			dx = colonyCenter.x - newPosition.x;
			dy = colonyCenter.y - newPosition.y;
			dist = math.sqrt(dx * dx + dy * dy);
			inwardOrOutward *= 1f - (float)math.clamp(dist / pushRadius, 0.0, 1.0);

			vx += dx / dist * inwardOrOutward;
			vy += dy / dist * inwardOrOutward;

			if (vx != ovx || vy != ovy)
			{
				angle = math.atan2(vy, vx);
			}

			float excitement = ant.IsHoldingResource ? 1f : .3f;
			excitement *= ant.Speed / Colony.AntTargetSpeed;

			ant.Excitement = excitement;
			ant.Position = newPosition;
			//ant.Direction = new float2(vx, vy);
			ant.FacingAngle = angle;
			//ant.Speed = math.length(new float2(vx, vy));

			// targetSpeed = ant.Speed + (targetSpeed - ant.Speed) * Colony.AntAcceleration;
			//
			// float randomAngle = ant.FacingAngle + Rng.NextFloat(-Colony.RandomSteering, Colony.RandomSteering);
			// steering += new float2(math.cos(randomAngle), math.sin(randomAngle)); // * Colony.RandomSteeringStrength;
			//
			//
			// // Pheromone steering
			// float2 pheromoneSteering = float2.zero;
			// for (int i = -1; i <= 1; i+=2)
			// {
			// 	float angle = ant.FacingAngle + i * math.PI / 4f;
			// 	
			// 	//int2 test = new int2(ant.Position.x + math.cos(angle) * 3f, ant.Position.y + math.sin(angle) * 3f);
			// 	int testX = (int)( ant.Position.x + ( math.cos(angle) * Colony.PheromoneSeekDistance ) );
			// 	int testY = (int)( ant.Position.y + (math.sin(angle)  *  Colony.PheromoneSeekDistance));
			//
			// 	if (testX < 0 || testX >= Colony.MapSize || testY < 0 || testY >= Colony.MapSize)
			// 	{
			// 		
			// 	}
			// 	else
			// 	{
			// 		int index = testX + testY * (int)Colony.MapSize;
			// 		PheromoneBufferElement pheromoneBufferElement = PheromonesBuffer[index];
			//
			// 		pheromoneSteering += new float2(math.cos(angle), math.sin(angle)) * pheromoneBufferElement.Strength;
			// 	}
			// 	
			// }
			//
			// steering += pheromoneSteering * Colony.PheromoneSteeringStrength;
			//
			//
			// // wall steering
			// float2 wallSteering = float2.zero;
			//
			// // cast rays in on the two sides of the ant
			// for (int i = -1; i <= 1; i+= 2)
			// {
			// 	float angle = ant.FacingAngle + i * math.PI / 4f;
			// 	
			// 	float2 seekDirection = new float2(math.cos(angle), math.sin(angle));
			//
			// 	if (Obstacles.Value.Raycast<RayAABBIntersecter<float2>>(new Ray2D(ant.Position, seekDirection),
			// 		    out var h,
			// 		    maxDistance: Colony.WallSeekDistance))
			// 	{
			// 		
			// 		float2 normal = math.normalize(h.point - ant.Position);
			// 		float2 reflect = math.reflect(seekDirection, normal);
			// 		wallSteering += reflect * (Colony.WallSteeringStrength * (1f / math.distance(h.point, ant.Position)));
			// 	}
			//
			// }
			//
			// steering += wallSteering;
			//
			//
			// float2 goalDirection = !ant.IsHoldingResource ? math.normalize(ResourcePosition - ant.Position) : math.normalize(new float2(Colony.MapSize * .5f, Colony.MapSize * .5f) - ant.Position);
			//
			// steering += goalDirection * Colony.GoalSteeringStrength;
			//
			// if (Obstacles.Value.Raycast<RayAABBIntersecter<float2>>(new Ray2D(ant.Position, math.normalize(steering)), out var h1))
			// {
			// 	float2 normal = math.normalize(h1.point - ant.Position);
			// 	float2 reflect = math.reflect( steering, normal);
			// 	steering += reflect * (Colony.WallSteeringStrength * (1f / math.distance(h1.point, ant.Position)));
			// }
			//
			// NativeList<float2> nearbyObstacles = new NativeList<float2>(Allocator.Temp);
			//
			//
			//
			// Obstacles.Value.RangeAABB(
			// 	new AABB2D( 
			// 				(ant.Position + steering - new float2(Colony.WallNearbySeekRadius, Colony.WallNearbySeekRadius)), 
			// 				(ant.Position + steering + new float2(Colony.WallNearbySeekRadius, Colony.WallNearbySeekRadius) ) ),
			// 	nearbyObstacles
			// 	);
			//
			// float2 nearbyWallSteering  = float2.zero;
			// if (!nearbyObstacles.IsEmpty)
			// {
			// 	foreach (float2 o in nearbyObstacles)
			// 	{
			// 		float2 dv = ant.Position - o;
			// 		float sqrDistance = dv.x * dv.x + dv.y * dv.y;
			//
			// 		if (sqrDistance < Colony.ObstacleRadius * Colony.ObstacleRadius)
			// 		{
			// 			float distance = math.sqrt(sqrDistance);
			// 			dv /= distance;
			//
			// 			nearbyWallSteering += dv * Colony.ObstacleRadius;
			// 		}
			// 	}
			// 	
			// 	steering += nearbyWallSteering * Colony.WallSteeringStrength;
			// }
			//
			// nearbyObstacles.Dispose();
			//


			// float2 direction = math.normalize(steering);
			// float2 targetPosition = ant.Position + direction * ant.Speed;
			//
			//
			// if (targetPosition.x < 0 || targetPosition.x > Colony.MapSize) direction.x *= -1;
			// if (targetPosition.y < 0 || targetPosition.y > Colony.MapSize) direction.y *= -1;
			//
			// ant.Direction = direction;
			// ant.Speed += (targetSpeed - ant.Speed) * Colony.AntAcceleration;
			// ant.Position += ant.Direction * ant.Speed * DeltaTime;

			//float excitement = ant.IsHoldingResource ? 1f : .3f;
			// excitement *= ant.Speed / Colony.AntTargetSpeed;
			//
			// ant.Excitement = excitement;
			//
			//
			// if (math.distance(ant.Position, ResourcePosition) < ant.Speed) ant.IsHoldingResource = true;
			// if (math.distance(ant.Position, new float2(Colony.MapSize * .5f, Colony.MapSize * .5f)) < ant.Speed) ant.IsHoldingResource = false;
			//
		}

	}


	internal struct RayAABBIntersecter<T> : IQuadtreeRayIntersecter<T>
	{
		public bool IntersectRay(in PrecomputedRay2D ray, T obj, AABB2D objBounds, out float distance)
		{
			bool intersects = objBounds.IntersectsRay(ray, out float2 point);

			if (intersects)
			{
				distance = math.length(ray.origin - point);
			}
			else
			{
				distance = 0f;
			}

			return intersects;
		}
	}


	internal struct RangeAABBUniqueVisitor<T> : IQuadtreeRangeVisitor<T> where T : unmanaged, IEquatable<T>
	{

		public NativeParallelHashSet<T> Results;

		public bool OnVisit(T obj, AABB2D objBounds, AABB2D queryRange)
		{
			if (objBounds.Overlaps(queryRange))
				Results.Add(obj);

			return true;
		}
	}


	internal static class QuadtreeExtensions
	{
		public static void RangeAABBUnique<T>(this NativeQuadtree<T> quadtree, AABB2D range, NativeParallelHashSet<T> results) where T : unmanaged, IEquatable<T>
		{
			var visitor = new RangeAABBUniqueVisitor<T>
			{
				Results = results
			};

			quadtree.Range(range, ref visitor);
		}
	}
}