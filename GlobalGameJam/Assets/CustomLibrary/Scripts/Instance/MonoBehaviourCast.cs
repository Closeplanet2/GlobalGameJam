using TMPro;
using UnityEngine;

namespace CustomLibrary.Scripts.Instance
{
    public abstract class MonoBehaviourCast<TBase> : MonoBehaviour where TBase : MonoBehaviourCast<TBase>
    {
        public static TValue Cast<TValue>(TBase source) where TValue : TBase => source as TValue;
         public TValue Cast<TValue>() where TValue : TBase => this as TValue;
    }
}