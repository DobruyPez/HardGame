using UnityEngine;
using DungeonPrototype.Core;

namespace DungeonPrototype.Environment
{
    public class CrystalDrainAlarmRelay : MonoBehaviour
    {
        [SerializeField] private float partialDrainThreatThreshold = 0.25f;
        [SerializeField] private float wakeThreatThreshold = 0.9f;

        private void OnEnable()
        {
            GameEvents.CrystalDrainProgress += OnCrystalDrainProgress;
            GameEvents.CrystalDrainEnded += OnCrystalDrainEnded;
        }

        private void OnDisable()
        {
            GameEvents.CrystalDrainProgress -= OnCrystalDrainProgress;
            GameEvents.CrystalDrainEnded -= OnCrystalDrainEnded;
        }

        private void OnCrystalDrainProgress(Mana.ManaCrystal crystal, float normalizedDrained, float deltaMana)
        {
            if (normalizedDrained >= partialDrainThreatThreshold)
            {
                float threat = Mathf.Lerp(0.2f, 0.7f, normalizedDrained);
                GameEvents.RaiseNoise(crystal.transform.position, 10f + normalizedDrained * 8f, threat);
            }
        }

        private void OnCrystalDrainEnded(Mana.ManaCrystal crystal, float drainedAmount, bool depleted)
        {
            float threat = depleted ? 1f : Mathf.Clamp01(drainedAmount / crystal.MaxMana);
            if (threat >= wakeThreatThreshold)
            {
                GameEvents.RaiseNoise(crystal.transform.position, 22f, threat);
            }
        }
    }
}
