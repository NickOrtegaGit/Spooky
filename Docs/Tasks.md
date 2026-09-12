# Tasks

Not yet built. Planned for v1 — see [[Scope Plan]].

## v1 requirements

Small **fixed** task list. Randomization is v3, after procedural generation.

Each task needs:

- A networked object players can interact with
- Completion state replicated to all clients (NetworkVariable)
- Server-authoritative validation — the host decides a task is complete, not
  the client claiming it

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

A letter is pre-written; the player types it out. One mistake restarts it,
because a typewriter cannot backspace. Build this one first — it sets the
tone for what a "task" means here.

## Open questions

- How is minigame state networked? The minigame itself is probably local to
  the player doing it, with only **start / fail / complete** replicated.
  Needs deciding before the second minigame is written.
- Can two players work the same task at once?
- Does failing a minigame make extra noise?

## Why server authority matters here

A client that can self-report "task complete" can trivially cheat. Same
reasoning as movement in [[Player]] — validate on the host.
