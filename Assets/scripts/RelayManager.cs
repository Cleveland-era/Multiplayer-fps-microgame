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

    // ��ʼ��Unity�������ҵ�¼
    private async Task InitializeServices()
    {
        if (IsInitialized) return;

        try
        {
            await UnityServices.InitializeAsync();
            await SignInPlayer();
            IsInitialized = true;
            Debug.Log("[RelayManager] �ɹ���ʼ��Unity Services");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] ��ʼ��servicesʧ�ܻ�������¼ʧ��: {e.Message}");
            IsInitialized = false;
        }
    }

    private async Task SignInPlayer()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[RelayManager] ������¼�ɹ� ���ID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] ������¼ʧ��: {e.Message}");
        }
    }

    // ����Relay����ͼ�����
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

    // ʹ�ü���������Relay
    public async Task ConnectToRelayService(string joinCode)
    {
        try
        {
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log($"[RelayManager] �ɹ����ӵ� Relay��������: {joinCode}");

            // ʹ�û�ȡ��������Ϣ���� Relay ����������
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            Debug.Log("[RelayManager] ���紫�����óɹ���׼�������ͻ��ˡ�");

            NetworkManager.Singleton.StartClient();
            Debug.Log("[RelayManager] Netcode�ͻ��������ɹ���");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] ֱ�����ӵ� Relay ����ʧ��: {e}");
        }
    }

    private void SetupNetworkTransport(string ipv4, ushort port, byte[] allocationId, byte[] key, byte[] connectionData)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(ipv4, port, allocationId, key, connectionData);
        Debug.Log("[RelayManager]�ѷ���transport");
    }
}