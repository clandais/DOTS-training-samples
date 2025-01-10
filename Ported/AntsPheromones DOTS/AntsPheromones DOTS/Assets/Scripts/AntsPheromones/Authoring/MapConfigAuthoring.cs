using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace AntsPheromones.Authoring
{
    public class MapConfigAuthoring : MonoBehaviour
    {
        public int MapSize = 128;

        private class MapBaker : Baker<MapConfigAuthoring>
        {
            public override void Bake(MapConfigAuthoring configAuthoring)
            {
                var rng = Random.CreateFromIndex((uint)configAuthoring.GetInstanceID());
                var resourceAngle = rng.NextFloat() * 2f * math.PI;
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Map()
                {
                    Size = configAuthoring.MapSize,
                    ResourcePosition =  new float2(1,1) * configAuthoring.MapSize * .5f + new float2(math.cos(resourceAngle) * configAuthoring.MapSize * .475f, math.sin(resourceAngle) * configAuthoring.MapSize * .475f),
                    ColonyPosition =  new float2(1,1) * configAuthoring.MapSize * .5f
                });
            }
        }
    }

    public struct Map : IComponentData
    {
        public int Size;
        public float2 ResourcePosition;
        public float2 ColonyPosition;
    }
}
