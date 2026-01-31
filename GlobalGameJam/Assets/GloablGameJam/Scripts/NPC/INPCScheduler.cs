using GloablGameJam.Scripts.Character;

namespace GloablGameJam.Scripts.NPC
{
    public interface INPCScheduler : ICharacterComponent
    {
        void IRebuildScheduleCache();
        void IInterrupt(NPCScheduleItem interruptItem, bool replaceCurrent = false);
        void IResumeFromInterrupt();
    }
}