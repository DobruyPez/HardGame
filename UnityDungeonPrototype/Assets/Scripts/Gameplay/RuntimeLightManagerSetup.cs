using UnityEngine;
using DungeonPrototype.Mana;
using DungeonPrototype.Environment;

namespace DungeonPrototype.Gameplay
{
    /// <summary>
    /// Runtime light manager setup - automatically adds LightManager to all crystals.
    /// </summary>
    [ExecuteAlways]
    public class RuntimeLightManagerSetup : MonoBehaviour
    {
        private void OnEnable()
        {
            SetupLightManagers();
        }

        private void SetupLightManagers()
        {
            ManaCrystal[] crystals = FindObjectsOfType<ManaCrystal>();
            
            foreach (ManaCrystal crystal in crystals)
            {
                if (crystal.GetComponent<LightManager>() == null)
                {
                    LightManager manager = crystal.gameObject.AddComponent<LightManager>();
                    Debug.Log($"✅ Added LightManager to {crystal.gameObject.name}");
                }
            }
        }
    }
}
