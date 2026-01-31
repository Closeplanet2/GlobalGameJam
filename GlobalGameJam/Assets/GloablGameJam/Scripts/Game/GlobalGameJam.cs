using System;
using Assembly_CSharp_Editor.Assets.CustomLibrary.Scripts.PlayerInputManager;
using CustomLibrary.Events.PlayerInputManager;
using CustomLibrary.Scripts.GameEventSystem;
using GloablGameJam.Scripts.Camera;
using GloablGameJam.Scripts.Character;
using GloablGameJam.Scripts.NPC;
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

        public CharacterManager CurrentPlayer => _currentPlayer;

        [Header("References")]
        [SerializeField] private CameraManager _cameraManager;

        [Header("Mask Swap")]
        [SerializeField, Min(0f)] private float _swapRange = 6f;
        [SerializeField] private LayerMask _characterLayerMask = ~0;
        [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Swap Cooldowns")]
        [SerializeField, Min(0f)] private float _globalSwapCooldownSeconds = 0.75f;
        private float _nextAllowedGlobalSwapTime;

        [Header("Swap Consequences")]
        [SerializeField, Min(0f)] private float _leftBehindStunSeconds = 2.0f;

        [Header("Debug")]
        [SerializeField] private bool log = true;

        protected override void Awake()
        {
            base.Awake();

            if (_startingPlayer != null)
            {
                _startingPlayer.ISetCharacterState(CharacterState.PlayerControlled);
            }

            SetCurrentPlayer(_startingPlayer != null ? _startingPlayer : _currentPlayer);

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

            if (Time.time < _nextAllowedGlobalSwapTime) return;
            if (!_currentPlayer.ICanBeMaskSwapped()) return;

            var cam = _cameraManager.IManagedCamera();
            if (cam == null) return;

            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var target = FindTargetCharacter(ray);
            if (target == null) return;

            if (!target.ICanBeMaskSwapped()) return;

            var leftBehind = _currentPlayer;

            // Swap
            SwapControl(leftBehind, target);

            // Cooldowns
            _nextAllowedGlobalSwapTime = Time.time + _globalSwapCooldownSeconds;
            leftBehind.IMarkMaskSwappedNow();
            target.IMarkMaskSwappedNow();

            // Stun last body
            leftBehind.IStun(_leftBehindStunSeconds);

            // Alert all NPCs to investigate last body location
            AlertAllNpcs(leftBehind.transform.position);

            SetCurrentPlayer(target);

            if (log) Debug.Log($"[MaskSwap] player now '{target.name}', left '{leftBehind.name}' stunned+POI", this);
        }

        private void AlertAllNpcs(Vector3 poi)
        {
            for (var i = 0; i < NPCScheduler.All.Count; i++)
            {
                var scheduler = NPCScheduler.All[i];
                if (scheduler == null) continue;

                var cm = scheduler.GetComponentInParent<CharacterManager>();
                if (cm == null) continue;
                if (cm.IGetCharacterState() != CharacterState.NPCControlled) continue;

                var perception = scheduler.GetComponent<NPCPerception>();
                if (perception != null) perception.ISetAlerted();

                scheduler.ITryInterruptInvestigate(poi, replaceCurrent: true);
            }
        }

        private CharacterManager FindTargetCharacter(Ray ray)
        {
            var hits = Physics.RaycastAll(ray, _swapRange, _characterLayerMask, _triggerInteraction);
            if (hits == null || hits.Length == 0) return null;

            Array.Sort(hits, static (a, b) => a.distance.CompareTo(b.distance));

            for (var i = 0; i < hits.Length; i++)
            {
                var col = hits[i].collider;
                if (col == null) continue;

                if (IsSelfCollider(col)) continue;

                var cm = col.GetComponentInParent<CharacterManager>();
                if (cm == null) continue;
                if (cm == _currentPlayer) continue;

                if (cm.IGetCharacterState() != CharacterState.NPCControlled) continue;

                return cm;
            }

            return null;
        }

        private bool IsSelfCollider(Collider col)
        {
            for (var i = 0; i < _currentPlayerColliders.Length; i++)
            {
                if (_currentPlayerColliders[i] == col) return true;
            }

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