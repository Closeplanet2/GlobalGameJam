using GloablGameJam.Scripts.Character;
using UnityEngine;
using UnityEngine.AI;

namespace GloablGameJam.Scripts.NPC
{
    public class NPCMovement : MonoBehaviour, ICharacterComponent
    {
        private ICharacterManager _characterManager;
        private NavMeshAgent _agent;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float angularSpeed = 720f;
        [SerializeField] private float stoppingDistance = 0.2f;

        public void ISetCharacterManager(ICharacterManager characterManager)
        {
            _characterManager = characterManager;
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            ApplyAgentSettings();
        }

        private void OnValidate()
        {
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            if (_agent != null) ApplyAgentSettings();
        }

        public void IHandleCharacterComponent()
        {
            
        }

        private void ApplyAgentSettings()
        {
            _agent.speed = moveSpeed;
            _agent.angularSpeed = angularSpeed;
            _agent.stoppingDistance = stoppingDistance;
            _agent.updatePosition = true;
            _agent.updateRotation = true;
        }

        public void SetDestination(Vector3 worldPos)
        {
            if (!_agent.isOnNavMesh) return;
            _agent.isStopped = false;
            _agent.SetDestination(worldPos);
        }

        public void Stop()
        {
            if (!_agent.isOnNavMesh) return;
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        public bool HasArrived()
        {
            if (!_agent.isOnNavMesh) return true;
            if (_agent.pathPending) return false;
            if (_agent.remainingDistance == Mathf.Infinity) return false;
            return _agent.remainingDistance <= _agent.stoppingDistance;
        }

        public Vector3 CurrentDestination()
        {
            return _agent.destination;
        }
    }
}