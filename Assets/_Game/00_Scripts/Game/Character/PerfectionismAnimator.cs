using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Slafurry.Game.Character
{
    /// <summary>
    /// Plays Perfectionism animations via Playables API — no AnimatorController needed.
    /// Control from outside through public Play methods.
    ///
    /// Transition logic:
    ///   Idle → Interact: play TransitionToIdle first, then Interact
    ///   Interact → Idle: play TransitionToStand first, then Idle
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PerfectionismAnimator : MonoBehaviour
    {
        [Header("Clips")]
        [SerializeField] private AnimationClip idleClip;
        [SerializeField] private AnimationClip angryClip;
        [SerializeField] private AnimationClip interactClip;
        [SerializeField] private AnimationClip transitionToStandClip;
        [SerializeField] private AnimationClip transitionToIdleClip;

        [Header("Crossfade")]
        [SerializeField] private float crossfadeDuration = 0.15f;

        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;

        private AnimationClipPlayable _current;
        private AnimationClipPlayable _previous;

        private float _crossfadeElapsed;
        private bool _crossfading;

        private Coroutine _transitionRoutine;

        private void Awake()
        {
            Initialize(GetComponent<Animator>());
        }

        private void Initialize(Animator animator)
        {
            _graph = PlayableGraph.Create("PerfectionismGraph");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            _mixer = AnimationMixerPlayable.Create(_graph, 2);

            var output = AnimationPlayableOutput.Create(_graph, "Output", animator);
            output.SetSourcePlayable(_mixer);

            _graph.Play();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void Tick(float deltaTime)
        {
            if (!_crossfading) return;

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

        // ─── Simple Plays (no transition) ──────────────────

        public void PlayIdle()
        {
            CancelTransition();
            PlayClip(idleClip, true);
        }

        public void PlayAngry()
        {
            CancelTransition();
            PlayClip(angryClip, true);
        }

        public void PlayInteract()
        {
            CancelTransition();
            PlayClip(interactClip, false);
        }

        // ─── Transition Plays ───────────────────────────────

        /// <summary>
        /// Idle → Interact: play TransitionToIdle, then Interact.
        /// </summary>
        public void StartInteract()
        {
            CancelTransition();
            _transitionRoutine = StartCoroutine(TransitionSequence(transitionToIdleClip, interactClip, false));
        }

        /// <summary>
        /// Interact → Idle: play TransitionToStand, then Idle.
        /// </summary>
        public void StopInteract()
        {
            CancelTransition();
            _transitionRoutine = StartCoroutine(TransitionSequence(transitionToStandClip, idleClip, true));
        }

        public void PlayByName(string clipName)
        {
            switch (clipName)
            {
                case "Idle": PlayIdle(); break;
                case "Angry": PlayAngry(); break;
                case "Interact": PlayInteract(); break;
                case "StartInteract": StartInteract(); break;
                case "StopInteract": StopInteract(); break;
                default:
                    Debug.LogWarning($"[PerfectionismAnimator] Unknown clip: {clipName}");
                    break;
            }
        }

        // ─── Internal ───────────────────────────────────────

        private IEnumerator TransitionSequence(AnimationClip transitionClip, AnimationClip nextClip, bool loop)
        {
            PlayClip(transitionClip, false);

            yield return new WaitForSeconds(transitionClip.length);

            PlayClip(nextClip, loop);

            _transitionRoutine = null;
        }

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
            if (clip == null)
            {
                Debug.LogWarning("[PerfectionismAnimator] Clip is null.");
                return;
            }

            if (_previous.IsValid())
                _previous.Destroy();

            _previous = _current;
            _current = AnimationClipPlayable.Create(_graph, clip);
            _current.SetApplyFootIK(false);

            _graph.Connect(_current, 0, _mixer, 1);
            _mixer.SetInputWeight(1, 0f);

            _crossfadeElapsed = 0f;
            _crossfading = true;
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
                _graph.Destroy();
        }
    }
}
