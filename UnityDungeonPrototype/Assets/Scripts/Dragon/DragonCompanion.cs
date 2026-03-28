using UnityEngine;
using DungeonPrototype.Core;

namespace DungeonPrototype.Dragon
{
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

        [Header("Essence")]
        [SerializeField] private Gradient essenceByMana;

        public float CurrentMana { get; private set; }
        public float MaxMana => maxMana;
        public DragonStage CurrentStage { get; private set; } = DragonStage.Hatchling;

        public float ManaWeight => CurrentMana;
        public Color EssenceColor => essenceByMana.Evaluate(Mathf.Clamp01(CurrentMana / maxMana));

        private Vector3 _targetScale;

        private void Awake()
        {
            transform.localScale = hatchlingScale;
            _targetScale = hatchlingScale;
            BroadcastState(0f);
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * scaleLerpSpeed);
        }

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

        public bool IsAtLeastStage(DragonStage stage) => CurrentStage >= stage;

        private void UpdateTargetScale()
        {
            float t = Mathf.Clamp01(CurrentMana / maxMana);
            _targetScale = Vector3.Lerp(hatchlingScale, sacredScale, t);
        }

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
                GameEvents.RaiseDragonStageChanged(CurrentStage);
            }
        }

        private void BroadcastState(float delta)
        {
            GameEvents.RaiseDragonManaChanged(CurrentMana, maxMana, delta);
            GameEvents.RaiseDragonEssenceColorChanged(EssenceColor);
        }
    }
}
