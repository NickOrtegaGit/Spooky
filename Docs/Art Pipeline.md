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

## Current state

Placeholder green square on the [[Player]] prefab. No real art yet.

## Lighting

URP 2D gives 2D lights — worth it for a haunted house (flashlight cones,
darkness). This was the reason for choosing the URP template over built-in.
Not set up yet.
