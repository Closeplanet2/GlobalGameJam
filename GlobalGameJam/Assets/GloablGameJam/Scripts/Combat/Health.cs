using System;
using UnityEngine;

namespace GloablGameJam.Scripts.Combat
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _maxHealth = 100f;
        [SerializeField, Min(0f)] private float _invulnerableSecondsOnHit = 0.1f;

        private float _currentHealth;
        private float _invulnerableUntil;

        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public bool IsDead => _currentHealth <= 0f;

        public event Action<Health> Died;
        public event Action<Health, float> Damaged; // amount
        public event Action<Health, float> Healed;  // amount

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        public void HealFull()
        {
            var before = _currentHealth;
            _currentHealth = _maxHealth;
            var delta = _currentHealth - before;
            if (delta > 0f) Healed?.Invoke(this, delta);
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            if (amount <= 0f) return;

            var before = _currentHealth;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);

            var delta = _currentHealth - before;
            if (delta > 0f) Healed?.Invoke(this, delta);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            if (amount <= 0f) return;
            if (Time.time < _invulnerableUntil) return;

            _invulnerableUntil = Time.time + _invulnerableSecondsOnHit;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            Damaged?.Invoke(this, amount);

            if (_currentHealth <= 0f)
            {
                Died?.Invoke(this);
            }
        }

        public void GrantInvulnerability(float seconds)
        {
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + Mathf.Max(0f, seconds));
        }
    }
}
