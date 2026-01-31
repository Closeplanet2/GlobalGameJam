using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    public abstract class NPCScheduleItem : MonoBehaviour
    {
        [Header("Loop Scheduling (optional)")]
        [SerializeField] private bool includeInLoop = true;
        [SerializeField] private uint triggerTime;
        [SerializeField] private uint durationTicks = 1;
        [SerializeField] private int priority = 0;

        public bool IncludeInLoop => includeInLoop;
        public uint TriggerTime => triggerTime;
        public uint DurationTicks => durationTicks;
        public int Priority => priority;

        public virtual void OnStart(ICharacterManager characterManager, uint clock) { }
        public virtual void OnTick(ICharacterManager characterManager, uint clock) { }
        public virtual void OnEnd(ICharacterManager characterManager, uint clock) { }
        public virtual bool IsComplete(ICharacterManager characterManager, uint clock) => false;
    }
}