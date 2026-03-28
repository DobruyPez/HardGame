using UnityEngine;
using DungeonPrototype.Core;
using DungeonPrototype.Dragon;
using DungeonPrototype.Player;

namespace DungeonPrototype.Guardians
{
    public class GuardianDeathRewardRelay : MonoBehaviour
    {
        [SerializeField] private DragonCompanion dragon;
        [SerializeField] private PlayerResourceInventory inventory;

        private void OnEnable()
        {
            GameEvents.GuardianKilled += OnGuardianKilled;
        }

        private void OnDisable()
        {
            GameEvents.GuardianKilled -= OnGuardianKilled;
        }

        private void OnGuardianKilled(GuardianController guardian, int materialAmount, float manaReturned)
        {
            if (inventory != null)
            {
                inventory.AddGuardianMaterials(materialAmount);
            }

            if (dragon != null && manaReturned > 0f)
            {
                dragon.AddMana(manaReturned);
            }
        }
    }
}
