using DungeonPrototype.Core;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonPrototype.Dragon
{
    /// <summary>
    /// Manages the dragon companion's core state: mana storage, growth stages, and essence color.
    /// 
    /// Growth Stages (based on mana thresholds):
    /// - Hatchling (0-34 mana): Small, weak, cannot open gates alone
    /// - Companion (35-74 mana): Medium, can repel nearby guardians  
    /// - Sacred (75+ mana): Large, full power, opens high gates
    /// 
    /// The dragon slowly loses mana over time (drainInterval/drainAmount configurable).
    /// When mana reaches 0, the player dies (via GameEvents.RaiseDragonHPIsZero).
    /// 
    /// Visual representation switches between stage models via UpdateStageModel().
    /// Size smoothly lerps between hatchling/sacred scale based on current mana percentage.
    /// </summary>
    public class DragonCompanion : MonoBehaviour
    {
        [Header("Mana")]
        [SerializeField] private float maxMana = 100f;
        [SerializeField] private float stage2Threshold = 35f;
        [SerializeField] private float stage3Threshold = 75f;

        [Header("Growth")]
        [SerializeField] private Vector3 hatchlingScale = Vector3.one * 0.6f;
        [SerializeField] private Vector3 sacredScale = Vector3.one * 1.75f;
        [SerializeField] private float scaleLerpSpeed = 3f;

        /// <summary>List of dragon stage models (Hatchling, Companion, Sacred). Set in inspector.</summary>
        public List<DragonStageData> stageModels;

        [Header("Essence")]
        [SerializeField] private Gradient essenceByMana;

        [Header("Mana Drain")]
        /// <summary>How often the dragon loses mana while idle (seconds between drain ticks).</summary>
        [SerializeField] private float manaDrainInterval = 5f;
        
        /// <summary>Mana amount lost per drain tick. Formula: health_loss = amount per mana_drain_interval.</summary>
        [SerializeField] private float manaDrainAmount = 1f;

        /// <summary>Current mana stored in the dragon. Increases via crystal drain, decreases via starvation.</summary>
        public float CurrentMana { get; private set; }
        
        /// <summary>Maximum mana capacity.</summary>
        public float MaxMana => maxMana;
        
        /// <summary>Current growth stage based on mana thresholds.</summary>
        public DragonStage CurrentStage { get; private set; } = DragonStage.Hatchling;

        /// <summary>Alias for CurrentMana - used by pressure plates to check if dragon is heavy enough to open gates.</summary>
        public float ManaWeight => CurrentMana;
        
        /// <summary>Dragon's visual essence color, interpolated from gradient based on mana percentage.</summary>
        public Color EssenceColor => essenceByMana.Evaluate(Mathf.Clamp01(CurrentMana / maxMana));

        private Vector3 _targetScale;
        private float _drainTimer;

        private void Awake()
        {
            // Initialize dragon as small hatchling
            transform.localScale = hatchlingScale;
            _targetScale = hatchlingScale;
            
            // Pre-instantiate all stage models (efficiency: done once, not every frame)
            InitializeStageModels();
            UpdateStageModel(CurrentStage);
            BroadcastState(0f);

            _drainTimer = manaDrainInterval;
        }

        /// <summary>
        /// Updates dragon scale smoothly and periodically drain mana (starvation mechanic).
        /// </summary>
        private void Update()
        {
            // Smoothly grow/shrink based on mana percentage (Hatchling -> Sacred transition)
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * scaleLerpSpeed);

            // Handle starvation: reduce mana periodically
            _drainTimer -= Time.deltaTime;
            if (_drainTimer <= 0f)
            {
                DrainManaOverTime();
                _drainTimer = manaDrainInterval;
            }
        }

        /// <summary>
        /// Pre-instantiates and caches all dragon stage models (Hatchling, Companion, Sacred).
        /// This happens once in Awake() - not every frame - for performance.
        /// Models are disabled except the current stage model.
        /// </summary>
        private void InitializeStageModels()
        {
            foreach (var stageData in stageModels)
            {
                if (stageData.modelPrefab != null)
                {
                    // Create model as child of dragon
                    stageData.instantiatedModel = Instantiate(stageData.modelPrefab, transform);

                    // Set local position/rotation/scale
                    stageData.instantiatedModel.transform.localPosition = Vector3.zero;
                    stageData.instantiatedModel.transform.localRotation = Quaternion.identity;
                    stageData.instantiatedModel.transform.localScale = stageData.scale;
                    
                    // Disable until we need it
                    stageData.instantiatedModel.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Switches the active dragon model to match a new stage.
        /// Called when dragon crosses a mana threshold.
        /// </summary>
        private void UpdateStageModel(DragonStage newStage)
        {
            // Find the data for this stage
            DragonStageData newStageData = stageModels.Find(data => data.stage == newStage);

            if (newStageData == null || newStageData.modelPrefab == null)
                return;

            // Disable all stage models
            foreach (var stageData in stageModels)
            {
                if (stageData.instantiatedModel != null)
                    stageData.instantiatedModel.SetActive(false);
            }

            // Enable only the current stage model
            newStageData.instantiatedModel.SetActive(true);
        }

        /// <summary>
        /// Called periodically (every manaDrainInterval seconds) to simulate dragon starvation.
        /// If dragon reaches 0 mana, player loses immediately.
        /// </summary>
        private void DrainManaOverTime()
        {
            if (CurrentMana <= 0f)
            {
                GameEvents.RaiseDragonHPIsZero();
                return;
            }

            float drainedAmount = Mathf.Min(manaDrainAmount, CurrentMana);
            RemoveMana(drainedAmount);
        }

        /// <summary>
        /// Adds mana to the dragon, typically from crystal drain interactions.
        /// Triggers stage evaluation and model update if mana crosses a threshold.
        /// </summary>
        /// <param name="amount">Mana to add. Clamped to max capacity.</param>
        /// <returns>Actual mana added (may be less if already at max).</returns>
        public float AddMana(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            float before = CurrentMana;
            CurrentMana = Mathf.Clamp(CurrentMana + amount, 0f, maxMana);
            float gained = CurrentMana - before;

            if (gained > 0f)
            {
                EvaluateStage();
                UpdateTargetScale();
                BroadcastState(gained);
            }

            return gained;
        }

        /// <summary>
        /// Removes mana from the dragon (starvation, or future mechanics).
        /// Returns mana to players via GuardianDeathRewardRelay after defeating enemies.
        /// </summary>
        /// <param name="amount">Mana to remove.</param>
        /// <returns>Actual mana removed.</returns>
        public float RemoveMana(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            float before = CurrentMana;
            CurrentMana = Mathf.Clamp(CurrentMana - amount, 0f, maxMana);
            float removed = before - CurrentMana;

            if (removed > 0f)
            {
                EvaluateStage();
                UpdateTargetScale();
                BroadcastState(-removed);
            }

            return removed;
        }

        /// <summary>Checks if dragon has reached at least a specific stage (Hatchling <= Companion <= Sacred).</summary>
        public bool IsAtLeastStage(DragonStage stage) => CurrentStage >= stage;

        /// <summary>
        /// Updates target scale based on current mana percentage.
        /// Smooth transition between hatchling (small) and sacred (large).
        /// Called by AddMana() and RemoveMana().
        /// </summary>
        private void UpdateTargetScale()
        {
            float t = Mathf.Clamp01(CurrentMana / maxMana);
            _targetScale = Vector3.Lerp(hatchlingScale, sacredScale, t);
        }

        /// <summary>
        /// Evaluates if the dragon has crossed a mana threshold and should change stages.
        /// Called whenever mana changes. If stage changed, broadcasts event and updates model.
        /// </summary>
        private void EvaluateStage()
        {
            DragonStage previous = CurrentStage;

            if (CurrentMana >= stage3Threshold)
            {
                CurrentStage = DragonStage.Sacred;
            }
            else if (CurrentMana >= stage2Threshold)
            {
                CurrentStage = DragonStage.Companion;
            }
            else
            {
                CurrentStage = DragonStage.Hatchling;
            }

            if (previous != CurrentStage)
            {
                UpdateStageModel(CurrentStage);
                GameEvents.RaiseDragonStageChanged(CurrentStage);
            }
        }

        /// <summary>
        /// Broadcasts current dragon state to all listeners (gates, guardians, UI).
        /// Called after any mana change.
        /// </summary>
        /// <param name="delta">Mana change amount (positive for gain, negative for loss).</param>
        private void BroadcastState(float delta)
        {
            GameEvents.RaiseDragonManaChanged(CurrentMana, maxMana, delta);
            GameEvents.RaiseDragonEssenceColorChanged(EssenceColor);
        }
    }
}
