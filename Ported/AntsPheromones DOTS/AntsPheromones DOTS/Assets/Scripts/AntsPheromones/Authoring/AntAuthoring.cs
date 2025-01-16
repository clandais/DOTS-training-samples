using AntsV2.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace AntsPheromones.Authoring
{
	public class AntAuthoring : MonoBehaviour
	{
		public Ant ant;
		public Direction direction;
		public Position position;
		public Speed speed;

		public Color DefaultColor;
		public Color HoldingResourceColor;
		
		#region Baker

		private class AntAuthoringBaker : Baker<AntAuthoring>
		{


			public override void Bake(AntAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, authoring.ant);
				AddComponent(entity, authoring.position);
				AddComponent(entity, authoring.speed);
				AddComponent(entity, authoring.direction);

				float4 defaultColor;
				defaultColor.x = authoring.DefaultColor.linear.r;
				defaultColor.y = authoring.DefaultColor.linear.g;
				defaultColor.z = authoring.DefaultColor.linear.b;
				defaultColor.w = authoring.DefaultColor.linear.a;
				
				float4 holdingResourceColor;
				holdingResourceColor.x = authoring.HoldingResourceColor.linear.r;
				holdingResourceColor.y = authoring.HoldingResourceColor.linear.g;
				holdingResourceColor.z = authoring.HoldingResourceColor.linear.b;
				holdingResourceColor.w = authoring.HoldingResourceColor.linear.a;
				
				AntColors antColors = new AntColors
				{
					DefaultColor = defaultColor,
					HoldingResourceColor = holdingResourceColor
				};
				
				AddComponent(entity, antColors);
				
				AddComponent(entity, new URPMaterialPropertyBaseColor
				{
					Value = defaultColor
				});
			}
		}

		#endregion
	}

	public struct AntColors : IComponentData
	{
		public float4 DefaultColor;
		public float4 HoldingResourceColor;
	}
}