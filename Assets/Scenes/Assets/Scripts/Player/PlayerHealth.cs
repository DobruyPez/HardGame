using UnityEngine;
using DungeonPrototype.Core;

namespace DungeonPrototype.Player
{
    /// <summary>
    /// Manages the player (princess) health state.
    /// Takes damage from: guardian attacks, dragon starvation.
    /// Healed by: guardian defeats (via GuardianDeathRewardRelay).
    /// 
    /// When health reaches 0, the level is lost immediately.
    /// Dragon starvation timer is separate (DragonHungerSystem), preventing an inactive dragon from slowly killing player.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        private const float DAMAGE_THRESHOLD = 0.01f; // Avoid damage calculations for values below this

        [SerializeField] private float maxHealth = 100f;

        /// <summary>Player's current health points. Clamped between 0 and maxHealth.</summary>
        public float CurrentHealth { get; private set; }
        
        /// <summary>Maximum health capacity.</summary>
        public float MaxHealth => maxHealth;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            GameEvents.RaisePlayerHealthChanged(CurrentHealth, maxHealth);
        }

        /// <summary>
        /// Damages the player. Cannot damage below 0 HP. Broadcasts health change event for UI updates.
        /// Logs when player dies for debugging purposes.
        /// </summary>
        /// <param name="damage">Amount of health to remove. Values <= 0 are ignored.</param>
        public void TakeDamage(float damage)
        {
            if (damage <= DAMAGE_THRESHOLD || CurrentHealth <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            GameEvents.RaisePlayerHealthChanged(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f)
            {
                Debug.Log("Princess has fallen - Level Failed");
            }
        }

        /// <summary>
        /// Restores player health. Cannot exceed maxHealth. Used after defeating guardians.
        /// </summary>
        /// <param name="amount">Amount of health to restore. Values <= 0 are ignored.</param>
        public void Heal(float amount)
        {
            if (amount <= DAMAGE_THRESHOLD || CurrentHealth <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            GameEvents.RaisePlayerHealthChanged(CurrentHealth, maxHealth);
        }
    }
}
