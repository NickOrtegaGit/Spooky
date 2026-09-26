# Noise

**Not built yet.** The monster currently only sees — it has line-of-sight
detection and nothing else. Hearing is the missing half, and several systems
already assume it exists.

## Why it matters

[[Tasks]] already states the design: *doing a task attracts the monster. Tasks
summon the threat.* That is the core tension of the loop and none of it works
until noise does. Right now a player can do every task in perfect safety as
long as they stay out of sight.

Noise is also what makes the house feel alive rather than a stealth puzzle —
a distant crash tells you where someone else is, and whether they are in
trouble.

## What should make noise

Almost everything. Roughly loudest first:

- **Failing a minigame** — the smash in [[Tasks]]' dish stack, a typewriter
  bell
- **Bursting a door open at a sprint** — see [[House Layout]]; louder than a
  normal door, and the reason this doc exists
- **Completing a task** — the machine finishing, not the player
- **Dropping an item** ([[Items]])
- **Opening or closing a door** normally — quiet
- **Sprinting footsteps** — quiet but continuous, which is a different shape
  of signal to a one-off bang
- **Walking** — very quiet, maybe nothing
- **Crouching or sneaking**, if that ever exists — nothing

## Shape of the system

Sketch, not a decision:

- A **`NoiseEvent`**: world position, loudness, and a timestamp.
- Something raises one; the monster is the only listener that matters for now,
  though other players hearing each other is worth considering later.
- **Loudness is a radius**, not a volume. A noise the monster is inside the
  radius of is heard; outside, it is not. Simpler than falloff curves and
  easier to reason about while placing tasks.
- The monster **investigates the loudest recent noise**, walking to its
  position rather than to the player. Arriving and finding nothing is the
  point — it should be foolable.
- Noises **expire**. A crash thirty seconds ago should not still be pulling
  the monster across the house.

Open: does the monster remember more than one noise? A queue would let it
sweep a route; a single most-recent would make it easy to lead around by the
nose. Probably one at a time, replaced by anything louder.

## Consequences to think about

- **This is what makes the monster foolable.** A player who can make noise
  deliberately — dropping something in the far room — can buy time for the
  team. That is a mechanic worth having, and it argues for at least one
  item that exists purely to be thrown.
- Server-authoritative, like everything else the monster reacts to. A client
  that can emit fake noises can move the monster at will.
- Noise needs an audible counterpart for players or it is invisible; the
  monster reacting to something the player never heard reads as a bug.

## Feeds into

- [[Monster AI]] — needs an investigate state, and a reason to leave its patrol
- [[Tasks]] — the noise a task makes is what makes doing it dangerous
- [[Items]] — a throwable that exists to make noise elsewhere
