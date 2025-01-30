using AutoFarmers.Authoring;
using AutoFarmers.Systems.Jobs.Famer;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace AutoFarmers.Systems.FarmerStates
{
	//[UpdateAfter(typeof(PathFindingInitialisationSystem))]
	public partial struct FarmerNoneIntentionUpdateSystem : ISystem
	{

		private EntityQuery _farmerQuery;
		private Random _rng;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
			state.RequireForUpdate<Farmer>();
			state.RequireForUpdate<Farm>();

			_farmerQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAspect<FarmerAspect>()
				.WithAll<NoneGoal>()
				.Build(ref state);
			
			_rng = Random.CreateFromIndex(state.GlobalSystemVersion);
			state.RequireForUpdate(_farmerQuery);

		}

	//	[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
			EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);


			var job = new FarmerIdle
			{
				Rng = _rng,
				CommandBuffer = ecb.AsParallelWriter()
			};

			state.Dependency = job.ScheduleParallel(_farmerQuery, state.Dependency);

		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{

		}
	}
}