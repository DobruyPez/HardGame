using DungeonPrototype.Core;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DungeonPrototype.Dragon
{
    internal class DragonGrowthController : MonoBehaviour
    {
        [Header("Growth Settings")]
        [SerializeField] private Vector3 hatchlingScale = Vector3.one * 0.6f;
        [SerializeField] private Vector3 sacredScale = Vector3.one * 1.75f;
        [SerializeField] private float scaleLerpSpeed = 3f;

        [Header("Stage Thresholds")]
        [SerializeField] private float stage2Threshold = 35f;
        [SerializeField] private float stage3Threshold = 75f;

        [Header("Stage Models")]
        public List<DragonStageData> stageModels;

        private Vector3 _targetScale;
        private float _maxMana;
        public DragonStage CurrentStage = DragonStage.Hatchling;

        private void Awake()
        {
            // Инициализация начального масштаба
            transform.localScale = hatchlingScale;
            _targetScale = hatchlingScale;

            // Инициализация моделей
            InitializeStageModels();
            UpdateStageModel(CurrentStage);
        }

        private void OnEnable()
        {
            GameEvents.DragonManaChanged += OnDragonManaChanged;
        }

        private void OnDisable()
        {
            GameEvents.DragonManaChanged -= OnDragonManaChanged;
        }

        private void Update()
        {
            // Плавное изменение масштаба
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * scaleLerpSpeed);
        }

        private void OnDragonManaChanged(float currentMana, float maxMana, float delta)
        {
            _maxMana = maxMana;

            // Обновляем целевой масштаб
            UpdateTargetScale(currentMana);

            // Обновляем этап и модель
            EvaluateStage(currentMana);
        }

        private void UpdateTargetScale(float currentMana)
        {
            float t = Mathf.Clamp01(currentMana / _maxMana);
            _targetScale = Vector3.Lerp(hatchlingScale, sacredScale, t);
        }

        private void EvaluateStage(float currentMana)
        {
            DragonStage previous = CurrentStage;

            if (currentMana >= stage3Threshold)
            {
                CurrentStage = DragonStage.Sacred;
            }
            else if (currentMana >= stage2Threshold)
            {
                CurrentStage = DragonStage.Companion;
            }
            else
            {
                CurrentStage = DragonStage.Hatchling;
            }

            if (previous != CurrentStage)
            {
                //UpdateStageModel(CurrentStage);
                GameEvents.RaiseDragonStageChanged(CurrentStage);
            }
        }

        private void InitializeStageModels()
        {
            foreach (var stageData in stageModels)
            {
                if (stageData.modelPrefab != null)
                {
                    stageData.instantiatedModel = Instantiate(stageData.modelPrefab, transform);
                    stageData.instantiatedModel.transform.localPosition = Vector3.zero;
                    stageData.instantiatedModel.transform.localRotation = Quaternion.identity;
                    stageData.instantiatedModel.transform.localScale = stageData.scale;
                    stageData.instantiatedModel.SetActive(false);
                }
            }
        }

        private void UpdateStageModel(DragonStage newStage)
        {
            DragonStageData newStageData = stageModels.Find(data => data.stage == newStage);

            if (newStageData == null || newStageData.modelPrefab == null)
                return;

            foreach (var stageData in stageModels)
            {
                if (stageData.instantiatedModel != null)
                    stageData.instantiatedModel.SetActive(false);
            }

            newStageData.instantiatedModel.SetActive(true);
        }

        // Публичные методы для ручного управления
        public void SetScale(float normalizedValue)
        {
            float t = Mathf.Clamp01(normalizedValue);
            _targetScale = Vector3.Lerp(hatchlingScale, sacredScale, t);
        }

        public void ForceSetStage(DragonStage stage)
        {
            CurrentStage = stage;
            UpdateStageModel(CurrentStage);
        }

        public DragonStage GetCurrentStage() => CurrentStage;
    }
}

