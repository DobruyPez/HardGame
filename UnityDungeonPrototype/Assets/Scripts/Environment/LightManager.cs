using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DungeonPrototype.Core;
using DungeonPrototype.Mana;

namespace DungeonPrototype.Environment
{
    /// <summary>
    /// Manages smooth light fade effects for mana crystals during depletion.
    /// Listens to crystal drain events and smoothly dims the associated light.
    /// </summary>
    public class LightManager : MonoBehaviour
    {
        [Header("Light Settings")]
        [SerializeField] private Light lightComponent;
        [SerializeField] private float maxIntensity = 2.0f;
        [SerializeField] private float minIntensity = 0.0f;
        [SerializeField] private float fadeDurationSeconds = 1.5f;

        private ManaCrystal _linkedCrystal;
        private AdvancedLightFading _lightFading;

        private void Awake()
        {
            if (lightComponent == null)
            {
                lightComponent = GetComponent<Light>();
            }

            if (lightComponent == null)
            {
                lightComponent = GetComponentInChildren<Light>();
            }

            _lightFading = GetComponent<AdvancedLightFading>();
            if (_lightFading == null)
            {
                _lightFading = gameObject.AddComponent<AdvancedLightFading>();
            }

            _lightFading.Configure(lightComponent, minIntensity, maxIntensity, fadeDurationSeconds);

            // Auto-discover ManaCrystal in same object or parents
            _linkedCrystal = GetComponent<ManaCrystal>();
            if (_linkedCrystal == null)
            {
                _linkedCrystal = GetComponentInParent<ManaCrystal>();
            }
        }

        private void OnEnable()
        {
            GameEvents.CrystalDepleted += OnCrystalDepleted;
            GameEvents.CrystalDrainProgress += OnCrystalDrainProgress;
        }

        private void OnDisable()
        {
            GameEvents.CrystalDepleted -= OnCrystalDepleted;
            GameEvents.CrystalDrainProgress -= OnCrystalDrainProgress;
        }

        private void OnCrystalDrainProgress(ManaCrystal crystal, float drainProgress, float amount)
        {
            if (crystal == _linkedCrystal)
            {
                // Scale light intensity with remaining mana
                float manaRatio = 1f - drainProgress;
                if (_lightFading != null)
                {
                    _lightFading.SetTargetFromNormalized(manaRatio);
                }
            }
        }

        private void OnCrystalDepleted(ManaCrystal crystal)
        {
            if (crystal == _linkedCrystal)
            {
                // Smoothly fade out the light
                if (_lightFading != null)
                {
                    _lightFading.FadeToMin();
                }
            }
        }
    }
}
