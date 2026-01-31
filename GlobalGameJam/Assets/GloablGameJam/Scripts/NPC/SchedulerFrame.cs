namespace GloablGameJam.Scripts.NPC
{
    public struct SchedulerFrame
    {
        public NPCScheduleItem active;
        public uint activeStart;
        public uint activeEnd;
        public bool activeStarted;
        public uint clock;
    }
}