using UnityEngine;

namespace GloablGameJam.Scripts.Camera
{
    public class CameraManager : MonoBehaviour, ICameraManager
    {
        private Vector3 _cameraFollowVelocity = Vector3.zero;
        private float _lookAngle;
        private float _pivotAngle;
        private float _cameraDefaultZPosition;
        private Vector3 _cameraVectorPosiiton;
        
        [Header("Children")]
        [SerializeField] private UnityEngine.Camera managedCamera;
        [SerializeField] private Transform pivotTransform;

        [Header("Follow Target")]
        [SerializeField] private bool followTargetEnabled = true;
        [SerializeField] private float cameraFollowSpeed = 1f;

        [Header("Rotate Camera")]
        [SerializeField] private bool rotateCameraEnabled = true;
        [SerializeField] private float cameraLookSpeed = 1f;
        [SerializeField] private float cameraPivotSpeed = 1f;
        [SerializeField] private float cameraMinPivotAngle = 0f;
        [SerializeField] private float cameraMaxPivotAngle = 0f;

        [Header("Camera Collisions")]
        [SerializeField] private bool cameraCollisionsEnabled = true;
        [SerializeField] private float cameraCollisionRadius = 0.2f;
        [SerializeField] private LayerMask cameraCollisionLayers;
        [SerializeField] private float cameraCollisionOffset = 0.2f;
        [SerializeField] private float miniumCollisionOffset = 0.2f;
        [SerializeField] private float miniumCollisionSpeed = 0.2f;

        private void Start()
        {
            _cameraDefaultZPosition = managedCamera.transform.localPosition.z;
        }

        public UnityEngine.Camera IManagedCamera()
        {
            return managedCamera;
        }

        public void IFollowTarget(Transform targetTransform)
        {
            if(!followTargetEnabled) return;
            var targetPosition = Vector3.SmoothDamp(transform.position, targetTransform.position, ref _cameraFollowVelocity, cameraFollowSpeed);
            transform.position = targetPosition;
        }

        public void IRotateCamera(Vector2 mouseInput)
        {
            if(!rotateCameraEnabled) return;
            _lookAngle += mouseInput.x * cameraLookSpeed;
            _pivotAngle += mouseInput.y * cameraPivotSpeed;
            _pivotAngle = Mathf.Clamp(_pivotAngle, cameraMinPivotAngle, cameraMaxPivotAngle);

            var cameraRotation = Vector3.zero;
            cameraRotation.y = _lookAngle;
            transform.rotation = Quaternion.Euler(cameraRotation);

            cameraRotation = Vector3.zero;
            cameraRotation.x = _pivotAngle;
            pivotTransform.localRotation = Quaternion.Euler(cameraRotation);
        }

        public void IHandleCameraCollisions()
        {
            if(!cameraCollisionsEnabled) return;
            var defaultZ = _cameraDefaultZPosition;
            var maxDistance = Mathf.Abs(defaultZ);
            var direction = -pivotTransform.forward;
            var targetZ = defaultZ;
            if (Physics.SphereCast(pivotTransform.position, cameraCollisionRadius, direction, out RaycastHit hit, maxDistance, cameraCollisionLayers, QueryTriggerInteraction.Ignore))
            {
                var allowedDistance = Mathf.Max(0f, hit.distance - cameraCollisionOffset);
                targetZ = -allowedDistance;
                if (Mathf.Abs(targetZ) < miniumCollisionOffset) targetZ = -miniumCollisionOffset;
            }
            _cameraVectorPosiiton.z = Mathf.Lerp(managedCamera.transform.localPosition.z, targetZ, miniumCollisionSpeed * Time.deltaTime);
            managedCamera.transform.localPosition = _cameraVectorPosiiton;
        }
    }
}