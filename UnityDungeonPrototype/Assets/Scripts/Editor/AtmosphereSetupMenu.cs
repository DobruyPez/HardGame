using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DungeonPrototype.Environment;
using DungeonPrototype.Mana;
using DungeonPrototype.Dragon;

/// <summary>
/// Editor menu for setting up atmosphere and visual effects for the DungeonPrototype.
/// </summary>
public class AtmosphereSetupMenu
{
    [MenuItem("Tools/Dungeon Prototype/Setup Atmosphere & Effects")]
    public static void SetupAtmosphere()
    {
        // Step 1: Setup Global Fog
        SetupGlobalFog();

        // Step 2: Create Post-Processing Volume
        CreatePostProcessingVolume();

        // Step 3: Add LightManagers to crystals
        AddLightManagersToCrystals();

        // Step 4: Add HungerVisualEffects coordinator
        AddHungerVisualEffects();

        // Step 5: Apply wave shader to walls
        ApplyWallShaders();

        EditorUtility.DisplayDialog("Atmosphere Setup", 
            "✅ Atmosphere and visual effects configured successfully!\n\n" +
            "• Global fog applied (dark blue, density 0.05)\n" +
            "• Post-processing volume created\n" +
            "• Light managers added to crystals\n" +
            "• Hunger effects coordinator added\n" +
            "• Wall shaders applied",
            "OK");
    }

    private static void SetupGlobalFog()
    {
        // Configure RenderSettings
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.1f, 0.1f, 0.15f, 1f); // Dark blue
        RenderSettings.fogDensity = 0.05f;
        RenderSettings.fogMode = FogMode.Exponential;

        Debug.Log("✅ Global fog configured: color (0.1,0.1,0.15), density 0.05");
    }

    private static void CreatePostProcessingVolume()
    {
        GameObject volumeGO = new GameObject("PostProcessingVolume");
        volumeGO.transform.SetParent(null);

        // Add FogAndPostProcessingSetup component
        var fogAndPostProcessing = volumeGO.AddComponent<FogAndPostProcessingSetup>();

        // Try to add PostProcessVolume if package is available
        var volumeType = System.Type.GetType("UnityEngine.Rendering.PostProcessing.PostProcessVolume, PostProcessing");
        if (volumeType != null)
        {
            var volume = (Component)volumeGO.AddComponent(volumeType);
            Debug.Log("✅ Post-Processing Volume created with PostProcessVolume component");
        }
        else
        {
            Debug.LogWarning("⚠ Post-Processing Stack v2 not installed. Using fallback FogAndPostProcessingSetup.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void AddLightManagersToCrystals()
    {
        ManaCrystal[] crystals = FindObjectsOfType<ManaCrystal>();

        foreach (ManaCrystal crystal in crystals)
        {
            // Check if LightManager already exists
            LightManager existing = crystal.GetComponent<LightManager>();
            if (existing != null) continue;

            // Add LightManager to crystal
            var lightManager = crystal.gameObject.AddComponent<LightManager>();

            // Try to auto-discover light
            Light light = crystal.GetComponentInChildren<Light>();
            if (light != null)
            {
                // Use reflection to set the light field
                var lightField = typeof(LightManager).GetField("crystalLight", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (lightField != null)
                {
                    lightField.SetValue(lightManager, light);
                }
                Debug.Log($"✅ LightManager added to {crystal.gameObject.name} with linked light");
            }
            else
            {
                Debug.LogWarning($"⚠ No light found for crystal {crystal.gameObject.name}");
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void AddHungerVisualEffects()
    {
        // Find or create SceneSystems container
        GameObject systemsContainer = GameObject.Find("LevelRoot/SceneSystems");
        if (systemsContainer == null)
        {
            systemsContainer = new GameObject("SceneSystems");
            Transform levelRoot = GameObject.Find("LevelRoot")?.transform;
            if (levelRoot != null)
            {
                systemsContainer.transform.SetParent(levelRoot);
            }
        }

        // Add HungerVisualEffects component
        var hungerEffects = systemsContainer.AddComponent<HungerVisualEffects>();

        // Try to find post-processing volume
        var postProcessing = FindObjectOfType<FogAndPostProcessingSetup>();
        if (postProcessing != null)
        {
            var postProcessingField = typeof(HungerVisualEffects).GetField("postProcessing", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (postProcessingField != null)
            {
                postProcessingField.SetValue(hungerEffects, postProcessing);
            }
        }

        // Try to find dragon hunger system
        var dragonHunger = FindObjectOfType<DragonHungerSystem>();
        if (dragonHunger != null)
        {
            var dragonHungerField = typeof(HungerVisualEffects).GetField("dragonHunger", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (dragonHungerField != null)
            {
                dragonHungerField.SetValue(hungerEffects, dragonHunger);
            }
        }

        Debug.Log("✅ HungerVisualEffects coordinator added to SceneSystems");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void ApplyWallShaders()
    {
        // Find all walls and apply adaptive shader
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        int wallsModified = 0;

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer.gameObject.name.Contains("Wall") || renderer.gameObject.name.Contains("wall"))
            {
                // Create or find material for this renderer
                Material wallMaterial = new Material(Shader.Find("DungeonPrototype/AdaptiveWall"));
                if (wallMaterial.shader == null)
                {
                    Debug.LogWarning($"⚠ Shader 'DungeonPrototype/AdaptiveWall' not found. Using default.");
                    wallMaterial = renderer.material;
                }
                else
                {
                    // Copy original texture if it exists
                    if (renderer.material.HasProperty("_MainTex"))
                    {
                        wallMaterial.SetTexture("_MainTex", renderer.material.GetTexture("_MainTex"));
                    }

                    renderer.material = wallMaterial;
                    wallsModified++;
                }
            }
        }

        Debug.Log($"✅ Applied AdaptiveWall shader to {wallsModified} wall renderers");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
