# Typewriter

The first real minigame, and the reference for what a task feels like. **Not
built yet** — but the framework under it is. It subclasses `PanelMinigame`,
which already supplies the overlay, the slide-up panel, and the
`EndSequence(bool)` hook the failure shake goes in. See [[Tasks]].

## The loop

1. Player interacts with the typewriter (E) — see [[Tasks]] for the shared
   start flow
2. Screen darkens with a **local-only** overlay
3. A large paper sprite **slides up** onto the screen
4. A passage of text sits on the paper in near-transparent font
5. The player types it out; correct characters fill in solid
6. **One mistake and it fails** — a typewriter cannot backspace
7. On failure: the whole thing **shakes**, the paper slides back down, the
   player is returned to normal play
8. Failure is not permanent — interact again to restart from the beginning
9. On success: the paper slides down, the task registers as **complete**, and
   the typewriter is no longer interactable

## Why it works

The player is **committed** while typing. They cannot see much, cannot move,
and cannot react. That is the vulnerability [[Gameplay Loop]] wants — the
tension is not the typing, it is what might be walking toward you while you
do it.

Failure costs **time**, not the run. Restarting a long passage with a monster
nearby is the punishment.

## Content

Multiple passages, picked per attempt. Nonsensical short stories — the comedy
half of the horror-comedy. Authored as plain text, not generated.

Open: does a failed attempt re-roll to a different passage, or repeat the
same one? Repeating is more punishing and lets the player get faster at it.

## Networking

Per [[Tasks]], the minigame is **local to the player doing it**. The overlay,
the paper, the typing, the shake — none of it replicates.

Three things cross the wire: **start**, **fail**, **complete**.

All three are implemented — see [[Tasks]]. The typewriter inherits them and
needs no networking work of its own.

Implications, all handled:

- While one player is typing, others see them standing still at the
  typewriter. That is correct — and it is what makes a teammate watching for
  the monster valuable.
- A player caught mid-task is **kicked out of the minigame** — `SetCaught`
  drives `MinigameRunner.ForceExit()`. See [[Player]].
- The task is locked while occupied (`occupantClientId`), so two players
  cannot type the same passage at once.

## Open questions

- Is there a time limit per passage, or only the round timer?
- Does the monster approaching interrupt the minigame, or do you simply not
  notice it coming?
- How long should a passage be? Long enough to be a real risk, short enough
  that failing is not enraging. Needs playtesting.
