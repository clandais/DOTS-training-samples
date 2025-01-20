// using System;
// using AntsPheromones.Components;
// using NativeTrees;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using UnityEngine;
// using AntAspect = AntsPheromones.Components.AntAspect;
// using float2 = Unity.Mathematics.float2;
// using Random = Unity.Mathematics.Random;
//
// namespace AntsPheromones.Systems
// {
// 	[UpdateAfter(typeof(AntsWallRaycastSteeringSystem))]
// 	public partial struct AntsMovementSystem : ISystem
// 	{
// 		private Random _rng;
// 		private EntityQuery _antsQuery;
//
// 		[BurstCompile]
// 		public void OnCreate(ref SystemState state)
// 		{
// 			
// 			state.RequireForUpdate<Colony>();
// 			state.RequireForUpdate<Ant>();
// 			state.RequireForUpdate<Home>();
// 			state.RequireForUpdate<Resource>();
// 			state.RequireForUpdate<ObstacleQuadTree>();
//
// 			_antsQuery = new EntityQueryBuilder(Allocator.Temp)
// 				.WithAspect<AntAspect>().Build(ref state);
//
// 			
// 			
// 		}
//
// 		[BurstCompile]
// 		public void OnUpdate(ref SystemState state)
// 		{
// 			
//
// 			_rng = Random.CreateFromIndex(state.GlobalSystemVersion);
// 			var colony = SystemAPI.GetSingleton<Colony>();
// 			Entity resourceEntity = SystemAPI.GetSingletonEntity<Resource>();
// 			var pheromonesBuffer = SystemAPI.GetSingletonBuffer<PheromoneBufferElement>();
// 			var resourcePosition = SystemAPI.GetComponentRO<Position>(resourceEntity);
// 			var quadTree = SystemAPI.GetSingleton<ObstacleQuadTree>();
//
// 			var movementJob = new AntsMovementSystemJob
// 			{
// 				Colony = colony,
// 				Rng = _rng,
// 				Obstacles = quadTree,
// 				ResourcePosition = resourcePosition.ValueRO.Value,
// 				DeltaTime = SystemAPI.Time.DeltaTime,
// 				PheromonesBuffer = pheromonesBuffer
// 			};
//
// 			state.Dependency = movementJob.ScheduleParallel(_antsQuery, state.Dependency);
//
//
// 		}
//
// 		[BurstCompile]
// 		public void OnDestroy(ref SystemState state)
// 		{
//
// 		}
// 	}
//
//
// 	#region Movement Job
//
// 	[WithAll(typeof(AntAspect))]
// 	[BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
// 	public partial struct AntsMovementSystemJob : IJobEntity
// 	{
//
// 		[ReadOnly] public Colony Colony;
// 		[ReadOnly] public Random Rng;
// 		[ReadOnly] public float2 ResourcePosition;
// 		[ReadOnly] public ObstacleQuadTree Obstacles;
// 		[ReadOnly] public float DeltaTime;
// 		[ReadOnly] public DynamicBuffer<PheromoneBufferElement> PheromonesBuffer;
//
// 		// private float _targetSpeed;
// 		// private float _angle;
// 		//
// 		//
// 		// private float PheromoneSteering(AntAspect ant)
// 		// {
// 		// 	float output = 0f;
// 		//
// 		// 	for (int i = -1; i <= 1; i += 2)
// 		// 	{
// 		// 		float a = _angle + i * math.PI * .25f;
// 		// 		int testX = (int)(ant.Position.x + math.cos(a) * Colony.PheromoneSeekDistance);
// 		// 		int testY = (int)(ant.Position.y + math.sin(a) * Colony.PheromoneSeekDistance);
// 		//
// 		//
// 		// 		if (testX < 0 || testX >= Colony.MapSize || testY < 0 || testY >= Colony.MapSize)
// 		// 		{
// 		//
// 		// 		}
// 		// 		else
// 		// 		{
// 		// 			int index = testX + testY * (int)Colony.MapSize;
// 		// 			PheromoneBufferElement pheromoneBufferElement = PheromonesBuffer[index];
// 		//
// 		// 			output += pheromoneBufferElement.Strength * i;
// 		// 		}
// 		// 	}
// 		//
// 		// 	return math.sign(output);
// 		// }
// 		//
// 		//
// 		// private int WallSteering(AntAspect ant)
// 		// {
// 		// 	int output = 0;
// 		//
// 		// 	// cast rays in on the two sides of the ant
// 		// 	for (int i = -1; i <= 1; i += 2)
// 		// 	{
// 		// 		float a = _angle + i * math.PI * .25f;
// 		//
// 		// 		float2 testPosition = ant.Position + new float2(math.cos(a), math.sin(a));
// 		//
// 		// 		var seekDirection = new float2(math.cos(a), math.sin(a));
// 		//
// 		// 		var obstacles = new NativeList<float2>(Allocator.Temp);
// 		//
// 		// 		Obstacles.Value.RangeAABB(new AABB2D(
// 		// 				testPosition + new float2(-Colony.WallSeekDistance, -Colony.WallSeekDistance),
// 		// 				testPosition + new float2(Colony.WallSeekDistance, Colony.WallSeekDistance)),
// 		// 			obstacles);
// 		//
// 		// 		int value = obstacles.Length;
// 		//
// 		// 		if (value > 0)
// 		// 		{
// 		// 			output -= i;
// 		// 		}
// 		// 	}
// 		//
// 		//
// 		// 	return output;
// 		// }
//
// 		private void Execute(AntAspect ant, [EntityIndexInQuery] int idx)
// 		{
//
// 			// float pheromoneSteering = ant.PheromoneSteeringValue;
// 			// float wallSteering = ant.WallSteeringValue;
// 			//
// 			//
// 			// ant.FacingAngle += (ant.WallSteeringValue * Colony.WallSteeringStrength) + ( ant.PheromoneSteeringValue * Colony.PheromoneSteeringStrength);
// 			//
// 			//
// 			// float targetSpeed = Colony.AntTargetSpeed;
// 			// targetSpeed *= 1f - (math.abs(pheromoneSteering) + math.abs(wallSteering)) / 3f;
// 			// targetSpeed += (targetSpeed - ant.Speed ) * Colony.AntAcceleration;
// 			//
// 			//
// 			// float2 targetPosition = !ant.IsHoldingResource ? ResourcePosition : new float2(Colony.MapSize * .5f, Colony.MapSize * .5f);
// 			// float2 direction = math.normalize(targetPosition - ant.Position);
// 			
// 			if (math.lengthsq(ant.Position - ant.TargetPosition) < 4f * 4f)
// 			{
// 				ant.IsHoldingResource = !ant.IsHoldingResource;
// 				ant.FacingAngle += math.PI;
// 			}
// 			
// 			float excitement = ant.IsHoldingResource ? 1f : .1f;
// 			excitement *= (ant.Speed / (Colony.AntTargetSpeed ));
// 			
// 			
// 			float2 direction = math.normalize(ant.TargetPosition - ant.Position);
// 			
// 			ant.Excitement = excitement;
// 			ant.Position += (direction * ant.Speed * Colony.SimulationSpeed); //ant.TargetPosition; //+= (direction * Colony.SimulationSpeed * ant.Speed * DeltaTime);
// 			//ant.FacingAngle = math.atan2(direction.y, direction.x);
//
// 			// _targetSpeed = Colony.AntTargetSpeed * Colony.SimulationSpeed;
// 			// float randomSteering = Colony.RandomSteering * Colony.SimulationSpeed;
// 			// _angle = ant.FacingAngle + (Rng.NextFloat(-randomSteering, randomSteering));
// 			//
// 			// float pheromoneSteering = PheromoneSteering(ant);
// 			// int wallSteering = WallSteering(ant);
// 			//
// 			// _angle += wallSteering * Colony.WallSteeringStrength;
// 			// _angle += pheromoneSteering * Colony.PheromoneSteeringStrength;
// 			//
// 			// _targetSpeed *= 1f - (math.abs(pheromoneSteering) + math.abs(wallSteering)) / 3f;
// 			//
// 			// ant.Speed += (_targetSpeed - ant.Speed ) * Colony.AntAcceleration;
// 			//
// 			// float2 targetPosition = !ant.IsHoldingResource ? ResourcePosition : new float2(Colony.MapSize * .5f, Colony.MapSize * .5f);
// 			//
// 			// float2 direction = math.normalize(targetPosition - ant.Position);
// 			//
// 			// if (!Obstacles.Value.Raycast<RayAABBIntersecter<float2>>(new Ray2D(
// 			// 		    ant.Position,
// 			// 		    direction),
// 			// 	    out _,
// 			// 	    maxDistance: math.length(targetPosition - ant.Position)))
// 			// {
// 			// 	float targetAngle = math.atan2(targetPosition.y - ant.Position.y, targetPosition.x - ant.Position.x);
// 			//
// 			// 	if (targetAngle - _angle > math.PI)
// 			// 	{
// 			// 		_angle += math.PI * 2f;
// 			// 	}
// 			//
// 			// 	else if (targetAngle - _angle < -math.PI)
// 			// 	{
// 			// 		_angle -= math.PI * 2f;
// 			// 	}
// 			// 	else
// 			// 	{
// 			// 		if (math.abs(targetAngle - _angle) < math.PI * .5f)
// 			// 			_angle += (targetAngle - _angle) * Colony.GoalSteeringStrength;
// 			// 	}
// 			// }
// 			//
// 			//
// 			// if (math.lengthsq(ant.Position - targetPosition) < 4f * 4f)
// 			// {
// 			// 	ant.IsHoldingResource = !ant.IsHoldingResource;
// 			// 	_angle += math.PI;
// 			// }
// 			//
// 			// float vx = math.cos(_angle) * ant.Speed;
// 			// float vy = math.sin(_angle) * ant.Speed;
// 			// float ovx = vx;
// 			// float ovy = vy;
// 			//
// 			//
// 			// if (ant.Position.x + vx < 0 || ant.Position.x + vx >= Colony.MapSize) vx *= -1;
// 			// if (ant.Position.y + vy < 0 || ant.Position.y + vy >= Colony.MapSize) vy *= -1;
// 			//
// 			// float2 newPosition = ant.Position + new float2(vx, vy);
// 			//
// 			// float dx, dy, dist;
// 			//
// 			// var nearbyObstacles = new NativeList<float2>(Allocator.Temp);
// 			//
// 			// Obstacles.Value.RangeAABB(
// 			// 	new AABB2D(
// 			// 		newPosition + new float2(-Colony.WallNearbySeekRadius, -Colony.WallNearbySeekRadius),
// 			// 		newPosition + new float2(Colony.WallNearbySeekRadius, Colony.WallNearbySeekRadius)),
// 			// 	nearbyObstacles
// 			// 	);
// 			//
// 			// if (!nearbyObstacles.IsEmpty)
// 			// {
// 			// 	foreach (float2 obstacle in nearbyObstacles)
// 			// 	{
// 			// 		dx = newPosition.x - obstacle.x;
// 			// 		dy = newPosition.y - obstacle.y;
// 			// 		float sqrDist = dx * dx + dy * dy;
// 			//
// 			// 		if (sqrDist < Colony.ObstacleRadius * Colony.ObstacleRadius)
// 			// 		{
// 			// 			dist = math.sqrt(sqrDist);
// 			// 			dx /= dist;
// 			// 			dy /= dist;
// 			// 			newPosition.x = obstacle.x + dx * Colony.ObstacleRadius;
// 			// 			newPosition.y = obstacle.y + dy * Colony.ObstacleRadius;
// 			//
// 			// 			vx -= dx * (dx * vx + dy * vy) * 1.5f;
// 			// 			vy -= dy * (dx * vx + dy * vy) * 1.5f;
// 			// 		}
// 			// 	}
// 			// }
// 			//
// 			// float inwardOrOutward = -Colony.OutwardStrength;
// 			// float pushRadius = Colony.MapSize * .4f;
// 			//
// 			// if (ant.IsHoldingResource)
// 			// {
// 			// 	inwardOrOutward = Colony.InWardStrength;
// 			// 	pushRadius = Colony.MapSize;
// 			// }
// 			//
// 			// var colonyCenter = new float2(Colony.MapSize * .5f, Colony.MapSize * .5f);
// 			// dx = colonyCenter.x - newPosition.x;
// 			// dy = colonyCenter.y - newPosition.y;
// 			// dist = math.sqrt(dx * dx + dy * dy);
// 			// inwardOrOutward *= 1f - (float)math.clamp(dist / pushRadius, 0.0, 1.0);
// 			//
// 			// vx += dx / dist * inwardOrOutward;
// 			// vy += dy / dist * inwardOrOutward;
// 			//
// 			// if (Math.Abs(vx - ovx) > math.EPSILON || Math.Abs(vy - ovy) > math.EPSILON)
// 			// {
// 			// 	_angle = math.atan2(vy, vx);
// 			// }
// 			//
// 			// // float e = 1f -  math.clamp((math.length(targetPosition - ant.Position) / (Colony.MapSize * 1.2f)), 0f, 1f);
// 			// float excitement = ant.IsHoldingResource ? 1f : .1f;
// 			// excitement *= (ant.Speed / (Colony.AntTargetSpeed * Colony.SimulationSpeed ));
// 			//
// 			// ant.Excitement = excitement;
// 			// ant.Position = newPosition;
// 			// ant.FacingAngle = _angle;
// 		}
//
// 	}
//
// 	#endregion
//
//
// 	internal struct RayAABBIntersecter<T> : IQuadtreeRayIntersecter<T>
// 	{
// 		public bool IntersectRay(in PrecomputedRay2D ray, T obj, AABB2D objBounds, out float distance)
// 		{
// 			bool intersects = objBounds.IntersectsRay(ray, out float2 point);
//
// 			if (intersects)
// 			{
// 				distance = math.length(ray.origin - point);
// 			}
// 			else
// 			{
// 				distance = 0f;
// 			}
//
// 			return intersects;
// 		}
// 	}
//
//
// 	internal struct RangeAABBUniqueVisitor<T> : IQuadtreeRangeVisitor<T> where T : unmanaged, IEquatable<T>
// 	{
//
// 		public NativeParallelHashSet<T> Results;
//
// 		public bool OnVisit(T obj, AABB2D objBounds, AABB2D queryRange)
// 		{
// 			if (objBounds.Overlaps(queryRange))
// 				Results.Add(obj);
//
// 			return true;
// 		}
// 	}
//
//
// 	internal static class QuadtreeExtensions
// 	{
// 		public static void RangeAABBUnique<T>(this NativeQuadtree<T> quadtree, AABB2D range, NativeParallelHashSet<T> results) where T : unmanaged, IEquatable<T>
// 		{
// 			var visitor = new RangeAABBUniqueVisitor<T>
// 			{
// 				Results = results
// 			};
//
// 			quadtree.Range(range, ref visitor);
// 		}
// 	}
// }