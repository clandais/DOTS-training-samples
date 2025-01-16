using AntsV2.Components;
using Unity.Entities;
using UnityEngine;

namespace AntsPheromones.Authoring
{
	public class ResourceAuthoring : MonoBehaviour
	{
		#region Serialized Fields

		public Resource resource;

		#endregion
		public Position position;
		#region Nested type: ${0}

		private class ResourceAuthoringBaker : Baker<ResourceAuthoring>
		{
			public override void Bake(ResourceAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Renderable);
				AddComponent(entity, authoring.resource);
				AddComponent(entity, authoring.position);
			}
		}

		#endregion
	}
}