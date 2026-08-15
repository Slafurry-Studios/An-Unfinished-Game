using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Slafurry.Player.Animation
{
    /// <summary>
    /// Plays a single AnimationClip at a time with crossfade, driven entirely
    /// through the Playables API. This is the whole "animator state machine"
    /// replacement — there's no AnimatorController asset anywhere; the
    /// Animator component on the player only exists as the graph's output
    /// target (required by AnimationPlayableOutput).
    /// </summary>
    public class ClipPlayer
    {
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;

        private AnimationClipPlayable _current;
        private AnimationClipPlayable _previous;

        private float _crossfadeDuration;
        private float _crossfadeElapsed;
        private bool _crossfading;

        private bool _currentLoops;
        private float _currentClipLength;

        /// <summary>True once a non-looping clip has played all the way through.</summary>
        public bool IsCurrentClipFinished =>
            !_currentLoops && _current.IsValid() && _current.GetTime() >= _currentClipLength - 0.01f;

        public void Initialize(Animator animator, string graphName = "PlayerAnimationGraph")
        {
            _graph = PlayableGraph.Create(graphName);
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            _mixer = AnimationMixerPlayable.Create(_graph, 2);

            var output = AnimationPlayableOutput.Create(_graph, "Output", animator);
            output.SetSourcePlayable(_mixer);

            _graph.Play();
        }

        /// <summary>Starts crossfading to a new clip. Call once, from a state's Enter().</summary>
        public void Play(AnimationClip clip, bool loop, float crossFadeDuration)
        {
            if (!_graph.IsValid())
            {
                Debug.LogWarning("[ClipPlayer] Play() called before Initialize().");
                return;
            }

            if (clip == null)
            {
                Debug.LogWarning("[ClipPlayer] Tried to play a null AnimationClip — check the clip fields on PlayerAnimationStateMachine.");
                return;
            }

            // Whatever was fading in becomes the new "previous" so it keeps
            // fading out smoothly instead of popping straight to the new clip.
            if (_previous.IsValid())
                _previous.Destroy();

            _previous = _current;
            _current = AnimationClipPlayable.Create(_graph, clip);
            _current.SetApplyFootIK(false);

            _currentLoops = loop;
            _currentClipLength = Mathf.Max(clip.length, 0.0001f);

            _graph.Connect(_current, 0, _mixer, 1);
            _mixer.SetInputWeight(1, 0f);

            _crossfadeDuration = Mathf.Max(crossFadeDuration, 0.0001f);
            _crossfadeElapsed = 0f;
            _crossfading = true;
        }

        /// <summary>Advances the crossfade. Call once per frame from the state machine's Update.</summary>
        public void Tick(float deltaTime)
        {
            if (!_crossfading) return;

            _crossfadeElapsed += deltaTime;
            float t = Mathf.Clamp01(_crossfadeElapsed / _crossfadeDuration);

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

                // Move the now fully-weighted clip into slot 0 so slot 1 is
                // free again for the next Play() call.
                _graph.Disconnect(_mixer, 1);
                _graph.Connect(_current, 0, _mixer, 0);
                _mixer.SetInputWeight(0, 1f);
            }
        }

        public void Destroy()
        {
            if (_graph.IsValid())
                _graph.Destroy();
        }
    }
}
