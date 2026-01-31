using UnityEngine;

namespace GloablGameJam.Scripts.Camera
{
    public sealed class CameraManager : MonoBehaviour, ICameraManager
    {
        private Vector3 _followVelocity;
        private float _lookAngle;
        private float _pivotAngle;
        private float _defaultLocalZ;

        [Header("Children")]
        [SerializeField] private UnityEngine.Camera managedCamera;
        [SerializeField] private Transform pivotTransform;

        [Header("Follow Target")]
        [SerializeField] private bool followTargetEnabled = true;
        [SerializeField] private float cameraFollowSmoothTime = 0.15f;

        [Header("Rotate Camera")]
        [SerializeField] private bool rotateCameraEnabled = true;
        [SerializeField] private float cameraLookSpeed = 1f;
        [SerializeField] private float cameraPivotSpeed = 1f;
        [SerializeField] private float cameraMinPivotAngle = -30f;
        [SerializeField] private float cameraMaxPivotAngle = 70f;

        [Header("Camera Collisions")]
        [SerializeField] private bool cameraCollisionsEnabled = true;
        [SerializeField] private float cameraCollisionRadius = 0.2f;
        [SerializeField] private LayerMask cameraCollisionLayers;
        [SerializeField] private float cameraCollisionOffset = 0.2f;
        [SerializeField] private float minimumCollisionOffset = 0.2f;
        [SerializeField] private float collisionLerpSpeed = 10f;

        private void Start()
        {
            if (managedCamera != null)
            {
                _defaultLocalZ = managedCamera.transform.localPosition.z;
            }
        }

        public UnityEngine.Camera IManagedCamera() => managedCamera;

        public void IFollowTarget(Transform targetTransform)
        {
            if (!followTargetEnabled) return;
            if (targetTransform == null) return;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetTransform.position,
                ref _followVelocity,
                cameraFollowSmoothTime);
        }

        public void IRotateCamera(Vector2 mouseInput)
        {
            if (!rotateCameraEnabled) return;

            _lookAngle += mouseInput.x * cameraLookSpeed;
            _pivotAngle += mouseInput.y * cameraPivotSpeed;
            _pivotAngle = Mathf.Clamp(_pivotAngle, cameraMinPivotAngle, cameraMaxPivotAngle);

            transform.rotation = Quaternion.Euler(0f, _lookAngle, 0f);
            pivotTransform.localRotation = Quaternion.Euler(_pivotAngle, 0f, 0f);
        }

        public void IHandleCameraCollisions()
        {
            if (!cameraCollisionsEnabled) return;
            if (managedCamera == null || pivotTransform == null) return;

            var maxDistance = Mathf.Abs(_defaultLocalZ);
            var direction = -pivotTransform.forward;

            var targetZ = _defaultLocalZ;

            if (Physics.SphereCast(
                    pivotTransform.position,
                    cameraCollisionRadius,
                    direction,
                    out var hit,
                    maxDistance,
                    cameraCollisionLayers,
                    QueryTriggerInteraction.Ignore))
            {
                var allowedDistance = Mathf.Max(0f, hit.distance - cameraCollisionOffset);
                targetZ = -allowedDistance;

                if (Mathf.Abs(targetZ) < minimumCollisionOffset)
                {
                    targetZ = -minimumCollisionOffset;
                }
            }

            var local = managedCamera.transform.localPosition;
            local.z = Mathf.Lerp(local.z, targetZ, collisionLerpSpeed * Time.deltaTime);
            managedCamera.transform.localPosition = local;
        }
    }
}