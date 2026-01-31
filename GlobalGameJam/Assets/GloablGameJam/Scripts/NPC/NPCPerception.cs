using GloablGameJam.Scripts.Character;
using GloablGameJam.Scripts.Game;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    public sealed class NPCPerception : MonoBehaviour, ICharacterComponent
    {
        private NPCScheduler _scheduler;

        [Header("Alert")]
        [SerializeField, Min(0f)] private float alertSeconds = 12f;
        private float _alertUntil;

        [Header("Sight")]
        [SerializeField, Min(0f)] private float sightRange = 14f;
        [SerializeField, Range(0f, 180f)] private float fovDegrees = 120f;
        [SerializeField] private LayerMask losMask = ~0;

        [Header("Proximity Aggro")]
        [SerializeField, Min(0f)] private float proximityRange = 2.75f;
        [SerializeField, Min(0f)] private float proximitySecondsToChase = 0.75f;
        private float _proximityTimer;

        [Header("Debug")]
        [SerializeField] private bool log = true;

        public void ISetCharacterManager(ICharacterManager characterManager)
        {
            _scheduler = GetComponent<NPCScheduler>();
        }

        public bool ICanSeeNow(CharacterManager target)
        {
            return CanSee(target);
        }

        public void ISetAlerted()
        {
            _alertUntil = Time.time + alertSeconds;
            _proximityTimer = 0f;
        }

        public void IHandleCharacterComponent()
        {
            if (_scheduler == null) return;
            if (Time.time > _alertUntil) return;

            var gm = GlobalGameJam.Instance;
            if (gm == null) return;

            var player = gm.CurrentPlayer;
            if (player == null) return;

            var dist = Vector3.Distance(transform.position, player.transform.position);

            if (dist <= proximityRange)
            {
                _proximityTimer += Time.deltaTime;
                if (_proximityTimer >= proximitySecondsToChase)
                {
                    if (log) Debug.Log($"[Perception] {name} -> CHASE (proximity)", this);
                    _scheduler.ITryInterruptChase(player, replaceCurrent: true);
                    return;
                }
            }
            else
            {
                _proximityTimer = 0f;
            }

            if (CanSee(player))
            {
                if (log) Debug.Log($"[Perception] {name} -> CHASE (LOS)", this);
                _scheduler.ITryInterruptChase(player, replaceCurrent: true);
            }
        }

        private bool CanSee(CharacterManager target)
        {
            var origin = transform.position + Vector3.up * 1.5f;
            var targetPos = target.transform.position + Vector3.up * 1.2f;

            var toTarget = targetPos - origin;
            var dist = toTarget.magnitude;
            if (dist > sightRange) return false;

            var dir = toTarget / Mathf.Max(0.0001f, dist);
            var angle = Vector3.Angle(transform.forward, dir);
            if (angle > fovDegrees * 0.5f) return false;

            if (Physics.Raycast(origin, dir, out var hit, dist, losMask, QueryTriggerInteraction.Ignore))
            {
                return hit.collider != null && hit.collider.GetComponentInParent<CharacterManager>() == target;
            }

            return false;
        }
    }
}