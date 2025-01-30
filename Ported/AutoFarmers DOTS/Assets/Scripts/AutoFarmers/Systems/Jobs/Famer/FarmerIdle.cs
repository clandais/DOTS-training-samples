using System;
using AutoFarmers.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace AutoFarmers.Systems.Jobs.Famer
{
	[WithAll(typeof(FarmerAspect))]
//	[BurstCompile]
	public partial struct FarmerIdle : IJobEntity
	{
		
		[ReadOnly] public Random Rng;
		public EntityCommandBuffer.ParallelWriter CommandBuffer;
		
		public void Execute(FarmerAspect farmer, [EntityIndexInQuery] int index)
		{

			var lastIntention = farmer.GetIntention();
			var intention = farmer.PickNewIntention(Rng);
			
		//	CommandBuffer.SetComponentEnabled<TargetComponent>(index, farmer.GetEntity(), false);

			
			Debug.Log($"Farmer {index} changed intention from {lastIntention} to {intention}");
			
			switch (intention)
			{

				case Intention.None:
					CommandBuffer.SetComponentEnabled<NoneGoal>(index,farmer.GetEntity(), false);
					break;
				case Intention.SmashRocks:
					CommandBuffer.SetComponentEnabled<SmashRocksGoal>(index,farmer.GetEntity(), true);
					break;
				case Intention.TillGround:
					CommandBuffer.SetComponentEnabled<TillGroundGoal>(index,farmer.GetEntity(), true);
					break;
				case Intention.PlantSeeds:
					CommandBuffer.SetComponentEnabled<PlantSeedsGoal>(index, farmer.GetEntity(), true);
					break;
				case Intention.SellPlants:
					CommandBuffer.SetComponentEnabled<SellPlantsGoal>(index, farmer.GetEntity(), true);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			switch (lastIntention)
			{

				case Intention.None:
					CommandBuffer.SetComponentEnabled<NoneGoal>(index, farmer.GetEntity(), false);
					break;
				case Intention.SmashRocks:
					CommandBuffer.SetComponentEnabled<SmashRocksGoal>(index, farmer.GetEntity(), false);
					break;
				case Intention.TillGround:
					CommandBuffer.SetComponentEnabled<TillGroundGoal>(index, farmer.GetEntity(), false);
					break;
				case Intention.PlantSeeds:
					CommandBuffer.SetComponentEnabled<PlantSeedsGoal>(index, farmer.GetEntity(), false);
					break;
				case Intention.SellPlants:
					CommandBuffer.SetComponentEnabled<SellPlantsGoal>(index, farmer.GetEntity(), false);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}