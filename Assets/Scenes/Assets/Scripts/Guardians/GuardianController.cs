using UnityEngine;
using UnityEngine.AI;
using DungeonPrototype.Core;
using DungeonPrototype.Dragon;
using DungeonPrototype.Player;

namespace DungeonPrototype.Guardians
{
    /// <summary>
    /// Complete enemy AI system for guardians in the dungeon.
    /// 
    /// THREAT SYSTEM & STATE MACHINE:
    /// Guardians have 7 states based on threat level and awareness:
    /// - Dormant:      Idle, unaware. Woken by noise (stirThreat) or crystal depletion.
    /// - Stirring:     Vaguely aware. Random movement, searches for threat source.
    /// - Investigating: Moving towards last known noise location.
    /// - Hunting:      Actively pursuing the player. Will attack on contact.
    /// - Repelled:     Dragon (Companion+ stage) is nearby. Guardian flees.
    /// - Trapped:      Niche exit blocked. Cannot leave spawn area.
    /// - Dead:         Defeated. Drops materials and mana reward.
    /// 
    /// THREAT LEVELS (0-1 scale):
    /// - < stirThreat (0.25):  No reaction, stay dormant or continue investigating.
    /// - stirThreat-huntThreat (0.25-0.85): Wake up and investigate noise.
    /// - >= huntThreat (0.85): Full aggro hunt mode, directly chase player.
    /// 
    /// INPUT SYSTEMS:
    /// 1. Noise Events: Guardians listen to NoiseEmitted events from crystals/gates
    /// 2. Crystal Depletion: Triggers immediate hunt mode
    /// 3. Proximity Aggro: Direct line-of-sight to player triggers hunt
    /// 4. Hitbox Relays: GuardianAggroHitbox and GuardianAttackHitbox notify when player in range
    /// 
    /// OPTIMIZATION NOTES:
    /// - NavMeshAgent is disabled until attached to valid NavMesh (avoids errors on startup)
    /// - TryAttachToNavMesh() called each frame (cheap check, expensive only if attachment needed)
    /// - CanSeePlayerByDistanceAndSight() uses raycast only if proximity check passes (early exit)
    /// - HandleProximityAggro/HandleDragonRepel called every frame but have early returns
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class GuardianController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private DragonCompanion dragon;
        [SerializeField] private GuardianNicheBlocker nicheBlocker;

        [Header("Awareness")]
        /// <summary>How far guardian can hear noise (0.5 = hears half the noise radius, 2.0 = hears twice as far).</summary>
        [SerializeField] private float hearDistanceMultiplier = 1f;
        
        /// <summary>Threat level needed to stir from dormant (0-1 scale).</summary>
        [SerializeField] private float stirThreat = 0.25f;
        
        /// <summary>Threat level needed to enter hunt mode (0-1 scale).</summary>
        [SerializeField] private float huntThreat = 0.85f;
        
        /// <summary>UNUSED: Kept for reference. Future: line-of-sight view distance.</summary>
        [SerializeField] private float viewDistance = 12f;
        
        /// <summary>Distance at which guardian directly detects player without noise (primary proximity aggro).</summary>
        [SerializeField] private float proximityAggroDistance = 7f;
        
        /// <summary>If true, guardian only aggros on proximity if they can see player (raycast check).</summary>
        [SerializeField] private bool requireLineOfSightForProximityAggro = true;
        
        /// <summary>Distance from player at which guardian attempts melee attacks.</summary>
        [SerializeField] private float attackDistance = 2f;

        [Header("Combat")]
        [SerializeField] private float maxHealth = 60f;
        [SerializeField] private int materialDrop = 4;
        [SerializeField] private float manaReturnOnDeath = 10f;
        
        /// <summary>How far guardian flees from companion dragon (Companion+ stage).</summary>
        [SerializeField] private float repelDistance = 5f;
        
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.2f;
        
        /// <summary>Radius of aggro detection sphere (tutoria detection, entry to Stirring).</summary>
        [SerializeField] private float aggroHitboxRadius = 7f;
        
        /// <summary>Radius of attack hitbox (close-range attack detection).</summary>
        [SerializeField] private float attackHitboxRadius = 2f;

        [Header("NavMesh")]
        [SerializeField] private float navMeshAttachDistance = 3f;

        private NavMeshAgent _agent;
        private PlayerHealth _playerHealth;
        private GuardianAggroHitboxRelay _aggroHitbox;
        private GuardianAttackHitboxRelay _attackHitbox;
        private Vector3 _home; // Spawn position for returning home or fleeing
        private float _health;
        private float _nextAttackTime; // Prevents attack spam via cooldown
        private GuardianState _state = GuardianState.Dormant;

