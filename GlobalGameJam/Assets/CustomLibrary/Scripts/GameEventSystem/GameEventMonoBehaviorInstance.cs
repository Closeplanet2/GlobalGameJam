using CustomLibrary.Scripts.Instance;
using UnityEngine;

namespace CustomLibrary.Scripts.GameEventSystem
{
    public abstract class GameEventMonoBehaviorInstance<TBase> : MonoBehaviourInstance<TBase>, IEventListener
        where TBase : GameEventMonoBehaviorInstance<TBase>
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
