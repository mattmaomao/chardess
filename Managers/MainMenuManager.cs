using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void OnStartButton() {
        SceneManager.LoadScene("MainGame");
    }

    public void OnQuitButton() {
        Application.Quit();
    }
}
