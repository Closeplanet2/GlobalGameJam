using Assembly_CSharp_Editor.Assets.CustomLibrary.Scripts.PlayerInputManager;
using CustomLibrary.Events.PlayerInputManager;
using CustomLibrary.Scripts.GameEventSystem;
using GloablGameJam.Scripts.Character;
using GloablGameJam.Scripts.PlayerInputManager;
using UnityEngine;

namespace GloablGameJam.Scripts.Player
{
    public class PlayerCamera : GameEventInstance
    {
        private ICharacterManager _characterManager;
        private Vector2 _playerRotationInput;

        [EventHandler(Channel = PlayerInputManagerStatic.PLAYER_INPUT_MANAGER_CHANNEL, IgnoreCancelled = false)]
        public void OnPlayerInputEvent(PlayerInputEvent<GGJ_PlayerInputKeys> playerInputEvent)
        {
            var phase = playerInputEvent.InputPhase;
            if (phase == UnityEngine.InputSystem.InputActionPhase.Performed || phase == UnityEngine.InputSystem.InputActionPhase.Canceled)
            {
                if(playerInputEvent.ActionKey == GGJ_PlayerInputKeys.CharacterRotation)
                {
                    _playerRotationInput = playerInputEvent.CallbackContext.ReadValue<Vector2>();
                    Debug.Log(_playerRotationInput);
                }
            }
        }

        public void SetICharacterManager(ICharacterManager characterManager)
        {
            _characterManager = characterManager;
        }

        public void HandleAllPlayerCameraRotation()
        {
            HandleFollowTarget();
            HandleRotateCamera();
        }

        private void HandleFollowTarget()
        {
            _characterManager.ICameraManager().IFollowTarget(transform);
        }

        private void HandleRotateCamera()
        {
            _characterManager.ICameraManager().IRotateCamera(_playerRotationInput);
        }
    }
}