using Game.UI.HUD;
using UnityEngine;
using System;

namespace Game.Dialog
{
    public class DialogPane : MonoBehaviour
    {
        public Action<bool> OnDialogHUDReady;
        private DialogHUD dialogHUD;
        private DialogBucket currentBucket;
        private int currentIndex;

        private void Awake()
        {
            dialogHUD = FindAnyObjectByType<DialogHUD>();

            if (dialogHUD != null)
            {
                OnDialogHUDReady?.Invoke(true);
            }
        }

        public void StartDialog(DialogBucket bucket)
        {
            currentBucket = bucket;
            currentIndex = 0;

            dialogHUD.Show();
            ShowCurrentDialog();
        }

        public void NextDialog()
        {
            if (currentBucket == null)
                return;

            if (IsLast())
            {
                dialogHUD.Hide();
                return;
            }
                

            currentIndex++;
            ShowCurrentDialog();
        }

        private void ShowCurrentDialog()
        {
            Dialog dialog = currentBucket.dialogs[currentIndex];

            dialogHUD.SetDialog(
                dialog.name,
                dialog.dialog,
                IsLast()
            );
        }

        private bool IsLast()
        {
            return currentIndex >= currentBucket.dialogs.Length - 1;
        }
    }
}