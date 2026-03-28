using UnityEngine;
using DungeonPrototype.Mana;

namespace DungeonPrototype.Environment
{
    public class PortableCrystalLantern : MonoBehaviour
    {
        [SerializeField] private ManaCrystal linkedCrystal;
        [SerializeField] private Light lanternLight;
        [SerializeField] private float minIntensity = 0.2f;
        [SerializeField] private float maxIntensity = 2.5f;

        private void Update()
        {
            if (linkedCrystal == null || lanternLight == null)
            {
                return;
            }

            float t = Mathf.Clamp01(linkedCrystal.CurrentMana / linkedCrystal.MaxMana);
            lanternLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
        }
    }
}
