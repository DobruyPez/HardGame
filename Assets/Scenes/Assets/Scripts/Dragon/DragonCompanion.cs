using DungeonPrototype.Core;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonPrototype.Dragon
{
    public class DragonCompanion : MonoBehaviour
    {
        [Header("Mana")]
        [SerializeField] private float maxMana = 100f;

        [Header("Essence")]
        [SerializeField] private Gradient essenceByMana;

        [Header("Mana Drain")]
        [SerializeField] private float manaDrainInterval = 5f;
        [SerializeField] private float manaDrainAmount = 1f;

        public float CurrentMana { get; private set; }
        public float MaxMana => maxMana;
        public Color EssenceColor => essenceByMana.Evaluate(Mathf.Clamp01(CurrentMana / maxMana));

        private float _drainTimer;

        private void Awake()
        {
            CurrentMana = maxMana;
            BroadcastState(0f);
            _drainTimer = manaDrainInterval;
        }

        private void Update()
        {
            _drainTimer -= Time.deltaTime;
            if (_drainTimer <= 0f)
            {
                DrainManaOverTime();
                _drainTimer = manaDrainInterval;
            }
        }

        private void DrainManaOverTime()
        {
            if (CurrentMana <= 0f)
            {
                GameEvents.RaiseDragonHPIsZero();
                return;
            }

            float drainedAmount = Mathf.Min(manaDrainAmount, CurrentMana);
            RemoveMana(drainedAmount);
            Debug.Log($"Dragon lost {drainedAmount} mana over time. Current mana: {CurrentMana}");
        }

        public float AddMana(float amount)
        {
            if (amount <= 0f)
                return 0f;

            float before = CurrentMana;
            CurrentMana = Mathf.Clamp(CurrentMana + amount, 0f, maxMana);
            float gained = CurrentMana - before;

            if (gained > 0f)
            {
                BroadcastState(gained);
            }

            return gained;
        }

        public float RemoveMana(float amount)
        {
            if (amount <= 0f)
                return 0f;

            float before = CurrentMana;
            CurrentMana = Mathf.Clamp(CurrentMana - amount, 0f, maxMana);
            float removed = before - CurrentMana;

            if (removed > 0f)
            {
                BroadcastState(-removed);
            }

            return removed;
        }

        private void BroadcastState(float delta)
        {
            GameEvents.RaiseDragonManaChanged(CurrentMana, maxMana, delta);
            GameEvents.RaiseDragonEssenceColorChanged(EssenceColor);
        }
    }
}