# House Layout

## v1 — hand-built

1-2 hand-built layouts using Unity's Tilemap. Non-procedural. Enough for the
monster to patrol and tasks to be placed in.

Needs: walls with colliders, rooms, connecting hallways.

## v3 — procedural, deterministic

The hard constraint: **every client must see an identical layout.**

The host generates from a seed and shares either the seed or the generated
layout data. Layouts **cannot** generate independently per client — floating
point and iteration-order differences cause divergence, and divergence means
players walk through walls that exist only on someone else's screen.

Safer default: host generates, then sends the resulting layout data. Sharing
only the seed requires the generation code to be perfectly deterministic
across machines, which is easy to get subtly wrong.

## Feeds into

- [[Monster AI]] — patrol routes and pathfinding grid come from the layout
- [[Tasks]] — task placement needs valid rooms
