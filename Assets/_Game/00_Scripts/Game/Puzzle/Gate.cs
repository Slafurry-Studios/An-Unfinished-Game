using UnityEngine;

public class Gate : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private bool isActive = true;

    [Header("References")]
    [SerializeField] private Collider2D gateCollider;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        UpdateGate();
    }

    private void Update()
    {
        // Untuk testing langsung dari Inspector.
        UpdateGate();
    }

    public void Open()
    {
        if (!isActive)
            return;

        isOpen = true;
        UpdateGate();
    }

    public void Close()
    {
        if (!isActive)
            return;

        isOpen = false;
        UpdateGate();
    }

    public void Toggle()
    {
        if (!isActive)
            return;

        isOpen = !isOpen;
        UpdateGate();
    }

    private void UpdateGate()
    {
        if (animator != null)
        {
            animator.SetBool("IsOpen", isOpen);
        }

        if (gateCollider != null)
        {
            gateCollider.enabled = !isOpen;
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;
    }
}