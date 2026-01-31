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
    public class CharacterManager : MonoBehaviour, ICharacterManager
    {
        private Dictionary<Type, ICharacterComponent> _characterComponents = new();
        private Rigidbody _characterRigidBody;
        private NavMeshAgent _navMeshAgent;
        private float _npcTimer;

        [Header("Character Settings")]
        [SerializeField] private CharacterID characterID;
        
        [SerializeField] private CharacterState characterState;

        [Header("Animator Controller")]
        [SerializeField] private AnimatorController animatorController;

        [Header("Camera")]
        [SerializeField] private CameraManager cameraManager;

        private void Awake()
        {
            var monoBehaviours = GetComponents<MonoBehaviour>();
            for (var i = 0; i < monoBehaviours.Length; i++)
            {
                if (monoBehaviours[i] is ICharacterComponent characterComponent)
                {
                    characterComponent.ISetCharacterManager(this);
                    _characterComponents[characterComponent.GetType()] = characterComponent;
                }
            }
            _characterRigidBody = GetComponent<Rigidbody>();
            _navMeshAgent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (characterState == CharacterState.NPCControlled && ITryGetCharacterComponent<NPCScheduler>(out var nPCScheduler))
            {
                nPCScheduler.IHandleCharacterComponent();
            }
        }

        private void FixedUpdate()
        {
            if(characterState == CharacterState.PlayerControlled)
            {
                if(ITryGetCharacterComponent<PlayerMovement>(out var playerMovement)) playerMovement.IHandleCharacterComponent();
            }
        }

        private void LateUpdate()
        {
            if(characterState == CharacterState.PlayerControlled)
            {
                if(ITryGetCharacterComponent<PlayerCamera>(out var playerCamera)) playerCamera.IHandleCharacterComponent();
            }
        }

        public IAnimatorController IAnimatorController() => animatorController;

        public ICameraManager ICameraManager() => cameraManager;

        public Rigidbody ICharacterRigidbody() => _characterRigidBody;

        public bool ITryGetCharacterComponent<T>(out T value) where T : class, ICharacterComponent
        {
            if (_characterComponents.TryGetValue(typeof(T), out var component))
            {
                value = component as T;
                return value != null;
            }
            value = null;
            return false;
        }

        public void ISetCharacterState(CharacterState characterState)
        {
            this.characterState = characterState;
            _navMeshAgent.enabled = characterState == CharacterState.NPCControlled;
            GameEventSystem.Instance.Fire(new CharacterStateUpdated(characterID, characterState), CharacterManagerStatic.CHARACTER_MANAGER_CHANNEL);
        }
    }
}
