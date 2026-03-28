using UnityEngine;
using DungeonPrototype.Mana;
using DungeonPrototype.Environment;
using DungeonPrototype.Dragon;
using DungeonPrototype.UI;

namespace DungeonPrototype.Gameplay
{
    /// <summary>
    /// Runtime atmosphere initializer.
    /// Runs on scene load to configure fog, post-processing, and visual effects.
    /// </summary>
    public class AtmosphereInitializer : MonoBehaviour
    {
        [SerializeField] private bool setupOnAwake = true;

        private void Awake()
        {
            if (setupOnAwake)
            {
                InitializeAtmosphere();
            }
        }

        public void InitializeAtmosphere()
        {
            // Configure Global Fog
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.1f, 0.1f, 0.15f, 1f); // Dark blue: иссиня-черный
            RenderSettings.fogDensity = 0.05f;
            RenderSettings.fogMode = FogMode.Exponential;
            Debug.Log("✅ Global fog initialized: dark blue color, density 0.05");

            // Create or find Post-Processing Volume
            SetupPostProcessing();

            // Add LightManagers to crystals
            SetupCrystalLights();

            // Add HungerVisualEffects coordinator
            SetupHungerEffects();

            // Apply shaders to walls
            ApplyWallShaders();

            Debug.Log("✅ Atmosphere initialization complete!");
        }

        private void SetupPostProcessing()
        {
            GameObject volumeGO = GameObject.Find("PostProcessingVolume");
            if (volumeGO == null)
            {
                volumeGO = new GameObject("PostProcessingVolume");
            }

            var fogSetup = volumeGO.GetComponent<FogAndPostProcessingSetup>();
            if (fogSetup == null)
            {
                fogSetup = volumeGO.AddComponent<FogAndPostProcessingSetup>();
            }

            Debug.Log("✅ Post-Processing setup complete");
        }

        private void SetupCrystalLights()
        {
            ManaCrystal[] crystals = FindObjectsOfType<ManaCrystal>();

            foreach (ManaCrystal crystal in crystals)
            {
                if (crystal.GetComponent<LightManager>() == null)
                {
                    crystal.gameObject.AddComponent<LightManager>();
                    Debug.Log($"✅ LightManager added to {crystal.gameObject.name}");
                }
            }
        }

        private void SetupHungerEffects()
        {
            Transform sceneSystemsRoot = GameObject.Find("LevelRoot/SceneSystems")?.transform;
            if (sceneSystemsRoot != null && FindObjectOfType<HungerVisualEffects>() == null)
            {
                GameObject hungerGO = new GameObject("HungerEffects");
                hungerGO.transform.SetParent(sceneSystemsRoot);

                var hungerEffects = hungerGO.AddComponent<HungerVisualEffects>();
                Debug.Log("✅ HungerVisualEffects coordinator added");
            }
        }

        private void ApplyWallShaders()
        {
            Shader adaptiveWallShader = Shader.Find("DungeonPrototype/AdaptiveWall");
            if (adaptiveWallShader == null)
            {
                Debug.LogWarning("⚠ AdaptiveWall shader not found. Walls will use default material.");
                return;
            }

            Renderer[] allRenderers = FindObjectsOfType<Renderer>();
            int count = 0;

            foreach (Renderer renderer in allRenderers)
            {
                if (renderer.gameObject.name.Contains("Wall") || renderer.gameObject.name.Contains("wall"))
                {
                    Material mat = new Material(adaptiveWallShader);
                    if (renderer.material.HasProperty("_MainTex"))
                    {
                        mat.SetTexture("_MainTex", renderer.material.GetTexture("_MainTex"));
                    }
                    renderer.material = mat;
                    count++;
                }
            }

            Debug.Log($"✅ Applied AdaptiveWall shader to {count} wall renderers");
        }
    }
}
