using DungeonPrototype.Core;
using DungeonPrototype.Player;
using System.Collections;
using UnityEngine;

namespace DungeonPrototype.Dragon
{
    public class DragonTransformationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DragonGrowthController growthController;
        [SerializeField] private SimpleFirstPersonController playerController;
        [SerializeField] private Camera mainCamera;

        [Header("Scale Animation")]
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1.5f);
        [SerializeField] private float scaleAnimationDuration = 1f;

        [Header("Camera Rotation")]
        [SerializeField] private float cameraOrbitSpeed = 90f;
        [SerializeField] private float cameraOrbitRadius = 3f;
        [SerializeField] private float cameraHeightOffset = 1f;

        [Header("Camera Return")]
        [SerializeField] private float cameraReturnDuration = 1f;
        [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Timing")]
        [SerializeField] private float vfxPauseDuration = 0.5f;

        [Header("VFX")]
        [SerializeField] private ParticleSystem transformationVFX;
        [SerializeField] private AudioSource transformationSound;

        private bool _isTransforming = false;
        private DragonStage _targetStage;
        private Vector3 _originalCameraPosition;
        private Quaternion _originalCameraRotation;
        private Transform _cameraPivot;
        private float _orbitAngle = 0f;
        private DragonStage? _pendingStage = null;

        private void Awake()
        {
            if (growthController == null)
                growthController = GetComponent<DragonGrowthController>();

            if (mainCamera == null && Camera.main != null)
                mainCamera = Camera.main;

            if (playerController == null)
                playerController = FindObjectOfType<SimpleFirstPersonController>();

            if (playerController != null)
            {
                _cameraPivot = mainCamera.transform.parent;
            }
        }

        private void OnEnable()
        {
            GameEvents.DragonStageChanged += OnDragonStageChanged;
        }

        private void OnDisable()
        {
            GameEvents.DragonStageChanged -= OnDragonStageChanged;
        }

        public void StartTransformation()
        {
            if (_isTransforming) return;

            // Если есть ожидаемая стадия и она отличается от текущей
            if (_pendingStage.HasValue && _pendingStage.Value != growthController.GetCurrentStage())
            {
                _targetStage = _pendingStage.Value;
                _pendingStage = null; // сбрасываем, так как начинаем трансформацию
                StartCoroutine(TransformationSequence());
            }
            else
            {
                Debug.Log("DragonTransformationController: No pending transformation or dragon already at target stage.");
            }
        }

        // Определяет следующий этап развития
        //private DragonStage GetNextStage(DragonStage current)
        //{
        //    switch (current)
        //    {
        //        case DragonStage.Hatchling: return DragonStage.Companion;
        //        case DragonStage.Companion: return DragonStage.Sacred;
        //        default: return current; // уже максимальный
        //    }
        //}

        private void OnDragonStageChanged(DragonStage newStage)
        {
            if (_isTransforming) return;
            // Сохраняем новую стадию как "ожидаемую", но трансформацию не запускаем
            _pendingStage = newStage;
        }

        private IEnumerator TransformationSequence()
        {
            _isTransforming = true;

            // 1. Фризим игрока
            if (playerController != null)
            {
                playerController.SetMovementEnabled(false);
            }

            // Сохраняем позицию камеры и поворот
            SaveCameraState();

            // 2. Начинаем анимацию масштаба текущей модели
            yield return StartCoroutine(AnimateScale());

            // 3. Вращение камеры вокруг модели
            yield return StartCoroutine(OrbitCamera());

            // 4. Выключаем все модели
            DisableAllModels();

            // 5. Пауза для VFX
            PlayTransformationVFX();
            yield return new WaitForSeconds(vfxPauseDuration);

            // 6. Возвращаем камеру на исходную позицию
            yield return StartCoroutine(ReturnCamera());

            // 7. Расфризиваем игрока
            if (playerController != null)
            {
                playerController.SetMovementEnabled(true);
            }

            // 8. Включаем новую модель
            growthController.ForceSetStage(_targetStage);

            _isTransforming = false;
        }

        private IEnumerator AnimateScale()
        {
            Transform currentModel = GetCurrentActiveModel();
            if (currentModel == null) yield break;

            Vector3 originalScale = currentModel.localScale;
            float elapsedTime = 0f;

            while (elapsedTime < scaleAnimationDuration)
            {
                float t = elapsedTime / scaleAnimationDuration;
                float curveValue = scaleCurve.Evaluate(t);

                currentModel.localScale = originalScale * curveValue;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            currentModel.localScale = originalScale;
        }

        private IEnumerator OrbitCamera()
        {
            if (mainCamera == null) yield break;

            Transform modelTransform = GetCurrentActiveModel();
            if (modelTransform == null) yield break;

            if (_cameraPivot != null)
            {
                _cameraPivot.SetParent(null);
            }

            float orbitDuration = 360f / cameraOrbitSpeed;
            float elapsedTime = 0f;

            while (elapsedTime < orbitDuration)
            {
                _orbitAngle += cameraOrbitSpeed * Time.deltaTime;

                Vector3 orbitPosition = modelTransform.position +
                                        new Vector3(Mathf.Cos(_orbitAngle * Mathf.Deg2Rad), 0, Mathf.Sin(_orbitAngle * Mathf.Deg2Rad)) * cameraOrbitRadius;
                orbitPosition.y = modelTransform.position.y + cameraHeightOffset;

                mainCamera.transform.position = orbitPosition;
                mainCamera.transform.LookAt(modelTransform.position + Vector3.up * cameraHeightOffset);

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator ReturnCamera()
        {
            if (mainCamera == null) yield break;

            Vector3 startPosition = mainCamera.transform.position;
            Quaternion startRotation = mainCamera.transform.rotation;

            float elapsedTime = 0f;

            while (elapsedTime < cameraReturnDuration)
            {
                float t = elapsedTime / cameraReturnDuration;
                float curveValue = returnCurve.Evaluate(t);

                mainCamera.transform.position = Vector3.Lerp(startPosition, _originalCameraPosition, curveValue);
                mainCamera.transform.rotation = Quaternion.Lerp(startRotation, _originalCameraRotation, curveValue);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (_cameraPivot != null)
            {
                _cameraPivot.SetParent(playerController.transform);
                _cameraPivot.localPosition = Vector3.zero;
            }
        }

        private void SaveCameraState()
        {
            if (mainCamera != null)
            {
                _originalCameraPosition = mainCamera.transform.position;
                _originalCameraRotation = mainCamera.transform.rotation;
            }
        }

        private Transform GetCurrentActiveModel()
        {
            if (growthController == null) return null;

            foreach (var stageData in growthController.stageModels)
            {
                if (stageData.instantiatedModel != null && stageData.instantiatedModel.activeSelf)
                {
                    return stageData.instantiatedModel.transform;
                }
            }

            return null;
        }

        private void DisableAllModels()
        {
            foreach (var stageData in growthController.stageModels)
            {
                if (stageData.instantiatedModel != null)
                {
                    stageData.instantiatedModel.SetActive(false);
                }
            }
        }

        private void PlayTransformationVFX()
        {
            if (transformationVFX != null)
            {
                transformationVFX.Play();
            }

            if (transformationSound != null)
            {
                transformationSound.Play();
            }
        }
    }
}