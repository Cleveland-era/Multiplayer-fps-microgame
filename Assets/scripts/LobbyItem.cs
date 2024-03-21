using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

//lobbyitemÔ¤ÖÆ¼þ
public class LobbyItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private Button joinPublicLobbyButton;
    private string lobbyId;

    public void Setup(Lobby lobby)
    {
        lobbyId = lobby.Id;

        joinPublicLobbyButton.onClick.RemoveAllListeners();
        joinPublicLobbyButton.onClick.AddListener(() => LobbyManager.Instance.JoinPublicLobby(lobbyId));

        lobbyNameText.text = lobby.Name;
    }
}


