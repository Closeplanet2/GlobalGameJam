using GloablGameJam.Scripts.Character;
using GloablGameJam.Scripts.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace GloablGameJam.Scripts.NPC
{
    public sealed class NPCChaseTargetInterrupt : NPCScheduleItem, INPCInterruptItem
    {
        private Vector3 _lastSetDestination;

        [Header("Chase")]
        [SerializeField, Min(0f)] private float repathSeconds = 0.08f;
        [SerializeField, Min(0f)] private float repathMinDelta = 0.35f; // meters
        [SerializeField, Min(0f)] private float sightRange = 14f;
        [SerializeField, Range(0f, 180f)] private float fovDegrees = 120f;
        [SerializeField] private LayerMask losMask = ~0;

        [Header("Lose Sight")]
        [SerializeField, Min(0f)] private float loseSightGraceSeconds = 2.0f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float attackRange = 1.8f;
        [SerializeField, Min(0f)] private float attackCooldownSeconds = 0.9f;
        [SerializeField, Min(0f)] private float attackDamage = 20f;

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
            _nextAttackAt = 0f;
        }

        public override void OnStart(ICharacterManager characterManager, uint clock)
        {
            if (characterManager is not MonoBehaviour mb) return;

            _scheduler = mb.GetComponentInChildren<NPCScheduler>();
            mb.TryGetComponent(out _agent);

            if (_agent != null)
            {
                // Smooth pursuit feel
                _agent.autoBraking = false;
                _agent.isStopped = false;
            }
            var tuning = mb.GetComponent<NPCNavAgentTuning>();
            if (tuning != null) tuning.SetRun();
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

            // Destination = target if visible else last seen
            var dest = canSee ? _target.transform.position : (_hasLastSeen ? _lastSeenPos : _target.transform.position);

            if (Time.time >= _nextRepathAt)
            {
                _nextRepathAt = Time.time + repathSeconds;

                var delta = dest - _lastSetDestination;
                if (delta.sqrMagnitude >= repathMinDelta * repathMinDelta)
                {
                    _lastSetDestination = dest;
                    _agent.SetDestination(dest);
                }
            }

            // Attack
            var dist = Vector3.Distance(mb.transform.position, _target.transform.position);
            if (dist <= attackRange && Time.time >= _nextAttackAt && (canSee || dist <= attackRange * 0.85f))
            {
                _nextAttackAt = Time.time + attackCooldownSeconds;

                var hp = _target.GetComponent<GloablGameJam.Scripts.Combat.Health>();
                if (hp != null && !hp.IsDead)
                {
                    hp.TakeDamage(hp.MaxHealth); // insta-kill
                    if (log) Debug.Log($"[Chase] {mb.name} INSTA-KILLED {_target.name}", mb);
                }
            }

            // Lose sight -> search last seen (investigate) then end chase
            if (_lostSightTimer >= loseSightGraceSeconds)
            {
                if (_scheduler != null && _hasLastSeen)
                {
                    _scheduler.ITryInterruptInvestigate(_lastSeenPos, replaceCurrent: true);
                }

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
                Debug.Log($"[Chase] {mb.name} EXIT chase", mb);
            }

            if (_agent != null)
            {
                _agent.autoBraking = true;
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
    }
}
