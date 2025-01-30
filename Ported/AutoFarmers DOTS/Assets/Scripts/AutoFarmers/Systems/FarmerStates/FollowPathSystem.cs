using AutoFarmers.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AutoFarmers.Systems.FarmerStates
{
	public partial struct FollowPathSystem : ISystem
	{
		
		private EntityQuery _farmerQuery;
		
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
			_farmerQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAspect<FarmerAspect>()
				.WithAll<PathBufferElement>()
				.WithAll<Pathing>()
				.Build(ref state);
			
			state.RequireForUpdate(_farmerQuery);

		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
			
			
			var job = new FollowPath
			{
				Ecb = ecb.AsParallelWriter(),
				DeltaTime = SystemAPI.Time.DeltaTime,
			};
			
			state.Dependency = job.ScheduleParallel(_farmerQuery, state.Dependency);
		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{

		}
	}
	
	
	[WithAll(typeof(FarmerAspect))]
	[WithAll(typeof(PathBufferElement))]
	[WithAll(typeof(Pathing))]
	partial struct FollowPath : IJobEntity
	{
		
		[ReadOnly] public float DeltaTime;
		public EntityCommandBuffer.ParallelWriter Ecb;
		
		void Execute(FarmerAspect farmer,
			DynamicBuffer<PathBufferElement> buffer,
			Pathing pathing,
			[EntityIndexInQuery] int index)
		{
			var pos = farmer.Position;
			
			if (pathing.CurrentIndex >= buffer.Length)
			{
				Ecb.SetComponentEnabled<Pathing>(index, farmer.GetEntity(), false);
				Ecb.SetComponentEnabled<TargetComponent>(index, farmer.GetEntity(), false);
				return;
			}
			
			
			int currentIndex = pathing.CurrentIndex;
			
			var nextTarget = buffer[currentIndex].Value;
			var nextPos = new float2(nextTarget.x, nextTarget.x);
			
//			Debug.Log($"NextPos: {nextPos}");
			
			if (math.distance(pos, nextPos) < 0.1f)
			{
				Ecb.SetComponent(index, farmer.GetEntity(), new Pathing()
				{
					CurrentIndex = currentIndex + 1,
				});
			}
			else
			{
				var dir = math.normalize(nextPos - pos);
				farmer.Position += dir * DeltaTime;
			}
			
		}
	}
}