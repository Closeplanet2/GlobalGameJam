using UnityEngine;

namespace GloablGameJam.Scripts.Camera
{
    public class CameraManager : MonoBehaviour, ICameraManager
    {
        private Vector3 _cameraFollowVelocity = Vector3.zero;
        private float _lookAngle;
        private float _pivotAngle;
        
        [Header("Children")]
        [SerializeField] private UnityEngine.Camera managedCamera;
        [SerializeField] private Transform pivotTransform;

        [Header("Follow Target")]
        [SerializeField] private float cameraFollowSpeed = 1f;

        [Header("Rotate Camera")]
        [SerializeField] private float cameraLookSpeed = 1f;
        [SerializeField] private float cameraPivotSpeed = 1f;
    
        public UnityEngine.Camera IManagedCamera()
        {
            return managedCamera;
        }

        public void IFollowTarget(Transform targetTransform)
        {
            var targetPosition = Vector3.SmoothDamp(transform.position, targetTransform.position, ref _cameraFollowVelocity, cameraFollowSpeed);
            transform.position = targetPosition;
        }

        public void IRotateCamera(Vector2 mouseInput)
        {
            _lookAngle += mouseInput.x * cameraLookSpeed;
            _pivotAngle += mouseInput.y * cameraPivotSpeed;
            var cameraRotation = Vector3.zero;
            cameraRotation.y = _lookAngle;
            transform.rotation = Quaternion.Euler(cameraRotation);

            cameraRotation = Vector3.zero;
            cameraRotation.x = _pivotAngle;
            pivotTransform.localRotation = Quaternion.Euler(cameraRotation);
        }
    }
}