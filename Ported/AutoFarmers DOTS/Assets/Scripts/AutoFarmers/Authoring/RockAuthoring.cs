using Unity.Entities;
using UnityEngine;

namespace AutoFarmers.Authoring
{
    public class RockAuthoring : MonoBehaviour
    {
        private class RockAuthoringBaker : Baker<RockAuthoring>
        {
            public override void Bake(RockAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Rock());
            }
        }
    }

    public struct Rock : IComponentData
    {
        public int Health;
        public int StartHealth;
    }
}
