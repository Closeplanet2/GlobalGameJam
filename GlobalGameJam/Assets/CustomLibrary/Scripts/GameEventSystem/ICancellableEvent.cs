using UnityEngine;

namespace CustomLibrary.Scripts.GameEventSystem
{
    public interface ICancellableEvent : IBaseEvent
    {
        bool IsCancelled();
        void ISetCancelled(bool cancelled);
    }
}
