using Game.UI.HUD;
using Game.Dialog;
using UnityEngine;
using UnityEngine.Events;

public class DialogTrigger : BaseTrigger
{
    [Header("Dialog")]
    [SerializeField] private DialogBucket bucket;
    [SerializeField] private DialogHUD dialogHUD;

    [Header("Events")]
    [SerializeField] private UnityEvent onDialogEnd;
    [SerializeField] private UnityEvent[] onLineComplete;

    private void Start()
    {
        if (dialogHUD == null)
            dialogHUD = FindObjectOfType<DialogHUD>();

        if (dialogHUD != null)
            dialogHUD.OnDialogEnd += HandleDialogEnd;
    }

    private void OnDisable()
    {
        if (dialogHUD != null)
            dialogHUD.OnDialogEnd -= HandleDialogEnd;
    }

    public void StartDialog()
    {
        if (!CanTrigger()) return;
        if (bucket == null || dialogHUD == null) return;

        AddTriggerCount();
        dialogHUD.SetLineEvents(onLineComplete);
        dialogHUD.StartDialog(bucket);
    }

    private void HandleDialogEnd()
    {
        onDialogEnd?.Invoke();
    }
}
