using UnityEngine;

// sits in the scene on its own object (or on the HUD object)
// polls until both players are found, then calls HealthBarUI.InitialiseHUD
// this is the clean alternative to passing references at runtime from GameManager
// drag in your HealthBarUI reference and avatar sprites in the inspector

public class HealthBarInitialiser : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HealthBarUI healthBarUI;

    [Header("Character Info")]
    [SerializeField] private string p1Name   = "Player 1";
    [SerializeField] private string p2Name   = "Player 2";
    [SerializeField] private Sprite p1Avatar;
    [SerializeField] private Sprite p2Avatar;

    private bool initialised = false;

    // -------------------------------------------------------

    private void Update()
    {
        if (initialised) return;

        // keep trying each frame until both players are in the scene
        var controllers = FindObjectsByType<MultiplayerPlayerController>(FindObjectsSortMode.None);

        PlayerHealth p1Health = null;
        PlayerHealth p2Health = null;

        foreach (var c in controllers)
        {
            if (c.PlayerID == 1) p1Health = c.GetComponent<PlayerHealth>();
            if (c.PlayerID == 2) p2Health = c.GetComponent<PlayerHealth>();
        }

        if (p1Health == null || p2Health == null) return;   // not both spawned yet

        // both found — initialise and stop checking
        healthBarUI.InitialiseHUD(p1Health, p2Health, p1Avatar, p2Avatar, p1Name, p2Name);
        initialised = true;

        Debug.Log("[HealthBarInitialiser] both players found — HUD initialised");
    }
}
