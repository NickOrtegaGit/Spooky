# Tasks

Not yet built. Planned for v1 — see [[Scope Plan]].

## Built

`Assets/Scripts/Tasks/`

- **`Interactable`** — base class. Proximity highlight via a
  `MaterialPropertyBlock` driving the `Spooky/SpriteFlash` shader.
  `Interact()` is virtual; minigames override it.
- **`TaskObject : Interactable`** — completion in a `NetworkVariable<bool>`,
  set server-side. v1 completes instantly; a minigame replaces that.
- **`PlayerInteractor`** — owner-side `OverlapCircleAll` on the
  `Interactable` layer, E to interact. **The server re-checks range** in the
  RPC, so a client cannot claim to use a task across the map.
- **`TaskTracker`** — counts completions, replicated. Needs its **own**
  GameObject with a NetworkObject: NGO forbids NetworkBehaviours on the
  NetworkManager object.

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

## Open questions

- How is minigame state networked? The minigame itself is probably local to
  the player doing it, with only **start / fail / complete** replicated.
  Needs deciding before the second minigame is written.
- Can two players work the same task at once?
- Does failing a minigame make extra noise?

## Why server authority matters here

A client that can self-report "task complete" can trivially cheat. Same
reasoning as movement in [[Player]] — validate on the host.
