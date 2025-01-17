using AntsPheromones.Authoring;
using AntsPheromones.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;

namespace AntsPheromones.Systems
{
	[RequireMatchingQueriesForUpdate]
	[UpdateAfter(typeof(SpawnSystem))]
	
	public partial class PheromonesInitializationSystem : SystemBase
	{
		
		private Entity _prototype;
		
		protected override void OnUpdate()
		{
			
			Enabled = false;
			
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

			Entity _prototype = EntityManager.CreateEntity();
			RenderMeshUtility.AddComponents(
				_prototype,
				EntityManager,
				renderMeshDescription,
				renderMeshArray,
				MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

			EntityManager.AddComponentData(_prototype, new URPMaterialPropertyEmissionColor());
			EntityManager.AddComponentData(_prototype, new Pheromone());

			var bounds = new RenderBounds
			{
				Value = managedPheromonesCfg.Mesh.bounds.ToAABB()
			};

			var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

			var pheromonesBuffer = SystemAPI.GetSingletonBuffer<PheromoneBufferElement>();

			var spawnJob = new SpawnJob
			{
				MapSize = (int)colony.MapSize,
				Prototype = _prototype,
				Ecb = ecb.AsParallelWriter(),
				MeshBounds = bounds
			};

			Dependency = spawnJob.Schedule(pheromonesBuffer.Length, 128, Dependency);
			Dependency.Complete();
		}

		protected override void OnStopRunning()
		{
			EntityManager.DestroyEntity(_prototype);
		}
	}
	
	[GenerateTestsForBurstCompatibility]
	[BurstCompile]
	public struct SpawnJob : IJobParallelFor
	{
		public Entity Prototype;
		public int MapSize;
		public RenderBounds MeshBounds;
		public EntityCommandBuffer.ParallelWriter Ecb;


		[BurstCompile]
		public void Execute(int index)
		{
			Entity e = Ecb.Instantiate(index, Prototype);

			float2 position = new int2(index % MapSize, index / MapSize);

			Ecb.SetComponent(index, e, new LocalToWorld { Value = float4x4.Translate(new float3(position.x, -1f, position.y)) });
			Ecb.SetComponent(index, e, new URPMaterialPropertyEmissionColor());

			Ecb.SetComponent(index, e, MeshBounds);
			Ecb.SetComponent(index, e, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
			Ecb.SetComponent(index, e, new Pheromone { Index = index });
		}
	}
}