using Slafurry.System.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string _gameSceneName = "GameScene";
    [SerializeField] private string _settingsSceneName = "SettingsScene";
    [SerializeField] private string _aboutSceneName = "AboutScene";

    public void StartGame()
    {
        SceneSystem.Load(_gameSceneName);
    }

    public void ContinueGame()
    {
        // Load System
        SceneSystem.Load(_gameSceneName);
    }

    public void ShowAbout()
    {
        SceneManager.LoadScene(_aboutSceneName);
    }

    public void ShowSettings()
    {
        SceneManager.LoadScene(_settingsSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}