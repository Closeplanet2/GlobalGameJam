using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    /// <summary>
    /// Schedule item that moves an NPC to a target position using NPCMovement (NavMeshAgent).
    /// Completes when the destination is reached.
    /// </summary>
    public sealed class NPCWalkToScheduleItem : NPCScheduleItem
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        public override void OnStart(ICharacterManager characterManager, uint clock)
        {
            base.OnStart(characterManager, clock);
            if(characterManager.ITryGetCharacterComponent<NPCMovement>(out var nPCMovement)) nPCMovement.SetDestination(target.position);
        }

        public override void OnTick(ICharacterManager characterManager, uint clock)
        {
            base.OnTick(characterManager, clock);
            if(target == null) return;
            if(characterManager.ITryGetCharacterComponent<NPCMovement>(out var nPCMovement)) nPCMovement.SetDestination(target.position);
        }

        public override bool IsComplete(ICharacterManager characterManager, uint clock)
        {
            base.IsComplete(characterManager, clock);
            if(!characterManager.ITryGetCharacterComponent<NPCMovement>(out var nPCMovement)) return false;
            return nPCMovement.HasArrived();
        }

        public override void OnEnd(ICharacterManager characterManager, uint clock)
        {
            base.OnEnd(characterManager, clock);
            if(!characterManager.ITryGetCharacterComponent<NPCMovement>(out var nPCMovement)) nPCMovement.Stop();
        }
    }
}