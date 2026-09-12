# Art Pipeline

## Tools

Aseprite, same workflow as BABEL.

## Aseprite importer

`com.unity.2d.aseprite` 3.0.1 ships with the URP 2D template. `.aseprite`
files import directly into Unity — no PNG export step, and re-saving in
Aseprite updates the asset automatically.

## Pixel art settings

Not yet configured. When real sprites land, set on each texture:

- Filter Mode: **Point (no filter)** — otherwise pixel art blurs
- Compression: **None**
- Pixels Per Unit: pick one value and use it everywhere (16 or 32 typical)

Consider the Pixel Perfect Camera component for stable pixel rendering.

## Settled: 16 PPU

**16x16 pixel tiles = 1 Unity unit.** Use this everywhere. The player is 1
unit, so exactly one tile — convenient for the grid-based pathfinding in
[[Monster AI]].

## Current state

Placeholder art only:

- Green square on the [[Player]] prefab
- `Assets/Art/Tiles/wall_placeholder.png` — grey, dark 1px border
- `Assets/Art/Tiles/floor_placeholder.png` — dark checker

Both 16x16, imported at Point filter / no compression / 16 PPU. Swapping in
real Aseprite art is a one-field change on the Tile asset.

## Lighting

URP 2D gives 2D lights — worth it for a haunted house (flashlight cones,
darkness). This was the reason for choosing the URP template over built-in.
Not set up yet.
