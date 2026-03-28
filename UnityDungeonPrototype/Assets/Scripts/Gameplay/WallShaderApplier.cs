using UnityEngine;

namespace DungeonPrototype.Gameplay
{
    /// <summary>
    /// Runtime wall shader application - applies adaptive shader to all walls on scene start.
    /// </summary>
    [ExecuteAlways]
    public class WallShaderApplier : MonoBehaviour
    {
        private void OnEnable()
        {
            ApplyShaders();
        }

        private void ApplyShaders()
        {
            Shader adaptiveShader = Shader.Find("DungeonPrototype/AdaptiveWall");
            if (adaptiveShader == null)
            {
                Debug.LogWarning("⚠ AdaptiveWall shader not found. Walls will appear with default material.");
                return;
            }

            Renderer[] allRenderers = FindObjectsOfType<Renderer>();
            foreach (Renderer renderer in allRenderers)
            {
                if (renderer.gameObject.name.Contains("Wall"))
                {
                    Material newMat = new Material(adaptiveShader);
                    
                    // Preserve original texture if available
                    if (renderer.material.HasProperty("_MainTex"))
                    {
                        Texture originalTex = renderer.material.GetTexture("_MainTex");
                        if (originalTex != null)
                        {
                            newMat.SetTexture("_MainTex", originalTex);
                        }
                    }

                    renderer.material = newMat;
                }
            }

            Debug.Log("✅ Applied AdaptiveWall shader to walls");
        }
    }
}
