# Monster AI

Built for v1. Patrol -> chase working, verified across host and client.

## Requirements

**Host-authoritative.** The host runs the state machine and replicates the
result, so every client sees the same monster in the same place doing the
same thing. Clients never simulate the monster independently. See
[[Architecture]].

## v1 states — built

`Assets/Scripts/Monster/MonsterAI.cs`

- **Patrol** — walks `PatrolRoute` children in order, looping
- **Chase** — moves toward the detected player

State lives in a `NetworkVariable<State>` so clients can drive audio and
animation off it later without extra RPCs.

### Detection

- `detectionRadius` 4 — enter chase
- `loseInterestRadius` 9 — leave chase

The gap between them is deliberate **hysteresis**. Equal values make a player
at the boundary flicker between states every frame.

- Line of sight: `Physics2D.Raycast` against the `Walls` layer (layer 6).
  Walls genuinely hide players — the core hiding mechanic.

### Tuning

Select the Monster prefab to see gizmos: yellow = detection radius, orange =
lose-interest radius, cyan = patrol route.

## Pathfinding — deliberately deferred

Currently `DirectPathfinder`: walk straight at the target. Correct in one
open room, wrong as soon as the house has concave geometry.

**A\* is a swap-in, not a rewrite**, because movement goes through
`IPathfinder.GetNextStep()` — the single place direction is computed. Adding
A\* means one new class implementing that interface; the state machine,
networking, and detection are untouched.

Deferred because in a single open room, correct A\* and broken A\* look
identical — it would be untestable code. Build it when [[House Layout]] has
multiple rooms to path around.

Keep the seam honest: never compute `(target - pos).normalized` outside
`MoveToward()`.

## Prior art

BABEL used enemy/boss AI state machines. Same pattern applies; the new part
is that state lives on the host and replicates, rather than running locally.

## Gotcha: prefabs cannot reference scene objects

The waypoint array was originally `[SerializeField] Transform[]` assigned in
the scene. Converting the Monster to a prefab **silently nulled every entry**
— a prefab asset lives on disk and cannot hold references to objects that
exist only in a scene.

Fix: `PatrolRoute` is a scene singleton the monster looks up in
`OnNetworkSpawn`. Same pattern as `SpawnManager` for [[Player]] spawns.

Watch for this with any prefab that needs scene data.

## Possible: noise attraction

**Not decided.** One candidate for coupling the monster to the task loop, so
the two systems create tension instead of running in parallel. Would need an
`Investigate` state — moving toward a position without having seen a player.

Undecided: noise radius, whether task types differ in loudness, whether
failure is louder. Likely a third state (**Investigate**) that moves toward a
noise position without having seen a player.

## Open questions

- **What happens when the monster catches a player?** Nothing currently — it
  just overlaps and shoves. Options: respawn, spectator, or downed+revivable.
  Needed before v1 is really playable.
- Should chase have a memory/investigate state (move to last known position)
  rather than instantly giving up?
