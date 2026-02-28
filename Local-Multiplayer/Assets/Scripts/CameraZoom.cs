using Unity.Cinemachine;
using UnityEngine;
public class CameraZoom : MonoBehaviour
{
    public CameraTargetController targetController;

    public float minZoom = 10f;
    public float maxZoom = 25f;

    public float minDistance = 5f;
    public float maxDistance = 30f;

    public float zoomSpeed = 5f;

    private CinemachineCamera cineCam;
    private CinemachineFollow follow;

    void Awake()
    {
        cineCam = GetComponent<CinemachineCamera>();
        follow = GetComponent<CinemachineFollow>();


    }

    void LateUpdate()
    {
        float distance = targetController.GetDistance();

        float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
        float targetZoom = Mathf.Lerp(minZoom, maxZoom, t);

        Vector3 currentOffset = follow.FollowOffset;
        Vector3 newOffset = new Vector3(
            currentOffset.x,
            currentOffset.y,
            Mathf.Lerp(currentOffset.z, -targetZoom, Time.deltaTime * zoomSpeed)
        );

        follow.FollowOffset = newOffset;
    }
}
