using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace GloablGameJam.Scripts.PlayerInputManager
{
    public class GGJ_PlayerInputManager : CustomLibrary.Scripts.PlayerInputManager.PlayerInputManager<GGJ_PlayerInputKeys>
    {
        private GGJ_PlayerInputActions _playerInputActions;
        private GGJ_PlayerInputActions.GlobalGameJamActions _globalGameJamActions;

        public override ReadOnlyArray<InputAction> GetInputActions()
        {
            _playerInputActions = new GGJ_PlayerInputActions();
            _globalGameJamActions = _playerInputActions.GlobalGameJam;
            return _globalGameJamActions.Get().actions;
        }

        private void OnEnable()
        {
            _playerInputActions.Enable();
        }

        private void OnDisable()
        {
            _playerInputActions.Disable();
        }
    }
}
