using UnityEngine;

public class CameraTargetController : MonoBehaviour
{
    private Transform player1;
    private Transform player2;

    public float playerDistance; //debug

    void Update()
    {
        // If players not assigned yet, keep trying
        if (player1 == null || player2 == null)
        {
            MultiplayerPlayerController[] players =
                FindObjectsOfType<MultiplayerPlayerController>();

            foreach (var p in players)
            {
                if (p.PlayerID == 1)
                    player1 = p.transform;
                else if (p.PlayerID == 2)
                    player2 = p.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (player1 == null || player2 == null) return;

        Vector3 midpoint = (player1.position + player2.position) / 2f;
        transform.position = midpoint;

        playerDistance = Vector3.Distance(player1.position, player2.position);
    }

    public float GetDistance()
    {
        return playerDistance;
    }






}
