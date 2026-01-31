using UnityEngine;

namespace GloablGameJam.Scripts.Animation
{
    public interface IAnimatorController
    {
        bool IGetBool(AnimatorKey animatorKey);
        void IUpdateFloatValue(AnimatorKey animatorKey, float value, float dampTime = 0.1f);
        void IUpdateBoolValue(AnimatorKey animatorKey, bool value);
        void IPlayTargetAniamtion(string targetAnimation, bool isInteracting, float normalizedTransitionDuration = 0.2f);
    }
}
