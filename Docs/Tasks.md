# Tasks

The interaction system and the minigame framework are built. What is missing
is real minigames — the panel currently runs a placeholder timer.

## Built

`Assets/Scripts/Tasks/`

- **`Interactable`** — base class. Proximity highlight via a
  `MaterialPropertyBlock` driving the `Spooky/SpriteFlash` shader.
  `Interact()` is virtual; minigames override it.
- **`TaskObject : Interactable`** — completion in a `NetworkVariable<bool>`,
  set server-side. Also holds `occupantClientId` (a `NetworkVariable<ulong>`,
  `ulong.MaxValue` meaning free), so a task can only be worked by one player
  at a time. `Interact` claims the task and RPCs the *starting client only*
  to run the minigame; `ReportFailServerRpc` / `ReportCompleteServerRpc` come
  back the other way. A completed task disables its collider, so it stops
  being detectable at all.
- **`PlayerInteractor`** — owner-side `OverlapCircleAll` on the
  `Interactable` layer, E to interact. **The server re-checks range** in the
  RPC, so a client cannot claim to use a task across the map.
- **`TaskTracker`** — counts completions, replicated. Needs its **own**
  GameObject with a NetworkObject: NGO forbids NetworkBehaviours on the
  NetworkManager object.

`Assets/Scripts/Minigames/`

- **`Minigame`** — abstract base. Local only. Subclasses call `Complete()` or
  `Fail()`; the runner handles the rest.
- **`MinigameRunner`** — plain MonoBehaviour singleton in the scene (no
  NetworkObject) holding the one minigame the local player is doing.
  `ForceExit()` bails out without success, for when the player is caught.
