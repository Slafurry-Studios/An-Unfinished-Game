using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace Slafurry.Game.Character
{
    public class SunkCostPatrol : MonoBehaviour
    {
        [Header("Clips")]
        [SerializeField] private AnimationClip idleClip;
        [SerializeField] private AnimationClip moveClip;
        [SerializeField] private AnimationClip transitionToMoveClip;
        [SerializeField] private AnimationClip interactClip;

        [Header("Crossfade")]
        [SerializeField] private float crossfadeDuration = 0.15f;

        [Header("Waypoints")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointReachDistance = 0.2f;
        [SerializeField] private float waitDuration = 1.5f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;

        [Header("Player Detection")]
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private Transform visualRoot;

        [Header("Events")]
        [Tooltip("Dipanggil sekali saat player pertama kali masuk interactRange.")]
        [SerializeField] private UnityEvent onPlayerReached;
        [Tooltip("Dipanggil sekali saat player keluar dari interactRange.")]
        [SerializeField] private UnityEvent onPlayerLeft;
        [Tooltip("Dipanggil setelah interactClip selesai diputar sampai habis.")]
        [SerializeField] private UnityEvent onInteractComplete;
        [Tooltip("Dipanggil sekali saat AI sampai di sebuah waypoint (sebelum menunggu).")]
        [SerializeField] private UnityEvent onArrivePatrol;

        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable _current;
        private AnimationClipPlayable _previous;
        private float _crossfadeElapsed;
        private bool _crossfading;
        private bool _currentLoop;
        private float _currentClipLength;

        private Coroutine _transitionRoutine;
        private Coroutine _interactRoutine;
        private Transform _visual;
        private bool _facingRight;
        private int _currentWaypointIndex;
        private bool _isWaiting;
        private bool _isInteracting;
        private bool _isMoving;
        private Transform _playerTransform;

        private void Awake()
        {
            _visual = visualRoot != null ? visualRoot : transform;
            _facingRight = false;
            ApplyFacing();
        }

        private void Start()
        {
            Initialize(GetComponent<Animator>());

            if (waypoints == null || waypoints.Length == 0)
            {
                Debug.LogWarning("[SunkCostPatrol] No waypoints assigned.");
                enabled = false;
                return;
            }

            _currentWaypointIndex = 0;
            _playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

            PlayIdle();
        }

        private void Initialize(Animator animator)
        {
            _graph = PlayableGraph.Create("SunkCostGraph");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);
            _mixer = AnimationMixerPlayable.Create(_graph, 2);
            var output = AnimationPlayableOutput.Create(_graph, "Output", animator);
            output.SetSourcePlayable(_mixer);
            _graph.Play();
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);

            if (_isWaiting || _isInteracting) return;

            if (IsPlayerInRange())
            {
                if (!_isInteracting)
                    StartInteract();
                return;
            }

            if (_isInteracting)
            {
                StopInteract();
                return;
            }

            Patrol();
        }

        private void Tick(float deltaTime)
        {
            if (_crossfading)
            {
                _crossfadeElapsed += deltaTime;
                float t = Mathf.Clamp01(_crossfadeElapsed / crossfadeDuration);
                _mixer.SetInputWeight(0, 1f - t);
                _mixer.SetInputWeight(1, t);

                if (t >= 1f)
                {
                    _crossfading = false;
                    if (_previous.IsValid())
                    {
                        _graph.Disconnect(_mixer, 0);
                        _previous.Destroy();
                    }
                    _graph.Disconnect(_mixer, 1);
                    _graph.Connect(_current, 0, _mixer, 0);
                    _mixer.SetInputWeight(0, 1f);
                }
            }

            if (_currentLoop && _current.IsValid() && _currentClipLength > 0f)
            {
                if (_current.GetTime() >= _currentClipLength)
                    _current.SetTime(_current.GetTime() - _currentClipLength);
            }
        }

        // ─── Patrol ──────────────────────────────────────

        private void Patrol()
        {
            Transform target = waypoints[_currentWaypointIndex];
            Vector3 direction = (target.position - transform.position).normalized;
            UpdateFacing(direction);

            float distanceToTarget = Vector2.Distance(transform.position, target.position);

            if (distanceToTarget <= waypointReachDistance)
            {
                StartCoroutine(WaitAtWaypoint());
                return;
            }

            if (!_isMoving)
            {
                _isMoving = true;
                StartMoveTransition();
            }

            transform.position = Vector2.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime
            );
        }

        private IEnumerator WaitAtWaypoint()
        {
            if (_isMoving)
            {
                _isMoving = false;
                StartStopTransition();
            }

            _isWaiting = true;
            onArrivePatrol?.Invoke();

            yield return new WaitForSeconds(waitDuration);

            _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
            _isWaiting = false;
        }

        // ─── Interact ────────────────────────────────────

        private void StartInteract()
        {
            _isInteracting = true;

            if (_isMoving)
            {
                _isMoving = false;
                CancelTransition();
                PlayIdle();
            }

            FacePlayer();
            PlayInteract();

            onPlayerReached?.Invoke();

            CancelInteractWatch();
            _interactRoutine = StartCoroutine(WatchInteractComplete());
        }

        private void StopInteract()
        {
            _isInteracting = false;
            CancelInteractWatch();
            PlayIdle();

            onPlayerLeft?.Invoke();
        }

        private IEnumerator WatchInteractComplete()
        {
            // interactClip non-loop, jadi cukup tunggu durasinya
            yield return new WaitForSecondsRealtime(interactClip != null ? interactClip.length : 0f);
            _interactRoutine = null;
            onInteractComplete?.Invoke();
        }

        private void CancelInteractWatch()
        {
            if (_interactRoutine != null)
            {
                StopCoroutine(_interactRoutine);
                _interactRoutine = null;
            }
        }

        // ─── Animation Transitions ───────────────────────

        /// <summary>
        /// Idle → TransitionToMove → Move
        /// </summary>
        private void StartMoveTransition()
        {
            CancelTransition();
            _transitionRoutine = StartCoroutine(MoveSequence());
        }

        private IEnumerator MoveSequence()
        {
            PlayClip(transitionToMoveClip, false);
            yield return new WaitForSecondsRealtime(transitionToMoveClip.length);
            PlayClip(moveClip, true);
            _transitionRoutine = null;
        }

        /// <summary>
        /// Move → TransitionToMove(reversed) → Idle
        /// </summary>
        private void StartStopTransition()
        {
            CancelTransition();
            _transitionRoutine = StartCoroutine(StopSequence());
        }

        private IEnumerator StopSequence()
        {
            PlayClipReversed(transitionToMoveClip);
            yield return new WaitForSecondsRealtime(transitionToMoveClip.length);
            PlayIdle();
            _transitionRoutine = null;
        }

        // ─── Simple Plays ────────────────────────────────

        private void PlayIdle()
        {
            CancelTransition();
            PlayClip(idleClip, true);
        }

        private void PlayInteract()
        {
            CancelTransition();
            PlayClip(interactClip, false);
        }

        // ─── Internal ────────────────────────────────────

        private void CancelTransition()
        {
            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
                _transitionRoutine = null;
            }
        }

        private void PlayClip(AnimationClip clip, bool loop)
        {
            if (clip == null) return;

            if (_previous.IsValid())
                _previous.Destroy();

            _previous = _current;

            if (_mixer.GetInputCount() > 1)
                _graph.Disconnect(_mixer, 1);

            _current = AnimationClipPlayable.Create(_graph, clip);
            _current.SetApplyFootIK(false);

            _currentLoop = loop;
            _currentClipLength = Mathf.Max(clip.length, 0.0001f);

            _graph.Connect(_current, 0, _mixer, 1);
            _mixer.SetInputWeight(1, 0f);

            _crossfadeElapsed = 0f;
            _crossfading = true;
        }

        private void PlayClipReversed(AnimationClip clip)
        {
            if (clip == null) return;

            if (_previous.IsValid())
                _previous.Destroy();

            _previous = _current;

            if (_mixer.GetInputCount() > 1)
                _graph.Disconnect(_mixer, 1);

            _current = AnimationClipPlayable.Create(_graph, clip);
            _current.SetApplyFootIK(false);
            _current.SetSpeed(-1f);
            _current.SetTime(clip.length);

            _currentLoop = false;
            _currentClipLength = Mathf.Max(clip.length, 0.0001f);

            _graph.Connect(_current, 0, _mixer, 1);
            _mixer.SetInputWeight(1, 0f);

            _crossfadeElapsed = 0f;
            _crossfading = true;
        }

        // ─── Facing ──────────────────────────────────────

        private bool IsPlayerInRange()
        {
            if (_playerTransform == null) return false;
            return Vector2.Distance(transform.position, _playerTransform.position) <= interactRange;
        }

        private void FacePlayer()
        {
            if (_playerTransform == null) return;
            float dx = _playerTransform.position.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.01f) return;
            _facingRight = dx > 0f;
            ApplyFacing();
        }

        private void UpdateFacing(Vector3 direction)
        {
            if (Mathf.Abs(direction.x) < 0.01f) return;
            bool shouldFaceRight = direction.x > 0f;
            if (shouldFaceRight != _facingRight)
            {
                _facingRight = shouldFaceRight;
                ApplyFacing();
            }
        }

        private void ApplyFacing()
        {
            Vector3 scale = _visual.localScale;
            float absX = Mathf.Abs(scale.x);
            scale.x = _facingRight ? -absX : absX;
            _visual.localScale = scale;
        }

        // ─── Gizmos ──────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (waypoints == null) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawWireSphere(waypoints[i].position, waypointReachDistance);
                int next = (i + 1) % waypoints.Length;
                if (waypoints[next] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
            }

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
                _graph.Destroy();
        }
    }
}