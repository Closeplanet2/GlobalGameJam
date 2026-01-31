using System.Collections;
using GloablGameJam.Scripts.Combat;
using UnityEngine;

namespace GloablGameJam.Scripts.Game
{
    public sealed class PlayerRespawnController : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private Health health;
        [SerializeField] private MonoBehaviour[] disableDuringRespawn;

        private bool _respawning;

        private void Reset()
        {
            health = GetComponent<Health>();
        }

        public void SetRespawnPoint(Transform point)
        {
            respawnPoint = point;
        }

        public void BeginRespawn(float delaySeconds)
        {
            if (_respawning) return;
            if (!isActiveAndEnabled) return;

            _respawning = true;
            StartCoroutine(RespawnRoutine(Mathf.Max(0f, delaySeconds)));
        }

        private IEnumerator RespawnRoutine(float delaySeconds)
        {
            SetControlsEnabled(false);

            if (delaySeconds > 0f)
            {
                yield return new WaitForSeconds(delaySeconds);
            }

            if (respawnPoint != null)
            {
                transform.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
            }

            if (health != null)
            {
                health.ReviveFull();
            }

            SetControlsEnabled(true);
            _respawning = false;
        }

        private void SetControlsEnabled(bool enabled)
        {
            if (disableDuringRespawn == null) return;

            for (var i = 0; i < disableDuringRespawn.Length; i++)
            {
                var b = disableDuringRespawn[i];
                if (b != null) b.enabled = enabled;
            }
        }
    }
}