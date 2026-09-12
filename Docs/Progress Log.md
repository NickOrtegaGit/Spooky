# Progress Log

Newest first.

## 2026-09-11 — v1 networking foundation working

Two local instances, host-authoritative movement, verified end to end.

- Unity 6000.3.7f1, URP 2D template
- NGO 2.13.2 + Transport 2.6.0 installed
- NetworkManager + UnityTransport wired (127.0.0.1:7777)
- Player prefab with NetworkObject + NetworkTransform + PlayerMovement
- Server-authoritative movement: client input -> RPC -> host physics ->
  replicated position
- `NetworkHUD` for Host/Client/Server buttons
- Git repo initialized, first commit `646c210`

Detail in [[Networking]] and [[Player]].

### Problems hit, and fixes

- **Project created with the plain 2D template**, not URP. Deleted and
  recreated with `Universal 2D`.
- **`NetworkTransport` field silently null** — it does not auto-populate.
  Assign it manually or `StartHost()` fails at runtime.
- **Scripts written outside the Editor aren't noticed** until Unity regains
  focus (or Assets -> Refresh).
- **`Input.GetAxisRaw` throws** — project is new Input System only. See
  [[Tech Stack]].
- **Stale build holding UDP 7777** caused `transport start failure` on the
  next host. Always fully quit the build first. See [[Networking]].

## 2026-09-11 (later) — spawn points + first room

- `SpawnManager` assigns positions round-robin; server-only, set in
  `OnNetworkSpawn` before `NetworkTransform`'s first update so clients never
  see a snap from origin. Four points at (+/-2, +/-2).
- First tilemap room painted, 16 PPU. Wall collision verified on **both**
  host and client.

Gotcha: Unity 6 replaced `Used By Composite` with a **Composite Operation**
dropdown — see [[House Layout]].

## 2026-09-11 (later still) — monster AI

Host-authoritative patrol -> chase, verified in sync across both instances.

- `MonsterAI` state machine, server-only `FixedUpdate`, state in a
  `NetworkVariable`
- Line-of-sight detection against the `Walls` layer
- `MonsterSpawner` spawns it on `OnServerStarted` (host only)
- `IPathfinder` seam so A\* drops in later — see [[Monster AI]]

Gotchas hit:

- Prefab conversion nulled the scene waypoint references. Fixed with a
  `PatrolRoute` scene singleton.
- Monster spawning at origin sat on top of the players and instantly chased,
  so patrol was never visible. Moved its spawn to a corner.
- Code-spawned prefabs must be registered in `DefaultNetworkPrefabs.asset`.
  The Player prefab is exempt — `NetworkConfig.PlayerPrefab` covers it.

## 2026-09-12 — caught state

Monster contact now marks players caught and immobilizes them. See
[[Player]].

Deliberately **not** deciding the real penalty yet — respawn, spectator, and
revive all build on this same replicated flag, so it is foundation, not a
placeholder.

Gotcha: the Monster prefab was missing `NetworkTransform`. It replicated its
*existence* (collider worked, catches fired) but never its *position* — so on
clients it sat at origin while the host's patrolled. `NetworkObject`'s
`SynchronizeTransform` permits transform sync; `NetworkTransform` is what
actually sends it. **Any networked object that moves needs both.**

## 2026-09-12 (later) — player animation

Four-directional idle/walk, replicated. See [[Player]].

Gotchas: Aseprite needed Animated Sprite import mode; blend tree Y parameter
silently defaults to the X parameter; Animator params must be driven from
NetworkVariables or only your own character animates.

## Next

- [ ] Task objects with networked completion ([[Tasks]])
- [ ] Decide the real catch penalty once mechanics are fleshed out
- [ ] Monster patrol -> chase ([[Monster AI]])
- [ ] Task objects with networked completion ([[Tasks]])
