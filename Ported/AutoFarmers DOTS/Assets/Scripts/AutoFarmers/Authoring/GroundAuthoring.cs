using Unity.Entities;
using UnityEngine;

namespace AutoFarmers.Authoring
{
    public class GroundAuthoring : MonoBehaviour
    {
        private class GroundAuthoringBaker : Baker<GroundAuthoring>
        {
            public override void Bake(GroundAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Ground());
            }
        }
    }
    
    public struct Ground : IComponentData {}
}

