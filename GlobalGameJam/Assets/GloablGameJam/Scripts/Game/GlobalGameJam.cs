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
    public sealed class GlobalGameJam : GameEventMonoBehaviorInstance<GlobalGameJam>
    {
        [Header("Player")]
        [SerializeField] private CharacterManager _startingPlayer;

        private CharacterManager _currentPlayer;
        private Collider[] _currentPlayerColliders = Array.Empty<Collider>();

        [Header("References")]
        [SerializeField] private CameraManager _cameraManager;

        [Header("Mask Swap")]
        [SerializeField, Min(0f)] private float _swapRange = 6f;

        [Tooltip("Every character body collider must be on a layer included here.")]
        [SerializeField] private LayerMask _characterLayerMask = ~0;

        [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

        protected override void Awake()
        {
            base.Awake();

            if (_startingPlayer != null)
            {
                // Ensure we begin as the player.
                _startingPlayer.ISetCharacterState(CharacterState.PlayerControlled);
            }

            SetCurrentPlayer(_startingPlayer != null ? _startingPlayer : _currentPlayer);

            // Keep cursor locked for FPS-style swapping.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
            if (e.ActionKey != GGJ_PlayerInputKeys.DropMask) return;
            if (e.InputPhase != InputActionPhase.Started) return;

            TrySwapMaskByRaycast();
        }

        private void TrySwapMaskByRaycast()
        {
            if (_cameraManager == null) return;
            if (_currentPlayer == null) return;

            // Optional: prevent swapping while YOU are on cooldown too.
            if (!_currentPlayer.ICanBeMaskSwapped()) return;

            var cam = _cameraManager.IManagedCamera();
            if (cam == null) return;

            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var target = FindTargetCharacter(ray);
            if (target == null) return;

            if (!target.ICanBeMaskSwapped()) return;

            SwapControl(_currentPlayer, target);

            // Mark both as swapped to prevent ping-pong.
            _currentPlayer.IMarkMaskSwappedNow();
            target.IMarkMaskSwappedNow();

            SetCurrentPlayer(target);
        }

        private CharacterManager FindTargetCharacter(Ray ray)
        {
            var hits = Physics.RaycastAll(ray, _swapRange, _characterLayerMask, _triggerInteraction);
            if (hits == null || hits.Length == 0) return null;

            Array.Sort(hits, static (a, b) => a.distance.CompareTo(b.distance));

            for (var i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                var col = h.collider;
                if (col == null) continue;

                // Skip self colliders
                if (IsSelfCollider(col)) continue;

                var cm = col.GetComponentInParent<CharacterManager>();
                if (cm == null) continue;
                if (cm == _currentPlayer) continue;

                // Optional safety: only allow swapping into NPC-controlled characters
                // (prevents weirdness if multiple players exist).
                // If you want swapping into any character, remove this check.
                // if (cm.GetCharacterState() != CharacterState.NPCControlled) continue;

                return cm;
            }

            return null;
        }

        private bool IsSelfCollider(Collider col)
        {
            if (col == null) return false;

            for (var i = 0; i < _currentPlayerColliders.Length; i++)
            {
                if (_currentPlayerColliders[i] == col) return true;
            }

            // Fallback for odd hierarchies
            var cm = col.GetComponentInParent<CharacterManager>();
            return cm != null && cm == _currentPlayer;
        }

        private static void SwapControl(CharacterManager fromPlayer, CharacterManager toNpc)
        {
            fromPlayer.ISetCharacterState(CharacterState.NPCControlled);
            toNpc.ISetCharacterState(CharacterState.PlayerControlled);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}