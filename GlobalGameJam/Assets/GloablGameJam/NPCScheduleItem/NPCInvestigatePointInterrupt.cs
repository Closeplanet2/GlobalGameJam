using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    public sealed class NPCInvestigatePointInterrupt : NPCScheduleItem
    {
        [Header("Investigate")]
        [SerializeField] private Transform investigateTarget;

        private NPCMovement _movement;

        private void Awake()
        {
            _movement = GetComponent<NPCMovement>();
        }

        public void SetInvestigatePosition(Vector3 pos)
        {
            if (investigateTarget == null)
            {
                var go = new GameObject("InvestigateTarget_Runtime");
                go.transform.SetParent(transform);
                investigateTarget = go.transform;
            }
            investigateTarget.position = pos;
        }

        public override void OnStart(ICharacterManager characterManager, uint clock)
        {
            if (_movement == null || investigateTarget == null) return;
            _movement.SetDestination(investigateTarget.position);
        }

        public override void OnTick(ICharacterManager characterManager, uint clock)
        {
            if (_movement == null || investigateTarget == null) return;
            _movement.SetDestination(investigateTarget.position);
        }

        public override bool IsComplete(ICharacterManager characterManager, uint clock)
        {
            return _movement != null && _movement.HasArrived();
        }

        public override void OnEnd(ICharacterManager characterManager, uint clock)
        {
            if (_movement == null) return;
            _movement.Stop();
        }
    }
}