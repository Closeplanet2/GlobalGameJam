using GloablGameJam.Scripts.Character;
using UnityEngine;
using UnityEngine.AI;

namespace GloablGameJam.Scripts.NPC
{
    public sealed class NPCChaseTargetInterrupt : NPCScheduleItem, INPCInterruptItem
    {
        [Header("Chase")]
        [SerializeField, Min(0f)] private float repathSeconds = 0.15f;
        [SerializeField, Min(0f)] private float sightRange = 12f;
        [SerializeField, Range(0f, 180f)] private float fovDegrees = 110f;
        [SerializeField] private LayerMask losMask = ~0;

        [Header("Lose Sight")]
        [SerializeField, Min(0f)] private float loseSightGraceSeconds = 2.0f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float attackRange = 1.8f;
        [SerializeField, Min(0f)] private float attackCooldownSeconds = 1.0f;
        [SerializeField, Min(0f)] private float attackDamage = 10f;

        [Header("Debug")]
        [SerializeField] private bool log = true;

        private CharacterManager _target;
        private NPCScheduler _scheduler;
        private NavMeshAgent _agent;

        private float _nextRepathAt;
        private float _nextAttackAt;

        private float _lostSightTimer;
        private Vector3 _lastSeenPos;
        private bool _hasLastSeen;

        public void SetTarget(CharacterManager target)
        {
            _target = target;
            _lostSightTimer = 0f;
            _hasLastSeen = false;
            _nextRepathAt = 0f;
        }

        public override void OnStart(ICharacterManager characterManager, uint clock)
        {
            if (characterManager is not MonoBehaviour mb) return;

            _scheduler = mb.GetComponentInChildren<NPCScheduler>();
            mb.TryGetComponent(out _agent);

            if (log) Debug.Log($"[Chase] {mb.name} started chasing", mb);
        }

        public override void OnTick(ICharacterManager characterManager, uint clock)
        {
            if (_target == null) return;
            if (characterManager is not MonoBehaviour mb) return;
            if (_agent == null || !_agent.enabled) return;

            var canSee = CanSeeTarget(mb.transform);
            if (canSee)
            {
                _lostSightTimer = 0f;
                _lastSeenPos = _target.transform.position;
                _hasLastSeen = true;
            }
            else
            {
                _lostSightTimer += Time.deltaTime;
            }

            // Chase destination: target if visible, otherwise last seen.
            var dest = canSee ? _target.transform.position : (_hasLastSeen ? _lastSeenPos : _target.transform.position);

            if (Time.time >= _nextRepathAt)
            {
                _nextRepathAt = Time.time + repathSeconds;
                _agent.isStopped = false;
                _agent.SetDestination(dest);
            }

            // Attack if close enough (only if we can see or are extremely close).
            var dist = Vector3.Distance(mb.transform.position, _target.transform.position);
            if (dist <= attackRange && Time.time >= _nextAttackAt && (canSee || dist <= attackRange * 0.75f))
            {
                _nextAttackAt = Time.time + attackCooldownSeconds;

                // Minimal “drop-in” damage:
                // If your CharacterManager doesn't have health, this will still compile by using SendMessage fallback.
                TryDealDamage(_target, attackDamage);

                if (log) Debug.Log($"[Chase] {mb.name} attacked {_target.name} for {attackDamage}", mb);
            }

            // If we've lost sight for too long, go search last seen and end chase.
            if (_lostSightTimer >= loseSightGraceSeconds)
            {
                if (_scheduler != null && _hasLastSeen)
                {
                    _scheduler.ITryInterruptInvestigate(_lastSeenPos, replaceCurrent: true);
                }

                // End chase
                _target = null;
            }
        }

        public override bool IsComplete(ICharacterManager characterManager, uint clock)
        {
            return _target == null;
        }

        public override void OnEnd(ICharacterManager characterManager, uint clock)
        {
            if (characterManager is MonoBehaviour mb && log)
            {
                Debug.Log($"[Chase] {mb.name} ended chase", mb);
            }

            _target = null;
            _lostSightTimer = 0f;
            _hasLastSeen = false;
        }

        private bool CanSeeTarget(Transform self)
        {
            if (_target == null) return false;

            var origin = self.position + Vector3.up * 1.5f;
            var targetPos = _target.transform.position + Vector3.up * 1.2f;

            var toTarget = targetPos - origin;
            var dist = toTarget.magnitude;
            if (dist > sightRange) return false;

            var dir = toTarget / Mathf.Max(0.0001f, dist);
            var angle = Vector3.Angle(self.forward, dir);
            if (angle > fovDegrees * 0.5f) return false;

            if (Physics.Raycast(origin, dir, out var hit, dist, losMask, QueryTriggerInteraction.Ignore))
            {
                return hit.collider != null && hit.collider.GetComponentInParent<CharacterManager>() == _target;
            }

            return false;
        }

        private static void TryDealDamage(CharacterManager target, float damage)
        {
            if (target == null) return;

            // Preferred: if you add a real health component later.
            // For now: SendMessage is a safe "drop-in" hook.
            target.gameObject.SendMessage("ITakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
    }
}