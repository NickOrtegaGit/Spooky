using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// On each client, the locally-owned player claims that client's Cinemachine
/// camera. Nothing here is networked — where you look is not game state, and
/// every client runs its own camera.
/// </summary>
public class PlayerCameraBinder : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        var cam = FindFirstObjectByType<CinemachineCamera>();
        if (cam == null)
        {
            Debug.LogWarning("No CinemachineCamera in the scene; camera will not follow the player.");
            return;
        }

        cam.Follow = transform;
        cam.ForceCameraPosition(transform.position, Quaternion.identity);
    }
}
