using Unity.Entities;
using Unity.Mathematics;
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

                AddComponent(entity , new IntentionComponent()
                {
                    Value = Intention.None
                });
                
                                
                AddComponent(entity, new NoneGoal(){});
                AddComponent(entity, new SmashRocksGoal());
                AddComponent(entity, new TillGroundGoal());
                AddComponent(entity, new PlantSeedsGoal());
                AddComponent(entity, new SellPlantsGoal());
                
                AddComponent<Pathing>(entity);
                AddComponent(entity, new TargetComponent());

                AddBuffer<PathBufferElement>(entity);
            }
        }
    }

    public struct Farmer : IComponentData
    {
        public bool IsOriginal;
    }



    public struct TargetComponent : IComponentData, IEnableableComponent
    {
        public int2 Value;
    }

    public struct Pathing : IComponentData, IEnableableComponent
    {
        public int CurrentIndex;
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
    
    public struct PathBufferElement : IBufferElementData
    {
        public int2 Value;
        
    }
}

