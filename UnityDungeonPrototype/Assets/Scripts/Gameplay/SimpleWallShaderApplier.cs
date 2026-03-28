using UnityEngine;

namespace DungeonPrototype
{
    /// <summary>
    /// Simple runtime wall shader application that works with basic components only.
    /// Applied on scene load to give walls adaptive appearance based on lighting.
    /// </summary>
    public class SimpleWallShaderApplier : MonoBehaviour
    {
        [SerializeField] private bool applyOnStart = true;

        private void Start()
        {
            if (applyOnStart)
            {
                ApplyWallShaders();
                SetupGlobalFog();
            }
        }

        private void ApplyWallShaders()
        {
            Shader adaptiveShader = Shader.Find("DungeonPrototype/AdaptiveWall");
            if (adaptiveShader == null)
            {
                Debug.LogWarning("⚠ Shader 'DungeonPrototype/AdaptiveWall' not found. Using default material for walls.");
                return;
            }

            Renderer[] allRenderers = FindObjectsOfType<Renderer>();
            int count = 0;

            foreach (Renderer renderer in allRenderers)
            {
                // Apply to any object with "Wall" in name
                if (renderer.gameObject.name.Contains("Wall"))
                {
                    Material adaptiveMaterial = new Material(adaptiveShader);
                    
                    // Preserve original texture if available
                    Material originalMaterial = renderer.material;
                    if (originalMaterial.HasProperty("_MainTex"))
                    {
                        Texture mainTex = originalMaterial.GetTexture("_MainTex");
                        if (mainTex != null)
                        {
                            adaptiveMaterial.SetTexture("_MainTex", mainTex);
                        }
                    }

                    renderer.material = adaptiveMaterial;
                    count++;
                }
            }

            Debug.Log($"✅ Applied AdaptiveWall shader to {count} wall renderers");
        }

        private void SetupGlobalFog()
        {
            // Dark blue, misty fog
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            RenderSettings.fogDensity = 0.05f;
            RenderSettings.fogMode = FogMode.Exponential;
            
            Debug.Log("✅ Global fog configured: dark blue, density 0.05");
        }
    }
}
