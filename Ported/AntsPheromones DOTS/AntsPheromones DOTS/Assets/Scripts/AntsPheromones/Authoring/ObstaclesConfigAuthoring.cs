using Unity.Entities;
using UnityEngine;

namespace AntsPheromones.Authoring
{
    public class ObstaclesConfigAuthoring : MonoBehaviour
    {
        public int obstacleRingCount = 3;
        [Range(0f, 1f)] public float obstaclesPerRing = .8f;
        public float obstacleRadius = 2f;
        public GameObject obstaclePrefab;

        private class ObstacleBaker : Baker<ObstaclesConfigAuthoring>
        {
            public override void Bake(ObstaclesConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ObstaclesConfig()
                {
                    obstacleRingCount = authoring.obstacleRingCount,
                    obstaclesPerRing = authoring.obstaclesPerRing,
                    obstacleRadius = authoring.obstacleRadius,
                    obstaclePrefab = GetEntity(authoring.obstaclePrefab, TransformUsageFlags.Dynamic),
                });
            }
        }
    }
    
                
    public struct ObstaclesConfig : IComponentData                              
    {
        public int obstacleRingCount;                                                   
        public float obstaclesPerRing;
        public float obstacleRadius;
        public Entity obstaclePrefab;
    }
}
