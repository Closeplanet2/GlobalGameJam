using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    public interface INPCScheduleItem
    {
        uint ITriggerTime();
        uint ITaskDuration();
        bool IIsInterruptItem();
        int IInterruptPriority();
        bool IIsComplete(ICharacterManager characterManager, uint internalClock) => false;
        void IStartTask(ICharacterManager characterManager, uint internalClock);
        void ITickTask(ICharacterManager characterManager, uint internalClock);
        void IEndTask(ICharacterManager characterManager, uint internalClock);
    }
}
