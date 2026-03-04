using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SceneScript : MonoBehaviour
{
    [Header("Canvas's")]
    [SerializeField] private GameObject mainCanvasGO;
    [SerializeField] private GameObject optionsCanvasGO;
    [SerializeField] private GameObject controllerCanvasGO;
    [SerializeField] private GameObject keyboardCanvasGO;
    [SerializeField] private GameObject creditsCanvasGO;


    [Header("First Selected Options")]
    [SerializeField] private GameObject startGameFirst;
    [SerializeField] private GameObject optionsFirst;
    [SerializeField] private GameObject controllerFirst;
    [SerializeField] private GameObject keyboardFirst;
    [SerializeField] private GameObject creditsFirst;


    void Start()
    {
        mainCanvasGO.SetActive(true);
        optionsCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(false);
        creditsCanvasGO.SetActive(false);


        EventSystem.current.SetSelectedGameObject(startGameFirst);

    }

    #region Scene Switcher
    public void SceneSwitch(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        Debug.Log($"Loading scene: {sceneName}");

    }
    #endregion

    #region Canvas Activations/Deactivations

    private void OpenMainMenu()
    {
        mainCanvasGO.SetActive(true);
        optionsCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(false);
        creditsCanvasGO.SetActive(false);

        EventSystem.current.SetSelectedGameObject(startGameFirst);

    }
    private void OpenOptions()
    {
        mainCanvasGO.SetActive(false);
        optionsCanvasGO.SetActive(true);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(false);
        creditsCanvasGO.SetActive(false);


        EventSystem.current.SetSelectedGameObject(optionsFirst);


    }

    private void OpenController()
    {
        mainCanvasGO.SetActive(false);
        optionsCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(true);
        keyboardCanvasGO.SetActive(false);
        creditsCanvasGO.SetActive(false);

        EventSystem.current.SetSelectedGameObject(controllerFirst);


    }

    private void OpenKeyboard()
    {
        mainCanvasGO.SetActive(false);
        optionsCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(true);
        creditsCanvasGO.SetActive(false);

        EventSystem.current.SetSelectedGameObject(keyboardFirst);


    }

    private void OpenCredits()
    {
        mainCanvasGO.SetActive(false);
        optionsCanvasGO.SetActive(false);
        creditsCanvasGO.SetActive(true);

        EventSystem.current.SetSelectedGameObject(creditsFirst);
    }

    #endregion

    #region Main Menu Actions

    public void OnOptionsPress()
    {
        OpenOptions();
    }

    public void BackToMain()
    {
        OpenMainMenu();
    }

    public void OnCreditsPress()
    {
        OpenCredits();
    }

    #endregion

    #region Settings Menu Actions

    public void OnControllerPress()
    {
        OpenController();
    }

    public void OnKeyboardPress()
    {
        OpenKeyboard();
    }

    #endregion
    #region Close Application
    public void QuitGame()
    {
        Application.Quit();
    }
    #endregion

}
