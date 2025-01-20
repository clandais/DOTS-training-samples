using AutoFarmers.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace AutoFarmers
{

	public enum Intention
	{
		None,
		SmashRocks,
		TillGround,
		PlantSeeds,
		SellPlants
	}

	
	// public struct IntentionComponent : IComponentData
	// {
	// 	public Intention Value;
	// }
	//
	// public struct TargetComponent : IComponentData
	// {
	// 	public int2 Value;
	// }
	
	[BurstCompile]
	public readonly partial struct FarmerAspect : IAspect
	{
		private readonly Entity Entity;
		readonly RefRW<Farmer> Farmer;
		readonly RefRW<LocalTransform> LocalTransform;
		readonly RefRW<IntentionComponent> IntentionComponent; 
		// readonly RefRW<IntentionComponent> IntentionComponent;
		// readonly RefRW<TargetComponent> TargetComponent;
		
		// private Intention Intention
		// {
		// 	get => IntentionComponent.ValueRO.Value;
		// 	set => IntentionComponent.ValueRW.Value = value;
		// }

		
		public Entity GetEntity() => Entity;
		
		public float2 Position
		{
			get => LocalTransform.ValueRO.Position.xz;
			set => LocalTransform.ValueRW.Position.xz = value;
		}
		
		// public int2 Target
		// {
		// 	get => TargetComponent.ValueRO.Value;
		// 	set => TargetComponent.ValueRW.Value = value;
		// }
		//
		// public Intention GetIntention()
		// {
		// 	return IntentionComponent.ValueRO.Value;
		// }

		public Intention PickNewIntention(Random rng)
		{
			int rand = rng.NextInt(0, 4);
			var intention = Intention.None;
			
			switch (rand)
			{
				case 0:
					intention = Intention.SmashRocks;
					break;
				case 1:
					intention = Intention.TillGround;
					break;
				case 2:
					intention = Intention.PlantSeeds;
					break;
				case 3:
					intention = Intention.SellPlants;
					break;
			}
			
			IntentionComponent.ValueRW.Value = intention;
			
			return intention;
		}

		[BurstCompile]
		public void Think(Random rng)
		{
			// if (GetIntention() == Intention.None)
			// {
			// 	PickNewIntention(rng);
			// }
		}

		[BurstCompile]
		public void Update(DynamicBuffer<RockedTile> rockBuffer, Random rng)
		{
		}

		public void MoveTowards(float2 rectCenter)
		{
			float2 direction = math.normalizesafe(rectCenter - Position);
			Position += direction * 0.1f;
		}

		public void FindPathToRock(Rock nearest,  DynamicBuffer<RockedTile> buffer, Farm farm)
		{
			
			NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
			int[] dirsX = new int[] { 1,-1,0,0 };
			int[] dirsY = new int[] { 0,0,1,-1 };
			
			
			int nearestX = 0;
			int nearestY = 0;
			float nearestDist = float.MaxValue;

			int x, y;
			
			for (x = nearest.Rect.x; x <= nearest.Rect.x + nearest.Rect.width; x++)
			{
				for (y = nearest.Rect.y; y <= nearest.Rect.y + nearest.Rect.height; y++)
				{
					
					if (math.distance(Position, new float2(x, y)) < nearestDist)
					{
						nearestX = x;
						nearestY = y;
						nearestDist = math.distance(Position, new float2(x, y));
					}
				}
			}
			
			x = nearestX;
			y = nearestY;
			path.Add(new int2(x, y));

			int dist = int.MaxValue;
			while (dist > 0)
			{
				int minNextDist = int.MaxValue;
				int bestNewX = x;
				int bestNewY = y;

				for (int i = 0; i < dirsX.Length; i++)
				{
					int x2 = x + dirsX[i];
					int y2 = y + dirsY[i];
					
					if (x2 < 0 || y2 < 0 || x2 >= farm.MapSize || y2 >= farm.MapSize)
					{
						continue;
					}
					
				}
			}


		}
		
		private static int Hash(int x,int y, int mapWidth) {
			return y * mapWidth + x;
		}
		
		private static void UnHash(int hash, out int x, out int y, int mapWidth) {
			x = hash % mapWidth;
			y = hash / mapWidth;
		}

		public Intention GetIntention()
		{
			return IntentionComponent.ValueRO.Value;
		}
	}
}