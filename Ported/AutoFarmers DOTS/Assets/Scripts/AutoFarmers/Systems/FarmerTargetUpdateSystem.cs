using AutoFarmers.Authoring;
using AutoFarmers.Behaviours;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace AutoFarmers.Systems
{
    [RequireMatchingQueriesForUpdate]
    public partial class FarmerTargetUpdateSystem : SystemBase
    {
        private CinemachineCameraBehaviour _cinemachineCameraBehaviour;

        protected override void OnCreate()
        {
            RequireForUpdate<Farmer>();


        }

        protected override void OnUpdate()
        {

            _cinemachineCameraBehaviour = Object.FindFirstObjectByType<CinemachineCameraBehaviour>();
            LocalTransform tr = LocalTransform.Identity;

            Entities.ForEach((ref LocalTransform t, in Farmer f) =>
            {
                if (f.IsOriginal)
                {
                    tr = t;
                }

            }).Run();

            _cinemachineCameraBehaviour.SetFarmerTransform(tr);
        }
    }
}
