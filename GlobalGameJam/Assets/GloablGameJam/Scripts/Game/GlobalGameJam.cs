using Assembly_CSharp_Editor.Assets.CustomLibrary.Scripts.PlayerInputManager;
using CustomLibrary.Events.PlayerInputManager;
using CustomLibrary.Scripts.GameEventSystem;
using GloablGameJam.Scripts.Camera;
using GloablGameJam.Scripts.Character;
using GloablGameJam.Scripts.PlayerInputManager;
using UnityEngine;
using VInspector;

namespace GloablGameJam.Scripts.Game
{
    [RequireComponent(typeof(GlobalGameJamUI))]
    public class GlobalGameJam : GameEventMonoBehaviorInstance<GlobalGameJam>
    {
        private GlobalGameJamUI _globalGameJamUI;

        [SerializeField] private CharacterManager characterManager;
        [SerializeField] private CameraMaskDrop cameraMaskDrop;
        
        protected override void Awake()
        {
            base.Awake();
            _globalGameJamUI = GetComponent<GlobalGameJamUI>();
            if (cameraMaskDrop != null)
            {
                cameraMaskDrop.DroppedMask += OnDroppedMask;
            }
        }

        [EventHandler(Channel = PlayerInputManagerStatic.PLAYER_INPUT_MANAGER_CHANNEL, IgnoreCancelled = false)]
        public void OnPlayerInputEvent(PlayerInputEvent<GGJ_PlayerInputKeys> playerInputEvent)
        {
            if (playerInputEvent.ActionKey != GGJ_PlayerInputKeys.DropMask) return;
            if (cameraMaskDrop == null) return;
            var phase = playerInputEvent.InputPhase;
            if (phase == UnityEngine.InputSystem.InputActionPhase.Started)
            {
                if (characterManager == null) return;
                cameraMaskDrop.SetTarget(characterManager);
                cameraMaskDrop.SetHolding(true);
            }
            else if (phase == UnityEngine.InputSystem.InputActionPhase.Canceled)
            {
                cameraMaskDrop.SetHolding(false);
            }
        }

        private void OnDroppedMask(CharacterManager droppedFrom)
        {
            if (characterManager == droppedFrom)
            {
                characterManager = null;
            }
        }
    }
}