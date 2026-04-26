using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuControl : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("");
    }

    public void ContinueGame() 
    {
        
    }

    public void ExitGromGame()
    {
        Application.Quit();
    }
}
