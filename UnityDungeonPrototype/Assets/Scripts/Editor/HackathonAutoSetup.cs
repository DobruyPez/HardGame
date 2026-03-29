using DungeonPrototype;
using DungeonPrototype.Dragon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HackathonAutoSetup
{
    [MenuItem("Tools/Hackathon/Apply Intro + Dragon Setup")]
    public static void ApplySetup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += ApplySetup;
            return;
        }

        SetupDragon();
        SetupIntro();

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Hackathon setup applied and scene saved.");
    }

    private static void SetupDragon()
    {
        GameObject dragon = GameObject.Find("Dragon");
        if (dragon == null)
        {
            Debug.LogWarning("Dragon object not found.");
            return;
        }

        EnsureComponent<DragonArtStageController>(dragon);
        EnsureComponent<DragonPlaceholderHider>(dragon);

        MeshRenderer renderer = dragon.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        EnsureDragonVisual(dragon.transform, "DragonVisual_Hatched", "Assets/Prefabs/Hatched.prefab", true);
        EnsureDragonVisual(dragon.transform, "DragonVisual_Companion", "Assets/Prefabs/Companion.prefab", false);
        EnsureDragonVisual(dragon.transform, "DragonVisual_Sacred", "Assets/Prefabs/Sacred.prefab", false);
    }

    private static void SetupIntro()
    {
        GameObject sceneSystems = GameObject.Find("SceneSystems");
        if (sceneSystems == null)
        {
            GameObject levelRoot = GameObject.Find("LevelRoot");
            sceneSystems = new GameObject("SceneSystems");
            if (levelRoot != null)
            {
                sceneSystems.transform.SetParent(levelRoot.transform);
            }
        }

        Transform introT = sceneSystems.transform.Find("IntroManager");
        GameObject introManager = introT != null ? introT.gameObject : new GameObject("IntroManager");
        if (introT == null)
        {
            introManager.transform.SetParent(sceneSystems.transform);
            introManager.transform.localPosition = Vector3.zero;
            introManager.transform.localRotation = Quaternion.identity;
            introManager.transform.localScale = Vector3.one;
        }

        GameIntroSequence intro = EnsureComponent<GameIntroSequence>(introManager);

        SerializedObject so = new SerializedObject(intro);
        SetFloat(so, "blinkDuration", 3.4f);
        SetFloat(so, "introDelay", 1.3f);
        SetFloat(so, "preWakePulseDuration", 2.4f);
        SetFloat(so, "preWakePulseStrength", 0.09f);
        SetFloat(so, "handsFallbackRiseDuration", 2.2f);
        SetFloat(so, "dragonMessageDuration", 4.2f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureDragonVisual(Transform dragonRoot, string visualName, string prefabPath, bool active)
    {
        Transform existing = dragonRoot.Find(visualName);
        GameObject visual = existing != null ? existing.gameObject : null;

        if (visual == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("Missing prefab: " + prefabPath);
                return;
            }

            visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            visual.name = visualName;
            visual.transform.SetParent(dragonRoot, false);
        }

        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        visual.SetActive(active);

        Collider c = visual.GetComponent<Collider>();
        if (c != null)
        {
            c.enabled = false;
        }
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null)
        {
            c = go.AddComponent<T>();
        }
        return c;
    }

    private static void SetFloat(SerializedObject so, string propertyName, float value)
    {
        SerializedProperty p = so.FindProperty(propertyName);
        if (p != null)
        {
            p.floatValue = value;
        }
    }
}
