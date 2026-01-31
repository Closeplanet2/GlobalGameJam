using System;
using Assembly_CSharp_Editor.Assets.CustomLibrary.Scripts.PlayerInputManager;
using CustomLibrary.Events.PlayerInputManager;
using CustomLibrary.Scripts.GameEventSystem;
using GloablGameJam.Scripts.Camera;
using GloablGameJam.Scripts.Character;
using GloablGameJam.Scripts.PlayerInputManager;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GloablGameJam.Scripts.Game
{
    /// <summary>
    /// Handles mask swapping (possession) by raycasting from a camera-origin point
    /// towards a configurable crosshair screen point. Skips current player colliders.
    /// </summary>
    public sealed class GlobalGameJam : GameEventMonoBehaviorInstance<GlobalGameJam>
    {
        [Header("Player")]
        [SerializeField] private CharacterManager _startingPlayer;
        private CharacterManager _currentPlayer;

        private Collider[] _currentPlayerColliders = Array.Empty<Collider>();

        [Header("Raycast Source")]
        [Tooltip("Camera used to build the aim direction (ScreenPointToRay).")]
        [SerializeField] private CameraManager _aimCamera;

        [Tooltip("Where the ray begins from in world space. Put this on the camera as a child (RaycastOrigin). If null, uses aim camera transform.")]
        [SerializeField] private Transform _rayOrigin;

        [Header("Crosshair / Aim")]
        [Tooltip("If true, uses the screen point below instead of dead-centre (0.5, 0.5).")]
        [SerializeField] private bool _useCrosshairScreenPoint = false;

        [Tooltip("Viewport coordinates (0..1). (0.5,0.5) = centre. Use this if your crosshair isn't centered.")]
        [SerializeField] private Vector2 _crosshairViewportPoint = new Vector2(0.5f, 0.5f);

        [Header("Mask Swap")]
        [SerializeField, Min(0f)] private float _swapRange = 6f;

        [Tooltip("Layers that contain character colliders (NPCs + Player).")]
        [SerializeField] private LayerMask _characterLayerMask = ~0;

        [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Aim Assist (Jam-friendly)")]
        [Tooltip("SphereCast radius. Helps when characters are thin. 0 disables aim assist.")]
        [SerializeField, Min(0f)] private float _aimAssistRadius = 0.25f;

        [Header("Cursor")]
        [SerializeField] private bool _lockCursorOnEnable = true;
        [SerializeField] private bool _unlockOnEscape = true;

        [Header("Debug")]
        [SerializeField] private bool _debugRay = true;
        [SerializeField, Min(0f)] private float _debugSeconds = 1.25f;
        [SerializeField] private bool _verboseLogs = true;

        protected override void Awake()
        {
            base.Awake();

            if (_rayOrigin == null && _aimCamera != null)
            {
                _rayOrigin = _aimCamera.transform;
            }

            SetCurrentPlayer(_startingPlayer != null ? _startingPlayer : _currentPlayer);

            if (_lockCursorOnEnable)
            {
                SetCursorLocked(true);
            }
        }

        private void Update()
        {
            if (_unlockOnEscape && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
            }
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void SetCurrentPlayer(CharacterManager player)
        {
            _currentPlayer = player;
            _currentPlayerColliders = player != null
                ? player.GetComponentsInChildren<Collider>(includeInactive: true)
                : Array.Empty<Collider>();
        }

        [EventHandler(Channel = PlayerInputManagerStatic.PLAYER_INPUT_MANAGER_CHANNEL, IgnoreCancelled = false)]
        public void OnPlayerInputEvent(PlayerInputEvent<GGJ_PlayerInputKeys> e)
        {
            // You are using DropMask as the swap action currently.
            if (e.ActionKey != GGJ_PlayerInputKeys.DropMask) return;
            if (e.InputPhase != InputActionPhase.Started) return;

            TrySwapMaskByRaycast();
        }

        private void TrySwapMaskByRaycast()
        {
            if (_aimCamera == null)
            {
                Log("Swap failed: _aimCamera is null.");
                return;
            }

            if (_rayOrigin == null)
            {
                Log("Swap failed: _rayOrigin is null.");
                return;
            }

            if (_currentPlayer == null)
            {
                Log("Swap failed: _currentPlayer is null. Assign _startingPlayer in inspector.");
                return;
            }

            var ray = BuildAimRay();

            if (_debugRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * _swapRange, Color.magenta, _debugSeconds);
            }

            // Try SphereCast first (aim assist), then fallback to RaycastAll
            var target = TryFindTargetBySphereCast(ray) ?? TryFindTargetByRaycastAllSkippingSelf(ray);

            if (target == null)
            {
                Log("Swap failed: no valid NPC hit (after skipping self).");
                return;
            }

            SwapControl(_currentPlayer, target);
            SetCurrentPlayer(target);

            // Keep cursor locked during gameplay
            SetCursorLocked(true);

            Log($"Swap success: now controlling '{target.name}'.");
        }

        private Ray BuildAimRay()
        {
            // Build direction using camera + viewport point (supports off-centre crosshair).
            var vp = _useCrosshairScreenPoint ? _crosshairViewportPoint : new Vector2(0.5f, 0.5f);

            // Camera gives us direction; we override origin to be our ray origin transform.
            var cameraRay = _aimCamera.IManagedCamera().ViewportPointToRay(new Vector3(vp.x, vp.y, 0f));
            var origin = _rayOrigin.position;
            var dir = cameraRay.direction;

            return new Ray(origin, dir);
        }

        private CharacterManager TryFindTargetBySphereCast(Ray ray)
        {
            if (_aimAssistRadius <= 0f) return null;

            if (!Physics.SphereCast(ray, _aimAssistRadius, out var hit, _swapRange, _characterLayerMask, _triggerInteraction))
            {
                return null;
            }

            if (_debugRay)
            {
                Debug.DrawLine(ray.origin, hit.point, Color.cyan, _debugSeconds);
            }

            if (IsSelfCollider(hit.collider)) return null;

            var cm = hit.collider.GetComponentInParent<CharacterManager>();
            if (cm == null) return null;
            if (cm == _currentPlayer) return null;

            Log($"SphereCast selected: '{hit.collider.name}' -> '{cm.name}' (dist={hit.distance:0.00})");
            return cm;
        }

        private CharacterManager TryFindTargetByRaycastAllSkippingSelf(Ray ray)
        {
            var hits = Physics.RaycastAll(ray, _swapRange, _characterLayerMask, _triggerInteraction);
            if (hits == null || hits.Length == 0)
            {
                Log("RaycastAll: no hits.");
                return null;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (var i = 0; i < hits.Length; i++)
            {
                var h = hits[i];

                if (_debugRay)
                {
                    Debug.DrawLine(ray.origin, h.point, Color.yellow, _debugSeconds);
                }

                // Skip self colliders
                if (IsSelfCollider(h.collider)) continue;

                var cm = h.collider.GetComponentInParent<CharacterManager>();
                if (cm == null) continue;
                if (cm == _currentPlayer) continue;

                Log($"RaycastAll selected: '{h.collider.name}' -> '{cm.name}' (dist={h.distance:0.00})");
                return cm;
            }

            Log("RaycastAll: hits found, but none belonged to a non-self CharacterManager.");
            return null;
        }

        private bool IsSelfCollider(Collider col)
        {
            // Fast-path: if no cached colliders, just compare CharacterManager parent.
            if (_currentPlayer == null) return false;
            if (col == null) return false;

            for (var i = 0; i < _currentPlayerColliders.Length; i++)
            {
                if (_currentPlayerColliders[i] == col) return true;
            }

            // Also guard weird hierarchy cases
            var cm = col.GetComponentInParent<CharacterManager>();
            return cm != null && cm == _currentPlayer;
        }

        private static void SwapControl(CharacterManager fromPlayer, CharacterManager toNpc)
        {
            fromPlayer.ISetCharacterState(CharacterState.NPCControlled);
            toNpc.ISetCharacterState(CharacterState.PlayerControlled);
        }

        private void Log(string msg)
        {
            if (!_verboseLogs) return;
            Debug.Log($"[GlobalGameJam] {msg}", this);
        }
    }
}