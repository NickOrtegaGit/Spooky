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
