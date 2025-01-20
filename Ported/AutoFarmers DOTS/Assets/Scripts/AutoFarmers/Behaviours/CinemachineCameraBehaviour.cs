using Unity.Transforms;
using UnityEngine;

namespace AutoFarmers.Behaviours
{
    public class CinemachineCameraBehaviour : MonoBehaviour
    {
        [SerializeField] private Transform cameraTarget;


        public void SetFarmerTransform(LocalTransform farmerTransform)
        {
            cameraTarget.position = farmerTransform.Position;
            cameraTarget.rotation = farmerTransform.Rotation;
        }


        private void OnDrawGizmos()
        {
            if (cameraTarget)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(cameraTarget.position, 0.5f);
            }
        }
    }
}
