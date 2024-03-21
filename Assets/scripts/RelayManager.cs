using System;
using System.Collections;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using System.Threading.Tasks;


public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }
    public bool IsInitialized { get; private set; } = false;
    public event Action<string> OnJoinCodeCreated;
    private string myAllocationId = "";
    public string JoinCode { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator Start()
    {
        yield return InitializeServices();
    }

    // 初始化Unity服务和玩家登录
    private async Task InitializeServices()
    {
        if (IsInitialized) return;

        try
        {
            await UnityServices.InitializeAsync();
            await SignInPlayer();
            IsInitialized = true;
            Debug.Log("[RelayManager] 成功初始化Unity Services");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] 初始化services失败或匿名登录失败: {e.Message}");
            IsInitialized = false;
        }
    }

    private async Task SignInPlayer()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[RelayManager] 匿名登录成功 玩家ID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] 匿名登录失败: {e.Message}");
        }
    }

    // 创建Relay分配和加入码
    public async Task<string> CreateRelayAllocationAsync()
    {
        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(4);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            myAllocationId = allocation.AllocationId.ToString();
            Debug.Log($"[RelayManager] Relay allocation created with AllocationID: {myAllocationId}");

            SetupNetworkTransport(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

            Debug.Log($"[RelayManager] Relay allocation created successfully. Join Code: {joinCode}");
            OnJoinCodeCreated?.Invoke(joinCode); 
            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] Failed to create Relay allocation: {e.Message}");
            return null; 
        }
    }

    // 使用加入码连接Relay
    public async Task ConnectToRelayService(string joinCode)
    {
        try
        {
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log($"[RelayManager] 成功连接到 Relay，加入码: {joinCode}");

            // 使用获取的连接信息，与 Relay 服务建立连接
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            Debug.Log("[RelayManager] 网络传输配置成功，准备启动客户端。");

            NetworkManager.Singleton.StartClient();
            Debug.Log("[RelayManager] Netcode客户端启动成功。");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] 直接连接到 Relay 服务失败: {e}");
        }
    }

    private void SetupNetworkTransport(string ipv4, ushort port, byte[] allocationId, byte[] key, byte[] connectionData)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(ipv4, port, allocationId, key, connectionData);
        Debug.Log("[RelayManager]已分配transport");
    }
}