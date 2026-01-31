using System.Collections.Generic;
using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    public abstract class NPCScheduleItem : MonoBehaviour, INPCScheduleItem
    {
        
        [Header("Timing")]
        [SerializeField] private uint triggerTime;
        [SerializeField] private uint durationTicks = 1;
        [SerializeField] private int priority = 0;

        [Header("Scheduler")]
        [Tooltip("If false, this item will never be auto-selected by the schedule loop. It can only be entered via code interrupt.")]
        [SerializeField] private bool includeInLoop = true;

        public uint TriggerTime => triggerTime;
        public uint DurationTicks => durationTicks;
        public int Priority => priority;
        public bool IncludeInLoop => includeInLoop;

        public virtual bool IsComplete(ICharacterManager characterManager, uint clock) => false;

        public virtual void OnStart(ICharacterManager characterManager, uint clock) { }
        public virtual void OnTick(ICharacterManager characterManager, uint clock) { }
        public virtual void OnEnd(ICharacterManager characterManager, uint clock) { }
    }
}