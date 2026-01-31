using GloablGameJam.Scripts.Character;
using UnityEngine;
using UnityEngine.AI;

namespace GloablGameJam.Scripts.NPC
{
    public sealed class NPCChaseTargetInterrupt : NPCScheduleItem, INPCInterruptItem
    {
        [Header("Chase")]
        [SerializeField, Min(0f)] private float _repathSeconds = 0.15f;

        private CharacterManager _target;
        private float _nextRepathAt;

        public void SetTarget(CharacterManager target)
        {
            _target = target;
            _nextRepathAt = 0f;
        }

        public override void OnTick(ICharacterManager characterManager, uint clock)
        {
            if (_target == null) return;

            if (characterManager is not MonoBehaviour mb) return;
            if (!mb.TryGetComponent<NavMeshAgent>(out var agent)) return;

            if (Time.time < _nextRepathAt) return;
            _nextRepathAt = Time.time + _repathSeconds;

            agent.isStopped = false;
            agent.SetDestination(_target.transform.position);
        }

        public override bool IsComplete(ICharacterManager characterManager, uint clock)
        {
            return _target == null;
        }

        public override void OnEnd(ICharacterManager characterManager, uint clock)
        {
            _target = null;
        }
    }
}