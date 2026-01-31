using Assembly_CSharp_Editor.Assets.CustomLibrary.Scripts.PlayerInputManager;
using CustomLibrary.Events.PlayerInputManager;
using CustomLibrary.Scripts.GameEventSystem;
using GloablGameJam.Scripts.Animation;
using GloablGameJam.Scripts.Character;
using GloablGameJam.Scripts.PlayerInputManager;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace GloablGameJam.Scripts.Player
{
    public class PlayerMovement : GameEventInstance
    {
        private Vector3 _playerMoveDirection;
        private Vector3 _playerRotationDirection;
        private Vector2 _playerMovementInput;

        private ICharacterManager _characterManager;

        [Header("Movement Speed")]
        [SerializeField] private float movementSpeed;
        [SerializeField] private float rotationSpeed;

        [EventHandler(Channel = PlayerInputManagerStatic.PLAYER_INPUT_MANAGER_CHANNEL, IgnoreCancelled = false)]
        public void OnPlayerInputEvent(PlayerInputEvent<GGJ_PlayerInputKeys> playerInputEvent)
        {
            var phase = playerInputEvent.InputPhase;
            if (phase == UnityEngine.InputSystem.InputActionPhase.Performed || phase == UnityEngine.InputSystem.InputActionPhase.Canceled)
            {
                if(playerInputEvent.ActionKey == GGJ_PlayerInputKeys.CharacterMovement)
                {
                    _playerMovementInput = playerInputEvent.CallbackContext.ReadValue<Vector2>();
                }
            }
        }

        public void SetICharacterManager(ICharacterManager characterManager)
        {
            _characterManager = characterManager;
        }

        public void HandleAllPlayerMovement()
        {
            var moveAmount = Mathf.Clamp01(Mathf.Abs(_playerMovementInput.x) + Mathf.Abs(_playerMovementInput.y));
            _characterManager.IAnimatorController().IUpdateFloatValue(AnimatorKey.Horizontal, moveAmount);
            HandlePlayerMovement();
            HandlePlayerRotation();
        }

        private void HandlePlayerMovement()
        {
            _playerMoveDirection = _characterManager.ICameraManager().IManagedCameraTransform().forward * _playerMovementInput.y;
            _playerMoveDirection += _characterManager.ICameraManager().IManagedCameraTransform().right * _playerMovementInput.x;
            _playerMoveDirection.y = 0f;          
            _playerMoveDirection.Normalize();
            _playerMoveDirection *= movementSpeed;
            _characterManager.ICharacterRigidbody().linearVelocity = _playerMoveDirection;
        }

        private void HandlePlayerRotation()
        {
            var cameraTransform = _characterManager.ICameraManager().IManagedCameraTransform();
            _playerRotationDirection = (cameraTransform.forward * _playerMovementInput.y) + (cameraTransform.right * _playerMovementInput.x);
            _playerRotationDirection.y = 0f;
            if (_playerRotationDirection.sqrMagnitude < 0.0001f) return;
            _playerRotationDirection.Normalize();
            var targetRotation = Quaternion.LookRotation(_playerRotationDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}