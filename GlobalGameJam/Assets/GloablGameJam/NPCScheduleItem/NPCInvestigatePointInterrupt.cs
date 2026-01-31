using GloablGameJam.Scripts.Character;
using UnityEngine;
using UnityEngine.AI;

namespace GloablGameJam.Scripts.NPC
{
    public sealed class NPCInvestigatePointInterrupt : NPCScheduleItem, INPCInterruptItem
    {
        [Header("Investigate")]
        [SerializeField, Min(0f)] private float _arriveDistance = 1.0f;
        [SerializeField, Min(0f)] private float _lookSeconds = 1.0f;
        [SerializeField, Min(0f)] private float _repathSeconds = 0.25f;

        private Vector3 _point;
        private float _lookTimer;
        private float _nextRepathAt;
        private bool _hasPoint;

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
        }

        public override void OnTick(ICharacterManager characterManager, uint clock)
        {
            if (!_hasPoint) return;

            if (characterManager is not MonoBehaviour mb) return;
            if (!mb.TryGetComponent<NavMeshAgent>(out var agent)) return;

            if (Time.time >= _nextRepathAt)
            {
                _nextRepathAt = Time.time + _repathSeconds;
                agent.isStopped = false;
                agent.SetDestination(_point);
            }

            if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(_arriveDistance, agent.stoppingDistance))
            {
                _lookTimer += Time.deltaTime;
            }
        }

        public override bool IsComplete(ICharacterManager characterManager, uint clock)
        {
            if (!_hasPoint) return true;
            return _lookTimer >= _lookSeconds;
        }

        public override void OnEnd(ICharacterManager characterManager, uint clock)
        {
            _hasPoint = false;
            _lookTimer = 0f;
            _nextRepathAt = 0f;
        }
    }
}