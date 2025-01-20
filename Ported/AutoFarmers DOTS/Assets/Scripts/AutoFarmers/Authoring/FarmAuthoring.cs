using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AutoFarmers.Authoring
{
    public class FarmAuthoring : MonoBehaviour
    {
        public Farm farm;
        public GameObject farmerPrefab;
        public GameObject rockPrefab;
        public GameObject groundPrefab;
        public GameObject storePrefab;


        private class FarmerSpawnerAuthoringBaker : Baker<FarmAuthoring>
        {
            public override void Bake(FarmAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new FarmCfg
                {
                    FarmerPrefab = GetEntity(authoring.farmerPrefab, TransformUsageFlags.Dynamic),
                    RockPrefab = GetEntity(authoring.rockPrefab, TransformUsageFlags.Dynamic),
                    GroundPrefab = GetEntity(authoring.groundPrefab, TransformUsageFlags.Dynamic),
                    StorePrefab = GetEntity(authoring.storePrefab, TransformUsageFlags.Dynamic),
                });
                AddComponent(entity, authoring.farm);
            }
        }
    }

    public struct FarmCfg : IComponentData
    {
        public Entity FarmerPrefab;
        public Entity RockPrefab;
        public Entity GroundPrefab;
        public Entity StorePrefab;
    }

    [Serializable]
    public struct Farm : IComponentData
    {
        public int InitialFarmerCount;
        public int MaxFarmerCount;
        public int MapSize;
        public int StoreCount;
        public int RockSpawnAttempts;
    }

    public struct StoreTile : IBufferElementData
    {
        public bool IsOccupied;
    }
    
    public struct RockedTile : IBufferElementData
    {
        public bool IsOccupied;
        public Rock Rock;
    }

    public struct GraphNode : IBufferElementData
    {
        public int2 Position;
        public bool IsWalkable;
    }
}
