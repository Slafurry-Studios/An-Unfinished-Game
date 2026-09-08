using Game.UI.HUD;
using Game.Dialog;
using UnityEngine;

public class DialogTrigger : BaseTrigger
{
    [Header("Dialog")]
    [SerializeField] private DialogBucket bucket;
    private DialogHUD dialogHUD;
    void Start()
    {
        if (dialogHUD == null)
            dialogHUD = FindObjectOfType<DialogHUD>();
    }
    
    public void StartDialog()
    {
        if (!CanTrigger()) return;
        if (bucket == null || dialogHUD == null) return;

        AddTriggerCount();
        dialogHUD.StartDialog(bucket);
    }
}
