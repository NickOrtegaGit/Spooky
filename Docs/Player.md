# Player

## Prefab

`Assets/Prefabs/Player.prefab`

| Component | Settings |
|---|---|
| SpriteRenderer | green square placeholder |
| Rigidbody2D | Dynamic, Gravity Scale 0, Freeze Rotation Z |
| BoxCollider2D | default |
| NetworkObject | — |
| NetworkTransform | Authority Mode: Server |
| PlayerMovement | moveSpeed 5 |

Assigned to `NetworkManager -> NetworkConfig -> PlayerPrefab`. Must **not**
exist in the scene — NetworkManager spawns it at runtime. A copy left in the
scene causes duplicate players.

## Movement — server authoritative

`Assets/Scripts/PlayerMovement.cs`

Flow: client reads input -> `[Rpc(SendTo.Server)]` -> server simulates
physics in `FixedUpdate` -> `NetworkTransform` replicates position to all
clients.

Key details:

- `IsOwner` guard in `Update` — without it, one keyboard moves every player
- Input sent only **on change**, not every frame
- Non-server copies set to Kinematic in `OnNetworkSpawn` so two physics
  simulations don't fight over the same object
- Uses `Keyboard.current` from the new Input System — see [[Tech Stack]]

Expected: slight input lag on your own cube. That is the server-authoritative
design working, not a bug. Client-side prediction would fix it; not needed yet.

## TODO

- [ ] Spawn points — everyone currently spawns at `0,0,0` and overlaps
- [ ] Real sprite instead of the placeholder square
- [ ] Interaction range / "use" input for [[Tasks]]
