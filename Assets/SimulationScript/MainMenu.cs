using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class MainMenu : MonoBehaviour  
{
    public Button startButton;
    public Button exitButton;
    public void StartGame()
    {
        SceneManager.LoadScene("NBody", LoadSceneMode.Single);
    }

    public void ExitGame()
    {
# if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
# else
        Application.Quit();
# endif
    }

    void Start()
    {
        startButton.onClick.AddListener(StartGame);
        exitButton.onClick.AddListener(ExitGame);
    }

    void Update()
    {
    }
}