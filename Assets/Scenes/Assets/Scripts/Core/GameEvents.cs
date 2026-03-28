using System;
using UnityEngine;
using DungeonPrototype.Dragon;
using DungeonPrototype.Mana;
using DungeonPrototype.Guardians;

namespace DungeonPrototype.Core
{
    /// <summary>
    /// Central event hub for all game-wide communication.
    /// This static class acts as a pub-sub system, allowing decoupled systems to communicate
    /// without direct references. Manages crystal draining, dragon state changes, guardian reactions,
    /// player damage, and noise emission.
    /// 
    /// IMPORTANT: All subscribers must unsubscribe in OnDisable() to prevent memory leaks and orphaned listeners.
    /// Example pattern:
    ///   private void OnEnable() => GameEvents.CrystalDepleted += MyHandler;
    ///   private void OnDisable() => GameEvents.CrystalDepleted -= MyHandler;
    /// </summary>
    public static class GameEvents
    {
        // ============= MANA CRYSTAL EVENTS =============
        /// <summary>Raised when a player begins draining a crystal (presses E key).</summary>
        public static event Action<ManaCrystal, float> CrystalDrainStarted;
        
        /// <summary>Raised continuously while draining, providing progress feedback and delta mana.</summary>
        public static event Action<ManaCrystal, float, float> CrystalDrainProgress;
        
        /// <summary>Raised when player releases the drain interaction.</summary>
        public static event Action<ManaCrystal, float, bool> CrystalDrainEnded;
        
        /// <summary>Raised when a crystal is completely depleted. Critical for guardian hunt triggering.</summary>
        public static event Action<ManaCrystal> CrystalDepleted;

        // ============= THREAT & AWARENESS EVENTS =============
        /// <summary>
        /// Raised when significant noise is emitted (crystal drain, heavy footsteps, gates opening).
        /// Guardians listen to this to determine threat level and respond accordingly.
        /// Parameters: (position, noise_radius, threat_level_0_to_1)
        /// </summary>
        public static event Action<Vector3, float, float> NoiseEmitted;

        // ============= DRAGON STATE EVENTS =============
        /// <summary>
        /// Raised whenever dragon mana changes, including real-time updates while draining.
        /// Used for UI updates and threat calculations.
        /// Parameters: (current_mana, max_mana, delta_change)
        /// </summary>
        public static event Action<float, float, float> DragonManaChanged;
        
        /// <summary>Raised when dragon health reaches 0 (starvation). Triggers level failure.</summary>
        public static event Action DragonHPIsZero;
        
        /// <summary>Raised when dragon crosses a mana threshold and advances to a new stage (Hatchling→Companion→Sacred).</summary>
        public static event Action<DragonStage> DragonStageChanged;
        
        /// <summary>Raised when dragon's essence color changes. Used by gates and visual effects to match the dragon's power level.</summary>
        public static event Action<Color> DragonEssenceColorChanged;

        // ============= GUARDIAN & COMBAT EVENTS =============
        /// <summary>
        /// Raised when a guardian is defeated.
        /// Parameters: (guardian_controller, material_drops, mana_returned_to_dragon)
        /// Used to update inventory and dragon state after combat victory.
        /// </summary>
        public static event Action<GuardianController, int, float> GuardianKilled;
        
        /// <summary>Raised when player takes or heals damage. Parameters: (current_health, max_health).</summary>
        public static event Action<float, float> PlayerHealthChanged;

        // ============= EVENT RAISE METHODS (Called by systems) =============
        public static void RaiseCrystalDrainStarted(ManaCrystal crystal, float intensity)
            => CrystalDrainStarted?.Invoke(crystal, intensity);

        public static void RaiseCrystalDrainProgress(ManaCrystal crystal, float normalizedDrained, float deltaMana)
            => CrystalDrainProgress?.Invoke(crystal, normalizedDrained, deltaMana);

        public static void RaiseCrystalDrainEnded(ManaCrystal crystal, float drainedAmount, bool depleted)
            => CrystalDrainEnded?.Invoke(crystal, drainedAmount, depleted);

        public static void RaiseCrystalDepleted(ManaCrystal crystal)
            => CrystalDepleted?.Invoke(crystal);

        /// <summary>Called by noise sources (crystals, gates, heavy objects) to alert nearby guardians.</summary>
        public static void RaiseNoise(Vector3 position, float radius, float threat)
            => NoiseEmitted?.Invoke(position, radius, threat);

        public static void RaiseDragonManaChanged(float current, float max, float delta)
            => DragonManaChanged?.Invoke(current, max, delta);

        public static void RaiseDragonStageChanged(DragonStage stage)
            => DragonStageChanged?.Invoke(stage);

        public static void RaiseDragonEssenceColorChanged(Color color)
            => DragonEssenceColorChanged?.Invoke(color);

        public static void RaiseGuardianKilled(GuardianController guardian, int materialAmount, float manaReturned)
            => GuardianKilled?.Invoke(guardian, materialAmount, manaReturned);

        public static void RaisePlayerHealthChanged(float current, float max)
            => PlayerHealthChanged?.Invoke(current, max);

        public static void RaiseDragonHPIsZero() 
            => DragonHPIsZero?.Invoke();
    }
}
