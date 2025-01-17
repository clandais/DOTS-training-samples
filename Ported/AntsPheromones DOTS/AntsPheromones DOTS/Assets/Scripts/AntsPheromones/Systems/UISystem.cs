using AntsPheromones.Behaviours;
using AntsPheromones.Components;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace AntsPheromones.Systems
{
	[RequireMatchingQueriesForUpdate]
	[UpdateAfter(typeof(SpawnSystem))]
	public partial class UISystem : SystemBase
	{
		
		private bool _isInitialized;
		private SimulationSpeedSlider _slider;
		
		protected override void OnUpdate()
		{
			if( !_isInitialized )
			{
				_slider = UnityEngine.Object.FindFirstObjectByType<SimulationSpeedSlider>();
				_isInitialized = true;
			}
			
			var colony = SystemAPI.GetSingletonRW<Colony>();
			colony.ValueRW.SimulationSpeed = _slider.Value;
			
			

		}
	}
}