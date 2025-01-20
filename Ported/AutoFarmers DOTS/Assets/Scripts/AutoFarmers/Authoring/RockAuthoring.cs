using Unity.Entities;
using Unity.Transforms;
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
              //  AddComponent(entity, new PostTransformMatrix());
            }
        }
    }

    public struct Rock : IComponentData
    {
        public int Health;
        public int StartHealth;
        public RectInt Rect;
    }
}
