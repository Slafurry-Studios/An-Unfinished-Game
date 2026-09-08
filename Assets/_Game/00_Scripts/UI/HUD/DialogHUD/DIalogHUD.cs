using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using DialogSystem = Game.Dialog;
using System;
using Slafurry.System.Pause;
using Slafurry.System.Audio;
using Slafurry.Player.Animation;

namespace Game.UI.HUD
{
    public class DialogHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI dialogText;
        [SerializeField] private GameObject nextDialogClue;
        [SerializeField] private GameObject dialogUIPrefab;

        [Header("Type Effect")]
        [SerializeField] private float typingSpeed = 0.03f;

        [Header("SFX")]
        [SerializeField] private string sfxCategory = "UI";
        [SerializeField] private string typeSFX = "typing";

        [Header("Options")]
        [SerializeField] private bool allowSkip = true;

        [Header("Player")]
        [SerializeField] private PlayerAnimationStateMachine animStateMachine;

        private DialogSystem.DialogBucket currentBucket;
        private int currentIndex;
        private bool isLast;
        private bool isTyping;

        private string currentDialog;
        private Coroutine typingCoroutine;
        private UnityEvent[] currentLineEvents;
        public Action OnDialogEnd;

        private void Update()
        {
            if (!allowSkip) return;
            if (!dialogUIPrefab.activeSelf) return;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                NextDialog();
            }
        }

        public void StartDialog(DialogSystem.DialogBucket bucket)
        {
            if (bucket == null || bucket.dialogs.Length == 0) return;

            currentBucket = bucket;
            currentIndex = 0;

            Pause.On("Dialog");
            dialogUIPrefab.SetActive(true);

            ShowCurrentDialog();
        }

        public void SetLineEvents(UnityEvent[] events)
        {
            currentLineEvents = events;
        }

        public void NextDialog()
        {
            if (currentBucket == null) return;

            if (isTyping)
            {
                StopCoroutine(typingCoroutine);
                StopTypeSfx();

                dialogText.text = currentDialog;
                isTyping = false;

                if (!isLast)
                    nextDialogClue.SetActive(true);

                return;
            }

            if (isLast)
            {
                Pause.Off("Dialog");
                OnDialogEnd?.Invoke();
                dialogUIPrefab.SetActive(false);
                isLast = false;
                currentBucket = null;
                currentLineEvents = null;
                return;
            }

            currentIndex++;
            ShowCurrentDialog();
        }

        public void SkipDialog()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            StopTypeSfx();
            isTyping = false;
            isLast = false;

            Pause.Off("Dialog");
            OnDialogEnd?.Invoke();
            dialogUIPrefab.SetActive(false);
            currentBucket = null;
            currentLineEvents = null;
        }

        private void ShowCurrentDialog()
        {
            DialogSystem.Dialog dialog = currentBucket.dialogs[currentIndex];
            isLast = currentIndex >= currentBucket.dialogs.Length - 1;

            nameText.text = dialog.name;
            currentDialog = dialog.dialog;

            nextDialogClue.SetActive(false);

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeDialog());
        }

        private IEnumerator TypeDialog()
        {
            isTyping = true;
            dialogText.text = "";

            PlayTypeSfx();

            foreach (char c in currentDialog)
            {
                dialogText.text += c;
                yield return new WaitForSecondsRealtime(typingSpeed);
            }

            StopTypeSfx();

            dialogText.text = currentDialog;
            isTyping = false;

            InvokeLineEvent();

            if (!isLast)
                nextDialogClue.SetActive(true);
        }

        private void InvokeLineEvent()
        {
            if (currentLineEvents == null) return;
            if (currentIndex < currentLineEvents.Length)
                currentLineEvents[currentIndex]?.Invoke();
        }

        private void PlayTypeSfx()
        {
            if (AudioSystem.Instance == null) return;
            Audio.PlaySFX2D(sfxCategory, typeSFX, loop: true);
        }

        private void StopTypeSfx()
        {
            if (AudioSystem.Instance == null) return;
            Audio.StopSFX(sfxCategory, typeSFX);
        }
    }
}
