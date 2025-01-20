using Unity.Entities;
using UnityEngine;

namespace AutoFarmers.Authoring
{
    public class StoreAuthoring : MonoBehaviour
    {
        private class StoreAuthoringBaker : Baker<StoreAuthoring>
        {
            public override void Bake(StoreAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Store());
            }
        }
    }
    
    public struct Store : IComponentData {}
}

