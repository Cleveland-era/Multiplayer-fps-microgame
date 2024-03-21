using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Linq;


public class EndGameMenu : MonoBehaviour
{
    public float deathHeight = -10f;
    public GameObject endMenu;           
    public GameObject restartButton;    
    public GameObject quitToLobbyButton; 
    public GameObject clientWaitingText; 
    public GameObject victoryTitle;      
    public GameObject defeatTitle;       

    private bool gameEnded = false;

    void Start()
    {
        endMenu.SetActive(false);
        StartCoroutine(CheckDeathCondition());
    }

    IEnumerator CheckDeathCondition()
    {
        yield return new WaitForSeconds(1f); 

        while (!gameEnded)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player && player.transform.position.y < deathHeight)
            {
                gameEnded = true;
                EndGameServerRpc(NetworkManager.Singleton.LocalClientId);
                break;
            }
            yield return null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void EndGameServerRpc(ulong deadPlayerClientId)
    {
        ulong winnerClientId = NetworkManager.Singleton.ConnectedClientsIds.FirstOrDefault(id => id != deadPlayerClientId);
        EndGameClientRpc(PlayerGameState.Loser, deadPlayerClientId);
        if (winnerClientId != 0)
        {
            EndGameClientRpc(PlayerGameState.Winner, winnerClientId);
        }
    }

    [ClientRpc]
    void EndGameClientRpc(PlayerGameState gameState, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            ShowEndGameMenu(gameState);
        }
    }

    //Ω·À„ΩÁ√Ê
    void ShowEndGameMenu(PlayerGameState gameState)
    {
        Time.timeScale = 0f; 
        endMenu.transform.parent.gameObject.SetActive(true); 
        endMenu.SetActive(true); 

        victoryTitle.SetActive(gameState == PlayerGameState.Winner);
        defeatTitle.SetActive(gameState == PlayerGameState.Loser);

        if (NetworkManager.Singleton.IsHost)
        {
            restartButton.SetActive(true);
            quitToLobbyButton.SetActive(true);
            clientWaitingText.SetActive(false);
        }
        else
        {
            restartButton.SetActive(false);
            quitToLobbyButton.SetActive(false);
            clientWaitingText.SetActive(gameState == PlayerGameState.Loser);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GlobalCursorManager.CursorLocked = false;
    }

    public enum PlayerGameState
    {
        Winner,
        Loser
    }

    public void RestartGame()
    {
        if (NetworkManager.Singleton.IsHost && gameEnded)
        {         
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            Time.timeScale = 1f; 
        }
    }

    public void QuitToLobby()
    {
        if (NetworkManager.Singleton.IsHost && gameEnded)
        {            
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
            Time.timeScale = 1f;
        }
    }   
}










