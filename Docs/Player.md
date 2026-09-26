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

## Sprint and stamina

**Shift** sprints. Sprinting is faster in a straight line but **commits you to
a heading** — the trade, not a straight upgrade.

`PlayerMovement` steers rather than snapping while sprinting: the needed
velocity change is split into the part **along** the current heading and the
part **across** it, and each approaches its target at its own rate.
`sprintTurnAcceleration` is lower than `sprintAcceleration`, so turning at
speed costs distance. Walking still sets velocity directly and turns on a dime.

`PlayerStamina` is server-authoritative for the same reason movement is — a
client reporting its own stamina could sprint forever, and sprinting away from
the monster is exactly what is worth cheating at. The value replicates with
`NetworkVariableReadPermission.Owner`, so **only you ever receive your own
stamina**; other clients cannot see it at all.

Running the bar to empty locks sprinting out until it refills, and waits
`exhaustedRecoveryDelay` before recovery even begins — the Breath of the Wild
penalty. Everything is tunable in the Inspector.

`StaminaBar` sits above the player's head like the interact prompt, fades in
when stamina drains and out once it is full. Owner-only, and it scales the
fill sprite's X — which requires the fill sprite's **pivot on its left edge**,
or it drains toward its middle.

Gotchas:

- **The animation switch cannot wait for the server.** Movement hides the
  round trip behind acceleration; an animation switch does not, so the owner
  drives `IsSprinting` from its own input and remote copies use the replicated
  value. The owner also gates on its own stamina, so it never shows a sprint
  it is not getting.
- **Uncheck Has Exit Time** on the walk/sprint transitions. With it on, the
  switch waits for the current walk cycle to finish. Third time an animator
  transition has caused a mystery delay in this project — see
  [[House Layout]].
- Sprint frames must import with the **same Mesh Type and pivot** as the walk
  frames, or the sprite jumps when the clip changes.

## Animation

Art: `Assets/Art/Players/P1/Player_1.aseprite` — 8 tags (`idle_` / `walk_`
x `up`/`down`/`left`/`right`), imported as clips by the Aseprite importer.

Controller: `Assets/Art/Players/P1/P1.controller`

- Parameters: `MoveX` (float), `MoveY` (float), `IsMoving` (bool)
- Two 2D Simple Directional blend trees, `Idle` and `Walk`, each with four
  motions at (0,-1) (0,1) (-1,0) (1,0)
- Transitions both ways on `IsMoving`, **Has Exit Time off** — leaving it on
  makes the character finish its cycle before responding, which feels laggy

`Assets/Scripts/PlayerAnimator.cs` drives it:

- `facing` and `isMoving` are **NetworkVariables written by the server**.
  Every client applies them in `Update`, so *all* players animate — a
  local-only implementation would leave remote players frozen, because
  `PlayerMovement.Update` returns early on `!IsOwner`.
- `facing` only updates while moving, so the character keeps facing the
  direction it last walked when it stops
- Velocity snaps to the dominant axis — the art has no diagonals

### Setup gotchas

- Aseprite import must be **Animated Sprite** mode, not Sprite Sheet, or no
  clips are generated from tags
- Blend trees default **both** parameter dropdowns to the same value; the
  second must be set to `MoveY` by hand
- Generated clips are read-only sub-assets. If Loop Time needs changing,
  duplicate the clip (Cmd+D) to get an editable standalone `.anim`

## Camera

Cinemachine 3.1.7. **Deliberately not networked** — every client runs its own
camera, and where you look is not game state.

`PlayerCameraBinder` runs on each client: the locally-owned player
(`IsOwner`) assigns itself as the `CinemachineCamera`'s Follow target in
`OnNetworkSpawn`. Host and client each follow their own character.

- Position Composer with damping ~0.5 on X and Y
- `ForceCameraPosition` on spawn so it does not glide in from origin
- The blue box and yellow dot in the Editor are Cinemachine gizmos — editor
  only, never in a build. Toggle them in the Game view's Gizmos dropdown.

TODO: `CinemachineConfiner2D` once the house is bigger, to stop the camera
showing the void past the walls.

## Caught state

`Assets/Scripts/PlayerState.cs`

`NetworkVariable<bool> isCaught`, set server-side by `MonsterCatch` on
contact. While caught the player is **immobilized** and tinted grey.

Enforced in two places on purpose:

- `Update` — owner stops reading input (avoids pointless RPC traffic)
- `FixedUpdate` — **server** zeroes velocity regardless. A modified client
  that ignores its own caught flag still cannot move.

**What being caught leads to is deliberately undecided.** Respawn, spectator,
and downed-and-revivable all build on this same flag — see the open question
in [[Monster AI]]. Decide once more mechanics are fleshed out.

## TODO

- [x] Spawn points — `SpawnManager`, four points, server-assigned
- [ ] Real sprite instead of the placeholder square
- [ ] Interaction range / "use" input for [[Tasks]]
