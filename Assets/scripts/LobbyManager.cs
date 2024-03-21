using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;
    [Header("UI Elements")]
    public GameObject lobbyUI;
    public TextMeshProUGUI joinCodeText;
    public TextMeshProUGUI playersListText;
    public Button startGameButton;
    public TMP_InputField joinCodeInputField;
    public Toggle privateLobbyToggle; // ˽�˴�����ѡ��

    [SerializeField] private GameObject lobbyItemPrefab;
    [SerializeField] private Transform lobbiesListParent; 

    private bool isHost = false;
    private Lobby lobby;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(InitializeServicesAsync());
        UpdateStartGameButtonVisibility(); 
    }

    private IEnumerator InitializeServicesAsync()
    {
        yield return UnityServices.InitializeAsync();
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            Debug.LogError("Unity�����ʼ��ʧ�ܡ���ǰ״̬: " + UnityServices.State);
            yield break;
        }

        RegisterNetworkEvents();

        Debug.Log("Unity�����ʼ���ɹ���");
    }

    private void RegisterNetworkEvents()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.SingletonΪ�գ���ȷ��������ȷ��ʼ����");
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            UpdatePlayerList();
        }

        Debug.Log("�����¼�ע����ɡ�");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"�ͻ������ӳɹ���clientId: {clientId}");
        UpdatePlayerList();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"�ͻ��˶Ͽ����ӣ�clientId: {clientId}");
        UpdatePlayerList();
    }


    void UpdatePlayerList()
    {
        Debug.Log($"[LobbyManager] ���ڸ�������б������ӵĿͻ�����: {NetworkManager.Singleton.ConnectedClientsIds.Count}");

        playersListText.text = "����б�:\n";
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Debug.Log($"[LobbyManager] ���ID: {clientId}");
            playersListText.text += $"��� {clientId}\n";
        }
    }

    public async void OnCreateLobbyButtonClicked() 
    {
        bool isPrivate = privateLobbyToggle.isOn;
        await CreateLobbyAndSaveRelayCode(isPrivate);
    }

    public async Task CreateLobbyAndSaveRelayCode(bool isPrivate)
    {
        // ���ô���ѡ�����isPrivate�������������Ƿ���Ϊ˽��
        var lobbyOptions = new CreateLobbyOptions { IsPrivate = isPrivate };
        try
        {
            lobby = await Lobbies.Instance.CreateLobbyAsync("Sendog", 4, lobbyOptions);
            InvokeRepeating("SendHeartbeat", 0, 30f);
            isHost = true;
            Debug.Log("���������ɹ�����ǰ���������������");

            string relayJoinCode = await RelayManager.Instance.CreateRelayAllocationAsync();
            if (!string.IsNullOrEmpty(relayJoinCode))
            {
                var updateLobbyOptions = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                {
                    { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode) }
                }
                };
                await Lobbies.Instance.UpdateLobbyAsync(lobby.Id, updateLobbyOptions);
                UpdateJoinCodeUI(lobby.Id); 

                NetworkManager.Singleton.StartHost();
            }
            UpdateStartGameButtonVisibility(); 
        }
        catch (Exception ex)
        {
            Debug.LogError($"���������򱣴�Relay����ʧ��: {ex.Message}");
            isHost = false;
            UpdateStartGameButtonVisibility();
        }
    }

    private void UpdateJoinCodeUI(string code)
    {
        joinCodeText.text = $"�����룺{code}";
    }

    public async void JoinPublicLobby(string lobbyId)
    {
        Debug.Log($"[LobbyManager] ���Լ��빫������������ID: {lobbyId}");
        try
        {
            var lobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log("[LobbyManager] �ɹ����빫������");

            if (lobby.Data.TryGetValue("RelayJoinCode", out var relayJoinCodeData))
            {
                string relayJoinCode = relayJoinCodeData.Value;
                Debug.Log($"[LobbyManager] ׼��ʹ�� Relay ���������ӵ� Relay ���񣬼�����: {relayJoinCode}");

                if (RelayManager.Instance.JoinCode != relayJoinCode)
                {
                    // ���ӵ� Relay �����������紫������
                    await RelayManager.Instance.ConnectToRelayService(relayJoinCode);
                }
                else
                {
                    Debug.LogWarning("[LobbyManager] ���Լ����Լ������Ĵ��������ǲ�����ġ�");
                }
            }
            else
            {
                Debug.LogError("[LobbyManager] ��������������û���ҵ�Relay�����롣");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LobbyManager] ���빫������ʧ��: {ex.Message}");
        }
    }

    // �����ṩ�Ĵ���ID��Ϊ������������
    public async Task JoinPrivateLobby(string lobbyId)
    {
        try
        {            
            var lobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId);
            if (lobby.Data.TryGetValue("RelayJoinCode", out var relayJoinCodeData))
            {
                string relayJoinCode = relayJoinCodeData.Value;
                // ʹ��Relay���������ӵ�Relay����
                await RelayManager.Instance.ConnectToRelayService(relayJoinCode);
                Debug.Log("[LobbyManager] �ɹ�ʹ��Relay���������Relay��");
            }
            else
            {
                Debug.LogError("[LobbyManager] �ڴ����������Ҳ���Relay�����롣");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LobbyManager] �������ʧ��: {ex.Message}");
        }
    }


    public async void OnJoinPrivateLobbyButtonClicked()
    {
        var lobbyId = joinCodeInputField.text;
        if (!string.IsNullOrEmpty(lobbyId))
        {
            await JoinPrivateLobby(lobbyId);
        }
        else
        {
            Debug.LogError("[LobbyManager] ����IDΪ�ա�");
        }
    }

    public void StartGame()
    {
        Debug.Log("[LobbyManager] StartGame���������á��������ڳ��Կ�ʼ��Ϸ��");

        if (NetworkSceneManager.Instance != null)
        {
            Debug.Log("[LobbyManager] ���ڵ��� NetworkSceneManager ���󳡾����ġ�");
            NetworkSceneManager.Instance.RequestServerChangeSceneServerRpc("Game");
        }
        else
        {
            Debug.LogError("[LobbyManager] �޷��ҵ� NetworkSceneManager ʵ����");
        }
    }

    public void OnRefreshLobbiesButtonClicked()
    {
        FetchAndDisplayLobbies();
    }

    public async void FetchAndDisplayLobbies()
    {
        Debug.Log("[LobbyManager] ��ʼ��ȡ�����б�...");
        Debug.Log($"[LobbyManager] lobbiesListParent: {lobbiesListParent}, lobbyItemPrefab: {lobbyItemPrefab}");

        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 10 
            };

            Debug.Log("[LobbyManager] ����ִ�д�����ѯ...");
            var response = await Lobbies.Instance.QueryLobbiesAsync(options);
            Debug.Log($"[LobbyManager] ������ѯ��ɣ����ҵ� {response.Results.Count} ��������");

            foreach (Transform child in lobbiesListParent)
            {
                Destroy(child.gameObject);
            }
            Debug.Log("[LobbyManager] ����ɵĴ����б�");

            foreach (var lobby in response.Results)
            {
                var item = Instantiate(lobbyItemPrefab, lobbiesListParent);
                var lobbyItem = item.GetComponent<LobbyItem>();
                if (lobbyItem != null)
                {
                    lobbyItem.Setup(lobby);
                }
                else
                {
                    Debug.LogError($"��Ԥ����{lobbyItemPrefab.name}��ʵ�����Ҳ���LobbyItem�����");
                }
            }


            Debug.Log("[LobbyManager] �����б���ʾ������ɡ�");
        }
        catch (Exception e)
        {
            Debug.LogError($"��ȡ�����б�ʧ��: {e.Message}");
        }
    }

    async void SendHeartbeat()
    {
        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id);
            Debug.Log("�������ͳɹ�");
        }
        catch (Exception ex)
        {
            Debug.LogError("��������ʱ��������: " + ex.Message);
        }
    }

    private void UpdateStartGameButtonVisibility()
    {
        startGameButton.gameObject.SetActive(isHost);
    }

    public void CopyJoinCodeToClipboard()
    {
        if (!string.IsNullOrEmpty(joinCodeText.text))
        {
            GUIUtility.systemCopyBuffer = joinCodeText.text;
            Debug.Log("[LobbyManager] �������Ѹ��Ƶ������塣");
        }
        else
        {
            Debug.LogError("[LobbyManager] ������Ϊ�գ��޷����ơ�");
        }
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("[LobbyManager] ���ڷ������˵���");
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene("Menu");
    }

}


