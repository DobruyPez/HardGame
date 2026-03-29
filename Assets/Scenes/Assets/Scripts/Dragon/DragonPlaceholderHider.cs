using UnityEngine;

namespace DungeonPrototype.Dragon
{
    public class DragonPlaceholderHider : MonoBehaviour
    {
        private void Awake()
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }
}
