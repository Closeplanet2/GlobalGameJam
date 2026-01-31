using System.Collections.Generic;
using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    public class NPCPatrolScheduleItem : NPCScheduleItem
    {
        [Header("Patrol Points")]
        [SerializeField] private List<Transform> points = new();

        [Header("Options")]
        [SerializeField] private bool randomStartPoint = false;
        [SerializeField] private uint waitTicksAtPoint = 0;
        [SerializeField] private bool completeAfterOneLoop = false;

        private int _index;
        private uint _waitUntilClock;
        private bool _completedLoop;

        public override void OnStart(ICharacterManager characterManager, uint clock)
        {
            _completedLoop = false;
            _waitUntilClock = 0;
            if (points == null || points.Count == 0) return;
            _index = randomStartPoint ? Random.Range(0, points.Count) : 0;
            IssueDestination(characterManager);
        }

        public override void OnTick(ICharacterManager characterManager, uint clock)
        {
            if (points == null || points.Count == 0) return;
            if (_waitUntilClock != 0 && clock < _waitUntilClock) return;
            if(!characterManager.ITryGetCharacterComponent<NPCMovement>(out var nPCMovement)) return;
            if (nPCMovement.HasArrived())
            {
                if (waitTicksAtPoint > 0) _waitUntilClock = clock + waitTicksAtPoint;
                else _waitUntilClock = 0;
                AdvanceIndex();
                IssueDestination(characterManager);
                return;
            }
            var p = points[_index];
            if (p != null)  nPCMovement.SetDestination(p.position);
        }

        public override bool IsComplete(ICharacterManager characterManager, uint clock)
        {
            return completeAfterOneLoop && _completedLoop;
        }

        public override void OnEnd(ICharacterManager characterManager, uint clock)
        {
            if(characterManager.ITryGetCharacterComponent<NPCMovement>(out var nPCMovement)) nPCMovement.Stop();
        }

        private void IssueDestination(ICharacterManager characterManager)
        {
            var p = points[_index];
            if (p == null) return;
            if(characterManager.ITryGetCharacterComponent<NPCMovement>(out var nPCMovement)) nPCMovement.SetDestination(p.position);
        }

        private void AdvanceIndex()
        {
            if (points.Count <= 1) return;
            var next = _index + 1;
            if (next >= points.Count)
            {
                next = 0;
                _completedLoop = true;
            }
            _index = next;
        }
    }
}