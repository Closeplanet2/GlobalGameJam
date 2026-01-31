using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    public abstract class NPCScheduleItem : MonoBehaviour, INPCScheduleItem
    {
        [Header("Timing")]
        [SerializeField] private uint triggerTime;     
        [SerializeField] private uint taskDuration = 1;

        [Header("Interrupt")]
        [SerializeField] private bool isInterruptItem;  
        [SerializeField] private int interruptPriority;
        
        public uint ITriggerTime() => triggerTime;
        public uint ITaskDuration() => taskDuration;
        public bool IIsInterruptItem() => isInterruptItem;
        public int IInterruptPriority() => interruptPriority;
        public virtual bool IIsComplete(ICharacterManager characterManager, uint internalClock) => false;
        public abstract void IStartTask(ICharacterManager characterManager, uint internalClock);
        public abstract void ITickTask(ICharacterManager characterManager, uint internalClock);
        public abstract void IEndTask(ICharacterManager characterManager, uint internalClock);
    }
}
