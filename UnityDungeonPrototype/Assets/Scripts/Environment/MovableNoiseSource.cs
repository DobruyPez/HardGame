using UnityEngine;
using DungeonPrototype.Core;

namespace DungeonPrototype.Environment
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovableNoiseSource : MonoBehaviour
    {
        [SerializeField] private float minVelocityForNoise = 0.9f;
        [SerializeField] private float noiseRadius = 9f;
        [SerializeField] private float noiseThreat = 0.35f;
        [SerializeField] private float pulseInterval = 0.4f;

        private Rigidbody _rb;
        private float _timer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (_rb == null)
            {
                return;
            }

            if (_rb.velocity.magnitude < minVelocityForNoise)
            {
                _timer = 0f;
                return;
            }

            _timer += Time.deltaTime;
            if (_timer >= pulseInterval)
            {
                _timer = 0f;
                GameEvents.RaiseNoise(transform.position, noiseRadius, noiseThreat);
            }
        }
    }
}
