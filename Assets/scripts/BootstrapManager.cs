using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

//���ؽ��� �ȴ�Unity Services��ʼ��
public class BootstrapManager : MonoBehaviour
{
    public GameObject loadingScreen;
    public TextMeshProUGUI loadingText;
    public float additionalWaitTime = 2.0f;

    void Start()
    {
        loadingScreen.SetActive(true);
        loadingText.text = "������...";
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

