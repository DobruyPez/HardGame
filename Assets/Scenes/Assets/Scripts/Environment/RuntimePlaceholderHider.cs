using UnityEngine;

namespace DungeonPrototype.Environment
{
    // Hides blockout meshes at runtime while keeping colliders and gameplay scripts active.
    public sealed class RuntimePlaceholderHider : MonoBehaviour
    {
        [SerializeField] private bool includeChildren;

        private void Awake()
        {
            if (includeChildren)
            {
                MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].enabled = false;
                }

                return;
            }

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }
}
