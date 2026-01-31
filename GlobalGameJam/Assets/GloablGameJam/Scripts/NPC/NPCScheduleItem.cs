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

        public uint TriggerTime => triggerTime;
        public uint DurationTicks => durationTicks;
        public int Priority => priority;

        public virtual bool IsComplete(ICharacterManager characterManager, uint clock) => false;
        public virtual void OnStart(ICharacterManager characterManager, uint clock)
        {
            Debug.Log($"[{name}] Start at {clock}", this);
        }

        public virtual void OnTick(ICharacterManager characterManager, uint clock)
        {
            Debug.Log($"[{name}] Tick at {clock}", this);
        }

        public virtual void OnEnd(ICharacterManager characterManager, uint clock)
        {
            Debug.Log($"[{name}] End at {clock}", this);
        }
    }
}