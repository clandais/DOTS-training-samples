using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace AntsPheromones.Authoring
{
    public class AntAuthoring : MonoBehaviour
    {
        private class AntAuthoringBaker : Baker<AntAuthoring>
        {
            public override void Bake(AntAuthoring authoring)
            {
                var rng = Random.CreateFromIndex( (uint) authoring.GetInstanceID() );
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Ant
                {
                });
            }
        }
    }
    
    public struct Ant : IComponentData
    {
        public float2 Position;
        public float FacingAngle;
        public float Speed;
        public bool HoldingResource;

    }
}

