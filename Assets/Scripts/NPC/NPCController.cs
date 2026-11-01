using System;
using UnityEngine;
using UnityEngine.AI;
using Hearthwood.Core;
using Hearthwood.Dialogue;
using Hearthwood.Player;

namespace Hearthwood.NPC
{
    /// <summary>
    /// Runtime controller for an NPC. Bound to an <see cref="NPCData"/> asset, it exposes the
    /// NPC as an <see cref="IInteractable"/> (player presses E to talk) and an
    /// <see cref="IDialogueSpeaker"/> (drives dialogue), and runs a simple wander AI on a
    /// <see cref="NavMeshAgent"/>. Attach to the NPC root GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NPCController : MonoBehaviour, IInteractable, IDialogueSpeaker
    {
        [Header("Data")]
        [SerializeField, Tooltip("The NPC definition asset. Drives name, dialogue, faction and wander.")]
        private NPCData _data;

        [Header("Interaction")]
        [SerializeField, Min(0.1f), Tooltip("Distance within which the player can press E to talk / the focus prompt shows.")]
        private float _interactRange = 3f;

        [Header("Wander")]
        [SerializeField, Min(0f), Tooltip("Minimum seconds between wander point picks.")]
        private float _wanderMinInterval = 5f;

        [SerializeField, Min(0f), Tooltip("Maximum seconds between wander point picks.")]
        private float _wanderMaxInterval = 15f;

        [SerializeField, Range(0.05f, 1f), Tooltip("Distance at which the NavMeshAgent is considered to have reached its destination.")]
        private float _reachDistance = 0.3f;

        /// <summary>
        /// Raised (through the callback) when the player's focus enters/exits this NPC.
        /// Required by <see cref="IInteractable"/> as a get/set <c>Action&lt;bool&gt;</c>.
        /// </summary>
        public Action<bool> OnFocusChanged { get; set; }

        /// <summary>Prompt shown above the NPC while the player is in range.</summary>
        public string InteractPrompt => "Press E to talk to " + (_data != null ? _data.DisplayName : "the villager");

        /// <summary>True if <paramref name="ctx"/> may talk to this NPC right now.</summary>
        /// <param name="ctx">Snapshot of the interacting player.</param>
        public bool CanInteract(PlayerContext ctx)
        {
            if (_data == null) return false;
            if (!ctx.IsAlive) return false;
            var runner = DialogueRunner.Instance;
            if (runner != null && runner.IsRunning) return false;
            if (ctx.Transform != null)
            {
                float dist = Vector3.Distance(transform.position, ctx.Transform.position);
                if (dist > _interactRange) return false;
            }
            return true;
        }

        /// <summary>Begin dialogue with this NPC (the dialogue runner pauses the game).</summary>
        /// <param name="ctx">Snapshot of the interacting player.</param>
        public void Interact(PlayerContext ctx)
        {
            if (!CanInteract(ctx)) return;
            _state = AIState.Talking;
            if (_agent != null) _agent.isStopped = true;
            DialogueRunner.Instance?.StartDialogue(this, _data.GetDialogueForContext(ctx), ctx);
        }

        /// <summary>Stable id (matches <c>NPCData.Id</c>).</summary>
        public string SpeakerId => _data != null ? _data.Id : string.Empty;

        /// <summary>Localized display name.</summary>
        public string SpeakerName => _data != null ? _data.DisplayName : "Villager";

        /// <summary>Portrait sprite for the dialogue panel (null if unset).</summary>
        public Sprite Portrait => _data != null ? _data.Portrait : null;

        /// <summary>The default dialogue asset from the bound data.</summary>
        public DialogueData GetDefaultDialogue() => _data != null ? _data.DefaultDialogue : null;

        /// <summary>Context-sensitive dialogue (falls back to the default).</summary>
        /// <param name="ctx">Snapshot of the interacting player.</param>
        public DialogueData GetDialogueForContext(PlayerContext ctx)
            => _data != null ? _data.GetDialogueForContext(ctx) : null;

        /// <summary>
        /// Bind a data asset at runtime. Safe to call before <c>Start</c> (the <c>AddComponent</c>
        /// path used by <see cref="NPCSpawner"/>) or after (re-initializes the AI state).
        /// </summary>
        /// <param name="data">The NPC definition to bind.</param>
        public void Bind(NPCData data)
        {
            _data = data;
            if (_initialized) Initialize();
        }

        private enum AIState { Idle, Wander, Talking }

        private NavMeshAgent _agent;
        private AIState _state = AIState.Idle;
        private Vector3 _spawnPosition;
        private float _wanderTimer;
        private bool _focused;
        private bool _initialized;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_agent != null) _agent.stoppingDistance = _reachDistance;
        }

        private void Start()
        {
            Initialize();
            var runner = DialogueRunner.Instance;
            if (runner != null) runner.OnDialogueEnded += HandleDialogueEnded;
        }

        private void OnDestroy()
        {
            var runner = DialogueRunner.Instance;
            if (runner != null) runner.OnDialogueEnded -= HandleDialogueEnded;
        }

        private void Update()
        {
            UpdateFocus();
            UpdateAI();
        }

        private void Initialize()
        {
            if (_data == null) { _state = AIState.Idle; _initialized = true; return; }
            _spawnPosition = transform.position;
            _state = _data.WanderRadius > 0f ? AIState.Wander : AIState.Idle;
            if (_state == AIState.Wander)
            {
                _wanderTimer = UnityEngine.Random.Range(0f, 2f);
                PickWanderTarget();
            }
            _initialized = true;
        }

        private void UpdateFocus()
        {
            bool shouldFocus = false;
            var player = PlayerController3D.Instance;
            var gm = GameManager.Instance;
            bool exploring = gm == null || gm.State == GameState.Exploring;
            if (exploring && player != null && player.Transform != null)
            {
                float dist = Vector3.Distance(transform.position, player.Transform.position);
                shouldFocus = dist <= _interactRange;
            }
            if (shouldFocus != _focused)
            {
                _focused = shouldFocus;
                OnFocusChanged?.Invoke(_focused);
            }
        }

        private void UpdateAI()
        {
            if (_agent == null || !_agent.isOnNavMesh) return;
            if (_state != AIState.Wander)
            {
                _agent.isStopped = true;
                return;
            }
            _agent.isStopped = false;
            _wanderTimer -= Time.deltaTime;
            if (_wanderTimer <= 0f)
            {
                PickWanderTarget();
                _wanderTimer = UnityEngine.Random.Range(_wanderMinInterval, _wanderMaxInterval);
            }
        }

        private void PickWanderTarget()
        {
            if (_data == null || _agent == null || _data.WanderRadius <= 0f) return;
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float radius = UnityEngine.Random.Range(0f, _data.WanderRadius);
            Vector3 candidate = _spawnPosition + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, Mathf.Max(0.5f, _data.WanderRadius), NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
        }

        private void HandleDialogueEnded()
        {
            if (_state != AIState.Talking) return;
            _state = _data != null && _data.WanderRadius > 0f ? AIState.Wander : AIState.Idle;
        }
    }
}
