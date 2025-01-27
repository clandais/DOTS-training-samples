using AutoFarmers.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace AutoFarmers.Systems.Jobs
{
	[WithAll(typeof(Ground))]
	[BurstCompile]
	internal partial struct SpawnGroundJob : IJobEntity
	{
		public EntityCommandBuffer.ParallelWriter ecb;
		[ReadOnly] public int MapSize;


		[BurstCompile]
		private void Execute([EntityIndexInQuery] int index, Entity entity, ref LocalTransform localTransform)
		{

			int x = index / MapSize;
			int y = index % MapSize;

			ecb.SetComponent(index, entity, LocalTransform.FromPosition(new float3(x, 0f, y)));
		}
	}
}