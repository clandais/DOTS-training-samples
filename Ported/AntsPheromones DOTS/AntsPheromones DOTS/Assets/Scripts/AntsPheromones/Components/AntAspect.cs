using AntsPheromones.Authoring;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace AntsPheromones.Components
{
    public readonly partial struct AntAspect : IAspect
    {
        public readonly Entity Self;
        readonly RefRW<LocalTransform> LocalTransform;
        readonly RefRW<Ant> Ant;


        public float2 Position
        {
            get => LocalTransform.ValueRO.Position.xz;
            set
            {
                LocalTransform.ValueRW.Position.xz = value;
                Ant.ValueRW.Position = value;
            }
        }
        
        public float FacingAngle
        {
            get => Ant.ValueRO.FacingAngle;
            set
            {
                Ant.ValueRW.FacingAngle = value;
                LocalTransform.ValueRW.Rotation = quaternion.EulerXYZ(0, value, 0f);
            }
        }
        
        public float Speed
        {
            get => Ant.ValueRO.Speed;
            set => Ant.ValueRW.Speed = value;
        }
        
        public quaternion Rotation
        {
            get => LocalTransform.ValueRO.Rotation;
            set => LocalTransform.ValueRW.Rotation = value;
        }
        public bool IsHoldingResource
        {
            get => Ant.ValueRO.HoldingResource;
            set => Ant.ValueRW.HoldingResource = value;
        } 
    }
}
