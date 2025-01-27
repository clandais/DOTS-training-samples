using AutoFarmers.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace AutoFarmers.Systems.Jobs
{
	[BurstCompile]
	struct SpawnRocksJob : IJob
	{

		[ReadOnly] public FarmCfg FarmCfg;
		[ReadOnly] public Farm Farm;
		[ReadOnly] public Random Rng;
		[ReadOnly] public DynamicBuffer<StoreTile> StoreTileBuffer;
		public EntityCommandBuffer ECB;
		public NativeArray<bool> RockTileBuffer;
		public NativeList<RockedTile> RockBuffer;
        
		public void Execute()
		{
			for (int i = 0; i < Farm.RockSpawnAttempts; i++)
			{
				int width = Rng.NextInt(0, 4);
				int height = Rng.NextInt(0, 4);
				int rockX = Rng.NextInt(0, Farm.MapSize - width);
				int rockY = Rng.NextInt(0, Farm.MapSize - height);
				RectInt rect = new RectInt(rockX,rockY,width,height);
				bool blocked = false;
                
				for (int x = rockX; x <= rockX + width; x++)
				{
					for (int y = rockY; y <= rockY + height; y++)
					{
						if (StoreTileBuffer[x * Farm.MapSize + y].IsOccupied || RockTileBuffer[x * Farm.MapSize + y])
						{
							blocked = true;
						}
					}
				}
                
				if (!blocked)
				{
					var rockEntity = ECB.Instantiate(FarmCfg.RockPrefab);
                    
					int health = (rect.width + 1) * (rect.height + 1) * 15;
                    
					
					var rock = new Rock
					{
						Rect = rect,
						StartHealth = health,
						Health = health,
					};
					
					ECB.SetComponent(rockEntity, rock);
                    
                    
					float depth = Rng.NextFloat(.4f, .8f);
                    
					ECB.AddComponent(rockEntity, new PostTransformMatrix()
					{
						Value = float4x4.TRS(
							new float3(rect.center.x+.5f, depth*.5f, rect.center.y+.5f),
							quaternion.identity, 
							new float3(rect.width+.5f, depth, rect.height+.5f))
					});

					for (int x = rockX; x <= rockX+width; x++)
					{
						for (int y = rockY; y <= rockY+height; y++)
						{
							RockTileBuffer[x * Farm.MapSize + y] = true;
						}
					}
					
					
					RockBuffer.Add( new RockedTile { Rock = rock, IsOccupied = true});
				}
			}
		}
	}
}