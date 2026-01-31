using UnityEngine;

namespace CustomLibrary.Scripts.GameEventSystem
{
    public abstract class GameEventInstance : MonoBehaviour, IEventListener
    {
        protected void OnEnable()
        {
            GameEventSystem.Instance.RegisterListeners(this);
        }

        protected void Osable()
        {
            GameEventSystem.Instance.UnRegisterListeners(this);
        }
    }
}
