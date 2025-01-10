using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AntsPheromones.Authoring
{
    public class ObstacleAuthoring : MonoBehaviour
    {
        private class ObstacleBaker : Baker<ObstacleAuthoring>
        {
            public override void Bake(ObstacleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Obstacle{});
            }
        }
    }

    public struct Obstacle : IComponentData
    {
        public float2 Position;
        public float Radius;
    }
}
