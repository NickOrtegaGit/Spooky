# House Layout

## v1 — hand-built

1-2 hand-built layouts using Unity's Tilemap. Non-procedural. Enough for the
monster to patrol and tasks to be placed in.

### Built so far

One room, in `SampleScene`:

- `Grid` -> `Floor` (order 0) and `Walls` (order 1)
- Floor: 16x8, origin (-8,-4)
- Walls: 18x11, origin (-9,-6) — encloses the floor
- Walls carry `TilemapCollider2D` + `CompositeCollider2D` + static
  `Rigidbody2D`

Static walls need **no** `NetworkObject` — they are identical in the scene on
every client. Only things that move or change state need network identity.

### Unity 6 note

`TilemapCollider2D` no longer has a `Used By Composite` checkbox. It is now a
**Composite Operation** dropdown — set it to **Merge**. Without it, each tile
keeps its own collider and players catch on the seams.

### Still needed

Multiple rooms, hallways, a second layout.

## v3 — procedural, deterministic

The hard constraint: **every client must see an identical layout.**

The host generates from a seed and shares either the seed or the generated
layout data. Layouts **cannot** generate independently per client — floating
point and iteration-order differences cause divergence, and divergence means
players walk through walls that exist only on someone else's screen.

Safer default: host generates, then sends the resulting layout data. Sharing
only the seed requires the generation code to be perfectly deterministic
across machines, which is easy to get subtly wrong.

## Hard constraint: rooms must read as rooms

[[Gameplay Loop]] has players finding tasks by **inferring location from
description** — "cook something" means find the kitchen. That only works if
a kitchen looks like a kitchen.

This constrains v3 procedural generation more than it first appears: rooms
cannot be randomly furnished. Generation has to place *coherent* rooms, or
the core exploration mechanic breaks.

## Feeds into

- [[Monster AI]] — patrol routes and pathfinding grid come from the layout
- [[Tasks]] — task placement needs valid rooms
