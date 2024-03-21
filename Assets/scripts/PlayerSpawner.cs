using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;


public class PlayerSpawner : NetworkBehaviour
{
    public GameObject playerPrefab; 

    private Vector3 spawnPosition1 = new Vector3(-40f, 2f, 0f);
    private Vector3 spawnPosition2 = new Vector3(40f, 2f, 0f);
    private bool isFirstSpawnPointUsed = false;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (NetworkManager.Singleton != null)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game") 
        {
            RequestSpawnPlayer();
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnPlayerServerRpc(ServerRpcParams rpcParams = default)
    {
        SpawnPlayer(rpcParams.Receive.SenderClientId);
    }

    private void RequestSpawnPlayer()
    {
        RequestSpawnPlayerServerRpc();
    }

    private void SpawnPlayer(ulong clientId)
    {
        Vector3 spawnPosition = isFirstSpawnPointUsed ? spawnPosition2 : spawnPosition1;
        isFirstSpawnPointUsed = !isFirstSpawnPointUsed;

        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.SpawnAsPlayerObject(clientId);
        }
        else
        {
            Debug.LogError("Spawned player object does not have a NetworkObject component.");
        }
    }

}















