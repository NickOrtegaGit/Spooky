# Monster AI

Not yet built. Planned for v1 — see [[Scope Plan]].

## Requirements

**Host-authoritative.** The host runs the state machine and replicates the
result, so every client sees the same monster in the same place doing the
same thing. Clients never simulate the monster independently. See
[[Architecture]].

## v1 states

Keep it to two. Resist adding more until these feel right.

- **Patrol** — follow a fixed route through the house
- **Chase** — move toward a detected player

Transition: patrol -> chase on detection; chase -> patrol on losing the
player.

## Pathfinding

Unity NavMesh is 3D-first and awkward for 2D top-down. Plan is A* or
grid-based pathfinding over the tilemap. Decide once [[House Layout]] exists
— the layout shape drives the pathfinding choice.

## Prior art

BABEL used enemy/boss AI state machines. Same pattern applies; the new part
is that state lives on the host and replicates, rather than running locally.

## Open questions

- Detection: line of sight, radius, or both?
- What does catching a player do? (No death/spectator system designed yet.)
- Does the monster path around walls in v1, or patrol a hand-placed route?
