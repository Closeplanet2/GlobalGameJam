using UnityEngine;

namespace GloablGameJam.Scripts.Animation
{
    [RequireComponent(typeof(Animator))]
    public class AnimatorController : MonoBehaviour, IAnimatorController
    {
        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public bool IGetBool(AnimatorKey animatorKey)
        {
            return _animator.GetBool(animatorKey.ToString());
        }
        
        public void IUpdateFloatValue(AnimatorKey animatorKey, float value, float dampTime = 0.1f)
        {
            _animator.SetFloat(animatorKey.ToString(), value, dampTime, Time.deltaTime);
        }

        public void IUpdateBoolValue(AnimatorKey animatorKey, bool value)
        {
            _animator.SetBool(animatorKey.ToString(), value);
        }

        public void IPlayTargetAniamtion(string targetAnimation, bool isInteracting, float normalizedTransitionDuration = 0.2f)
        {
            IUpdateBoolValue(AnimatorKey.IsInteracting, isInteracting);
            _animator.CrossFade(targetAnimation, normalizedTransitionDuration);
        }
    }
}
