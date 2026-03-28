using UnityEngine;
using DungeonPrototype.Core;
using DungeonPrototype.Player;

namespace DungeonPrototype.Dragon
{
    public class DragonHungerSystem : MonoBehaviour
    {
        [SerializeField] private DragonGrowthController dragon;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private float starvingAfterSeconds = 90f;
        [SerializeField] private float damagePerTick = 4f;
        [SerializeField] private float tickInterval = 2f;

        private float _lastFedTime;
        private float _damageTickTimer;

        private void Awake()
        {
            _lastFedTime = Time.time;
            _damageTickTimer = 0f;
        }

        private void OnEnable()
        {
            GameEvents.DragonHPIsZero += OnDragonManaChanged;
        }

        private void OnDisable()
        {
            GameEvents.DragonHPIsZero -= OnDragonManaChanged;
        }

        private void Update()
        {
            if (dragon == null || playerHealth == null)
            {
                return;
            }

            if (dragon.CurrentStage != DragonStage.Hatchling)
            {
                _damageTickTimer = 0f;
                return;
            }

            if (Time.time - _lastFedTime < starvingAfterSeconds)
            {
                return;
            }

            _damageTickTimer += Time.deltaTime;
            if (_damageTickTimer >= tickInterval)
            {
                _damageTickTimer = 0f;
                playerHealth.TakeDamage(damagePerTick);
            }
        }

        private void OnDragonManaChanged()
        {
            if (playerHealth == null)
            {
                return;
            }

            playerHealth.TakeDamage(damagePerTick);
            _lastFedTime = Time.time;
            _damageTickTimer = 0f;
        }
    }
}
