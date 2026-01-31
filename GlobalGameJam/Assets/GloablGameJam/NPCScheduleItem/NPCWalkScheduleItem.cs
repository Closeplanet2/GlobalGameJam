using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    public class NPCWalkScheduleItem : NPCScheduleItem
    {
        [Header("Walk Target")]
        [SerializeField] private Vector3 targetWalkTarget;

        public override void IStartTask(ICharacterManager characterManager, uint internalClock)
        {
            if (characterManager.ITryGetCharacterComponent<NPCMovement>(out var move))
                move.ISetTarget(targetWalkTarget);
        }

        public override void ITickTask(ICharacterManager characterManager, uint internalClock)
        {
            if (!characterManager.ITryGetCharacterComponent<NPCMovement>(out var move)) return;
        }

        public override bool IIsComplete(ICharacterManager characterManager, uint internalClock)
        {
            if (!characterManager.ITryGetCharacterComponent<NPCMovement>(out var move))return true;
            return move.IHasReachedTarget();
        }

        public override void IEndTask(ICharacterManager characterManager, uint internalClock)
        {
            if (characterManager.ITryGetCharacterComponent<NPCMovement>(out var move)) move.IStop();
        }
    }
}