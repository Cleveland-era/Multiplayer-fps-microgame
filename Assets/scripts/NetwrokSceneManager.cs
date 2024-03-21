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
        Debug.Log($"[NetworkSceneManager] RequestServerChangeSceneServerRpc���������ã�����ĳ�������Ϊ: {newSceneName}");

        if (IsServer)
        {
            Debug.Log($"[NetworkSceneManager] ���������ڳ��Ը��ĳ����� {newSceneName}");
            var sceneLoadOperation = NetworkManager.Singleton.SceneManager.LoadScene(newSceneName, LoadSceneMode.Single);

            // �������Ƿ��Ѿ���ʼ
            if (sceneLoadOperation != SceneEventProgressStatus.Started)
            {
                Debug.LogError($"[NetworkSceneManager] ���� {newSceneName} ��ʼ����ʧ�ܡ����鳡�������Ƿ���ȷ�����ҳ����Ƿ�����ӵ�Build Settings��");
            }
        }
        else
        {
            Debug.LogWarning("[NetworkSceneManager] �Ƿ�����ʵ��������RequestServerChangeSceneServerRpc�������õ��ý������ԡ�");
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

