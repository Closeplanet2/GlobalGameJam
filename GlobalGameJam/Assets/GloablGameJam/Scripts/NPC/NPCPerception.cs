using GloablGameJam.Scripts.Character;
using GloablGameJam.Scripts.Game;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    public sealed class NPCPerception : MonoBehaviour, ICharacterComponent
    {
        private ICharacterManager _characterManager;
        private NPCScheduler _scheduler;

        [Header("Alert")]
        [SerializeField, Min(0f)] private float _alertSeconds = 10f;
        private float _alertUntilTime;

        [Header("Sight")]
        [SerializeField, Min(0f)] private float _sightRange = 10f;
        [SerializeField, Range(0f, 180f)] private float _fovDegrees = 90f;
        [SerializeField] private LayerMask _losMask = ~0;

        [Header("Too Close Too Long")]
        [SerializeField, Min(0f)] private float _proximityRange = 2.5f;
        [SerializeField, Min(0f)] private float _proximitySecondsToChase = 1.0f;
        private float _proximityTimer;

        public void ISetCharacterManager(ICharacterManager characterManager)
        {
            _characterManager = characterManager;
            _scheduler = GetComponent<NPCScheduler>();
        }

        public void ISetAlerted()
        {
            _alertUntilTime = Time.time + _alertSeconds;
            _proximityTimer = 0f;
        }

        public void IHandleCharacterComponent()
        {
            if (_scheduler == null) return;
            if (Time.time > _alertUntilTime) return;

            var gm = GlobalGameJam.Instance;
            if (gm == null) return;

            var player = gm.CurrentPlayer;
            if (player == null) return;

            // Too close too long -> chase (even if LOS is flaky)
            var dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= _proximityRange)
            {
                _proximityTimer += Time.deltaTime;
                if (_proximityTimer >= _proximitySecondsToChase)
                {
                    _scheduler.ITryInterruptChase(player, replaceCurrent: true);
                    return;
                }
            }
            else
            {
                _proximityTimer = 0f;
            }

            // LOS chase
            if (CanSee(player))
            {
                _scheduler.ITryInterruptChase(player, replaceCurrent: true);
            }
        }

        private bool CanSee(CharacterManager target)
        {
            var origin = transform.position + Vector3.up * 1.5f;
            var targetPos = target.transform.position + Vector3.up * 1.2f;

            var toTarget = targetPos - origin;
            var dist = toTarget.magnitude;
            if (dist > _sightRange) return false;

            var dir = toTarget / Mathf.Max(0.0001f, dist);
            var angle = Vector3.Angle(transform.forward, dir);
            if (angle > _fovDegrees * 0.5f) return false;

            if (Physics.Raycast(origin, dir, out var hit, dist, _losMask, QueryTriggerInteraction.Ignore))
            {
                return hit.collider != null && hit.collider.GetComponentInParent<CharacterManager>() == target;
            }

            return false;
        }
    }
}