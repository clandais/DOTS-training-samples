using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace AutoFarmers.Systems.Jobs
{
	[BurstCompile]
	internal struct SpawnStoresJob : IJobParallelFor
	{
		public EntityCommandBuffer.ParallelWriter ECB;
		[NativeDisableParallelForRestriction] public NativeArray<bool> StoreTileBuffer;
		[ReadOnly] public Entity StorePrefab;
		[ReadOnly] public int MapSize;
		[ReadOnly] public int MaxStores;
		private int StoreCount;
		[ReadOnly] public Random Rng;
        

		public void Execute(int index)
		{
			if (index >= MapSize * MapSize)
			{
				return;
			}


			int x = Rng.NextInt(0, MapSize);
			int y = Rng.NextInt(0, MapSize);
			int idx = x * MapSize + y;

			if (StoreTileBuffer[idx] || StoreCount >= MaxStores)
			{
				StoreTileBuffer[idx] = false;
			}
			else
			{
				StoreTileBuffer[idx] = true;

				ECB.Instantiate(index, StorePrefab);
				ECB.SetComponent(index, StorePrefab, LocalTransform.FromPosition(new float3(x, 0f, y)));
				StoreCount++;
			}
		}
	}
}