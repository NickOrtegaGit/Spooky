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

## 2026-09-19 — VHS effect, doors, overhead fade

Three visual systems, all verified working.

**VHS / cassette-tape distortion.** A fullscreen pass whose strength is driven
by gameplay — baseline, plus monster proximity, plus caught, plus in-task.
Distortion becomes a tell, not just a look. Local per client. See [[Shaders]].

**Doors.** Painted into the tilemap rather than placed: frame tiles in Walls,
a marker tile on a Markers layer, and a networked leaf the host spawns at each
marker. Server-authoritative open state with a replicated swing direction, so
a door shuts the way it opened. See [[House Layout]].

**Overhead fade.** Wall tops on their own tilemap fade tile-by-tile around the
local player, so walking behind something never hides you from yourself. The
monster deliberately does **not** trigger it — hiding places have to keep
working. Takes a list of layers. See [[House Layout]].

### Problems hit, and fixes

- **Tiles ship with `TileFlags.LockColor`.** `SetColor` does nothing, with no
  error, and `GetColor` reads back the value you wrote because the tilemap
  caches it. Cost most of a session. Full writeup in [[House Layout]] —
  worth reading before any future tint work.
- Unity **rewrites asset files from memory** while the project is open, so an
  external `sed` silently vanished. Quit Unity before editing assets on disk.
- `Blit.hlsl` is in **core**, not universal.
- A component that overwrites an Inspector value at runtime fights the
  Inspector. `Door` originally hardcoded sorting order, so setting it in the
  Inspector appeared to do nothing; it now offsets from the authored value.
- Aseprite frames of differing size each center on their own pivot, so a door
  appears to slide as it opens. Pivot every frame on the hinge instead.
- `AnimatedTile` cannot do triggered animation — it loops on the tilemap's own
  clock with no per-tile state. Animated tilemap objects need to be real
  GameObjects.
- **`Mesh Type: Tight` crops each animation frame to its used pixels**, so
  frames differ in size and the sprite drifts as it plays. Full Rect fixes it.
  Changing the pivot afterward invalidates both the tile's `leafOffset` and
  the prefab's collider offset.
- **Interaction range was measured from `transform.position`**, which for a
  bottom-hinged door sits below the doorway. The prompt appeared and the
  server silently rejected the RPC — no error, no log. Now measured to
  collider bounds in both places. See [[House Layout]].

## 2026-09-20 — vertical doors

Doors now work in both orientations. `Door.orientation` switches which axis
decides the swing, so one script covers both; each orientation brings its own
art, animator controller and prefab. `SwingUp` became `SwingPositive` — up for
UpDown doors, right for LeftRight ones.

Each `DoorTile` now names its own prefab, so one spawner on one Markers
tilemap handles every door type.

Also added: closing a door pushes anyone standing in it clear, along the axis
they walk through the door on. Physics depenetration was exiting by shortest
overlap, which wedged players into the wall beside a vertical door. See
[[House Layout]].

### Problems hit, and fixes

- **A sprite dragged into a Tile Palette becomes a plain `Tile`**, not the
  custom tile type — identical to look at, silently skipped by the spawner.
  Second time this has cost a session. `DoorSpawner.logScan` now prints each
  painted cell's real type.
- Renaming the animator parameter broke the existing horizontal controller
  until it was renamed to match. Animator parameters are matched by string.

## Next

- [ ] Real minigames on the panel, starting with the typewriter
      ([[Typewriter]])
- [ ] Set **Player Layers** on both door prefabs — it defaults to Everything,
      so the close-push currently sweeps the monster too ([[House Layout]])
- [ ] Verify the monster stays hidden behind overhead art while the local
      player is inside a faded region ([[House Layout]])
- [ ] Decide the real catch penalty once mechanics are fleshed out
- [ ] Free a task when its occupant **disconnects** mid-minigame — hook
      `ServerReleaseIfOccupiedBy` to `OnClientDisconnectCallback` ([[Tasks]])
- [ ] Replace the OnGUI HUD with a real menu; wire up Lobby ([[Networking]])
