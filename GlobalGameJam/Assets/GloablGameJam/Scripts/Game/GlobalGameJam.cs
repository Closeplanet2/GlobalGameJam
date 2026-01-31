using System;
using Assembly_CSharp_Editor.Assets.CustomLibrary.Scripts.PlayerInputManager;
using CustomLibrary.Events.PlayerInputManager;
using CustomLibrary.Scripts.GameEventSystem;
using GloablGameJam.Scripts.Camera;
using GloablGameJam.Scripts.Character;
using GloablGameJam.Scripts.Combat;
using GloablGameJam.Scripts.NPC;
using GloablGameJam.Scripts.PlayerInputManager;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GloablGameJam.Scripts.Game
{
    public sealed class GlobalGameJam : GameEventMonoBehaviorInstance<GlobalGameJam>
    {
        [Header("Player")]
        [SerializeField] private CharacterManager _startingPlayer;
        [SerializeField] private Transform _respawnPoint;

        private CharacterManager _currentPlayer;
        private Collider[] _currentPlayerColliders = Array.Empty<Collider>();
        private Health _currentPlayerHealth;

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

        [Header("Respawn")]
        [SerializeField, Min(0f)] private float _respawnInvulnerableSeconds = 1.0f;

        [Header("Debug")]
        [SerializeField] private bool log = true;

        [Header("Alert Behaviour")]
        [SerializeField] private bool _allowImmediateChaseOnAlert = true;
        [SerializeField, Min(0f)] private float _immediateChaseSightSeconds = 1.0f;

        protected override void Awake()
        {
            base.Awake();

            if (_startingPlayer != null)
            {
                _startingPlayer.ISetCharacterState(CharacterState.PlayerControlled);
            }

            SetCurrentPlayer(_startingPlayer != null ? _startingPlayer : _currentPlayer);

            if (_respawnPoint == null && _startingPlayer != null)
            {
                // Fallback: spawn where starting player begins.
                _respawnPoint = _startingPlayer.transform;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void SetCurrentPlayer(CharacterManager player)
        {
            if (_currentPlayerHealth != null)
            {
                _currentPlayerHealth.Died -= OnCurrentPlayerDied;
            }

            _currentPlayer = player;

            _currentPlayerColliders = player != null
                ? player.GetComponentsInChildren<Collider>(includeInactive: true)
                : Array.Empty<Collider>();

            _currentPlayerHealth = player != null ? player.GetComponent<Health>() : null;
            if (_currentPlayerHealth != null)
            {
                _currentPlayerHealth.Died += OnCurrentPlayerDied;
            }
        }

        private bool _isReloading;

        private void OnCurrentPlayerDied(Health hp)
        {
            if (_isReloading) return;
            _isReloading = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }

        private void Respawn()
        {
            if (_startingPlayer == null) return;
            if (_respawnPoint == null) return;

            // Return control to the starting player for simplicity.
            if (_currentPlayer != null && _currentPlayer != _startingPlayer)
            {
                _currentPlayer.ISetCharacterState(CharacterState.NPCControlled);
            }

            _startingPlayer.ISetCharacterState(CharacterState.PlayerControlled);
            _startingPlayer.transform.position = _respawnPoint.position;
            _startingPlayer.transform.rotation = _respawnPoint.rotation;

            var hp = _startingPlayer.GetComponent<Health>();
            if (hp != null)
            {
                hp.HealFull();
                hp.GrantInvulnerability(_respawnInvulnerableSeconds);
            }

            ClearNpcAlertState();
            SetCurrentPlayer(_startingPlayer);

            _nextAllowedGlobalSwapTime = Time.time + _globalSwapCooldownSeconds;

            if (log) Debug.Log("[Respawn] Player respawned + NPCs reset", this);
        }

        private void ClearNpcAlertState()
        {
            for (var i = 0; i < NPCScheduler.All.Count; i++)
            {
                var scheduler = NPCScheduler.All[i];
                if (scheduler == null) continue;

                var perception = scheduler.GetComponent<NPCPerception>();
                // Just letting alert time expire is fine; but we can hard-clear by setting alert far in the past.
                if (perception != null)
                {
                    // No direct clear method; simplest is re-create with zero duration:
                    perception.ISetAlerted(); // refresh
                }
            }
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

            SwapControl(leftBehind, target);

            _nextAllowedGlobalSwapTime = Time.time + _globalSwapCooldownSeconds;
            leftBehind.IMarkMaskSwappedNow();
            target.IMarkMaskSwappedNow();

            leftBehind.IStun(_leftBehindStunSeconds);

            AlertAllNpcs(leftBehind.transform.position);

            SetCurrentPlayer(target);

            if (log) Debug.Log($"[MaskSwap] now '{target.name}', left '{leftBehind.name}' (stunned+POI)", this);
        }

        private void AlertAllNpcs(Vector3 poi)
        {
            var player = _currentPlayer; // after swap, current player already updated by SetCurrentPlayer
            for (var i = 0; i < NPCScheduler.All.Count; i++)
            {
                var scheduler = NPCScheduler.All[i];
                if (scheduler == null) continue;

                var cm = scheduler.GetComponentInParent<CharacterManager>();
                if (cm == null) continue;
                if (cm.IGetCharacterState() != CharacterState.NPCControlled) continue;

                var perception = scheduler.GetComponent<NPCPerception>();
                if (perception != null)
                {
                    perception.ISetAlerted();

                    // Option: if they can see you right now, go straight to chase.
                    if (_allowImmediateChaseOnAlert && player != null && perception.ICanSeeNow(player))
                    {
                        scheduler.ITryInterruptChase(player, replaceCurrent: true);
                        continue;
                    }
                }

                // Default: investigate the last body spot.
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