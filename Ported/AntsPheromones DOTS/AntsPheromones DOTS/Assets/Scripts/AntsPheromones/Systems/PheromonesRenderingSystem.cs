using AntsPheromones.Authoring;
using AntsV2.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;
using AntAspect = AntsPheromones.Components.AntAspect;

namespace AntsPheromones.Systems
{
	[RequireMatchingQueriesForUpdate]
	[UpdateAfter(typeof(SpawnSystem))]
	public partial class PheromonesRenderingSystem : SystemBase
	{
		private bool _initialized;

		protected override void OnUpdate()
		{

			if (!_initialized)
			{
				_initialized = true;

				var colony = SystemAPI.GetSingleton<Colony>();

				Entity singleton = SystemAPI.GetSingletonEntity<PheromoneSingleton>();
				var managedPheromonesCfg = EntityManager.GetComponentObject<ManagedPheromoneComponent>(singleton);

				var filterSettings = RenderFilterSettings.Default;
				filterSettings.ShadowCastingMode = ShadowCastingMode.Off;
				filterSettings.ReceiveShadows = false;

				var renderMeshArray = new RenderMeshArray(new[] { managedPheromonesCfg.Material }, new[] { managedPheromonesCfg.Mesh });
				var renderMeshDescription = new RenderMeshDescription
				{
					FilterSettings = filterSettings,
					LightProbeUsage = LightProbeUsage.Off
				};

				Entity prototype = EntityManager.CreateEntity();
				RenderMeshUtility.AddComponents(
					prototype,
					EntityManager,
					renderMeshDescription,
					renderMeshArray,
					MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

				EntityManager.AddComponentData(prototype, new URPMaterialPropertyBaseColor());

				EntityManager.AddComponentData(prototype, new Pheromone());

				var bounds = new RenderBounds
				{
					Value = managedPheromonesCfg.Mesh.bounds.ToAABB()
				};

				var ecbJob = new EntityCommandBuffer(Allocator.TempJob);

				var pheromonesBuffer = SystemAPI.GetSingletonBuffer<PheromoneBufferElement>();

				var spawnJob = new SpawnJob
				{
					MapSize = (int)colony.MapSize,
					Prototype = prototype,
					Ecb = ecbJob.AsParallelWriter(),
					MeshBounds = bounds
				};

				JobHandle spawnHandle = spawnJob.Schedule(pheromonesBuffer.Length, 128);
				spawnHandle.Complete();

				ecbJob.Playback(EntityManager);
				ecbJob.Dispose();
				EntityManager.DestroyEntity(prototype);
				return;
			}


			{

				float dt = SystemAPI.Time.DeltaTime;
				var pheromonesBuffer = SystemAPI.GetSingletonBuffer<PheromoneBufferElement>();
				var colony = SystemAPI.GetSingleton<Colony>();

				foreach (AntAspect ant in SystemAPI.Query<AntAspect>())
				{
					int x = (int)math.floor(ant.Position.x);
					int y = (int)math.floor(ant.Position.y);

					if (x < 0 || x >= colony.MapSize || y < 0 || y >= colony.MapSize) continue;

					int index = x + y * (int)colony.MapSize;
					PheromoneBufferElement pheromoneBufferElement = pheromonesBuffer[index];

					float pheromoneStrength = pheromoneBufferElement.Strength;
					pheromoneStrength += colony.TrailAddSpeed * ant.Excitement * dt * (1f - pheromoneBufferElement.Strength);


					if (pheromoneStrength > 1f) pheromoneStrength = 1f;

					pheromoneStrength *= colony.TrailDecaySpeed;

					pheromoneBufferElement.Strength = pheromoneStrength;
					pheromonesBuffer[index] = pheromoneBufferElement;

				}


				var materialJob = new MaterialUpdateJob
				{
					PheromonesBuffer = pheromonesBuffer
				};

				Dependency = materialJob.ScheduleParallel(Dependency);

				// foreach (var (materialColor, pheromone)
				//          in SystemAPI.Query<RefRW<URPMaterialPropertyBaseColor>, RefRO<Pheromone>>())
				// {
				// 	var pheromoneBufferElement = pheromonesBuffer[pheromone.ValueRO.Index];
				// 	materialColor.ValueRW.Value = new float4(pheromoneBufferElement.Strength, 0f, 0f, 1f);
				// }
			}

		}
	}

	[GenerateTestsForBurstCompatibility]
	public struct SpawnJob : IJobParallelFor
	{
		public Entity Prototype;
		public int MapSize;
		public RenderBounds MeshBounds;
		public EntityCommandBuffer.ParallelWriter Ecb;


		public void Execute(int index)
		{
			Entity e = Ecb.Instantiate(index, Prototype);

			float2 position = new int2(index % MapSize, index / MapSize);

			Ecb.SetComponent(index, e, new LocalToWorld { Value = float4x4.Translate(new float3(position.x, -1f, position.y)) });
			Ecb.SetComponent(index, e, new URPMaterialPropertyBaseColor());

			Ecb.SetComponent(index, e, MeshBounds);
			Ecb.SetComponent(index, e, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
			Ecb.SetComponent(index, e, new Pheromone { Index = index });
		}
	}

	[BurstCompile]
	public partial struct MaterialUpdateJob : IJobEntity
	{
		[ReadOnly] public DynamicBuffer<PheromoneBufferElement> PheromonesBuffer;

		private void Execute(ref URPMaterialPropertyBaseColor materialColor, [ReadOnly] in Pheromone pheromone)
		{
			PheromoneBufferElement pheromoneBufferElement = PheromonesBuffer[pheromone.Index];
			materialColor.Value = new float4(pheromoneBufferElement.Strength, 0f, 0f, 1f);
		}
	}
}