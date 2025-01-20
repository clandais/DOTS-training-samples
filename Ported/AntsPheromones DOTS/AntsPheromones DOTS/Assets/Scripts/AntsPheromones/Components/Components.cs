using System;
using NativeTrees;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AntsPheromones.Components
{

	public struct Ant : IComponentData
	{
		public float WallSteering;
		public float PheromoneSteering;
		public float ResourceSteering;
		public bool HasResource;
		public float Excitement;
	}

	public struct Position : IComponentData
	{
		public float2 Value;
	}

	public struct Velocity : IComponentData
	{
		public float2 Value;
	}
	
	public struct Acceleration : IComponentData
	{
		public float2 Value;
	}

	public struct Direction : IComponentData
	{
		public float2 Value;
	}

	public struct Speed : IComponentData
	{
		public float Value;
	}

	public struct Obstacle : IComponentData
	{
		public float2 Value;
	}

	public struct Home : IComponentData
	{

	}

	public struct PheromoneSteering : IComponentData
	{
		public float Value;
	}
	
	public struct NearbyWallSteering : IComponentData
	{
		public float Value;
	}

	public struct TargetPosition : IComponentData
	{
		public float2 Value;
	}
	
	public struct PositionsThisFrame : IBufferElementData
	{
		public int2 Value;
		//public NativeList<int2> Value;
	}
	
	
	[Serializable]
	public struct Resource : IComponentData
	{
	}

	[ChunkSerializable]
	public struct ObstacleQuadTree : IComponentData
	{
		public NativeQuadtree<float2> Value;
	}

	public struct PheromoneBufferElement : IBufferElementData
	{
		public float Strength;
	}

	public struct Pheromone : IComponentData
	{
		public int Index;
	}


	[Serializable]
	public struct Colony : IComponentData
	{
		public float MapSize;

		[Header("Prefabs")]
		public Entity HomePrefab;
		public Entity ResourcePrefab;
		public Entity AntPrefab;
		public Entity ObstaclePrefab;
		public Entity PheromonePrefab;

		[Header("Quad Tree")]
		public int ObjectsPerNode;
		
		[Range(1, 15)] public int MaxDepth;

		[Header("Obstacles")]
		public float ObstacleFill;
		public float ObstacleRadius;
		public int RingCount;
		public int AntCount;

		[Header("Ants")]
		public float AntTargetSpeed;
		public float AntAcceleration;
		public float AntScale;
		
		[Header("Steering")]
		public float RandomSteering;

		public float GoalSteeringStrength;

		public float WallNearbySeekRadius;
		public float WallSeekDistance;

		public float WallSteeringStrength;

		public float PheromoneSeekDistance;
		public float PheromoneSteeringStrength;

		public float OutwardStrength;
		public float InWardStrength;

		public float TrailAddSpeed;
		public float TrailDecaySpeed;


		public float SimulationSpeed;
	}
}