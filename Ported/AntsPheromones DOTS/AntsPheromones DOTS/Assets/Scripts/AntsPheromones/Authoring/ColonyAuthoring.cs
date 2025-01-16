using AntsV2.Components;
using Unity.Entities;
using UnityEngine;

namespace AntsPheromones.Authoring
{
	public class ColonyAuthoring : MonoBehaviour
	{
		#region Serialized Fields

		public GameObject antPrefab;
		public GameObject obstaclePrefab;
		public GameObject homePrefab;
		public GameObject resourcePrefab;
		public GameObject pheromonePrefab;

		public Colony colony;

		#endregion
		#region Nested type: ${0}

		private class ColonyAuthoringBaker : Baker<ColonyAuthoring>
		{
			public override void Bake(ColonyAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Renderable);
				Colony colony = authoring.colony;
				colony.AntPrefab = GetEntity(authoring.antPrefab, TransformUsageFlags.Renderable);
				colony.HomePrefab = GetEntity(authoring.homePrefab, TransformUsageFlags.Renderable);
				colony.ResourcePrefab = GetEntity(authoring.resourcePrefab, TransformUsageFlags.Renderable);
				colony.ObstaclePrefab = GetEntity(authoring.obstaclePrefab, TransformUsageFlags.Renderable);
				colony.PheromonePrefab = GetEntity(authoring.pheromonePrefab, TransformUsageFlags.Renderable);
				AddComponent(entity, colony);
			}
		}

		#endregion
	}
}