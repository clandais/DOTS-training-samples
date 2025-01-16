using AntsV2.Components;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace AntsPheromones.Authoring
{
	public class PheromoneAuthoring : MonoBehaviour
	{
		public Pheromone pheromone;
		#region Nested type: ${0}

		private class PheromoneAuthoringBaker : Baker<PheromoneAuthoring>
		{
			public override void Bake(PheromoneAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Renderable);
				Pheromone pheromone = authoring.pheromone;

				AddComponent(entity, pheromone);
				AddComponent(entity, new URPMaterialPropertyBaseColor());

			}
		}

		#endregion
	}
}