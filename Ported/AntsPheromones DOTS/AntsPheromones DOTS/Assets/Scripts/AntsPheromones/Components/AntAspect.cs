using AntsPheromones.Authoring;
using AntsV2.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace AntsPheromones.Components
{
	public readonly partial struct AntAspect : IAspect
	{
		public readonly Entity Self;

		private readonly RefRW<LocalTransform> LocalTransform;
		private readonly RefRW<Ant> Ant;

		private readonly RefRW<Position> Pos;
		private readonly RefRW<Direction> Dir;
		private readonly RefRW<Speed> Spd;
		private readonly RefRO<AntColors> BaseColors;

		private readonly  RefRW<URPMaterialPropertyBaseColor> BaseColor;

		public float2 Position
		{
			get => Pos.ValueRO.Value;
			set
			{
				Pos.ValueRW.Value = value;
				LocalTransform.ValueRW.Position = new float3(value.x, 0f, value.y);
			}
		}

		public float2 Direction
		{
			get => Dir.ValueRO.Value;
			set
			{
				Dir.ValueRW.Value = value;
				LocalTransform.ValueRW.Rotation = quaternion.LookRotation(new float3(value.x, 0f, value.y), new float3(0f, 1, 0f));
			}
		}

		public float FacingAngle
		{
			get => math.atan2(Dir.ValueRO.Value.y, Dir.ValueRO.Value.x);
			set => Direction = new float2(math.cos(value), math.sin(value));
		}

		public float Speed
		{
			get => Spd.ValueRO.Value;
			set => Spd.ValueRW.Value = value;
		}

		public quaternion Rotation
		{
			get => LocalTransform.ValueRO.Rotation;
			set => LocalTransform.ValueRW.Rotation = value;
		}

		public bool IsHoldingResource
		{
			get => Ant.ValueRO.HasResource;
			set
			{
				Ant.ValueRW.HasResource = value;

				if (IsHoldingResource)
				{
					BaseColor.ValueRW.Value = BaseColors.ValueRO.HoldingResourceColor;
				}
				else
				{
					BaseColor.ValueRW.Value = BaseColors.ValueRO.DefaultColor;
				}
			}
		}

		public float Excitement
		{
			get => Ant.ValueRO.Excitement;
			set => Ant.ValueRW.Excitement = value;
		}

	}
}