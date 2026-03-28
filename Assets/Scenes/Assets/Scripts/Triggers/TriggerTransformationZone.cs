using UnityEngine;
using DungeonPrototype.Dragon;
using DungeonPrototype.Player;

namespace DungeonPrototype.Triggers
{
    public class TriggerTransformationZone : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DragonTransformationController transformationController;

        [Header("Settings")]
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private float triggerDelay = 0f;

        private bool _hasTriggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (_hasTriggered && triggerOnce) return;

            // Проверяем, что это игрок
            if (other.CompareTag("Player") || other.GetComponent<SimpleFirstPersonController>() != null)
            {
                StartTransformation();
            }
        }

        private void StartTransformation()
        {
            _hasTriggered = true;

            if (triggerDelay > 0f)
                Invoke(nameof(Trigger), triggerDelay);
            else
                Trigger();
        }

        private void Trigger()
        {
            if (transformationController != null)
                transformationController.StartTransformation();
            else
                Debug.LogError("TriggerTransformationZone: transformationController is null!");
        }

        // Для повторного использования (если triggerOnce = false)
        public void ResetTrigger()
        {
            _hasTriggered = false;
        }

        // Визуализация зоны в редакторе
        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = Color.yellow;
            if (col is BoxCollider box)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                Gizmos.DrawWireSphere(transform.position + capsule.center, capsule.radius);
            }
        }
    }
}