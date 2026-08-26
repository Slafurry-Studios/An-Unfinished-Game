using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Dialog;
using System;
using Slafurry.System.Pause;

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
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip typeLoopSfx;

        [Header("Options")]
        [SerializeField] private bool allowSkip = true;

        private DialogPane dialogPane;

        private bool isLast;
        private bool isTyping;

        private string currentDialog;
        private Coroutine typingCoroutine;
        public Action OnDialogEnd;

        private void Awake()
        {
            dialogPane = FindAnyObjectByType<DialogPane>();
        }

        private void Update()
        {
            if (!allowSkip) return;
            if (!dialogUIPrefab.activeSelf) return;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                SkipDialog();
            }
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

            Hide();
        }

        public void Show()
        {
            Pause.On("Dialog");
            dialogUIPrefab.SetActive(true);
        }

        public void Hide()
        {
            Pause.Off("Dialog");
            OnDialogEnd?.Invoke();
            dialogUIPrefab.SetActive(false);
        }

        public void SetDialog(string characterName, string dialog, bool isLast)
        {
            nameText.text = characterName;

            currentDialog = dialog;
            this.isLast = isLast;

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

            if (!isLast)
                nextDialogClue.SetActive(true);
        }

        public void NextDialog()
        {
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
                Hide();
                isLast = false;
                return;
            }

            dialogPane.NextDialog();
        }

        private void PlayTypeSfx()
        {
            if (audioSource == null || typeLoopSfx == null)
                return;

            audioSource.clip = typeLoopSfx;
            audioSource.loop = true;
            audioSource.Play();
        }

        private void StopTypeSfx()
        {
            if (audioSource == null)
                return;

            audioSource.Stop();
        }
    }
}