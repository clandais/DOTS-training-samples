using AntsV2.Components;
using Unity.Entities;
using UnityEngine;

namespace AntsPheromones.Authoring
{
	public class HomeAuthoring : MonoBehaviour
	{
		public Home home;
		public Position position;
		#region Nested type: ${0}

		private class HomeAuthoringBaker : Baker<HomeAuthoring>
		{
			public override void Bake(HomeAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Renderable);
				AddComponent(entity, authoring.home);
				AddComponent(entity, authoring.position);
			}
		}

		#endregion
	}
}