        public GuardianState State => _state;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _home = transform.position;
            _health = maxHealth;

            EnsureHitboxes();

            if (player != null)
            {
                _playerHealth = player.GetComponentInParent<PlayerHealth>();
            }

            if (_agent != null)
            {
                // Keep the agent disabled until we can snap it to a valid NavMesh position.
                _agent.enabled = false;
            }

            TryAttachToNavMesh();
            SetAgentStoppedSafe(true);
        }

        private void OnEnable()
        {
            GameEvents.NoiseEmitted += OnNoiseEmitted;
            GameEvents.CrystalDepleted += OnCrystalDepleted;
        }

        private void OnDisable()
        {
            GameEvents.NoiseEmitted -= OnNoiseEmitted;
            GameEvents.CrystalDepleted -= OnCrystalDepleted;
        }

        private void Update()
        {
            if (_state == GuardianState.Dead)
            {
                return;
            }

            // Scene can start before NavMesh is baked/loaded; keep trying to attach silently.
            TryAttachToNavMesh();

            if (_state == GuardianState.Hunting)
            {
                if (player != null)
                {
                    SetDestinationSafe(player.position);
                }

                if (player != null)
                {
                    float dist = Vector3.Distance(transform.position, player.position);
                    if (dist <= attackDistance)
                    {
                        TryAttackPlayer();
                    }
                }
            }

            HandleProximityAggro();

            HandleDragonRepel();
        }

        public void NotifyPlayerInAggroRange(Transform target)
        {
            if (_state == GuardianState.Dead || target == null)
            {
                return;
            }

            if (player == null)
            {
                player = target;
            }

            if (_playerHealth == null && player != null)
            {
                _playerHealth = player.GetComponentInParent<PlayerHealth>();
            }

            if (!CanSeePlayerByDistanceAndSight())
            {
                return;
            }

            if (_state == GuardianState.Dormant || _state == GuardianState.Stirring || _state == GuardianState.Investigating)
            {
                StartHunt(target.position);
            }
        }

        public void NotifyPlayerInAttackRange(Transform target)
        {
            if (target == null)
            {
                return;
            }

            if (player == null)
            {
                player = target;
            }

            if (_state != GuardianState.Hunting)
            {
                StartHunt(target.position);
            }

            TryAttackPlayer();
        }

        public void ApplyDamage(float amount)
        {
            if (_state == GuardianState.Dead || amount <= 0f)
            {
                return;
            }

            _health -= amount;
            if (_health <= 0f)
            {
                Die();
            }
        }

        private void OnNoiseEmitted(Vector3 position, float radius, float threat)
        {
            if (_state == GuardianState.Dead)
            {
                return;
            }

            // Check if noise is close enough to hear
            float dist = Vector3.Distance(transform.position, position);
            if (dist > radius * hearDistanceMultiplier)
            {
                return;
            }

            // If sleeping, evaluate waking based on threat level
            if (_state == GuardianState.Dormant)
            {
                TryWake(threat, position);
                return;
            }

            // If already awake, adjust state based on threat intensity
            if (threat >= huntThreat)
            {
                StartHunt(position);
                return;
            }

            if (threat >= stirThreat)
            {
                StartInvestigate(position);
            }
        }

        /// <summary>
        /// Called by CrystalDepleted event. Immediately triggers hunt mode since full depletion is maximum threat.
        /// </summary>
        private void OnCrystalDepleted(Mana.ManaCrystal crystal)
        {
            if (_state == GuardianState.Dead)
            {
                return;
            }

            // Crystal depletion = highest threat = immediate hunt
            StartHunt(crystal.transform.position);
        }

        /// <summary>
        /// Attempts to wake a dormant guardian based on noise threat level.
        /// Checks for niche blocking first (if spawn exit is blocked, stays trapped).
        /// </summary>
        private void TryWake(float threat, Vector3 source)
        {
            // If exit is blocked, enter Trapped state
            if (nicheBlocker != null && nicheBlocker.IsBlocked)
            {
                _state = GuardianState.Trapped;
                SetAgentStoppedSafe(true);
                return;
            }

            // If threat is high enough, go directly to hunt
            if (threat >= huntThreat)
            {
                StartHunt(source);
                return;
            }

            // Otherwise, just stir (minimal state)
            if (threat >= stirThreat)
            {
                _state = GuardianState.Stirring;
            }
        }

        /// <summary>
        /// Transitions to Investigating state and moves towards noise source.
        /// Only transitions from Dormant/Stirring (never interrupts active hunt/repulsion).
        /// </summary>
        private void StartInvestigate(Vector3 source)
        {
            if (_state == GuardianState.Dormant || _state == GuardianState.Stirring)
            {
                _state = GuardianState.Investigating;
            }

            SetAgentStoppedSafe(false);
            SetDestinationSafe(source);
        }

        /// <summary>
        /// Transitions to Hunting state. Guardian will pursue player until defeated or repelled by dragon.
        /// </summary>
        private void StartHunt(Vector3 source)
        {
            if (_state == GuardianState.Dead)
            {
                return;
            }

            _state = GuardianState.Hunting;
            SetAgentStoppedSafe(false);

            // Hunt player if available, otherwise investigate noise source
            if (player != null)
            {
                SetDestinationSafe(player.position);
            }
            else
            {
                SetDestinationSafe(source);
            }
        }

        /// <summary>
        /// Handles dragon repulsion mechanic. Companion+ stage dragons repel nearby guardians.
        /// Returns guardian home if dragon retreats out of repulsion range.
        /// </summary>
        private void HandleDragonRepel()
        {
            if (dragon == null || _state == GuardianState.Dead)
            {
                return;
            }

            // Only Companion stage and higher can repel
            if (!dragon.IsAtLeastStage(DragonStage.Companion))
            {
                return;
            }

            float dist = Vector3.Distance(transform.position, dragon.transform.position);
            
            // If outside repel range, return from repelled state
            if (dist > repelDistance)
            {
                if (_state == GuardianState.Repelled)
                {
                    _state = GuardianState.Investigating;
                    SetAgentStoppedSafe(false);
                    SetDestinationSafe(_home);
                }
                return;
            }

            // Inside repel range = flee from dragon
            _state = GuardianState.Repelled;
            SetAgentStoppedSafe(false);
            Vector3 fleeDir = (transform.position - dragon.transform.position).normalized;
            Vector3 fleeTarget = transform.position + fleeDir * repelDistance * 1.5f;
            SetDestinationSafe(fleeTarget);
        }

        /// <summary>
        /// Proximity aggro: Detects player via distance + optional line-of-sight check.
        /// Used as fall-back when noise events don't reach guardian.
        /// </summary>
        private void HandleProximityAggro()
        {
            if (_state == GuardianState.Dead || player == null)
            {
                return;
            }

            // Don't interrupt active hunt or repulsion
            if (_state == GuardianState.Hunting || _state == GuardianState.Repelled)
            {
                return;
            }

            if (CanSeePlayerByDistanceAndSight())
            {
                StartHunt(player.position);
            }
        }

        /// <summary>
        /// Checks if guardian can detect player via distance and optional line-of-sight.
        /// Distance check first (fast), raycast only if within range (late exit optimization).
        /// </summary>
        private bool CanSeePlayerByDistanceAndSight()
        {
            if (player == null)
            {
                return false;
            }

            // Offset eye position slightly above ground
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 target = player.position + Vector3.up * 1.2f;
            Vector3 toPlayer = target - origin;
            float dist = toPlayer.magnitude;
            
            // Distance check first (exit early if too far)
            if (dist > Mathf.Max(0.1f, proximityAggroDistance))
            {
                return false;
            }

            // If LoS not required, detect player just by proximity
            if (!requireLineOfSightForProximityAggro)
            {
                return true;
            }

            // Only do expensive raycast if proximity check passed
            if (!Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                return true; // No obstruction = can see
            }

            // Check if hit transform is player or player child
            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        /// <summary>
        /// Transitions guardian to defeated state. Cleans up NavMesh, awards loot, and destr objects.
        /// </summary>
        private void Die()
        {
            _state = GuardianState.Dead;
            SetAgentStoppedSafe(true);

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }

            // Notify inventory and dragon of reward
            GameEvents.RaiseGuardianKilled(this, materialDrop, manaReturnOnDeath);

            // Destroy guardian after a brief delay (allows VFX/sounds to play)
            // TODO: Replace with dissolve/material-fracture VFX animation instead of instant destruction
            Destroy(gameObject, 0.1f);
        }

        /// <summary>
        /// Attempts to attach guardian's NavMeshAgent to the scene's NavMesh.
        /// Called repeatedly until successful (NavMesh may not be loaded immediately on scene start).
        /// </summary>
        /// <returns>True if agent is now on NavMesh, false otherwise.</returns>
        private bool TryAttachToNavMesh()
        {
            if (_agent == null)
            {
                return false;
            }

            // Try to find nearest point on NavMesh
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(transform.position, out hit, navMeshAttachDistance, NavMesh.AllAreas))
            {
                return false; // No NavMesh nearby
            }

            // Enable agent if it's disabled
            if (!_agent.enabled)
            {
                _agent.enabled = true;
            }

            // If already on NavMesh, update home position and return
            if (_agent.isOnNavMesh)
            {
                _home = transform.position;
                return true;
            }

            // Warp agent to nearest valid position
            if (_agent.Warp(hit.position))
            {
                _home = hit.position;
                return _agent.isOnNavMesh;
            }

            return false;
        }

        /// <summary>
        /// Safely stops or resumes NavMeshAgent movement with multiple fallback checks.
        /// Handles cases where NavMesh isn't loaded or agent isn't attached yet.
        /// </summary>
        private void SetAgentStoppedSafe(bool stopped)
        {
            if (_agent == null)
            {
                return;
            }

            if (!_agent.enabled && !TryAttachToNavMesh())
            {
                return;
            }

            if (!_agent.isOnNavMesh && !TryAttachToNavMesh())
            {
                return;
            }

            _agent.isStopped = stopped;
        }

        /// <summary>
        /// Safely sets guardian's NavMeshAgent destination with multiple fallback checks.
        /// Ensures agent is attached and enabled before issuing movement command.
        /// </summary>
        private void SetDestinationSafe(Vector3 destination)
        {
            if (_agent == null)
            {
                return;
            }

            if (!_agent.enabled && !TryAttachToNavMesh())
            {
                return;
            }

            if (!_agent.isOnNavMesh && !TryAttachToNavMesh())
            {
                return;
            }

            _agent.SetDestination(destination);
        }

        /// <summary>
        /// Attempts melee attack on player with cooldown prevention.
        /// Lazily fetches PlayerHealth component if not cached.
        /// </summary>
        private void TryAttackPlayer()
        {
            if (_playerHealth == null && player != null)
            {
                _playerHealth = player.GetComponentInParent<PlayerHealth>();
            }

            if (_playerHealth == null)
            {
                return;
            }

            // Check attack cooldown
            if (Time.time < _nextAttackTime)
            {
                return;
            }

            _nextAttackTime = Time.time + Mathf.Max(0.1f, attackCooldown);
            _playerHealth.TakeDamage(attackDamage);
        }

        /// <summary>
        /// Ensures guardian has required hitbox components (Rigidbody and trigger spheres).
        /// Creates AggroHitbox and AttackHitbox if they don't exist.
        /// Called once in Awake().
        /// </summary>
        private void EnsureHitboxes()
        {
            // Get or create kinematic Rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Create/ensure aggro detection sphere (larger range)
            _aggroHitbox = EnsureHitbox<GuardianAggroHitboxRelay>("AggroHitbox", Mathf.Max(attackHitboxRadius + 0.5f, aggroHitboxRadius));
            
            // Create/ensure melee attack sphere (smaller range, higher priority)
            _attackHitbox = EnsureHitbox<GuardianAttackHitboxRelay>("AttackHitbox", Mathf.Max(0.5f, attackHitboxRadius));
        }

        /// <summary>
        /// Helper to ensure a hitbox component exists on a child node.
        /// Creates the node, collider, and relay component if missing.
        /// </summary>
        /// <typeparam name="T">Type of relay component (GuardianAggroHitboxRelay, GuardianAttackHitboxRelay).</typeparam>
        /// <param name="nodeName">Name of child GameObject to find or create.</param>
        /// <param name="radius">Radius of the trigger sphere.</param>
        /// <returns>The relay component on the hitbox node.</returns>
        private T EnsureHitbox<T>(string nodeName, float radius) where T : MonoBehaviour
        {
            Transform node = transform.Find(nodeName);
            if (node == null)
            {
                GameObject go = new GameObject(nodeName);
                go.transform.SetParent(transform, false);
                node = go.transform;
            }

            node.localPosition = new Vector3(0f, 1f, 0f);

            SphereCollider sphere = node.GetComponent<SphereCollider>();
            if (sphere == null)
            {
                sphere = node.gameObject.AddComponent<SphereCollider>();
            }

            sphere.isTrigger = true;
            sphere.radius = radius;

            T relay = node.GetComponent<T>();
            if (relay == null)
            {
                relay = node.gameObject.AddComponent<T>();
            }

            return relay;
        }
    }
}
