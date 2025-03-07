using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_LevelButton : MonoBehaviour
{
    public string sceneName;

    public void LoadLevel()
    {
        SceneManager.LoadScene(sceneName);
    }
}
