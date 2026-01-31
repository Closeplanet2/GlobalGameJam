using CustomLibrary.Scripts.GameEventSystem;
using GloablGameJam.Events;
using GloablGameJam.Scripts.Animation;
using GloablGameJam.Scripts.Camera;
using GloablGameJam.Scripts.Player;
using UnityEngine;

namespace GloablGameJam.Scripts.Character
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerCamera))]
    public class CharacterManager : MonoBehaviour, ICharacterManager
    {
        private PlayerMovement _playerMovement;
        private PlayerCamera _playerCamera;
        private Rigidbody _characterRigidBody;

        [Header("Character Settings")]
        [SerializeField] private CharacterID characterID;
        
         [SerializeField] private CharacterState characterState;

        [Header("Animator Controller")]
        [SerializeField] private AnimatorController animatorController;

        [Header("Camera")]
        [SerializeField] private CameraManager cameraManager;

        private void Awake()
        {
            _playerMovement = GetComponent<PlayerMovement>();
            _playerCamera = GetComponent<PlayerCamera>();
            _characterRigidBody = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            _playerMovement.SetICharacterManager(this);
            _playerCamera.SetICharacterManager(this);
        }

        private void FixedUpdate()
        {
            if(characterState == CharacterState.PlayerControlled)
            {
                _playerMovement.HandleAllPlayerMovement();
            }
        }

        private void LateUpdate()
        {
            if(characterState == CharacterState.PlayerControlled)
            {
                _playerCamera.HandleAllPlayerCameraRotation();
            }
        }

        public IAnimatorController IAnimatorController() => animatorController;

        public ICameraManager ICameraManager() => cameraManager;

        public Rigidbody ICharacterRigidbody() => _characterRigidBody;

        public void ISetCharacterState(CharacterState characterState)
        {
            this.characterState = characterState;
            GameEventSystem.Instance.Fire(new CharacterStateUpdated(characterID, characterState), CharacterManagerStatic.CHARACTER_MANAGER_CHANNEL);
        }
    }
}
