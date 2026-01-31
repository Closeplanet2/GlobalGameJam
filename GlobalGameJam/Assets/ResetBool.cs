using GloablGameJam.Scripts.Animation;
using UnityEngine;

public class ResetBool : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(AnimatorKey.IsInteracting.ToString(), false);
    }
}
