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
        private bool isTryingToDropMask;
        private float _dropMaskHoldTimer;
        private bool _dropMaskTriggered;
        private float _defaultFov   ;

        [SerializeField] private CharacterManager characterManager;

        [Header("Drop Mask")]
        [SerializeField] private float dropMaskHoldSeconds = 5f;
        [SerializeField] private float zoomOutFov = 75f;
        [SerializeField] private float zoomSpeed = 2f;
        
        protected override void Awake()
        {
            base.Awake();
            _globalGameJamUI = GetComponent<GlobalGameJamUI>();
        }

        private void Update()
        {
            if (characterManager == null)
            {
                ResetDropMaskState();
                return;
            }

            if (!isTryingToDropMask)
            {
                _dropMaskHoldTimer = 0f;
                _dropMaskTriggered = false;
                SmoothZoomTo(_defaultFov);
                return;
            }

            _dropMaskHoldTimer += Time.deltaTime;
            SmoothZoomTo(zoomOutFov);

            if (!_dropMaskTriggered && _dropMaskHoldTimer >= dropMaskHoldSeconds)
            {
                _dropMaskTriggered = true;
                CompleteDropMask();
            }
        }

        [EventHandler(Channel = PlayerInputManagerStatic.PLAYER_INPUT_MANAGER_CHANNEL, IgnoreCancelled = false)]
        public void OnPlayerInputEvent(PlayerInputEvent<GGJ_PlayerInputKeys> playerInputEvent)
        {
            if(playerInputEvent.InputPhase == UnityEngine.InputSystem.InputActionPhase.Performed)
            {
                if(playerInputEvent.ActionKey == GGJ_PlayerInputKeys.DropMask && characterManager != null)
                {
                    isTryingToDropMask = true;
                }
            }
            else if(playerInputEvent.InputPhase == UnityEngine.InputSystem.InputActionPhase.Canceled)
            {
                if(playerInputEvent.ActionKey == GGJ_PlayerInputKeys.DropMask) isTryingToDropMask = false;
            }
        }

        private void SmoothZoomTo(float targetFov)
        {
            var playerCamera = characterManager.ICameraManager();
            playerCamera.IManagedCamera().fieldOfView = Mathf.Lerp(playerCamera.IManagedCamera().fieldOfView, targetFov, zoomSpeed * Time.deltaTime);
        }

        private void ResetDropMaskState()
        {
            _dropMaskHoldTimer = 0f;
            _dropMaskTriggered = false;
            SmoothZoomTo(_defaultFov);
        }

        private void CompleteDropMask()
        {
            characterManager.ISetCharacterState(CharacterState.NPCControlled);
            characterManager = null;
        }

    }
}