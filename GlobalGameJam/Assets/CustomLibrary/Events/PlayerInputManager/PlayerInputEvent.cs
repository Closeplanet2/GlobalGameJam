using System;
using CustomLibrary.Scripts.GameEventSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CustomLibrary.Events.PlayerInputManager
{
    public class PlayerInputEvent<TInputKey> : BaseEvent where TInputKey : struct, Enum
    {
        public InputAction.CallbackContext CallbackContext { get; }
        public InputActionPhase InputPhase { get; }
        public TInputKey ActionKey { get; }

        public PlayerInputEvent(InputAction.CallbackContext callbackContext, InputActionPhase inputPhase, TInputKey actionKey)
        {
            CallbackContext = callbackContext;
            InputPhase = inputPhase;
            ActionKey = actionKey;
        }
    }
}
