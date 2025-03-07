using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_MainMenu : MonoBehaviour
{
    private UI_FadeEffect fadeEffect;
    public string sceneName;

    [SerializeField] private GameObject[] uiElements;

    private void Awake()
    {
        fadeEffect = GetComponentInChildren<UI_FadeEffect>();
    }

    private void Start()
    {
        fadeEffect.ScreenFade(0f, 1.5f, null);
    }

    public void NewGame()
    {
        fadeEffect.ScreenFade(1f, 1.5f, LoadLevelScene);
    }

    private void LoadLevelScene() => SceneManager.LoadScene(sceneName);

    public void SwitchUI(GameObject uiToEnable)
    {
        foreach (GameObject ui in uiElements)
        {
            ui.SetActive(false);
        }

        uiToEnable.SetActive(true);
    }
}
