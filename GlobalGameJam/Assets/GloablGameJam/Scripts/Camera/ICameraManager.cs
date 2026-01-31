using UnityEngine;

namespace GloablGameJam.Scripts.Camera
{
    public interface ICameraManager
    {
        UnityEngine.Camera IManagedCamera();
        Transform IManagedCameraTransform() => IManagedCamera().transform;
        void IFollowTarget(Transform targetTransform);
        void IRotateCamera(Vector2 mouseInput);
    }
}
