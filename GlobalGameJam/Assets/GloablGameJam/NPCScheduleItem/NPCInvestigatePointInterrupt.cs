using GloablGameJam.Scripts.Character;
using UnityEngine;
using UnityEngine.AI;

namespace GloablGameJam.Scripts.NPC
{
    public sealed class NPCInvestigatePointInterrupt : NPCScheduleItem, INPCInterruptItem
    {
        [Header("Investigate")]
        [SerializeField, Min(0f)] private float arriveDistance = 1.2f;
        [SerializeField, Min(0f)] private float lookSeconds = 1.5f;
        [SerializeField, Min(0f)] private float repathSeconds = 0.25f;

        [Header("Debug")]
        [SerializeField] private bool log = true;

        private Vector3 _point;
        private bool _hasPoint;
        private float _lookTimer;
        private float _nextRepathAt;

        public void SetInvestigatePoint(Vector3 point)
        {
            _point = point;
            _hasPoint = true;
            _lookTimer = 0f;
            _nextRepathAt = 0f;
        }

        public override void OnStart(ICharacterManager characterManager, uint clock)
        {
            if (!_hasPoint) return;
            if (characterManager is not MonoBehaviour mb) return;
            if (!mb.TryGetComponent<NavMeshAgent>(out var agent)) return;

            agent.isStopped = false;
            agent.SetDestination(_point);

            if (log) Debug.Log($"[Investigate] {mb.name} investigating {_point}", mb);
        }

        public override void OnTick(ICharacterManager characterManager, uint clock)
        {
            if (!_hasPoint) return;
            if (characterManager is not MonoBehaviour mb) return;
            if (!mb.TryGetComponent<NavMeshAgent>(out var agent)) return;

            if (Time.time >= _nextRepathAt)
            {
                _nextRepathAt = Time.time + repathSeconds;
                agent.isStopped = false;
                agent.SetDestination(_point);
            }

            if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(arriveDistance, agent.stoppingDistance))
            {
                _lookTimer += Time.deltaTime;
            }
        }

        public override bool IsComplete(ICharacterManager characterManager, uint clock)
        {
            return !_hasPoint || _lookTimer >= lookSeconds;
        }

        public override void OnEnd(ICharacterManager characterManager, uint clock)
        {
            if (characterManager is MonoBehaviour mb && log)
            {
                Debug.Log($"[Investigate] {mb.name} done", mb);
            }

            _hasPoint = false;
            _lookTimer = 0f;
            _nextRepathAt = 0f;
        }
    }
}