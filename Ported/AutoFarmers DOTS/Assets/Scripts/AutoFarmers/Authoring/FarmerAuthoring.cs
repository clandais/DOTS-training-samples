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
                // AddComponent(entity, new IntentionComponent
                // {
                //     Value = Intention.None,
                // });
                
                AddComponent(entity, new NoneGoal(){});
                AddComponent(entity , new IntentionComponent()
                {
                    Value = Intention.None
                });
                
                AddComponent(entity, new SmashRocksGoal());
                AddComponent(entity, new TillGroundGoal());
                AddComponent(entity, new PlantSeedsGoal());
                AddComponent(entity, new SellPlantsGoal());
            }
        }
    }

    public struct Farmer : IComponentData
    {
        public bool IsOriginal;
    }
    
    
    public struct IntentionComponent : IComponentData
    {
        public Intention Value;
    }
    
    public struct NoneGoal : IComponentData, IEnableableComponent { }
    public struct SmashRocksGoal : IComponentData, IEnableableComponent { }
    public struct TillGroundGoal : IComponentData, IEnableableComponent { }
    public struct PlantSeedsGoal : IComponentData, IEnableableComponent { }
    public struct SellPlantsGoal : IComponentData, IEnableableComponent { }
}

