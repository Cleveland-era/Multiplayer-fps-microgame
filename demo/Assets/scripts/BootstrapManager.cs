using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

//加载界面 等待Unity Services初始化
public class BootstrapManager : MonoBehaviour
{
    public GameObject loadingScreen;
    public TextMeshProUGUI loadingText;
    public float additionalWaitTime = 2.0f;

    void Start()
    {
        loadingScreen.SetActive(true);
        loadingText.text = "加载中...";
        StartCoroutine(LoadMenuSceneAsync());
    }

    IEnumerator LoadMenuSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Menu");
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(additionalWaitTime);
    }
}

