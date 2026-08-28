using Slafurry.Player;
using UnityEngine;
using UnityEngine.Events;


public enum SacrificeAction
{
    None,
    CanLeft,
    CanRight,
    CanJump,
    CanCrouch,
    NoLeft,
    NoRight,
    NoJump,
    NoCrouch,
    GravityUp,
    GravityDown,
    GravityLeft,
    GravityRight,
    NoGravity,
    ClearAllEffect
}

public class ControlSacrifice : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private SacrificeAction action = SacrificeAction.None;

    [Header("Animasi (opsional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip clip;

    [SerializeField] private UnityEvent onApplied;

    private void Awake()
    {
        if (player == null)
            player = FindObjectOfType<PlayerMovement>();
    }

    private void Start()
    {
        if (animator != null && clip != null)
            animator.Play(clip.name, 0, 0f);
    }

    public void ApplySacrifice()
    {
        if (player == null)
        {
            Debug.LogWarning($"{name}: PlayerMovement tidak ditemukan di scene.", this);
            return;
        }

        switch (action)
        {
            case SacrificeAction.CanLeft: player.EnableLeftMovement(); break;
            case SacrificeAction.CanRight: player.EnableRightMovement(); break;
            case SacrificeAction.CanJump: player.EnableJump(); break;
            case SacrificeAction.CanCrouch: player.EnableCrouch(); break;
            case SacrificeAction.NoLeft: player.DisableLeftMovement(); break;
            case SacrificeAction.NoRight: player.DisableRightMovement(); break;
            case SacrificeAction.NoJump: player.DisableJump(); break;
            case SacrificeAction.NoCrouch: player.DisableCrouch(); break;
            case SacrificeAction.GravityUp: player.SetGravityUp(); break;
            case SacrificeAction.GravityDown: player.SetGravityDown(); break;
            case SacrificeAction.GravityLeft: player.SetGravityLeft(); break;
            case SacrificeAction.GravityRight: player.SetGravityRight(); break;
            case SacrificeAction.NoGravity: player.DisableGravity(); break;
            case SacrificeAction.ClearAllEffect: ClearAll(); break;
        }

        onApplied?.Invoke();
    }

    private void ClearAll()
    {
        player.EnableLeftMovement();
        player.EnableRightMovement();
        player.EnableJump();
        player.EnableCrouch();
        player.EnableGravity();
        player.ResetMoveSpeedMultiplier();
    }
}