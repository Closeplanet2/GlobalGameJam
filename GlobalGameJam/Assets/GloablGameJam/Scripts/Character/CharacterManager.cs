using System;
using System.Collections.Generic;
using CustomLibrary.Scripts.GameEventSystem;
using GloablGameJam.Events;
using GloablGameJam.Scripts.Animation;
using GloablGameJam.Scripts.Camera;
using GloablGameJam.Scripts.NPC;
using GloablGameJam.Scripts.Player;
using UnityEngine;
using UnityEngine.AI;

namespace GloablGameJam.Scripts.Character
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CharacterManager : MonoBehaviour, ICharacterManager
    {
        private readonly Dictionary<Type, ICharacterComponent> _characterComponents = new();

        private Rigidbody _rigidbody;
        private NavMeshAgent _agent;

        private float _nextAllowedMaskSwapTime;
        private float _stunUntilTime;

        [Header("Character Settings")]
        [SerializeField] private CharacterID characterID;
        [SerializeField] private CharacterState characterState;
        [SerializeField, Min(0f)] private float stunSecondsDefault = 2.0f;

        [Header("Mask Visual")]
        [SerializeField] private GameObject maskObject;

        [Header("Animator Controller")]
        [SerializeField] private AnimatorController animatorController;

        [Header("Camera")]
        [SerializeField] private CameraManager cameraManager;

        [Header("Mask Swap")]
        [SerializeField, Min(0f)] private float maskSwapCooldownSeconds = 1.0f;

        [Header("Key Ownership")]
        [Tooltip("If enabled, possessing this character unlocks a unique key id.")]
        [SerializeField] private bool grantsKeyOnPossess;
        [SerializeField] private string keyId = "";

        private void Awake()
        {
            CacheCharacterComponents();

            _rigidbody = GetComponent<Rigidbody>();
            _agent = GetComponent<NavMeshAgent>();

            ApplyState(characterState, fireEvent: false);
        }

        private void CacheCharacterComponents()
        {
            var monoBehaviours = GetComponents<MonoBehaviour>();
            for (var i = 0; i < monoBehaviours.Length; i++)
            {
                if (monoBehaviours[i] is not ICharacterComponent c) continue;
                c.ISetCharacterManager(this);
                _characterComponents[c.GetType()] = c;
            }
        }

        private void Update()
        {
            if (characterState != CharacterState.NPCControlled) return;

            if (ITryGetCharacterComponent<NPCScheduler>(out var scheduler))
            {
                scheduler.IHandleCharacterComponent();
            }

            if (ITryGetCharacterComponent<NPCPerception>(out var perception))
            {
                perception.IHandleCharacterComponent();
            }
        }

        private void FixedUpdate()
        {
            if (characterState != CharacterState.PlayerControlled) return;
            if (IIsStunned()) return;

            if (ITryGetCharacterComponent<PlayerMovement>(out var movement))
            {
                movement.IHandleCharacterComponent();
            }
        }

        private void LateUpdate()
        {
            if (characterState != CharacterState.PlayerControlled) return;
            if (IIsStunned()) return;

            if (ITryGetCharacterComponent<PlayerCamera>(out var camera))
            {
                camera.IHandleCharacterComponent();
            }
        }

        public CharacterState IGetCharacterState() => characterState;
        public IAnimatorController IAnimatorController() => animatorController;
        public ICameraManager ICameraManager() => cameraManager;
        public Rigidbody ICharacterRigidbody() => _rigidbody;

        public bool ITryGetCharacterComponent<T>(out T value) where T : class, ICharacterComponent
        {
            if (_characterComponents.TryGetValue(typeof(T), out var component) && component is T typed)
            {
                value = typed;
                return true;
            }

            value = null;
            return false;
        }

        public bool IIsStunned() => Time.time < _stunUntilTime;

        public void IStun(float seconds)
        {
            _stunUntilTime = Mathf.Max(_stunUntilTime, Time.time + Mathf.Max(0f, seconds));

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }

        public void IStunDefault() => IStun(stunSecondsDefault);

        public bool ICanBeMaskSwapped() => Time.time >= _nextAllowedMaskSwapTime;

        public void IMarkMaskSwappedNow()
        {
            _nextAllowedMaskSwapTime = Time.time + maskSwapCooldownSeconds;
        }

        public bool IGrantsKeyOnPossess(out string ownedKeyId)
        {
            ownedKeyId = keyId;
            return grantsKeyOnPossess && !string.IsNullOrWhiteSpace(keyId);
        }

        public void ISetCharacterState(CharacterState state)
        {
            ApplyState(state, fireEvent: true);
        }

        private void ApplyState(CharacterState state, bool fireEvent)
        {
            characterState = state;

            var npc = state == CharacterState.NPCControlled;

            if (_agent != null) _agent.enabled = npc;
            if (_rigidbody != null) _rigidbody.isKinematic = npc;

            if (maskObject != null)
            {
                maskObject.SetActive(state == CharacterState.PlayerControlled);
            }

            if (npc && _rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            if (fireEvent && GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.Fire(
                    new CharacterStateUpdated(characterID, characterState),
                    CharacterManagerStatic.CHARACTER_MANAGER_CHANNEL);
            }
        }
    }
}