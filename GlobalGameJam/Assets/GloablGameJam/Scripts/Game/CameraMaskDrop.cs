using System;
using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Scripts.Game
{
    public class CameraMaskDrop : MonoBehaviour
    {
        [Header("Hold")]
        [SerializeField] private float holdSeconds = 5f;

        [Header("Zoom")]
        [SerializeField] private float zoomOutFov = 75f;
        [SerializeField] private float zoomSpeed = 2f;

        [Header("Shake")]
        [SerializeField] private float maxShakeAmplitude = 0.12f;
        [SerializeField] private float maxShakeFrequency = 14f;

        public event Action<CharacterManager> DroppedMask;

        private CharacterManager _target;
        private float _defaultFov;
        private float _holdTimer;
        private bool _isHolding;
        private bool _completed;

        private Transform _camTransform;
        private Vector3 _baseLocalPos;
        private float _shakeSeed;

        public void SetTarget(CharacterManager character)
        {
            _target = character;
            ResetState();
            CacheCameraDefaults();
        }

        public void SetHolding(bool holding)
        {
            _isHolding = holding;

            if (!holding)
            {
                _holdTimer = 0f;
                _completed = false;
            }
        }

        private void Update()
        {
            if (_target == null) return;
            var cam = GetManagedCamera();
            if (cam == null) return;
            EnsureCameraCached(cam);
            if (!_isHolding)
            {
                ReturnToDefault(cam);
                return;
            }
            _holdTimer += Time.deltaTime;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, zoomOutFov, zoomSpeed * Time.deltaTime);
            ApplyShake(cam);
            if (!_completed && _holdTimer >= holdSeconds)
            {
                _completed = true;
                CompleteDropMask();
            }
        }

        private UnityEngine.Camera GetManagedCamera()
        {
            if (_target == null) return null;
            var camMgr = _target.ICameraManager();
            if (camMgr == null) return null;
            return camMgr.IManagedCamera();
        }

        private void CacheCameraDefaults()
        {
            var cam = GetManagedCamera();
            if (cam == null) return;
            _defaultFov = cam.fieldOfView;
            _camTransform = cam.transform;
            _baseLocalPos = _camTransform.localPosition;
            _shakeSeed = UnityEngine.Random.value * 1000f;
        }

        private void EnsureCameraCached(UnityEngine.Camera cam)
        {
            if (_camTransform == cam.transform) return;
            _defaultFov = cam.fieldOfView;
            _camTransform = cam.transform;
            _baseLocalPos = _camTransform.localPosition;
            _shakeSeed = UnityEngine.Random.value * 1000f;
        }

        private void ReturnToDefault(UnityEngine.Camera cam)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, _defaultFov, zoomSpeed * Time.deltaTime);
            if (_camTransform != null)
            {
                _camTransform.localPosition =  Vector3.Lerp(_camTransform.localPosition, _baseLocalPos, zoomSpeed * Time.deltaTime);
            }
        }

        private void ApplyShake(UnityEngine.Camera cam)
        {
            if (_camTransform == null) return;
            var t = Mathf.InverseLerp(_defaultFov, zoomOutFov, cam.fieldOfView);
            t = Mathf.Clamp01(t);
            var amp = maxShakeAmplitude * t;
            var freq = Mathf.Lerp(0f, maxShakeFrequency, t);
            var time = Time.time * freq;
            var x = (Mathf.PerlinNoise(_shakeSeed, time) - 0.5f) * 2f;
            var y = (Mathf.PerlinNoise(_shakeSeed + 10f, time) - 0.5f) * 2f;
            _camTransform.localPosition = _baseLocalPos + new Vector3(x, y, 0f) * amp;
        }

        private void CompleteDropMask()
        {
            var old = _target;
            _isHolding = false;
            _holdTimer = 0f;
            old.ISetCharacterState(CharacterState.NPCControlled);
            DroppedMask?.Invoke(old);
            _target = null;
        }

        private void ResetState()
        {
            _holdTimer = 0f;
            _completed = false;
            _isHolding = false;
        }
    }
}