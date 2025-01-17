using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AntsPheromones.Authoring
{
	public class ManagedPheromoneAuthoring : MonoBehaviour
	{
		#region Serialized Fields

		public Material Material;
		public Mesh Mesh;
		public Color BaseColor;
		
		#endregion
		#region Nested type: ${0}

		private class ManagedPheromoneAuthoringBaker : Baker<ManagedPheromoneAuthoring>
		{
			public override void Bake(ManagedPheromoneAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.None);
				AddComponent(entity, new PheromoneSingleton());

				float4 baseColor;
				baseColor.x = authoring.BaseColor.linear.r;
				baseColor.y = authoring.BaseColor.linear.g;
				baseColor.z = authoring.BaseColor.linear.b;
				baseColor.w = authoring.BaseColor.linear.a;
				
				AddComponentObject(entity, new ManagedPheromoneComponent
				{
					Material = authoring.Material, 
					Mesh = authoring.Mesh,
					BaseColor = baseColor,
				});
			}
		}

		#endregion
	}

	public class ManagedPheromoneComponent : IComponentData
	{
		public Material Material;
		public Mesh Mesh;
		public float4 BaseColor;
		public float4 EmissionColor;
	}

	public struct PheromoneSingleton : IComponentData
	{
	}
}