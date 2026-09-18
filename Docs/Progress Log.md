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

## 2026-09-12 (evening) — tasks + camera

**v1 scope is functionally complete.**

- Task interaction system: proximity highlight, E to interact, replicated
  completion. See [[Tasks]].
- `Spooky/SpriteFlash` shader — first real shader in the project. See
  [[Shaders]].
- Per-client Cinemachine camera. See [[Player]].

Gotchas:

- NGO refuses NetworkBehaviours on the NetworkManager GameObject — the
  TaskTracker needed its own object with a NetworkObject.
- Components were accidentally added to the `Floor` tilemap instead of the
  typewriter; worth verifying the Inspector header before adding.
- `SpriteRenderer.color` multiplies, so white tint is a no-op.

## 2026-09-12 (late) — Relay working

**v2 milestone.** Host creates a join code, client connects through Unity
Relay. Verified: allocation created, code issued, zero exceptions, nothing
bound to local 7777 — traffic really is going through Relay.

Built WebSocket-ready so the v4 browser target needs no rework. See
[[Networking]] for the package gotchas, which were the hard part.

## 2026-09-13 — Relay verified across machines

Sent a zipped Mac build to a second machine on a different network. Host
created a code, client joined, **worked first try.**

v2's core claim is now real: *playable over the internet with a join code.*

Distribution notes: zip with `ditto -c -k --sequesterRsrc --keepParent` (the
Finder's Compress can break the .app bundle). The build is unsigned, so the
recipient must **right-click -> Open**, not double-click.

## 2026-09-14 — lighting, prompts, items

- **Player lighting.** Global Light 2D down to ~0.08, Point Light 2D on the
  player. The darkness mechanic that defines the look. See [[Shaders]].
- **Interact prompt** above the player's head, fading in and out, unlit so it
  stays readable in the dark.
- **Full item system** with the flashlight as the first item. See [[Items]].

Gotchas:

- Unity's `Light` and `Light 2D` are different components; a 3D Point Light
  does nothing to 2D sprites.
- A light's `Target Sorting Layers` is per-light, not per-object — excluding
  characters to stop the beam backlighting the holder would also stop it
  lighting *other* players and the monster. Wrong trade.
- Anything meant to be read rather than seen in the world (prompts, future
  HUD) should use an **unlit** material so lighting cannot dim it.

## 2026-09-17 — the task panel

**Verified working.** Interacting with a task slides a panel up over the
world; the placeholder completes after 5 uninterrupted seconds, X backs out,
and the monster catching you drops the panel and frees the task.

The panel is the FNAF camera-flip: it covers the screen, you are committed
while it is up, and every task will share that framing. See [[Tasks]].

- `PanelMinigame` — shared overlay fade + slide, `InputReady` gate, and an
  `EndSequence(bool)` hook for per-minigame flourishes
- `PanelTaskMinigame` — the placeholder: visible countdown, X to leave
- `MinigameRunner` — scene singleton holding the one active minigame

Notes:

- The caught-player interrupt needed **no new code**. `PlayerState.SetCaught`
  already RPC'd the caught client to call `MinigameRunner.ForceExit()`;
  `OnEnd()` just has to stop the timer.
- `PanelMinigame` hides the panel at `shown + (0, -Screen.height)` — pixels,
  while `anchoredPosition` is in Canvas Scaler reference units. Correct at
  1080p, off elsewhere. Not yet hit in practice.
- TextMeshPro ships inside `com.unity.ugui` in Unity 6; no separate package.

## Next

- [ ] Real minigames on the panel, starting with the typewriter
      ([[Typewriter]])
- [ ] Decide the real catch penalty once mechanics are fleshed out
- [ ] Free a task when its occupant **disconnects** mid-minigame — hook
      `ServerReleaseIfOccupiedBy` to `OnClientDisconnectCallback` ([[Tasks]])
- [ ] Replace the OnGUI HUD with a real menu; wire up Lobby ([[Networking]])
