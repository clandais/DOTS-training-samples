using AntsPheromones.Authoring;
using AntsPheromones.Components;
using Unity.Collections;
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
		private readonly RefRW<Velocity> Vel;
		private readonly RefRW<Acceleration> Acc;
		
		private readonly RefRO<AntColors> BaseColors;

		
		private readonly  RefRW<URPMaterialPropertyBaseColor> BaseColor;

		private readonly DynamicBuffer<PositionsThisFrame> PositionsThisFrame;
		
		
		public void AddPositionThisFrame(int2 position)
		{

			foreach (PositionsThisFrame positionsThisFrame in PositionsThisFrame)
			{
				if (positionsThisFrame.Value.Equals(position))
				{
					return;
				}
			}
			
			PositionsThisFrame.Add( new PositionsThisFrame{ Value = position});
		}
		
		
		public NativeArray<PositionsThisFrame> GetPositionsThisFrame()
		{
			return PositionsThisFrame.AsNativeArray();
		}
		
		public void ClearPositionsThisFrame()
		{
			PositionsThisFrame.Clear();
		}
		
		
		
		public float2 Position
		{
			get => LocalTransform.ValueRO.Position.xz;
			set
			{
				Pos.ValueRW.Value = value;
				LocalTransform.ValueRW.Position.xz = value;
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
		
		public float2 Velocity
		{
			get => Vel.ValueRO.Value;
			set => Vel.ValueRW.Value = value;
		}
		
		public float2 Acceleration
		{
			get => Acc.ValueRO.Value;
			set => Acc.ValueRW.Value = value;
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