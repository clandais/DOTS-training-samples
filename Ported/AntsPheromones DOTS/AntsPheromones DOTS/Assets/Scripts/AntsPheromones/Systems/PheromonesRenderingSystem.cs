using AntsPheromones.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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

			var updateJob = new PheromonesUpdateJob
			{
				Colony = colony,
				Buffer = bufferArray,
				DeltaTime = dt,
			};
			state.Dependency = updateJob.ScheduleParallel(_antsQuery, state.Dependency);
			state.Dependency.Complete();
			

			var pheromoneDecayJob = new PheromoneDecayJob
			{
				Colony = colony,
				Buffer = bufferArray
			};
			state.Dependency =	pheromoneDecayJob.ScheduleParallel(state.Dependency);
			state.Dependency.Complete();


			for (int i = 0; i < pheromoneDecayJob.Buffer.Length; i++)
			{
				pheromonesBuffer[i] = pheromoneDecayJob.Buffer[i];
			}
			
			
			pheromonesBuffer = SystemAPI.GetSingletonBuffer<PheromoneBufferElement>();
			
			
			state.Dependency = new MaterialUpdateJob
			{
				PheromonesBuffer = pheromonesBuffer
			}.ScheduleParallel(state.Dependency);


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

	[BurstCompile]
	internal partial struct PheromoneDecayJob : IJobEntity
	{

		[ReadOnly] public Colony Colony;

		[NativeDisableParallelForRestriction]
		public NativeArray<PheromoneBufferElement> Buffer;

		private void Execute([ReadOnly] in Pheromone pheromone)
		{

			PheromoneBufferElement element = Buffer[pheromone.Index];
			;
			for (int j = 0; j < Colony.SimulationSpeed; j++)
			{
				element.Strength *= Colony.TrailDecaySpeed;
			}

			;
			Buffer[pheromone.Index] = element;
		}
	}


	[WithAll(typeof(AntAspect))]
	[BurstCompile]
	internal partial struct PheromonesUpdateJob : IJobEntity
	{
		[ReadOnly] public Colony Colony;
		[NativeDisableParallelForRestriction]
		public NativeArray<PheromoneBufferElement> Buffer;
		[ReadOnly] public float DeltaTime;


		private void Execute(AntAspect ant, [EntityIndexInQuery] int _)
		{

			var positions = ant.GetPositionsThisFrame();

			foreach (PositionsThisFrame position in positions)
			{
				int x = position.Value.x;
				int y = position.Value.y;

				if (x < 0 || x >= Colony.MapSize || y < 0 || y >= Colony.MapSize) continue;


				int index = x + y * (int)Colony.MapSize;
				
				PheromoneBufferElement pheromoneBufferElement = Buffer[index];
				pheromoneBufferElement.Strength += 
					(ant.Excitement * Colony.TrailAddSpeed * DeltaTime * Colony.SimulationSpeed)
					*(1f - pheromoneBufferElement.Strength);
				pheromoneBufferElement.Strength = math.clamp(pheromoneBufferElement.Strength, 0f, 1f);
				Buffer[index] = pheromoneBufferElement;
			}

			ant.ClearPositionsThisFrame();

		}
	}
}