using GloablGameJam.Scripts.Animation;
using GloablGameJam.Scripts.Character;
using GloablGameJam.Scripts.NPC;
using UnityEngine;

namespace GloablGameJam.NPCScheduleItem
{
    public class NPCIdleScheduleItem : Scripts.NPC.NPCScheduleItem
    {
        [Header("Idle")]
        [SerializeField] private float idleBlendValue = 0f;

        public override void IStartTask(ICharacterManager characterManager, uint internalClock)
        {
            if (characterManager.ITryGetCharacterComponent<NPCMovement>(out var move)) move.IStop();
            characterManager.IAnimatorController().IUpdateFloatValue(AnimatorKey.Horizontal, idleBlendValue);
        }

        public override void ITickTask(ICharacterManager characterManager, uint internalClock)
        {
            
        }

        public override void IEndTask(ICharacterManager characterManager, uint internalClock)
        {
            
        }
    }
}
