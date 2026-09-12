using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-only catch detection. Marks players caught on contact; what that
/// leads to is deliberately not decided here.
/// </summary>
public class MonsterCatch : NetworkBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision) => TryCatch(collision.collider);
    private void OnTriggerEnter2D(Collider2D other) => TryCatch(other);

    private void TryCatch(Collider2D other)
    {
        if (!IsServer) return;

        var state = other.GetComponent<PlayerState>();
        if (state == null || state.IsCaught) return;

        state.SetCaught(true);
        Debug.Log($"Monster caught player {other.name}.");
    }
}
