using Slafurry.System.Pause;
using UnityEngine;

public class PauseHUD : MonoBehaviour
{
    [Header("Pause HUD Settings")]
    [SerializeField] private GameObject pauseMenu;

    private void Awake()
    {
        if (pauseMenu == null)
            Debug.LogWarning("Pause menu is not assigned in the inspector.");
    }

    public void ShowPauseMenu()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(true);
        Pause.On("Global");

    }

    public void HidePauseMenu()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        Pause.Off("Global");
    }
}