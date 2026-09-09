using System.Collections;
using UnityEngine;

namespace Slafurry.Game.Character
{
    public class BurnoutController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualRoot; // opsional: kalau sprite/model ada di child, flip di sini. Kalau null, pakai transform ini sendiri.

        [Header("Detection")]
        [SerializeField] private float cautiousRange = 5f;
        [SerializeField] private float aggroRange = 2.5f;

        [Header("Timing")]
        [SerializeField] private float cautiousDuration = 1.5f;

        private static readonly int IsIdle = Animator.StringToHash("IsIdle");
        private static readonly int IsCautious = Animator.StringToHash("IsCautious");
        private static readonly int AggroTrigger = Animator.StringToHash("Aggro");
        private static readonly int SuckTrigger = Animator.StringToHash("Suck");

        private BurnoutState _currentState = BurnoutState.Idle;
        private Coroutine _cautiousRoutine;
        private bool _hasTriggeredCautious;

        private Transform _visual;
        private bool _facingRight; // default hadap kiri -> false

        private enum BurnoutState
        {
            Idle,
            Cautious,
            Aggro,
            Suck
        }

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            _visual = visualRoot != null ? visualRoot : transform;
            _facingRight = false; // default hadap kiri
            ApplyFacing();
        }

        private void Start()
        {
            SetState(BurnoutState.Idle);
        }

        private void Update()
        {
            if (_currentState == BurnoutState.Suck) return;

            bool inAggro = IsPlayerInRange(aggroRange);
            bool inCautious = IsPlayerInRange(cautiousRange);

            switch (_currentState)
            {
                case BurnoutState.Idle:
                    if (inAggro)
                    {
                        StartAggro();
                    }
                    else if (inCautious)
                    {
                        if (!_hasTriggeredCautious)
                            StartCautious();
                    }
                    else
                    {
                        _hasTriggeredCautious = false;
                    }
                    break;

                case BurnoutState.Cautious:
                    if (inAggro)
                    {
                        StopCautious();
                        StartAggro();
                    }
                    else if (!inCautious)
                    {
                        StopCautious();
                        _hasTriggeredCautious = false;
                    }
                    break;
            }
        }

        private void SetState(BurnoutState newState)
        {
            _currentState = newState;

            animator.SetBool(IsIdle, newState == BurnoutState.Idle);
            animator.SetBool(IsCautious, newState == BurnoutState.Cautious);

            if (newState == BurnoutState.Idle)
            {
                animator.ResetTrigger(AggroTrigger);
                animator.ResetTrigger(SuckTrigger);
            }
        }

        private void StartCautious()
        {
            _hasTriggeredCautious = true;

            SetState(BurnoutState.Cautious);

            if (_cautiousRoutine != null)
                StopCoroutine(_cautiousRoutine);

            _cautiousRoutine = StartCoroutine(CautiousRoutine());
        }

        private void StopCautious()
        {
            if (_cautiousRoutine != null)
            {
                StopCoroutine(_cautiousRoutine);
                _cautiousRoutine = null;
            }

            if (_currentState == BurnoutState.Cautious)
                SetState(BurnoutState.Idle);
        }

        private IEnumerator CautiousRoutine()
        {
            yield return new WaitForSeconds(cautiousDuration);

            if (_currentState == BurnoutState.Cautious)
                SetState(BurnoutState.Idle);

            _cautiousRoutine = null;
        }

        private void StartAggro()
        {
            StopCautious();

            // Tentukan arah hadap sekali di sini, lalu dikunci sepanjang Aggro + Suck.
            UpdateFacingTowardsPlayer();

            SetState(BurnoutState.Aggro);

            animator.ResetTrigger(SuckTrigger);
            animator.SetTrigger(AggroTrigger);

            StartCoroutine(AggroSequence());
        }

        private IEnumerator AggroSequence()
        {
            yield return new WaitForSeconds(0.65f);

            if (_currentState == BurnoutState.Aggro)
                StartSuck();
        }

        private void StartSuck()
        {
            SetState(BurnoutState.Suck);

            animator.ResetTrigger(AggroTrigger);
            animator.SetTrigger(SuckTrigger);

            StartCoroutine(SuckSequence());
        }

        private IEnumerator SuckSequence()
        {
            yield return new WaitForSeconds(0.28f);

            Collider2D hit = FindPlayerInRange(aggroRange);
            if (hit != null)
            {
                PlayerHealth health = hit.GetComponent<PlayerHealth>();
                if (health != null)
                    health.TakeDamage(health.CurrentHealth);
            }

            yield return new WaitForSeconds(0.5f);

            _hasTriggeredCautious = false;
            SetState(BurnoutState.Idle);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
            Gizmos.DrawSphere(transform.position, cautiousRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, cautiousRange);

            Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
            Gizmos.DrawSphere(transform.position, aggroRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, aggroRange);
        }

        private bool IsPlayerInRange(float range)
        {
            Collider2D hit = FindPlayerInRange(range);
            return hit != null;
        }

        private Collider2D FindPlayerInRange(float range)
        {
            Collider2D[] results = new Collider2D[8];
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, range, results);

            for (int i = 0; i < count; i++)
            {
                if (results[i].gameObject != gameObject && results[i].CompareTag("Player"))
                    return results[i];
            }

            return null;
        }

        // ---- Facing ----

        private void UpdateFacingTowardsPlayer()
        {
            Collider2D hit = FindPlayerInRange(aggroRange);
            if (hit == null) return;

            float dx = hit.transform.position.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.01f) return; // player hampir lurus di atas/bawah, biarkan hadap terakhir

            _facingRight = dx > 0f;
            ApplyFacing();
        }

        private void ApplyFacing()
        {
            Vector3 scale = _visual.localScale;
            float absX = Mathf.Abs(scale.x);
            // Default sprite dianggap menghadap KIRI -> saat _facingRight true, mirror jadi kanan.
            scale.x = _facingRight ? -absX : absX;
            _visual.localScale = scale;
        }
    }
}