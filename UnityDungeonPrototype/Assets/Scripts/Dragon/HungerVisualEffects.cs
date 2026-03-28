using UnityEngine;
using DungeonPrototype.Core;
using DungeonPrototype.Dragon;

namespace DungeonPrototype.Environment
{
    /// <summary>
    /// Manages chromatic aberration intensity based on dragon hunger and lighting conditions.
    /// Coordinates between DragonHungerSystem and FogAndPostProcessingSetup.
    /// </summary>
    public class HungerVisualEffects : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FogAndPostProcessingSetup postProcessing;
        [SerializeField] private DragonHungerSystem dragonHunger;

        [Header("Aberration Thresholds")]
        [SerializeField] private float lowHungerThreshold = 0.3f; // When dragon is 30% hungry
        [SerializeField] private float criticalHungerThreshold = 0.7f; // When dragon is 70% hungry
        [SerializeField] private float baseLightIntensity = 0.5f;
        [SerializeField] private float maxAberrationIntensity = 3.0f;

        private float _currentAberrationIntensity = 0f;

        private void OnEnable()
        {
            GameEvents.DragonHungerChanged += OnDragonHungerChanged;
        }

        private void OnDisable()
        {
            GameEvents.DragonHungerChanged -= OnDragonHungerChanged;
        }

        private void OnDragonHungerChanged(float hungerRatio)
        {
            UpdateAberrationBasedOnHunger(hungerRatio);
        }

        private void UpdateAberrationBasedOnHunger(float hungerRatio)
        {
            float hungerIntensity = 0f;

            if (hungerRatio >= criticalHungerThreshold)
            {
                // Critical hunger: max aberration
                hungerIntensity = maxAberrationIntensity * (hungerRatio - criticalHungerThreshold) / (1f - criticalHungerThreshold);
            }
            else if (hungerRatio >= lowHungerThreshold)
            {
                // Low hunger: slight aberration
                hungerIntensity = (hungerRatio - lowHungerThreshold) / (criticalHungerThreshold - lowHungerThreshold) * maxAberrationIntensity * 0.5f;
            }

            // Also check environmental lighting
            float lightIntensity = GetEnvironmentLightIntensity();
            float combinedAberration = hungerIntensity + (1f - lightIntensity) * maxAberrationIntensity * 0.3f;

            if (postProcessing != null && combinedAberration != _currentAberrationIntensity)
            {
                _currentAberrationIntensity = combinedAberration;
                postProcessing.SetChromaticAberrationIntensity(_currentAberrationIntensity);
            }
        }

        private float GetEnvironmentLightIntensity()
        {
            // Calculate average light intensity from all lights in scene
            Light[] lights = FindObjectsOfType<Light>();
            if (lights.Length == 0) return 0f;

            float totalIntensity = 0f;
            foreach (Light light in lights)
            {
                if (light.enabled && light.type != LightType.Probe)
                {
                    totalIntensity += light.intensity;
                }
            }

            // Also consider ambient light
            totalIntensity += RenderSettings.ambientLight.grayscale;
            return Mathf.Clamp01(totalIntensity / (lights.Length + 1f));
        }
    }
}
