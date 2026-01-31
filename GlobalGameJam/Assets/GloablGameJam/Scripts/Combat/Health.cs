using System;
using UnityEngine;

namespace GloablGameJam.Scripts.Combat
{
    public sealed class Health : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        [Header("Invulnerability")]
        [SerializeField, Min(0f)] private float invulnerableSecondsOnHit = 0.1f;

        private float _currentHealth;
        private float _invulnerableUntil;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => _currentHealth;
        public bool IsDead => _currentHealth <= 0f;

        public event Action<Health> Died;
        public event Action<Health, float> Damaged;
        public event Action<Health, float> Healed;

        private void Awake()
        {
            _currentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            if (amount <= 0f) return;
            if (Time.time < _invulnerableUntil) return;

            _invulnerableUntil = Time.time + invulnerableSecondsOnHit;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            Damaged?.Invoke(this, amount);

            if (_currentHealth <= 0f)
            {
                Died?.Invoke(this);
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            if (amount <= 0f) return;

            var before = _currentHealth;
            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);

            var delta = _currentHealth - before;
            if (delta > 0f) Healed?.Invoke(this, delta);
        }

        public void HealFull()
        {
            if (IsDead) return;

            var before = _currentHealth;
            _currentHealth = maxHealth;

            var delta = _currentHealth - before;
            if (delta > 0f) Healed?.Invoke(this, delta);
        }

        public void ReviveFull()
        {
            _currentHealth = maxHealth;
            GrantInvulnerability(0.25f);
        }

        public void GrantInvulnerability(float seconds)
        {
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + Mathf.Max(0f, seconds));
        }
    }
}