using UnityEngine;
using UnityEngine.AI;
using DungeonPrototype.Core;
using DungeonPrototype.Dragon;

namespace DungeonPrototype.Guardians
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class GuardianController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private DragonCompanion dragon;
        [SerializeField] private GuardianNicheBlocker nicheBlocker;

        [Header("Awareness")]
        [SerializeField] private float hearDistanceMultiplier = 1f;
        [SerializeField] private float stirThreat = 0.25f;
        [SerializeField] private float huntThreat = 0.85f;
        [SerializeField] private float viewDistance = 12f;
        [SerializeField] private float attackDistance = 2f;

        [Header("Combat")]
        [SerializeField] private float maxHealth = 60f;
        [SerializeField] private int materialDrop = 4;
        [SerializeField] private float manaReturnOnDeath = 10f;
        [SerializeField] private float repelDistance = 5f;

        [Header("NavMesh")]
        [SerializeField] private float navMeshAttachDistance = 3f;

        private NavMeshAgent _agent;
        private Vector3 _home;
        private float _health;
        private GuardianState _state = GuardianState.Dormant;

        public GuardianState State => _state;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _home = transform.position;
            _health = maxHealth;
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
            if (_state == GuardianState.Dead || player == null)
            {
                return;
            }

            // Scene can start before NavMesh is baked/loaded; keep trying to attach silently.
            TryAttachToNavMesh();

            if (_state == GuardianState.Hunting)
            {
                SetDestinationSafe(player.position);

                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= attackDistance)
                {
                    // Hook your player damage or combat event here.
                }
            }

            HandleDragonRepel();
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

            if (!dragon.IsAtLeastStage(DragonStage.Companion))
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

            if (_agent.isOnNavMesh)
            {
                return true;
            }

            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, navMeshAttachDistance, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
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

            if (!_agent.isOnNavMesh && !TryAttachToNavMesh())
            {
                return;
            }

            _agent.SetDestination(destination);
        }
    }
}
