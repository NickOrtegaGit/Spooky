# Gameplay Loop

The shape of a full round, start to finish. Most of this is **not built** —
see [[Scope Plan]] for what lands when.

## The round

1. Players enter the house through a **grand entrance doorway**
2. The entrance **locks behind them**
3. Each player has a **to-do list** shown on a HUD
4. Players split up (or stay together) to find and complete tasks
5. A **time limit** pressures them (added later)
6. The **monster** patrols; noise from tasks draws it
7. When **all tasks are done**, the entrance unlocks
8. Players **escape through the entrance** — that is the win

Lose condition: caught. What "caught" costs is still undecided — see
[[Player]].

## Finding tasks — deliberately not marked

Tasks are described, not waypointed. The list says *what* to do; the player
works out *where*.

> "Cook something" -> go find the kitchen, look for the stove

This is the exploration mechanic. Marking tasks on a map would delete it.

Implication for [[House Layout]]: **rooms must be legible as rooms.** A
kitchen has to read as a kitchen from its props alone. Procedural generation
in v3 has to preserve that — random rooms full of random furniture would
break the core loop.

## Interaction

- Walk near an interactable -> **popup above the player's head**
- The object **highlights** — white outline animation, or flash the whole
  sprite white. See [[Shaders]].
- Press the interact key to begin

## Tasks are minigames, not progress bars

The point: tasks are **time-consuming nonsense**. Comedy from tedium, tension
from being vulnerable while doing them.

Example — **the typewriter**: a letter is already written on screen and the
player types it out. **One spelling mistake and you start over**, because it
is a typewriter and you cannot backspace. Silly, infuriating, perfect.

Design rules this implies:

- A task takes long enough that the monster arriving is a real threat
- The player is **committed** while doing it — that is the vulnerability
- Failure costs time, not the run
- Each task is its own little game, not a reskinned timer

## Noise draws the monster

Doing a task makes noise. The monster is drawn toward it.

This is what makes the loop tense rather than parallel: you are not just
avoiding the monster while doing tasks, **the tasks are what summon it.**
Speed becomes a real tradeoff against safety.

Exact mechanic undecided — noise radius, noise per task type, whether
different tasks are louder. See [[Monster AI]].

## Escaping a chase — later

Players need ways to break line of sight or slow the monster:

- **Closing / opening doors** — block the path, buy seconds
- Other options TBD

Deliberately deferred. Chase counterplay only matters once tasks and noise
exist to trigger chases.

## What this needs, roughly in order

- [ ] Interactable object base + proximity popup ([[Tasks]])
- [ ] One real minigame — the typewriter is the reference
- [ ] HUD to-do list, networked so everyone sees shared progress
- [ ] Multiple rooms that read as their function ([[House Layout]])
- [ ] Noise emission from tasks, monster attraction ([[Monster AI]])
- [ ] Locked entrance that unlocks on completion
- [ ] Time limit
- [ ] Doors that open/close, for chase counterplay
