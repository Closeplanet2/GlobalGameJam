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

        [Header("Character Settings")]
        [SerializeField] private CharacterID _characterID;
        [SerializeField] private CharacterState _characterState;
        [SerializeField] private GameObject maskObject;

        [Header("Animator Controller")]
        [SerializeField] private AnimatorController _animatorController;

        [Header("Camera")]
        [SerializeField] private CameraManager _cameraManager;

        private void Awake()
        {
            CacheCharacterComponents();
            _rigidbody = GetComponent<Rigidbody>();
            _agent = GetComponent<NavMeshAgent>();
            ApplyState(_characterState, fireEvent: false);
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
            if (_characterState != CharacterState.NPCControlled) return;
            if (ITryGetCharacterComponent<NPCScheduler>(out var scheduler)) scheduler.IHandleCharacterComponent();
        }

        private void FixedUpdate()
        {
            if (_characterState != CharacterState.PlayerControlled) return;
            if (ITryGetCharacterComponent<PlayerMovement>(out var movement)) movement.IHandleCharacterComponent();
        }

        private void LateUpdate()
        {
            if (_characterState != CharacterState.PlayerControlled) return;
            if (ITryGetCharacterComponent<PlayerCamera>(out var camera)) camera.IHandleCharacterComponent();
        }

        public IAnimatorController IAnimatorController() => _animatorController;

        public ICameraManager ICameraManager() => _cameraManager;

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

        public void ISetCharacterState(CharacterState state)
        {
            ApplyState(state, fireEvent: true);
        }

        private void ApplyState(CharacterState state, bool fireEvent)
        {
            _characterState = state;
            if (_agent != null)  _agent.enabled = state == CharacterState.NPCControlled;
            if(maskObject != null) maskObject.SetActive(state == CharacterState.PlayerControlled);
            if (fireEvent && GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.Fire(new CharacterStateUpdated(_characterID, _characterState), CharacterManagerStatic.CHARACTER_MANAGER_CHANNEL);
            }
        }
    }
}