using UnityEngine;
using DungeonPrototype.Dragon;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DungeonPrototype.Mana
{
    public class ManaDrainInteractor : MonoBehaviour
    {
        [SerializeField] private DragonCompanion dragon;
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private float drainRatePerSecond = 8f;
        [SerializeField] private LayerMask crystalMask;
        [SerializeField] private Transform sourcePoint;

        private ManaCrystal _activeCrystal;
        private float _drainedThisInteraction;

        private void Update()
        {
            if (IsDrainStarted())
            {
                TryStartDrain();
            }

            if (IsDrainHeld() && _activeCrystal != null)
            {
                ContinueDrain();
            }

            if (IsDrainEnded())
            {
                StopDrain();
            }
        }

        private bool IsDrainStarted()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        private bool IsDrainHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.isPressed;
#else
            return Input.GetKey(KeyCode.E);
#endif
        }

        private bool IsDrainEnded()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasReleasedThisFrame;
#else
            return Input.GetKeyUp(KeyCode.E);
#endif
        }

        private void TryStartDrain()
        {
            ManaCrystal nearest = FindNearestCrystal();
            if (nearest == null || dragon == null)
            {
                return;
            }

            _activeCrystal = nearest;
            _drainedThisInteraction = 0f;
            _activeCrystal.BeginDrain(1f);
        }

        private void ContinueDrain()
        {
            if (_activeCrystal == null)
            {
                return;
            }

            float amount = drainRatePerSecond * Time.deltaTime;
            float drained = _activeCrystal.Drain(amount);
            if (drained <= 0f)
            {
                return;
            }

            _drainedThisInteraction += drained;
            dragon.AddMana(drained);

            if (_activeCrystal.IsDepleted)
            {
                _activeCrystal = null;
            }
        }

        private void StopDrain()
        {
            if (_activeCrystal == null)
            {
                return;
            }

            _activeCrystal.EndDrain(_drainedThisInteraction);
            _activeCrystal = null;
            _drainedThisInteraction = 0f;
        }

        private ManaCrystal FindNearestCrystal()
        {
            Vector3 origin = sourcePoint != null ? sourcePoint.position : transform.position;
            Collider[] hits = Physics.OverlapSphere(origin, interactRange, crystalMask, QueryTriggerInteraction.Collide);

            ManaCrystal nearest = null;
            float nearestSqr = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                ManaCrystal crystal = hits[i].GetComponentInParent<ManaCrystal>();
                if (crystal == null || crystal.IsDepleted)
                {
                    continue;
                }

                float sqr = (crystal.transform.position - origin).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = crystal;
                }
            }

            return nearest;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = sourcePoint != null ? sourcePoint.position : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, interactRange);
        }
    }
}
