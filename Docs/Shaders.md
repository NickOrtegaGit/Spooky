# Shaders

Notes for making Spooky look good. Nothing here is built yet.

## What a shader is

A small program that runs **on the GPU**, in parallel, once per pixel. The
part that matters for 2D is the **fragment shader**, which answers one
question millions of times a frame:

> For this one pixel, what color should it be?

Conceptually:

```
color = texture(sprite, uv)       // the artist's pixel at this spot
color.rgb *= vec3(1.0, 0.3, 0.3)  // math on it — here, tint red
return color
```

`uv` is the position on the sprite (0-1 across). Every effect — tint, glow,
dissolve, fog, ripple — is some variation of "look up a pixel, do math."

The constraint that follows from parallelism: **a pixel cannot see what any
other pixel decided.** No loops over neighbors, no shared state. Effects that
seem to need it (blur, outlines) work by sampling the texture multiple times
at offsets instead.

## Two ways to write them in Unity

| | Shader Graph | HLSL |
|---|---|---|
| Form | Visual node editor | Text code |
| Preview | Live as you build | Recompile to see |
| Ships with URP | Yes | Yes |
| Good for | Learning, most 2D effects | Full control, unusual work |

**Start with Shader Graph.** Seeing the result change as you wire nodes is
how the operations actually become intuitive. Graduate to HLSL once the
concepts are familiar — that is also the version worth showing someone who
asks whether you really know shaders.

## Highest impact for this game, in order

Darkness *is* the horror mechanic here, so lighting pays off more than any
individual effect.

### 1. URP 2D Lights (not a shader — a feature)

Already available; this is why URP was chosen over built-in. See
[[Art Pipeline]].

- **Global Light 2D** — already in the scene. Drop its intensity and the
  room goes dark. Do this first; it costs nothing.
- **Spot Light 2D** parented to the player — a flashlight cone.
- **Point Light 2D** on lamps, candles, task objects.

### 2. Vision / fog-of-war mask

Only show what is lit or in line of sight. Thematically exact for a
monster-hunting game, and it pairs with the raycast logic [[Monster AI]]
already uses for detection.

### 3. Post-processing via Volume

Vignette, chromatic aberration, desaturation when the monster is near. URP
does these through a **Volume** component — **no shader writing required**.
Cheap dread.

### 4. Sprite effects

Currently the caught state tints by setting `SpriteRenderer.color` (see
[[Player]]). A shader could do a proper white flash, an outline, or a
dissolve instead.

## Watch out: 2D lights vs. pixel art

Smooth gradient lighting over 16x16 pixel art can look muddy — the light has
more color resolution than the art does, so it washes out the deliberate
palette.

Known fixes:

- Palette-quantize the light output (snap lighting to N steps)
- Render lit at low resolution, then upscale
- Keep lights hard-edged rather than soft falloff

Worth deciding the look **before** building a lot of art around it.

## Scope warning

Shaders are a rabbit hole with no bottom. [[Architecture]] is explicit that
this project's differentiator is **networking**, not visuals — a beautiful
game with no working multiplayer misses the point of building it.

Budget accordingly: lighting and a Volume profile get most of the visual
payoff for very little time. Save deep shader work for after v2 Relay works.

## First session plan

1. Turn down the existing **Global Light 2D**, watch the room go dark
2. Add a **Spot Light 2D** to the Player prefab as a flashlight
3. Add a **Volume** with a vignette
4. *Then* open Shader Graph and make something simple pulse or flash

## Resources

- Unity's Shader Graph docs (bundled with the package — Window > Shader Graph)
- The Book of Shaders (thebookofshaders.com) — best intro to fragment shader
  thinking, not Unity-specific
