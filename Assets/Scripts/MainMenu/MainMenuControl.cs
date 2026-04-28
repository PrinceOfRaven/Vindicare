using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuControl : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("");
    }

    //TODO: добавить систему сохранений и загружать их, а не сцену игры
    public void ContinueGame() 
    {
        SceneManager.LoadScene(0);
        Debug.Log("Нажато");
    }

    public void ExitGromGame()
    {
        Application.Quit();
    }
}
