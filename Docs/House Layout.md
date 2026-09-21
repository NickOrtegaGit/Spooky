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

### Doors

Doors are painted, not placed. A doorway is three things:

- **Frame** — 9 ordinary tiles painted into `Walls` as a 3x3. The two middle
  cells the leaf occupies must carry **no collider**, or the composite blocks
  the doorway permanently and the door can never open.
- **Marker** — one `DoorTile` painted on the frame's **center cell**, on a
  dedicated Markers tilemap with its renderer disabled. It draws nothing at
  runtime; it only marks a position, and names the prefab to spawn there.

  **Gotcha:** dragging a marker *sprite* into a Tile Palette makes Unity
  generate a plain `Tile` from it, which looks identical and is silently
  skipped by the spawner. Drag the **`DoorTile` asset** into the palette
  instead. This has now cost two debugging sessions; `DoorSpawner.logScan`
  prints each painted cell's actual type.
- **Leaf** — the moving panel, a networked prefab the host spawns.

`DoorSpawner` scans the Markers tilemap on `OnServerStarted` and spawns a
leaf per marker, the same host-spawns/clients-receive pattern as
[[Monster AI]]'s `MonsterSpawner`. The door prefab lives only in
`Assets/Prefabs/` and `DefaultNetworkPrefabs.asset` — never in the scene, or
you get duplicates.

**Why not `tileData.gameObject`?** `TileBase` can instantiate a prefab per
tile, which looks like the obvious answer. It instantiates **un-networked, per
client**, so a door one player opened would stay shut for everyone else. The
tile marks the position; the host spawns.

Because doors only exist while a session runs, empty doorways in the editor
are correct. It also means leaf alignment can only be checked by hosting —
hence `DoorTile.leafOffset` being a tunable field rather than computed.

`Door` subclasses `Interactable`, so it inherits the proximity highlight and
the server-validated E press from [[Tasks]]. Two bools replicate: `IsOpen`,
and `SwingPositive` for which way the leaf swung. The swing is decided once
from the opener's position and deliberately **not** touched on close, so a
door shuts the way it opened wherever the player stands.

Collision flips with the replicated bool, **not** when the animation ends.
Waiting for the animation would let a player walk into a door that looks open
locally but is still solid on the host.

#### Both orientations, one script

`Door.orientation` is `UpDown` or `LeftRight`, and everything axis-dependent
reads it: which coordinate decides the swing, and which way a player gets
pushed on close. `SwingPositive` means **up** for an UpDown door and **right**
for a LeftRight one — hence the axis-neutral name.

Each orientation needs its own **animator controller** (the clips differ) and
its own **prefab** (art, controller, `orientation`). They share the same
seven-state structure and the same two parameters.

Each `DoorTile` carries its own **door prefab** and `leafOffset`, so one
`DoorSpawner` on one Markers tilemap handles every orientation — the marker
decides which door goes in its frame. Both marker types paint onto the same
tilemap.

#### Pushing players clear

Closing a door on someone standing in it turns the collider solid under them.
Left to physics, depenetration exits by the **shortest overlap**, which for a
tall vertical door is up or down — straight into the wall.

So the close explicitly pushes players out, along the axis they walk
**through** the door on: up/down through an UpDown door, left/right through a
LeftRight one. Not along the doorway's span, which is where the wall is.

Server-side sweep, plus an RPC to the pushed player's own client: the owner
simulates its own movement, so a server-only transform change is overwritten
on the next input frame. The RPC also zeroes velocity, or they drift back in.

Set **Player Layers** on each door prefab — it defaults to Everything, which
would sweep the monster too.

### Overhead art

Wall tops and anything else that should draw over the player live on their own
tilemap: no colliders, sorted above the player. `OverheadFader` fades them
tile by tile in an ellipse around the **local player only** — the monster must
stay concealed behind overhead art or hiding places stop working. It takes a
list, so several overhead layers can share one fader.

Purely local presentation. Nothing replicates.

### Gotcha: animated sprites drift unless the frames are uniform

A door that slides around as it animates is an **import** problem, not an
animation one. Three settings have to agree:

- **Mesh Type: Full Rect**, not Tight. Tight crops every frame to its used
  pixels, so frames end up different sizes and each centers on its own pivot.
  The Sprite Editor shows this — the frame rectangles will not match.
- **Pivot Alignment** on the hinge, not a corner. For an up/down door that is
  Bottom Center.
- The Aseprite canvas the same size across all tags, with the hinge on the
  same pixel.

Changing the pivot moves the sprite relative to its transform, so
`DoorTile.leafOffset` **and** the prefab's collider offset both need retuning
afterward. Expect to redo them together.

### Gotcha: interaction range measured from the pivot

`PlayerInteractor` measures to an interactable's **collider bounds**, via
`ClosestPoint`, not to `transform.position`. A door hinged at its bottom edge
has its transform below the whole doorway, so a pivot-based check rejected
interactions from above while the client still drew the prompt — a silent
failure with no log anywhere.

Client detection and the server's re-check share the same helper so they can
never disagree. Any future interactable with an off-center pivot depends on
this.

### Gotcha: tiles ship with their color locked

**Tiles generated from a sprite sheet get `TileFlags.LockColor`**, which makes
`Tilemap.SetColor` do **nothing** — silently. No error, no warning, and
`GetColor` still reads back the value you just wrote, because the tilemap
caches it. Every fade looks like it is working while the screen never changes.

All 373 tiles in this project shipped locked. To check and clear:

```
grep -rh "m_Flags: [^0]" Assets --include="*.asset" | sort | uniq -c
sed -i '' 's/^  m_Flags: 1$/  m_Flags: 0/' Assets/Art/Tiles/*.asset
```

**Quit Unity first** — with the project open it holds the assets in memory and
writes its own copy back over the edit. Reimport afterward.

Clearing the lock is safe on shared tiles: it only makes a tile *capable* of
per-cell tint. Nothing tints it unless something calls `SetColor`, and only
`OverheadFader` does, only on the layers it is given.

New art drawn from a sprite sheet comes in locked again, so re-run the grep
when a fade mysteriously does nothing.

### Still needed

Multiple rooms, hallways, a second layout. Vertical (left/right) doors — the
current `Door` handles up/down swings only.

## v3 — procedural, deterministic

The hard constraint: **every client must see an identical layout.**

The host generates from a seed and shares either the seed or the generated
layout data. Layouts **cannot** generate independently per client — floating
point and iteration-order differences cause divergence, and divergence means
players walk through walls that exist only on someone else's screen.

Safer default: host generates, then sends the resulting layout data. Sharing
only the seed requires the generation code to be perfectly deterministic
across machines, which is easy to get subtly wrong.

## Note: task objects need to be findable, not fixed

[[Gameplay Loop]] has players finding tasks by exploring rather than
following a waypoint. That does **not** mean every object has a fixed home —
a computer could be in any of several rooms, and searching for it is the
point.

Procedural generation is a v3 stretch goal and explicitly **not a priority**.
If it happens, good; if not, hand-built layouts are fine.

## Feeds into

- [[Monster AI]] — patrol routes and pathfinding grid come from the layout
- [[Tasks]] — task placement needs valid rooms
