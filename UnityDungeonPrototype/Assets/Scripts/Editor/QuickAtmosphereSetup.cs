using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DungeonPrototype.Editor
{
    /// <summary>
    /// Quick setup menu for applying atmosphere effects and light managers.
    /// </summary>
    public class QuickAtmosphereSetup
    {
        [MenuItem("Tools/Dungeon Prototype/Quick: Add Light Managers to Crystals")]
        public static void AddLightManagers()
        {
            var crystals = Object.FindObjectsOfType<DungeonPrototype.Mana.ManaCrystal>();
            
            foreach (var crystal in crystals)
            {
                // Add LightManager if not present
                if (crystal.GetComponent<DungeonPrototype.Environment.LightManager>() == null)
                {
                    crystal.gameObject.AddComponent<DungeonPrototype.Environment.LightManager>();
                    EditorUtility.SetDirty(crystal.gameObject);
                    Debug.Log($"✅ Added LightManager to {crystal.gameObject.name}");
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("✅ LightManagers added to all crystals!");
        }

        [MenuItem("Tools/Dungeon Prototype/Quick: Apply Adaptive Shader to Walls")]
        public static void ApplyWallShaders()
        {
            Shader adaptiveShader = Shader.Find("DungeonPrototype/AdaptiveWall");
            if (adaptiveShader == null)
            {
                Debug.LogError("❌ Shader 'DungeonPrototype/AdaptiveWall' not found!");
                return;
            }

            var renderers = Object.FindObjectsOfType<Renderer>();
            int applied = 0;

            foreach (var renderer in renderers)
            {
                if (renderer.gameObject.name.Contains("Wall"))
                {
                    Material newMat = new Material(adaptiveShader);
                    renderer.material = newMat;
                    EditorUtility.SetDirty(renderer.gameObject);
                    applied++;
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"✅ Applied AdaptiveWall shader to {applied} walls!");
        }

        [MenuItem("Tools/Dungeon Prototype/Quick: Setup All Atmosphere")]
        public static void SetupAll()
        {
            // Setup fog
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            RenderSettings.fogDensity = 0.05f;
            RenderSettings.fogMode = FogMode.Exponential;
            Debug.Log("✅ Fog configured");

            AddLightManagers();
            ApplyWallShaders();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Atmosphere Setup Complete", 
                "✅ Fog, Light Managers, and Shaders applied!\n\nMake sure to save the scene.", "OK");
        }
    }
}
