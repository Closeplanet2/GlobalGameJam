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

        public void IUpdateFloatValue(AnimatorKey animatorKey, float value, float dampTime = 0.1f)
        {
            _animator.SetFloat(animatorKey.ToString(), value, dampTime, Time.deltaTime);
        }
    }
}
