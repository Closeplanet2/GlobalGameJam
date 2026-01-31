namespace GloablGameJam.Scripts.NPC
{
    /// <summary>
    /// Marker interface for schedule items that should not be time-limited by DurationTicks.
    /// The scheduler will allow these to run until IsComplete() returns true.
    /// </summary>
    public interface INPCInterruptItem
    {
    }
}
