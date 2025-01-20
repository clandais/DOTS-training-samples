using Unity.Entities;
using UnityEngine;

namespace AutoFarmers.Authoring
{
    public class FarmerAuthoring : MonoBehaviour
    {
        private class FarmerAuthoringBaker : Baker<FarmerAuthoring>
        {
            public override void Bake(FarmerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Farmer());
            }
        }
    }
    
    public struct Farmer : IComponentData {}
}

