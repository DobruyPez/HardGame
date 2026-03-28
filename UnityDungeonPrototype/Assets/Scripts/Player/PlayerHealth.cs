using UnityEngine;
using DungeonPrototype.Core;

namespace DungeonPrototype.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            GameEvents.RaisePlayerHealthChanged(CurrentHealth, maxHealth);
        }

        public void TakeDamage(float damage)
        {
            if (damage <= 0f || CurrentHealth <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            GameEvents.RaisePlayerHealthChanged(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f)
            {
                Debug.Log("Princess has fallen.");
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || CurrentHealth <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            GameEvents.RaisePlayerHealthChanged(CurrentHealth, maxHealth);
        }
    }
}