- **`PanelMinigame : Minigame`** — the shared presentation every task gets: a
  dark overlay fades in and a panel slides up from the bottom, then both
  reverse on the way out. `InputReady` gates input until the panel is up;
  `EndSequence(bool)` is the hook for a per-minigame flourish (the
  typewriter's shake) before the slide-down. **This is the seam real
  minigames plug into.**
- **`PanelTaskMinigame : PanelMinigame`** — the current placeholder. Sit on
  the panel 5 seconds uninterrupted to complete; **X** backs out. Visible
  countdown. Replaced per-task by real minigames.
- **`PanelTestMinigame`**, **`PlaceholderMinigame`** — earlier test harnesses,
  kept until the real minigames land.

### The panel

Visually the panel is the **FNAF camera-flip**: it slides up over the world,
you are committed while it is up, and dropping it returns you to play. Every
task shares that framing, so a new minigame only has to fill the panel.

### Why the highlight needed a shader

`SpriteRenderer.color` **multiplies** the texture, so setting it white
changes nothing (`white x art = art`). Making a sprite fully white requires
replacing RGB, not tinting — hence `Assets/Art/Shaders/SpriteFlash.shader`.
See [[Shaders]].

## v1 requirements

Small **fixed** task list. Randomization is v3, after procedural generation.

Each task needs:

- A networked object players can interact with
- Completion state replicated to all clients (NetworkVariable)
- Server-authoritative validation — the host decides a task is complete, not
  the client claiming it

## Task schema

Every task (except hypothetical fetch quests) starts the same way:

1. Player enters interact range
2. The object highlights (`Spooky/SpriteFlash`)
3. "Press E" prompt appears above the player's head
4. E starts the task

So a task is: **a GameObject with a sprite, a collider, a NetworkObject, and
a `TaskObject` subclass.** What differs between tasks is only what happens
after E — the minigame itself.

### The minigame is local

A minigame runs **entirely on the client doing it**. Only three things cross
the network:

- **start** — so the server knows this task is occupied
- **fail** — returns to normal play, task stays incomplete
- **complete** — server marks the task done, replicated to everyone

Keystrokes, progress, and UI state never replicate. This keeps minigames
cheap to write and means a new one needs no networking work beyond calling
those three.

## Settled design

See [[Gameplay Loop]] for the full round.

- **Interaction:** proximity + keypress. Popup above the player's head,
  object highlights white.
- **Duration:** each task is a **minigame**, not a hold-to-complete bar.
  Long enough that the monster arriving matters.
- **Discovery:** tasks are described, never waypointed. "Cook something"
  means find the kitchen yourself.
- **Win condition:** all tasks complete -> the entrance unlocks -> escape
  through it.
- **Noise:** doing a task attracts the monster. Tasks summon the threat.

### Built: the dish stack

`Assets/Scripts/Minigames/DishStack*.cs`. Stack plates without dropping one on
the table.

A plate slides across the top of the stack; space stops it and it falls a
short distance onto the pile. Land it lopsided and the stack tips. Slide speed
rises per plate, so it tightens as you go. **X** leaves at any time — progress
is lost and the next attempt starts fresh.

A **static base plate** spawns first so the opening plate has something to
land on; without it the first drop is an instant loss.

**The stage is real 2D physics**, living far from the house at (500, 500) on
its own `DishStage` layer, filmed by its own orthographic camera into a
RenderTexture the panel displays through a RawImage. UI space has no physics,
and hand-rolled tipping would not feel the same.

`DishStackStage` is a scene singleton for the same reason `PatrolRoute` is:
the panel is a prefab and cannot hold scene references.

Gotchas, all of which cost time:

- **Render features run on off-screen cameras too.** `RoomOcclusionFeature`
  and `VhsRenderFeature` were painting over the stage's RenderTexture — the
  stage sits outside every room, so occlusion blacked it out entirely. Both
  now skip any camera with a `targetTexture`. A `cameraType == Game` check is
  **not** enough; an off-screen camera is still a Game camera.
- The table needs **two colliders**: a solid one plates land on, and a
  separate trigger as the fail zone. One collider cannot do both — a trigger
  does not block, and `OnTriggerEnter2D` never fires on a non-trigger.
- Collider **Density** only matters with **Use Auto Mass** on the Rigidbody2D.
  Two colliders at different densities give a plate a center-weighted mass.
- A RenderTexture wants `R8G8B8A8_SRGB` and **Point** filtering. UNORM renders
  dark; bilinear renders soft.
- For crisp pixels the RawImage must be an **integer multiple** of the stage
  art: 64px art → 512 (8x). Anything else resamples unevenly.
- Unity's `Light` and `Light 2D` are different components — the 3D one does
  nothing to sprites. Already in [[Progress Log]]; hit again anyway.

### Reference minigame: the typewriter

See [[Typewriter]] for the full spec. It sets the tone for what a "task"
means here: slow, silly, and punishing enough that the monster arriving
matters.

## Settled

- **How minigame state is networked.** Decided as planned: the minigame is
  local, and only **start / fail / complete** cross the wire. Implemented in
  `TaskObject` + `MinigameRunner`.
- **Can two players work the same task at once?** No. `occupantClientId`
  locks the task while it is being worked.
- **Being caught cancels the task.** `PlayerState.SetCaught` RPCs the caught
  player's own client, which calls `MinigameRunner.ForceExit()` — the panel
  drops and the task frees up. No new code was needed in the minigame.

## Open questions

- Does failing a minigame make extra noise?
- A player who **disconnects** mid-task leaves it occupied forever.
  `TaskObject.ServerReleaseIfOccupiedBy` exists for this but nothing calls it
  on disconnect — hook it to `NetworkManager.OnClientDisconnectCallback`.
- `TaskTracker.TotalCount` is counted locally per client via
  `FindObjectsByType`, not replicated. Fine while every task is placed in the
  scene; it will drift the moment tasks spawn dynamically (v3).

## Why server authority matters here

A client that can self-report "task complete" can trivially cheat. Same
reasoning as movement in [[Player]] — validate on the host.
