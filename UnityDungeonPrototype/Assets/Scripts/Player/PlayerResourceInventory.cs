using UnityEngine;

namespace DungeonPrototype.Player
{
    public class PlayerResourceInventory : MonoBehaviour
    {
        public int GuardianShardMaterials { get; private set; }

        public void AddGuardianMaterials(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            GuardianShardMaterials += amount;
        }
    }
}
