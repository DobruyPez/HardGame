using UnityEngine;
using DungeonPrototype.Dragon;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DungeonPrototype.Mana
{
    /// <summary>
    /// Handles player interaction with mana crystals.
    /// Players hold E-key to drain nearby crystals, transferring mana to the companion dragon.
    /// Optimized with spatial caching to avoid repeated Physics queries.
    /// </summary>
    public class ManaDrainInteractor : MonoBehaviour
    {
        [SerializeField] private DragonCompanion dragon;
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private float drainRatePerSecond = 8f;
        [SerializeField] private LayerMask crystalMask;
        [SerializeField] private Transform sourcePoint;

        private ManaCrystal _activeCrystal;
        private float _drainedThisInteraction;
        
        /// <summary>Cached list of nearby crystals. Refreshed only when needed, not every frame.</summary>
        private System.Collections.Generic.List<ManaCrystal> _nearbyNonDepletedCrystals = 
            new System.Collections.Generic.List<ManaCrystal>();
        
        /// <summary>Tracks when the crystal cache was last updated (frame number).</summary>
        private int _lastCachFrameUpdate = -999;

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
            // Find and cache nearest available crystal
            ManaCrystal nearest = FindNearestCrystal();
            if (nearest == null || dragon == null)
            {
                return;
            }

            _activeCrystal = nearest;
            _drainedThisInteraction = 0f;
            _activeCrystal.BeginDrain(1f);
        }

        /// <summary>
        /// Attempts to drain the active crystal, transferring the drained mana to the dragon.
        /// Automatically stops if crystal becomes depleted.
        /// </summary>
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

        /// <summary>
        /// Stops the current drain interaction and broadcasts the final amount drained.
        /// </summary>
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

        /// <summary>
        /// Finds the nearest non-depleted crystal within interaction range.
        /// Uses cached list to avoid repeated expensive Physics queries - only recalculates when needed.
        /// </summary>
        /// <returns>The closest ManaCrystal or null if none found.</returns>
        private ManaCrystal FindNearestCrystal()
        {
            // Only refresh cache if this is a new drain attempt (typically once per E-key press)
            // This avoids repeated Physics.OverlapSphere calls each frame during Update()
            if (Time.frameCount != _lastCachFrameUpdate)
            {
                RefreshCrystalCache();
                _lastCachFrameUpdate = Time.frameCount;
            }

            Vector3 origin = sourcePoint != null ? sourcePoint.position : transform.position;
            ManaCrystal nearest = null;
            float nearestSqr = float.MaxValue;

            // Find nearest from cached list (only Physics.OverlapSphere done once per TryStartDrain)
            for (int i = 0; i < _nearbyNonDepletedCrystals.Count; i++)
            {
                ManaCrystal crystal = _nearbyNonDepletedCrystals[i];
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

        /// <summary>
        /// Refreshes the cached list of nearby crystals by performing a Physics.OverlapSphere query.
        /// Called once per drain attempt, not every frame.
        /// </summary>
        private void RefreshCrystalCache()
        {
            _nearbyNonDepletedCrystals.Clear();
            Vector3 origin = sourcePoint != null ? sourcePoint.position : transform.position;
            Collider[] hits = Physics.OverlapSphere(origin, interactRange, crystalMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < hits.Length; i++)
            {
                ManaCrystal crystal = hits[i].GetComponentInParent<ManaCrystal>();
                if (crystal != null && !crystal.IsDepleted)
                {
                    _nearbyNonDepletedCrystals.Add(crystal);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = sourcePoint != null ? sourcePoint.position : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, interactRange);
        }
    }
}
