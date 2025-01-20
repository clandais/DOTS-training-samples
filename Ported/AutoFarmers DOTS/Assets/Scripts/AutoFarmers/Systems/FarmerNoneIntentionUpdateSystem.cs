using AutoFarmers.Authoring;
using AutoFarmers.Systems.Jobs.Famer;
using NativeTrees;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace AutoFarmers.Systems
{
	[UpdateAfter(typeof(PathFindingInitialisationSystem))]
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
				.WithAll<NoneGoal>()
				.WithAspect<FarmerAspect>()
				.Build(ref state);
			
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
			
			_rng = Random.CreateFromIndex(state.GlobalSystemVersion);
			
			var job = new FarmerIdle()
			{
				Rng = _rng,
				CommandBuffer = ecb.AsParallelWriter(),
			};
			
			state.Dependency = job.ScheduleParallel(_farmerQuery, state.Dependency);

		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{

		}
	}
}