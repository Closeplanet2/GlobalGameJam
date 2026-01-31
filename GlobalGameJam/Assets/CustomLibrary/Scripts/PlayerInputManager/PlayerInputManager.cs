using System;
using Assembly_CSharp_Editor.Assets.CustomLibrary.Scripts.PlayerInputManager;
using CustomLibrary.Events.PlayerInputManager;
using CustomLibrary.Scripts.Instance;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace CustomLibrary.Scripts.PlayerInputManager
{
    public abstract class PlayerInputManager<TInputKey> : MonoBehaviourInstance<PlayerInputManager<TInputKey>> 
        where TInputKey : struct, Enum
    {
        public abstract ReadOnlyArray<InputAction> GetInputActions();

        protected override void Awake()
        {
            base.Awake();
            foreach (var action in GetInputActions())
            {
                if (!Enum.TryParse(action.name, out TInputKey keyEnum)) continue;
                action.started += ctx => HandlePlayerInput(ctx, InputActionPhase.Started, keyEnum);
                action.performed += ctx => HandlePlayerInput(ctx, InputActionPhase.Performed, keyEnum);
                action.canceled += ctx => HandlePlayerInput(ctx, InputActionPhase.Canceled, keyEnum);
            }
        }

        private void HandlePlayerInput(InputAction.CallbackContext callbackContext, InputActionPhase inputPhase, TInputKey actionKey)
        {
            var playerInputEvent = new PlayerInputEvent<TInputKey>(callbackContext, inputPhase, actionKey);
            GameEventSystem.GameEventSystem.Instance.Fire(playerInputEvent, PlayerInputManagerStatic.PLAYER_INPUT_MANAGER_CHANNEL);
        }
    }
}
