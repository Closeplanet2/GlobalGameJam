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
    public class PlayerMovement : GameEventInstance, ICharacterComponent
    {
        private ICharacterManager _characterManager;
        private Vector3 _playerMoveDirection;
        private Vector3 _playerRotationDirection;
        private Vector2 _playerMovementInput;
        private bool _isPlayerCrouch;
        private bool _isPlayerSprinting;
        private bool _isGrounded;
        private float _inAirTimer;

        [Header("Movement Enables")]
        [SerializeField] private bool crouchEnabled = true;
        [SerializeField] private bool sprintingEnabled = true;
        [SerializeField] private bool fallingAndLandingEnabled = true;

        [Header("Movement Speed")]
        [SerializeField] private float crouchSpeed;
        [SerializeField] private float walkingSpeed;
        [SerializeField] private float runningSpeed;
        [SerializeField] private float sprintingSpeed;

        [Header("Animation Values")]
        [SerializeField] private float sprintBlendTreeValue = 2;
        [SerializeField] private float crouchBlendTreeValue = 2;

        [Header("Rotation Speed")]
        [SerializeField] private float rotationSpeed;

        [Header("Falling And Landing")]
        [SerializeField] private float leapingVelocity;
        [SerializeField] private float fallingVelocity;
        [SerializeField] private float groundRaycastRange = 0.2f;
        [SerializeField] private float groundRaycastDistance = 0.2f;
        [SerializeField] private float groundRaycastOffset = 0.2f;
        [SerializeField] private LayerMask groundLayer;


        [EventHandler(Channel = PlayerInputManagerStatic.PLAYER_INPUT_MANAGER_CHANNEL, IgnoreCancelled = false)]
        public void OnPlayerInputEvent(PlayerInputEvent<GGJ_PlayerInputKeys> playerInputEvent)
        {
            var phase = playerInputEvent.InputPhase;

            if (phase == UnityEngine.InputSystem.InputActionPhase.Performed)
            {
                var moveAmount = Mathf.Clamp01(Mathf.Abs(_playerMovementInput.x) + Mathf.Abs(_playerMovementInput.y));
                if(playerInputEvent.ActionKey == GGJ_PlayerInputKeys.CharacterMovement) _playerMovementInput = playerInputEvent.CallbackContext.ReadValue<Vector2>();
                if(playerInputEvent.ActionKey == GGJ_PlayerInputKeys.CharacterSprint) _isPlayerSprinting = moveAmount > 0.5f && sprintingEnabled;
                if(playerInputEvent.ActionKey == GGJ_PlayerInputKeys.CharacterCrouch) _isPlayerCrouch = crouchEnabled;
            }

            if(phase == UnityEngine.InputSystem.InputActionPhase.Canceled)
            {
                if(playerInputEvent.ActionKey == GGJ_PlayerInputKeys.CharacterMovement) _playerMovementInput = playerInputEvent.CallbackContext.ReadValue<Vector2>();
                if(playerInputEvent.ActionKey == GGJ_PlayerInputKeys.CharacterSprint) _isPlayerSprinting = false;
                if(playerInputEvent.ActionKey == GGJ_PlayerInputKeys.CharacterCrouch) _isPlayerCrouch = false;
            }
        }

        public void ISetCharacterManager(ICharacterManager characterManager)
        {
            _characterManager = characterManager;
        }

        public void IHandleCharacterComponent()
        {
            var moveAmount = Mathf.Clamp01(Mathf.Abs(_playerMovementInput.x) + Mathf.Abs(_playerMovementInput.y));
            if(_isPlayerSprinting) moveAmount = sprintBlendTreeValue;
            if(_isPlayerCrouch) moveAmount = crouchBlendTreeValue;
            _characterManager.IAnimatorController().IUpdateFloatValue(AnimatorKey.Horizontal, moveAmount);
            var isInteracting = _characterManager.IAnimatorController().IGetBool(AnimatorKey.IsInteracting);
            HandleFallingAndLanding(isInteracting);
            if(isInteracting) return;
            HandlePlayerMovement();
            HandlePlayerRotation();
            HandleJumping();
        }

        private void HandleFallingAndLanding(bool isInteracting)
        {
            if (!fallingAndLandingEnabled) return;
            var raycastOrigin = transform.position;
            raycastOrigin.y += groundRaycastOffset;
            var wasGrounded = _isGrounded;
            _isGrounded = Physics.SphereCast(raycastOrigin,groundRaycastRange,Vector3.down,out _,groundRaycastDistance,groundLayer);

            if (!wasGrounded && _isGrounded && !isInteracting)
            {
                _characterManager.IAnimatorController().IPlayTargetAniamtion("Falling To Landing", true);
                _inAirTimer = 0f;
            }

            if (!_isGrounded)
            {
                if (!isInteracting)_characterManager.IAnimatorController().IPlayTargetAniamtion("Falling Idle", true);
                _inAirTimer += Time.deltaTime;
                _characterManager.ICharacterRigidbody().AddForce(-Vector3.up * fallingVelocity * _inAirTimer);
            }
        }

        private void HandlePlayerMovement()
        {
            _playerMoveDirection = _characterManager.ICameraManager().IManagedCameraTransform().forward * _playerMovementInput.y;
            _playerMoveDirection += _characterManager.ICameraManager().IManagedCameraTransform().right * _playerMovementInput.x;
            _playerMoveDirection.y = 0f;          
            _playerMoveDirection.Normalize();

            var moveAmount = Mathf.Clamp01(Mathf.Abs(_playerMovementInput.x) + Mathf.Abs(_playerMovementInput.y));
            if(_isPlayerCrouch) _playerMoveDirection *= crouchSpeed;
            else if(_isPlayerSprinting) _playerMoveDirection *= sprintingSpeed;
            else if(moveAmount >= 0.5f) _playerMoveDirection *= runningSpeed;
            else _playerMoveDirection *= walkingSpeed;

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

        private void HandleJumping()
        {
            
        }
    }
}