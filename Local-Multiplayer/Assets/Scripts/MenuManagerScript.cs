using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class MenuManagerScript : MonoBehaviour
{
    [Header("Canvas's")]
    [SerializeField] private GameObject MainCanvasGO;
    [SerializeField] private GameObject menuCanvasGO;
    [SerializeField] private GameObject settingsCanvasGO;

    [Header("Scripts Disabled when paused")]
    [SerializeField] private MultiplayerPlayerController player;
    [SerializeField] private CombatSystem combat;
    [SerializeField] private PlayerHealth health;


    private bool isPaused;

    void Start()
    {
        MainCanvasGO.SetActive(true);
        menuCanvasGO.SetActive(false);
        settingsCanvasGO.SetActive(false);

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

        OpenMainMenu();
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

    private void OpenMainMenu()
    {
        MainCanvasGO.SetActive(false);
        menuCanvasGO.SetActive(true);
        settingsCanvasGO.SetActive(false);

    }

    private void CloseAllMenus()
    {
        menuCanvasGO.SetActive(false);
        settingsCanvasGO.SetActive(false);
    }
    #endregion
}
