using DungeonPrototype.Core;
using UnityEngine;

namespace DungeonPrototype.Dragon
{
    public class DragonArtStageController : MonoBehaviour
    {
        [Header("Stage Visuals")]
        [SerializeField] private GameObject hatchedVisual;
        [SerializeField] private GameObject companionVisual;
        [SerializeField] private GameObject sacredVisual;

        [Header("Thresholds")]
        [SerializeField] private float stage2Normalized = 0.35f;
        [SerializeField] private float stage3Normalized = 0.75f;

        private DragonStage _currentStage = (DragonStage)(-1);

        private void Awake()
        {
            AutoBindVisuals();
            AlignVisualToRoot(hatchedVisual);
            AlignVisualToRoot(companionVisual);
            AlignVisualToRoot(sacredVisual);
            DisableChildColliders();
            ApplyStage(DragonStage.Hatchling);
        }

        private void OnEnable()
        {
            GameEvents.DragonManaChanged += OnDragonManaChanged;
            GameEvents.DragonStageChanged += OnDragonStageChanged;
        }

        private void OnDisable()
        {
            GameEvents.DragonManaChanged -= OnDragonManaChanged;
            GameEvents.DragonStageChanged -= OnDragonStageChanged;
        }

        private void OnDragonStageChanged(DragonStage stage)
        {
            ApplyStage(stage);
        }

        private void OnDragonManaChanged(float current, float max, float delta)
        {
            if (max <= 0f)
            {
                return;
            }

            float normalized = Mathf.Clamp01(current / max);
            DragonStage stage = DragonStage.Hatchling;

            if (normalized >= stage3Normalized)
            {
                stage = DragonStage.Sacred;
            }
            else if (normalized >= stage2Normalized)
            {
                stage = DragonStage.Companion;
            }

            ApplyStage(stage);
        }

        private void ApplyStage(DragonStage stage)
        {
            if (_currentStage == stage)
            {
                return;
            }

            _currentStage = stage;
            SetActiveSafe(hatchedVisual, stage == DragonStage.Hatchling);
            SetActiveSafe(companionVisual, stage == DragonStage.Companion);
            SetActiveSafe(sacredVisual, stage == DragonStage.Sacred);
        }

        private void AutoBindVisuals()
        {
            if (hatchedVisual == null)
            {
                Transform t = transform.Find("DragonVisual_Hatched");
                if (t != null)
                {
                    hatchedVisual = t.gameObject;
                }
            }

            if (companionVisual == null)
            {
                Transform t = transform.Find("DragonVisual_Companion");
                if (t != null)
                {
                    companionVisual = t.gameObject;
                }
            }

            if (sacredVisual == null)
            {
                Transform t = transform.Find("DragonVisual_Sacred");
                if (t != null)
                {
                    sacredVisual = t.gameObject;
                }
            }
        }

        private void DisableChildColliders()
        {
            DisableColliderOn(hatchedVisual);
            DisableColliderOn(companionVisual);
            DisableColliderOn(sacredVisual);
        }

        private static void DisableColliderOn(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            Collider c = go.GetComponent<Collider>();
            if (c != null)
            {
                c.enabled = false;
            }
        }

        private static void AlignVisualToRoot(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            Transform t = go.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }
    }
}
