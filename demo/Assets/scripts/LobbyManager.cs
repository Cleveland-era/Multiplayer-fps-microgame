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
    public Toggle privateLobbyToggle; // 私人大厅勾选框

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
            Debug.LogError("Unity服务初始化失败。当前状态: " + UnityServices.State);
            yield break;
        }

        RegisterNetworkEvents();

        Debug.Log("Unity服务初始化成功。");
    }

    private void RegisterNetworkEvents()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton为空，请确保其已正确初始化。");
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            UpdatePlayerList();
        }

        Debug.Log("网络事件注册完成。");
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
        Debug.Log($"客户端连接成功，clientId: {clientId}");
        UpdatePlayerList();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"客户端断开连接，clientId: {clientId}");
        UpdatePlayerList();
    }


    void UpdatePlayerList()
    {
        Debug.Log($"[LobbyManager] 正在更新玩家列表。已连接的客户端数: {NetworkManager.Singleton.ConnectedClientsIds.Count}");

        playersListText.text = "玩家列表:\n";
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Debug.Log($"[LobbyManager] 玩家ID: {clientId}");
            playersListText.text += $"玩家 {clientId}\n";
        }
    }

    public async void OnCreateLobbyButtonClicked() 
    {
        bool isPrivate = privateLobbyToggle.isOn;
        await CreateLobbyAndSaveRelayCode(isPrivate);
    }

    public async Task CreateLobbyAndSaveRelayCode(bool isPrivate)
    {
        // 设置大厅选项，根据isPrivate参数决定大厅是否设为私人
        var lobbyOptions = new CreateLobbyOptions { IsPrivate = isPrivate };
        try
        {
            lobby = await Lobbies.Instance.CreateLobbyAsync("Sendog", 4, lobbyOptions);
            InvokeRepeating("SendHeartbeat", 0, 30f);
            isHost = true;
            Debug.Log("大厅创建成功，当前玩家现在是主机。");

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
            Debug.LogError($"创建大厅或保存Relay代码失败: {ex.Message}");
            isHost = false;
            UpdateStartGameButtonVisibility();
        }
    }

    private void UpdateJoinCodeUI(string code)
    {
        joinCodeText.text = $"加入码：{code}";
    }

    public async void JoinPublicLobby(string lobbyId)
    {
        Debug.Log($"[LobbyManager] 尝试加入公共大厅，大厅ID: {lobbyId}");
        try
        {
            var lobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log("[LobbyManager] 成功加入公共大厅");

            if (lobby.Data.TryGetValue("RelayJoinCode", out var relayJoinCodeData))
            {
                string relayJoinCode = relayJoinCodeData.Value;
                Debug.Log($"[LobbyManager] 准备使用 Relay 加入码连接到 Relay 服务，加入码: {relayJoinCode}");

                if (RelayManager.Instance.JoinCode != relayJoinCode)
                {
                    // 连接到 Relay 服务并设置网络传输数据
                    await RelayManager.Instance.ConnectToRelayService(relayJoinCode);
                }
                else
                {
                    Debug.LogWarning("[LobbyManager] 尝试加入自己创建的大厅，这是不允许的。");
                }
            }
            else
            {
                Debug.LogError("[LobbyManager] 公共大厅数据中没有找到Relay加入码。");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LobbyManager] 加入公共大厅失败: {ex.Message}");
        }
    }

    // 根据提供的大厅ID作为加入码加入大厅
    public async Task JoinPrivateLobby(string lobbyId)
    {
        try
        {            
            var lobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId);
            if (lobby.Data.TryGetValue("RelayJoinCode", out var relayJoinCodeData))
            {
                string relayJoinCode = relayJoinCodeData.Value;
                // 使用Relay加入码连接到Relay服务
                await RelayManager.Instance.ConnectToRelayService(relayJoinCode);
                Debug.Log("[LobbyManager] 成功使用Relay加入码加入Relay。");
            }
            else
            {
                Debug.LogError("[LobbyManager] 在大厅数据中找不到Relay加入码。");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LobbyManager] 加入大厅失败: {ex.Message}");
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
            Debug.LogError("[LobbyManager] 大厅ID为空。");
        }
    }

    public void StartGame()
    {
        Debug.Log("[LobbyManager] StartGame方法被调用。主机正在尝试开始游戏。");

        if (NetworkSceneManager.Instance != null)
        {
            Debug.Log("[LobbyManager] 正在调用 NetworkSceneManager 请求场景更改。");
            NetworkSceneManager.Instance.RequestServerChangeSceneServerRpc("Game");
        }
        else
        {
            Debug.LogError("[LobbyManager] 无法找到 NetworkSceneManager 实例。");
        }
    }

    public void OnRefreshLobbiesButtonClicked()
    {
        FetchAndDisplayLobbies();
    }

    public async void FetchAndDisplayLobbies()
    {
        Debug.Log("[LobbyManager] 开始获取大厅列表...");
        Debug.Log($"[LobbyManager] lobbiesListParent: {lobbiesListParent}, lobbyItemPrefab: {lobbyItemPrefab}");

        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 10 
            };

            Debug.Log("[LobbyManager] 正在执行大厅查询...");
            var response = await Lobbies.Instance.QueryLobbiesAsync(options);
            Debug.Log($"[LobbyManager] 大厅查询完成，共找到 {response.Results.Count} 个大厅。");

            foreach (Transform child in lobbiesListParent)
            {
                Destroy(child.gameObject);
            }
            Debug.Log("[LobbyManager] 清除旧的大厅列表。");

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
                    Debug.LogError($"在预制体{lobbyItemPrefab.name}的实例上找不到LobbyItem组件。");
                }
            }


            Debug.Log("[LobbyManager] 大厅列表显示更新完成。");
        }
        catch (Exception e)
        {
            Debug.LogError($"获取大厅列表失败: {e.Message}");
        }
    }

    async void SendHeartbeat()
    {
        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id);
            Debug.Log("心跳发送成功");
        }
        catch (Exception ex)
        {
            Debug.LogError("发送心跳时发生错误: " + ex.Message);
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
            Debug.Log("[LobbyManager] 加入码已复制到剪贴板。");
        }
        else
        {
            Debug.LogError("[LobbyManager] 加入码为空，无法复制。");
        }
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("[LobbyManager] 正在返回主菜单。");
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene("Menu");
    }

}


