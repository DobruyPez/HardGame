using UnityEngine;
using DungeonPrototype.Core;

namespace DungeonPrototype.Dragon
{
    public class DragonManaThresholdMover : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private GameObject objectToMove;
        [SerializeField] private Transform targetPoint;
        [SerializeField] private Transform finalTargetPoint;

        [Header("Mana Threshold")]
        [SerializeField] private float manaThreshold = 50f;
        [SerializeField] private bool moveWhenManaAboveThreshold = true;

        [Header("Movement Settings")]
        [SerializeField] private bool instantMove = true; // Мгновенное перемещение
        [SerializeField] private float moveSpeed = 5f; // Скорость для плавного движения

        [Header("Rotation Settings")]
        [SerializeField] private GameObject objectToRotate;
        [SerializeField] private Vector3 rotationSpeed = Vector3.zero;
        [SerializeField] private bool rotateDuringMovement = true;
        [SerializeField] private bool stopRotationAfterMove = true;

        private bool _hasMoved = false;
        private bool _isRotating = false;
        private bool _isMovingToFinal = false;

        private void OnEnable()
        {
            GameEvents.DragonManaChanged += OnDragonManaChanged;
        }

        private void OnDisable()
        {
            GameEvents.DragonManaChanged -= OnDragonManaChanged;
        }

        private void OnDragonManaChanged(float currentMana, float maxMana, float delta)
        {
            if (_hasMoved) return;

            bool shouldMove = moveWhenManaAboveThreshold
                ? currentMana >= manaThreshold
                : currentMana <= manaThreshold;

            if (shouldMove && objectToMove != null && targetPoint != null)
            {
                MoveObject();
                _hasMoved = true;
            }
        }

        private void MoveObject()
        {
            if (instantMove)
            {
                objectToMove.transform.position = targetPoint.position;
                StartRotation();

                if (finalTargetPoint != null)
                {
                    StartCoroutine(MoveToFinalPoint());
                }
            }
            else
            {
                // Можно запустить корутину для плавного движения
                StartCoroutine(SmoothMove());
            }
        }

        private System.Collections.IEnumerator SmoothMove()
        {
            StartRotation(); // Добавить эту строку

            Vector3 startPos = objectToMove.transform.position;
            float progress = 0f;

            while (progress < 1f)
            {
                progress += Time.deltaTime * moveSpeed;
                objectToMove.transform.position = Vector3.Lerp(startPos, targetPoint.position, progress);
                yield return null;
            }

            objectToMove.transform.position = targetPoint.position;

            if (stopRotationAfterMove)
            {
                StopRotation();
            }
            if (finalTargetPoint != null)
            {
                yield return StartCoroutine(MoveToFinalPoint());
            }
        }

        private System.Collections.IEnumerator MoveToFinalPoint()
        {
            _isMovingToFinal = true;

            Vector3 startPos = objectToMove.transform.position;
            float progress = 0f;

            while (progress < 1f)
            {
                progress += Time.deltaTime * moveSpeed;
                objectToMove.transform.position = Vector3.Lerp(startPos, finalTargetPoint.position, progress);
                yield return null;
            }

            objectToMove.transform.position = finalTargetPoint.position;
            _isMovingToFinal = false;

            if (stopRotationAfterMove)
            {
                StopRotation();
            }

            Debug.Log($"Object reached final target point: {finalTargetPoint.name}");
        }

        private void StartRotation()
        {
            if (objectToRotate != null && rotateDuringMovement)
            {
                _isRotating = true;
            }
        }

        private void StopRotation()
        {
            _isRotating = false;
        }

        private void Update()
        {
            if (_isRotating && objectToRotate != null)
            {
                objectToRotate.transform.Rotate(rotationSpeed * Time.deltaTime);
            }
        }
    }
}