using AntsPheromones.Components;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using URPMaterialPropertyBaseColor = Unity.Rendering.URPMaterialPropertyBaseColor;

namespace AntsPheromones.Authoring
{
	public class PheromoneAuthoring : MonoBehaviour
	{
		public Pheromone Pheromone;
		#region Baker

		private class PheromoneAuthoringBaker : Baker<PheromoneAuthoring>
		{
			public override void Bake(PheromoneAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Renderable);
				Pheromone pheromone = authoring.Pheromone;

				AddComponent(entity, pheromone);
				AddComponent(entity, new URPMaterialPropertyEmissionColor());
				AddComponent(entity, new URPMaterialPropertyBaseColor());

			}
		}

		#endregion
	}
}