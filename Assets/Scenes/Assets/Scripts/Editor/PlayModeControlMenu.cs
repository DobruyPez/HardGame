#if UNITY_EDITOR
using UnityEditor;

namespace DungeonPrototype.EditorTools
{
    public static class PlayModeControlMenu
    {
        [MenuItem("Tools/PlayMode/Stop Play Mode")]
        public static void StopPlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }
    }
}
#endif
