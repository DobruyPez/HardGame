using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DungeonPrototype.Environment
{
    /// <summary>
    /// Reusable smooth light fading component for Unity 6.4+.
    /// </summary>
    public class AdvancedLightFading : MonoBehaviour
    {
        [SerializeField] private Light lightComponent;
        [SerializeField] private float minIntensity = 0.0f;
        [SerializeField] private float maxIntensity = 2.0f;
        [SerializeField] private float fadeDurationSeconds = 1.5f;

        private float _startIntensity;
        private float _targetIntensity;
        private float _timer;
        private bool _isFading;

        public void Configure(Light lightRef, float minValue, float maxValue, float durationSeconds)
        {
            lightComponent = lightRef;
            minIntensity = minValue;
            maxIntensity = maxValue;
            fadeDurationSeconds = Mathf.Max(0.01f, durationSeconds);

            if (lightComponent != null)
            {
                _targetIntensity = lightComponent.intensity;
            }
        }

        public void SetTargetFromNormalized(float normalizedValue)
        {
            float clamped = Mathf.Clamp01(normalizedValue);
            float target = Mathf.Lerp(minIntensity, maxIntensity, clamped);
            StartFade(target);
        }

        public void FadeToMin()
        {
            StartFade(minIntensity);
        }

        private void StartFade(float targetIntensity)
        {
            if (lightComponent != null)
            {
                _startIntensity = lightComponent.intensity;
                _targetIntensity = Mathf.Clamp(targetIntensity, minIntensity, maxIntensity);
                _timer = 0f;
                _isFading = true;
            }
        }

        private void Update()
        {
            if (!_isFading)
            {
                return;
            }

            if (lightComponent != null)
            {
                _timer += Time.deltaTime;
                float t = Mathf.Clamp01(_timer / fadeDurationSeconds);
                lightComponent.intensity = Mathf.Lerp(_startIntensity, _targetIntensity, t);

                if (t >= 1f)
                {
                    _isFading = false;
                }
            }
            else
            {
                _isFading = false;
            }
        }
    }
}
