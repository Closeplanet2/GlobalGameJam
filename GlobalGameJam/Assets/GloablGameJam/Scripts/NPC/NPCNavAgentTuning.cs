using UnityEngine;
using UnityEngine.AI;

namespace GloablGameJam.Scripts.NPC
{
    [DisallowMultipleComponent]
    public sealed class NPCNavAgentTuning : MonoBehaviour
    {
        [Header("Walk (patrol/investigate)")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 3.25f;

        [Header("Run (chase)")]
        [SerializeField, Min(0.1f)] private float runSpeed = 5.5f;

        [Header("Motion Feel")]
        [SerializeField, Min(0.1f)] private float acceleration = 20f;
        [SerializeField, Min(0.1f)] private float angularSpeed = 720f;
        [SerializeField, Min(0f)] private float stoppingDistance = 0.6f;

        private NavMeshAgent _agent;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null) return;

            _agent.speed = walkSpeed;
            _agent.acceleration = acceleration;
            _agent.angularSpeed = angularSpeed;
            _agent.stoppingDistance = stoppingDistance;

            // Reduces jitter from braking/replanning.
            _agent.autoBraking = false;

            // Typical.
            _agent.updateRotation = true;
            _agent.updatePosition = true;
        }

        public void SetWalk()
        {
            if (_agent == null) return;
            _agent.speed = walkSpeed;
        }

        public void SetRun()
        {
            if (_agent == null) return;
            _agent.speed = runSpeed;
        }
    }
}
