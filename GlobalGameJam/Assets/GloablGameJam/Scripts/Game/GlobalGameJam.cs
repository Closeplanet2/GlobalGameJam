using System;
using Assembly_CSharp_Editor.Assets.CustomLibrary.Scripts.PlayerInputManager;
using CustomLibrary.Events.PlayerInputManager;
using CustomLibrary.Scripts.GameEventSystem;
using GloablGameJam.Scripts.Camera;
using GloablGameJam.Scripts.Character;
using GloablGameJam.Scripts.Combat;
using GloablGameJam.Scripts.NPC;
using GloablGameJam.Scripts.PlayerInputManager;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GloablGameJam.Scripts.Game
{
    public sealed class GlobalGameJam : GameEventMonoBehaviorInstance<GlobalGameJam>
    {
        [Header("Player")]
        [SerializeField] private CharacterManager startingPlayer;
        [SerializeField] private Transform respawnPoint;

        private CharacterManager _currentPlayer;
        private Collider[] _currentPlayerColliders = Array.Empty<Collider>();
        private Health _currentPlayerHealth;

        [Header("References")]
        [SerializeField] private CameraManager cameraManager;

        [Header("Mask Swap")]
        [SerializeField, Min(0f)] private float swapRange = 6f;
        [SerializeField] private LayerMask characterLayerMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Swap Cooldowns")]
        [SerializeField, Min(0f)] private float globalSwapCooldownSeconds = 0.75f;
        private float _nextAllowedGlobalSwapTime;

        [Header("Cooldown UI")]
        [Tooltip("TextMeshPro that shows the global swap cooldown time left.")]
        [SerializeField] private TMP_Text swapCooldownText;

        [Tooltip("Optional object to enable/disable with the cooldown text (leave null to use text GameObject).")]
        [SerializeField] private GameObject swapCooldownRoot;

        [Header("Swap Consequences")]
        [SerializeField, Min(0f)] private float leftBehindStunSeconds = 2.0f;

        [Header("Interaction UI")]
        [SerializeField] private GameObject interactionPrompt;

        [Header("SFX")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip swapSfxClip;

        [Header("Debug")]
        [SerializeField] private bool log = true;

        [Header("Alert Behaviour")]
        [SerializeField] private bool allowImmediateChaseOnAlert = true;

        private bool _handlingDeath;
        private bool _promptVisible;

        protected override void Awake()
        {
            base.Awake();

            if (startingPlayer != null)
            {
                startingPlayer.ISetCharacterState(CharacterState.PlayerControlled);
            }

            if (respawnPoint == null && startingPlayer != null)
            {
                respawnPoint = startingPlayer.transform;
            }

            SetCurrentPlayer(startingPlayer != null ? startingPlayer : _currentPlayer);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            SetInteractionPromptVisible(false);
            SetCooldownUiActive(false);
        }

        public CharacterManager GetCurrentPlayer() => _currentPlayer;

        private void Update()
        {
            UpdateInteractionPrompt();
            UpdateSwapCooldownUi();
        }

        private void UpdateSwapCooldownUi()
        {
            if (swapCooldownText == null && swapCooldownRoot == null) return;

            var remaining = Mathf.Max(0f, _nextAllowedGlobalSwapTime - Time.time);
            var active = remaining > 0.001f;

            if (!active)
            {
                SetCooldownUiActive(false);
                return;
            }

            if (swapCooldownText != null)
            {
                // e.g. "0.7s"
                swapCooldownText.text = $"COOLDOWN... {remaining:0.0}s";
            }

            SetCooldownUiActive(true);
        }

        private void SetCooldownUiActive(bool active)
        {
            if (swapCooldownRoot != null)
            {
                if (swapCooldownRoot.activeSelf != active) swapCooldownRoot.SetActive(active);
                return;
            }

            if (swapCooldownText != null)
            {
                var go = swapCooldownText.gameObject;
                if (go.activeSelf != active) go.SetActive(active);
            }
        }

        private void UpdateInteractionPrompt()
        {
            if (interactionPrompt == null)
            {
                _promptVisible = false;
                return;
            }

            if (cameraManager == null || _currentPlayer == null)
            {
                SetInteractionPromptVisible(false);
                return;
            }

            // If global cooldown is active or the player body cannot swap, hide prompt.
            if (Time.time < _nextAllowedGlobalSwapTime || !_currentPlayer.ICanBeMaskSwapped())
            {
                SetInteractionPromptVisible(false);
                return;
            }

            var cam = cameraManager.IManagedCamera();
            if (cam == null)
            {
                SetInteractionPromptVisible(false);
                return;
            }

            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var target = FindTargetCharacter(ray);

            var show = target != null && target.ICanBeMaskSwapped();
            SetInteractionPromptVisible(show);
        }

        private void SetInteractionPromptVisible(bool visible)
        {
            if (interactionPrompt == null) return;
            if (_promptVisible == visible) return;

            _promptVisible = visible;
            interactionPrompt.SetActive(visible);
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

        private void OnCurrentPlayerDied(Health hp)
        {
            if (_handlingDeath) return;
            _handlingDeath = true;

            SetInteractionPromptVisible(false);
            SetCooldownUiActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var session = GameSessionManager.Instance;
            if (session != null)
            {
                session.NotifyPlayerDied();
                return;
            }

            Debug.LogError("[GlobalGameJam] No GameSessionManager present! Cannot process death.");
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
            if (cameraManager == null) return;
            if (_currentPlayer == null) return;

            // Global cooldown
            if (Time.time < _nextAllowedGlobalSwapTime) return;

            // Per-body cooldown (prevents instant re-swap spam between two bodies)
            if (!_currentPlayer.ICanBeMaskSwapped()) return;

            var cam = cameraManager.IManagedCamera();
            if (cam == null) return;

            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var target = FindTargetCharacter(ray);
            if (target == null) return;

            if (!target.ICanBeMaskSwapped()) return;

            var leftBehind = _currentPlayer;

            // Swap control
            leftBehind.ISetCharacterState(CharacterState.NPCControlled);
            target.ISetCharacterState(CharacterState.PlayerControlled);

            // Set cooldowns
            _nextAllowedGlobalSwapTime = Time.time + globalSwapCooldownSeconds;
            leftBehind.IMarkMaskSwappedNow();
            target.IMarkMaskSwappedNow();

            // Consequence
            leftBehind.IStun(leftBehindStunSeconds);

            AlertAllNpcs(leftBehind.transform.position);

            SetCurrentPlayer(target);

            PlaySfx(swapSfxClip);

            // Unlock key if this body grants one
            var session = GameSessionManager.Instance;
            if (session != null && target.IGrantsKeyOnPossess(out var keyId))
            {
                if (session.TryUnlockKey(keyId) && log)
                {
                    Debug.Log($"[Key] Unlocked '{keyId}' by possessing '{target.name}'", this);
                }
            }

            if (log)
            {
                Debug.Log($"[MaskSwap] now '{target.name}', left '{leftBehind.name}'", this);
            }
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            if (sfxSource == null) return;
            sfxSource.PlayOneShot(clip);
        }

        private void AlertAllNpcs(Vector3 poi)
        {
            var player = _currentPlayer;

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

                    if (allowImmediateChaseOnAlert && player != null && perception.ICanSeeNow(player))
                    {
                        scheduler.ITryInterruptChase(player, replaceCurrent: true);
                        continue;
                    }
                }

                scheduler.ITryInterruptInvestigate(poi, replaceCurrent: true);
            }
        }

        private CharacterManager FindTargetCharacter(Ray ray)
        {
            var hits = Physics.RaycastAll(ray, swapRange, characterLayerMask, triggerInteraction);
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
    }
}