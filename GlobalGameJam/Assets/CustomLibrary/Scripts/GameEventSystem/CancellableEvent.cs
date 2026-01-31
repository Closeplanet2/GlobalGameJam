using UnityEngine;

namespace CustomLibrary.Scripts.GameEventSystem
{
    public abstract class CancellableEvent : BaseEvent, ICancellableEvent
    {
        public bool Cancelled { get; private set; }
        public bool IsCancelled() => Cancelled;
        public void ISetCancelled(bool cancelled) => Cancelled = cancelled;
    }
}