using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SceneScript : MonoBehaviour
{
    [Header("Menu Options")]
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    [Header("First Selected Options")]
    [SerializeField] private GameObject mainMenuFirst;
    [SerializeField] private GameObject optionsFirst;

    void Start()
    {
        EventSystem.current.SetSelectedGameObject(mainMenuFirst);

    }
    public void SceneSwitch(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        Debug.Log($"Loading scene: {sceneName}");

    }

    public void ShowOptions()
    {
        optionsPanel.SetActive(true);

    }

    public void HideOptions()
    {
        optionsPanel.SetActive(false);
    }

    public void ShowCredits()
    {
        creditsPanel.SetActive(true);
    }

    public void HideCredits()
    {
        creditsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

}
