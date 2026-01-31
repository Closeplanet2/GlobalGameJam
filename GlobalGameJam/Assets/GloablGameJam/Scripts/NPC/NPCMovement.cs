using GloablGameJam.Scripts.Animation;
using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Scripts.NPC
{
    public class NPCMovement : MonoBehaviour, ICharacterComponent
    {
        private ICharacterManager _characterManager;
        private Vector3 _target;
        private bool _hasTarget;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private float stopDistance = 0.1f;

        public void ISetCharacterManager(ICharacterManager characterManager)
        {
            _characterManager = characterManager;
        }

        public void ISetTarget(Vector3 worldPosition)
        {
            _target = worldPosition;
            _hasTarget = true;
        }

        public void IStop()
        {
            _hasTarget = false;

            var rb = _characterManager.ICharacterRigidbody();
            var v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, v.y, 0f);

            _characterManager.IAnimatorController()
                .IUpdateFloatValue(AnimatorKey.Horizontal, 0f);
        }

        public bool IHasReachedTarget()
        {
            if (!_hasTarget) return true;

            var pos = transform.position;
            pos.y = _target.y;
            return Vector3.Distance(pos, _target) <= stopDistance;
        }

        public void IHandleCharacterComponent()
        {
            if (!_hasTarget) return;

            var rb = _characterManager.ICharacterRigidbody();

            var toTarget = _target - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
            {
                IStop();
                return;
            }

            var dir = toTarget.normalized;

            // Move
            var currentY = rb.linearVelocity.y;
            rb.linearVelocity = new Vector3(
                dir.x * moveSpeed,
                currentY,
                dir.z * moveSpeed
            );

            // Rotate
            var targetRotation = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            // Animate (0 = idle, 1 = walk)
            _characterManager.IAnimatorController()
                .IUpdateFloatValue(AnimatorKey.Horizontal, 1f);
        }
    }
}