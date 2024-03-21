using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetworkSceneManager : NetworkBehaviour
{

    [ServerRpc(RequireOwnership = false)]
    public void RequestServerChangeSceneServerRpc(string newSceneName)
    {
        Debug.Log($"[NetworkSceneManager] RequestServerChangeSceneServerRpc方法被调用，请求的场景名称为: {newSceneName}");

        if (IsServer)
        {
            Debug.Log($"[NetworkSceneManager] 服务器正在尝试更改场景到 {newSceneName}");
            var sceneLoadOperation = NetworkManager.Singleton.SceneManager.LoadScene(newSceneName, LoadSceneMode.Single);

            // 检查操作是否已经开始
            if (sceneLoadOperation != SceneEventProgressStatus.Started)
            {
                Debug.LogError($"[NetworkSceneManager] 场景 {newSceneName} 开始加载失败。请检查场景名称是否正确，并且场景是否已添加到Build Settings。");
            }
        }
        else
        {
            Debug.LogWarning("[NetworkSceneManager] 非服务器实例调用了RequestServerChangeSceneServerRpc方法，该调用将被忽略。");
        }
    }


    [SerializeField]
    private string m_SceneName;

    public static NetworkSceneManager Instance;

    void Awake()
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

#if UNITY_EDITOR
    public UnityEditor.SceneAsset SceneAsset;
    private void OnValidate()
    {
        if (SceneAsset != null)
        {
            m_SceneName = SceneAsset.name;
        }
    }
#endif
}

