using UnityEngine;
using UnityEngine.SceneManagement;

public class AboutMenu : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(_mainMenuSceneName);
    }
}