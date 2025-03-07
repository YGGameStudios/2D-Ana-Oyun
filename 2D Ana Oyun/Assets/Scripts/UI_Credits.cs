using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_Credits : MonoBehaviour
{
    [SerializeField] private RectTransform rectT;
    [SerializeField] private float scrollSpeed = 200f;

    [SerializeField] private string mainMenuSceneName = "MainMenu";
    private bool creditsSkipped;

    [SerializeField] private float offScreenPosition = 1800f;

    private UI_FadeEffect fadeEffect;

    private void Awake()
    {
        fadeEffect = GetComponentInChildren<UI_FadeEffect>();
    }

    private void Start()
    {
        fadeEffect.ScreenFade(0f, 1f, null);
    }

    private void Update()
    {
        rectT.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

        if(rectT.anchoredPosition.y > offScreenPosition)
            GoToMainMenu();
    }

    public void SkipCredits()
    {
        if (creditsSkipped == false)
        {
            scrollSpeed *= 10;
            creditsSkipped = true;
        }
        else
        {
            GoToMainMenu();
        }
    }

    private void SwitchToMainMenuScene()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void GoToMainMenu() => fadeEffect.ScreenFade(1f, 1.5f, SwitchToMainMenuScene);
}
