using Unity.Entities;
using UnityEngine;

namespace AntsPheromones.Authoring
{
	public class ManagedPheromoneAuthoring : MonoBehaviour
	{
		#region Serialized Fields

		public Material Material;
		public Mesh Mesh;

		#endregion
		#region Nested type: ${0}

		private class ManagedPheromoneAuthoringBaker : Baker<ManagedPheromoneAuthoring>
		{
			public override void Bake(ManagedPheromoneAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.None);
				AddComponent(entity, new PheromoneSingleton());
				AddComponentObject(entity, new ManagedPheromoneComponent { Material = authoring.Material, Mesh = authoring.Mesh });
			}
		}

		#endregion
	}

	public class ManagedPheromoneComponent : IComponentData
	{
		public Material Material;
		public Mesh Mesh;
	}

	public struct PheromoneSingleton : IComponentData
	{
	}
}