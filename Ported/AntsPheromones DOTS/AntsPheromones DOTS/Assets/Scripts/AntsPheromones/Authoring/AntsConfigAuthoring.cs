using Unity.Entities;
using UnityEngine;

namespace AntsPheromones.Authoring
{
    public class AntsConfigAuthoring : MonoBehaviour
    {
        
        public int antCount = 1000;
        public float antSpeed = .2f;
        [Range(0f, 1f)] public float antAcceleration = .07f;
        public GameObject antPrefab;
        public float randomSteering = 0.14f;
        public float wallSteeringStrength = 0.12f;
        
        private class AntsConfigAuthoringBaker : Baker<AntsConfigAuthoring>
        {
            
            
            
            public override void Bake(AntsConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new AntsConfig
                {
                    AntCount = authoring.antCount,
                    AntSpeed = authoring.antSpeed,
                    AntAcceleration = authoring.antAcceleration,
                    RandomSteering = authoring.randomSteering,
                    WallSteeringStrength = authoring.wallSteeringStrength,
                    AntPrefab = GetEntity(authoring.antPrefab, TransformUsageFlags.Dynamic),
                });
            }
        }
    }
    
    public struct AntsConfig : IComponentData
    {
        public int AntCount;
        public float AntSpeed;
        public float AntAcceleration;
        public float RandomSteering;
        public float WallSteeringStrength;
        public Entity AntPrefab;
    }
}

