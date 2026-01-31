using UnityEngine;

namespace GloablGameJam.Scripts.Animation
{
    public interface IAnimatorController
    {
        void IUpdateFloatValue(AnimatorKey animatorKey, float value, float dampTime = 0.1f);
    }
}
