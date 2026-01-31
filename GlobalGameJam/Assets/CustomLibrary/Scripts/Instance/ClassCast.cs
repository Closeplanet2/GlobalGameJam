using UnityEngine;

namespace CustomLibrary.Scripts.Instance
{
    public abstract class ClassCast<TBase> where TBase : ClassCast<TBase>
    {
        public static TValue Cast<TValue>(TBase source) where TValue : TBase => source as TValue;
        public TValue Cast<TValue>() where TValue : TBase => this as TValue;
    }
}
