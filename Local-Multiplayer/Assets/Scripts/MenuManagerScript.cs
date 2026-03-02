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

        player.enabled = false;
        OpenMainMenu();
    }

    public void Unpause()
    {
        isPaused = false;
        Time.timeScale = 1f;

        player.enabled = true;

        MainCanvasGO.SetActive(true);
        CloseAllMenus();
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
