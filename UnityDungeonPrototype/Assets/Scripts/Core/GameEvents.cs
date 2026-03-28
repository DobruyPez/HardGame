using System;
using UnityEngine;
using DungeonPrototype.Dragon;
using DungeonPrototype.Mana;
using DungeonPrototype.Guardians;

namespace DungeonPrototype.Core
{
    public static class GameEvents
    {
        public static event Action<ManaCrystal, float> CrystalDrainStarted;
        public static event Action<ManaCrystal, float, float> CrystalDrainProgress;
        public static event Action<ManaCrystal, float, bool> CrystalDrainEnded;
        public static event Action<ManaCrystal> CrystalDepleted;

        public static event Action<Vector3, float, float> NoiseEmitted;

        public static event Action<float, float, float> DragonManaChanged;
        public static event Action<DragonStage> DragonStageChanged;
        public static event Action<Color> DragonEssenceColorChanged;

        public static event Action<GuardianController, int, float> GuardianKilled;
        public static event Action<float, float> PlayerHealthChanged;

        public static void RaiseCrystalDrainStarted(ManaCrystal crystal, float intensity)
            => CrystalDrainStarted?.Invoke(crystal, intensity);

        public static void RaiseCrystalDrainProgress(ManaCrystal crystal, float normalizedDrained, float deltaMana)
            => CrystalDrainProgress?.Invoke(crystal, normalizedDrained, deltaMana);

        public static void RaiseCrystalDrainEnded(ManaCrystal crystal, float drainedAmount, bool depleted)
            => CrystalDrainEnded?.Invoke(crystal, drainedAmount, depleted);

        public static void RaiseCrystalDepleted(ManaCrystal crystal)
            => CrystalDepleted?.Invoke(crystal);

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
    }
}
