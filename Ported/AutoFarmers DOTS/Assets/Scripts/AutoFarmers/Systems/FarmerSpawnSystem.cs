using AutoFarmers.Authoring;
using Unity.Burst;
using Unity.Entities;

namespace AutoFarmers.Systems
{
    public partial struct FarmerSpawnSystem : ISystem
    {
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FarmCfg>();
            state.RequireForUpdate<Farm>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
