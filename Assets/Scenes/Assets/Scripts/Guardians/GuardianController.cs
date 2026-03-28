using UnityEngine;
using UnityEngine.AI;
using DungeonPrototype.Core;
using DungeonPrototype.Dragon;
using DungeonPrototype.Player;

namespace DungeonPrototype.Guardians
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class GuardianController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private DragonGrowthController dragon;
        [SerializeField] private GuardianNicheBlocker nicheBlocker;

        [Header("Awareness")]
        [SerializeField] private float hearDistanceMultiplier = 1f;
        [SerializeField] private float stirThreat = 0.25f;
        [SerializeField] private float huntThreat = 0.85f;
        [SerializeField] private float viewDistance = 12f;
        [SerializeField] private float proximityAggroDistance = 7f;
        [SerializeField] private bool requireLineOfSightForProximityAggro = true;
        [SerializeField] private float attackDistance = 2f;

        [Header("Combat")]
        [SerializeField] private float maxHealth = 60f;
        [SerializeField] private int materialDrop = 4;
        [SerializeField] private float manaReturnOnDeath = 10f;
        [SerializeField] private float repelDistance = 5f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float aggroHitboxRadius = 7f;
        [SerializeField] private float attackHitboxRadius = 2f;

        [Header("NavMesh")]
        [SerializeField] private float navMeshAttachDistance = 3f;

        private NavMeshAgent _agent;
        private PlayerHealth _playerHealth;
        private GuardianAggroHitboxRelay _aggroHitbox;
        private GuardianAttackHitboxRelay _attackHitbox;
        private Vector3 _home;
        private float _health;
        private float _nextAttackTime;
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

            float dist = Vector3.Distance(transform.position, position);
            if (dist > radius * hearDistanceMultiplier)
            {
                return;
            }

            if (_state == GuardianState.Dormant)
            {
                TryWake(threat, position);
                return;
            }

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

        private void OnCrystalDepleted(Mana.ManaCrystal crystal)
        {
            if (_state == GuardianState.Dead)
            {
                return;
            }

            StartHunt(crystal.transform.position);
        }

        private void TryWake(float threat, Vector3 source)
        {
            if (nicheBlocker != null && nicheBlocker.IsBlocked)
            {
                _state = GuardianState.Trapped;
                SetAgentStoppedSafe(true);
                return;
            }

            if (threat >= huntThreat)
            {
                StartHunt(source);
                return;
            }

            if (threat >= stirThreat)
            {
                _state = GuardianState.Stirring;
            }
        }

        private void StartInvestigate(Vector3 source)
        {
            if (_state == GuardianState.Dormant || _state == GuardianState.Stirring)
            {
                _state = GuardianState.Investigating;
            }

            SetAgentStoppedSafe(false);
            SetDestinationSafe(source);
        }

        private void StartHunt(Vector3 source)
        {
            if (_state == GuardianState.Dead)
            {
                return;
            }

            _state = GuardianState.Hunting;
            SetAgentStoppedSafe(false);

            if (player != null)
            {
                SetDestinationSafe(player.position);
            }
            else
            {
                SetDestinationSafe(source);
            }
        }

        private void HandleDragonRepel()
        {
            if (dragon == null || _state == GuardianState.Dead)
            {
                return;
            }

            if (dragon.CurrentStage != DragonStage.Companion)
            {
                return;
            }

            float dist = Vector3.Distance(transform.position, dragon.transform.position);
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

            _state = GuardianState.Repelled;
            SetAgentStoppedSafe(false);
            Vector3 fleeDir = (transform.position - dragon.transform.position).normalized;
            Vector3 fleeTarget = transform.position + fleeDir * repelDistance * 1.5f;
            SetDestinationSafe(fleeTarget);
        }

        private void HandleProximityAggro()
        {
            if (_state == GuardianState.Dead || player == null)
            {
                return;
            }

            if (_state == GuardianState.Hunting || _state == GuardianState.Repelled)
            {
                return;
            }

            if (CanSeePlayerByDistanceAndSight())
            {
                StartHunt(player.position);
            }
        }

        private bool CanSeePlayerByDistanceAndSight()
        {
            if (player == null)
            {
                return false;
            }

            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 target = player.position + Vector3.up * 1.2f;
            Vector3 toPlayer = target - origin;
            float dist = toPlayer.magnitude;
            if (dist > Mathf.Max(0.1f, proximityAggroDistance))
            {
                return false;
            }

            if (!requireLineOfSightForProximityAggro)
            {
                return true;
            }

            if (!Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        private void Die()
        {
            _state = GuardianState.Dead;
            SetAgentStoppedSafe(true);

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }

            GameEvents.RaiseGuardianKilled(this, materialDrop, manaReturnOnDeath);

            // Replace with dissolve/material-fracture VFX if needed.
            Destroy(gameObject, 0.1f);
        }

        private bool TryAttachToNavMesh()
        {
            if (_agent == null)
            {
                return false;
            }

            NavMeshHit hit;
            if (!NavMesh.SamplePosition(transform.position, out hit, navMeshAttachDistance, NavMesh.AllAreas))
            {
                return false;
            }

            if (!_agent.enabled)
            {
                _agent.enabled = true;
            }

            if (_agent.isOnNavMesh)
            {
                _home = transform.position;
                return true;
            }

            if (_agent.Warp(hit.position))
            {
                _home = hit.position;
                return _agent.isOnNavMesh;
            }

            return false;
        }

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

            if (Time.time < _nextAttackTime)
            {
                return;
            }

            _nextAttackTime = Time.time + Mathf.Max(0.1f, attackCooldown);
            _playerHealth.TakeDamage(attackDamage);
        }

        private void EnsureHitboxes()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            _aggroHitbox = EnsureHitbox<GuardianAggroHitboxRelay>("AggroHitbox", Mathf.Max(attackHitboxRadius + 0.5f, aggroHitboxRadius));
            _attackHitbox = EnsureHitbox<GuardianAttackHitboxRelay>("AttackHitbox", Mathf.Max(0.5f, attackHitboxRadius));
        }

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
