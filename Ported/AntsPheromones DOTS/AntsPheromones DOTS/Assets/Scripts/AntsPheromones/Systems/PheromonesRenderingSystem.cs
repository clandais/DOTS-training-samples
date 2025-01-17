using AntsPheromones.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using AntAspect = AntsPheromones.Components.AntAspect;

namespace AntsPheromones.Systems
{
	[UpdateAfter(typeof(PheromonesInitializationSystem))]
	public partial struct PheromonesRenderingSystem : ISystem
	{
		private EntityQuery _antsQuery;
		
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<Colony>();
			_antsQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAspect<AntAspect>().Build(ref state);
			
		}


		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			
			
			
			float dt = SystemAPI.Time.DeltaTime;
			var pheromonesBuffer = SystemAPI.GetSingletonBuffer<PheromoneBufferElement>();
			var colony = SystemAPI.GetSingleton<Colony>();
			

			var bufferArray = pheromonesBuffer.AsNativeArray();
			
			var pheromoneUpdateJob = new PheromonesUpdateJob
			{
				Colony = colony,
				Buffer = bufferArray,
				DeltaTime = dt,
			};
			
			state.Dependency = pheromoneUpdateJob.ScheduleParallel(_antsQuery, state.Dependency);
			state.Dependency.Complete();

			for (int i = 0; i < bufferArray.Length; i++)
			{
				var element = bufferArray[i];
				element.Strength -= (  colony.TrailDecaySpeed * colony.SimulationSpeed * dt);
				if (element.Strength < 0f) element.Strength = 0f;
				pheromonesBuffer[i] = element;
			}
			
			
			pheromonesBuffer = SystemAPI.GetSingletonBuffer<PheromoneBufferElement>();

			var materialJob = new MaterialUpdateJob
			{
				PheromonesBuffer = pheromonesBuffer,
			};

			state.Dependency = materialJob.ScheduleParallel(state.Dependency);

		}
	}


	[BurstCompile]
	public partial struct MaterialUpdateJob : IJobEntity
	{
		[ReadOnly] public DynamicBuffer<PheromoneBufferElement> PheromonesBuffer;

		private void Execute(ref URPMaterialPropertyEmissionColor materialColor, [ReadOnly] in Pheromone pheromone)
		{
			PheromoneBufferElement pheromoneBufferElement = PheromonesBuffer[pheromone.Index];
			materialColor.Value = new float4(pheromoneBufferElement.Strength, 0f, 0f, 1f);
		}
	}
	
	[WithAll(typeof(AntAspect))]
	[BurstCompile]
	partial struct PheromonesUpdateJob : IJobEntity
	{
		[ReadOnly] public Colony Colony;
		[NativeDisableParallelForRestriction]
		public NativeArray<PheromoneBufferElement> Buffer;
		
		[ReadOnly] public float DeltaTime;
		
		void Execute(AntAspect ant, [EntityIndexInQuery] int _)
		{
			int x = (int)math.floor(ant.Position.x);
			int y = (int)math.floor(ant.Position.y);

			if (x < 0 || x >= Colony.MapSize || y < 0 || y >= Colony.MapSize) return;
			
			int index = x + y * (int)Colony.MapSize;
			
			PheromoneBufferElement pheromoneBufferElement = Buffer[index];
			float pheromoneStrength = pheromoneBufferElement.Strength;
			pheromoneStrength += (Colony.TrailAddSpeed * ant.Excitement * (Colony.SimulationSpeed * DeltaTime)) *  
			                     (1f - pheromoneBufferElement.Strength);
			
			if (pheromoneStrength > 1f) pheromoneStrength = 1f;



			pheromoneBufferElement.Strength = pheromoneStrength;
			Buffer[index] = pheromoneBufferElement;

		}
	}
}