using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void LoadLobby()
    {
        SceneManager.LoadScene("Lobby"); 
    }

    public void Quit()
    {
        Application.Quit();
    }      
}
