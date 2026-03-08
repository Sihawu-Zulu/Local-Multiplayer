using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuManagerScript : MonoBehaviour
{
    [Header("Canvas's")]
    [SerializeField] private GameObject MainCanvasGO;
    [SerializeField] private GameObject menuCanvasGO;
    [SerializeField] private GameObject settingsCanvasGO;
    [SerializeField] private GameObject controllerCanvasGO;
    [SerializeField] private GameObject keyboardCanvasGO;

    [Header("Scripts Disabled when paused")]
    [SerializeField] private MultiplayerPlayerController player;
    [SerializeField] private CombatSystem combat;
    [SerializeField] private PlayerHealth health;

    [Header("First Selected Options")]
    [SerializeField] private GameObject menuFirst;
    [SerializeField] private GameObject settingsFirst;
    [SerializeField] private GameObject controllerFirst;
    [SerializeField] private GameObject keyboardFirst;





    private bool isPaused;

    void Start()
    {
        MainCanvasGO.SetActive(true);
        menuCanvasGO.SetActive(false);
        settingsCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(false);

    }

    void Update()
    {
        if (MenuInputManager.instance.MenuOpenCloseInput)
        {
            if (!isPaused)
            {
                Pause();
            }
            else
            {
                Unpause();
            }
        }
    }

    #region Pause/Unpause Functions

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        DisableAllPlayers();

        OpenMenu();
    }

    public void Unpause()
    {
        isPaused = false;
        Time.timeScale = 1f;

        EnableAllPlayers();

        MainCanvasGO.SetActive(true);
        CloseAllMenus();
    }
    #endregion

    #region Disabling/Enabling Movement Script

    private void DisableAllPlayers()
    {
        MultiplayerPlayerController[] players =
            FindObjectsOfType<MultiplayerPlayerController>();

        foreach (var player in players)
        {
            player.enabled = false;

            CombatSystem combat = player.GetComponent<CombatSystem>();
            if (combat != null)
                combat.SetCombatEnabled(false);
        }
    }

    private void EnableAllPlayers()
    {
        MultiplayerPlayerController[] players =
            FindObjectsOfType<MultiplayerPlayerController>();

        foreach (var player in players)
        {
            player.enabled = true;

            CombatSystem combat = player.GetComponent<CombatSystem>();
            if (combat != null)
                combat.SetCombatEnabled(true);
        }
    }

    #endregion

    #region Canvas Activations/Deactivations

    private void OpenMenu()
    {
        MainCanvasGO.SetActive(false);
        menuCanvasGO.SetActive(true);
        settingsCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(false);

        EventSystem.current.SetSelectedGameObject(menuFirst);

    }

    private void OpenSettingsMenu()
    {
        MainCanvasGO.SetActive(false);
        menuCanvasGO.SetActive(false);
        settingsCanvasGO.SetActive(true);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(false);

        EventSystem.current.SetSelectedGameObject(settingsFirst);


    }

    private void OpenControllerMenu()
    {
        MainCanvasGO.SetActive(false);
        menuCanvasGO.SetActive(false);
        settingsCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(true);
        keyboardCanvasGO.SetActive(false);

        EventSystem.current.SetSelectedGameObject(controllerFirst);

    }

    private void OpenKeyboardMenu()
    {
        MainCanvasGO.SetActive(false);
        menuCanvasGO.SetActive(false);
        settingsCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(true);

        EventSystem.current.SetSelectedGameObject(keyboardFirst);

    }

    private void CloseAllMenus()
    {
        menuCanvasGO.SetActive(false);
        settingsCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(false);

        EventSystem.current.SetSelectedGameObject(null);

    }
    #endregion

    #region Menu Actions

    public void OnSettingsPress()
    {
        OpenSettingsMenu();
    }

    public void OnResumePress()
    {
        Unpause();
    }

    #endregion

    #region Settings Menu Actions

    public void CloseSettingsPress()
    {
        OpenMenu();
    }

    public void OnControllerPress()
    {
        OpenControllerMenu();
    }

    public void OnKeyboardPress()
    {
        OpenKeyboardMenu();
    }

    #endregion
}
