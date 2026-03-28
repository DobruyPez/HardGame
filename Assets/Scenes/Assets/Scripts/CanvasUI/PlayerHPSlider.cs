using System;
using UnityEngine;
using UnityEngine.UI;
using DungeonPrototype.Core;

namespace Game
{
    /// <summary>
    /// Animates the player health bar UI element.
    /// Provides smooth lerp animation between current and target health values for visual feedback.
    /// Separate from PlayerHealth logic to allow smooth transitions when damage is taken.
    /// </summary>
    public class PlayerHPSlider : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        
        /// <summary>How quickly the displayed health animates towards actual health (higher = faster).</summary>
        [SerializeField] private float animationSpeed = 5f;
        
        /// <summary>Threshold below which we consider display health equal to target (prevents floating point precision issues).</summary>
        private const float LERP_COMPLETION_THRESHOLD = 0.01f;

        private float _currentDisplayHealth;
        private float _targetHealth;
        private float _maxHealth;

        private void Awake()
        {
            if (healthSlider == null)
            {
                healthSlider = GetComponent<Slider>();

                if (healthSlider == null)
                {
                    Debug.LogError("PlayerHPSlider: No Slider component found! This component requires a UI Slider.");
                    return;
                }
            }
        }

        /// <summary>Subscribe to health change events when component becomes active.</summary>
        private void OnEnable()
        {
            GameEvents.PlayerHealthChanged += OnPlayerHealthChanged;
        }

        /// <summary>Unsubscribe from health change events when component is disabled (prevents memory leaks).</summary>
        private void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= OnPlayerHealthChanged;
        }

        /// <summary>Updates the visual health bar with smooth lerp animation each frame.</summary>
        private void Update()
        {
            if (healthSlider == null) 
                return;

            // Only animate if display health differs significantly from target
            if (Math.Abs(_currentDisplayHealth - _targetHealth) > LERP_COMPLETION_THRESHOLD)
            {
                _currentDisplayHealth = Mathf.Lerp(_currentDisplayHealth, _targetHealth, Time.deltaTime * animationSpeed);
                healthSlider.value = _currentDisplayHealth / _maxHealth;
            }
        }

        /// <summary>
        /// Called by GameEvents when player health changes.
        /// Updates target health and initializes display value on first frame.
        /// </summary>
        private void OnPlayerHealthChanged(float currentHealth, float maxHealth)
        {
            _targetHealth = currentHealth;
            _maxHealth = maxHealth;

            // Initialize display value if this is the first update (prevents slider starting at 0)
            if (_currentDisplayHealth == 0 && _targetHealth > 0)
            {
                _currentDisplayHealth = _targetHealth;
                healthSlider.value = _currentDisplayHealth / _maxHealth;
            }
        }
    }
}