# Tech Stack

Verified versions as of 2026-09-11.

| Piece | Version | Notes |
|---|---|---|
| Unity | 6000.3.7f1 | URP 2D template (`Universal 2D`) |
| URP | 17.3.0 | `com.unity.render-pipelines.universal` |
| Netcode for GameObjects | 2.13.2 | NGO 2.x — API differs from 1.x tutorials |
| Unity Transport | 2.6.0 | Pulled in automatically by NGO |
| Input System | 1.18.0 | New Input System **only** — see warning below |
| Aseprite importer | 3.0.1 | Ships with the 2D template |

## Input System warning

`activeInputHandler: 1` — the project uses the **new Input System package
only**. The legacy `UnityEngine.Input` API (`Input.GetAxisRaw`, etc.) throws
`InvalidOperationException` at runtime.

Use `UnityEngine.InputSystem` (`Keyboard.current`, etc.). Most tutorials use
the old API — translate before pasting. See [[Player]].

## NGO 2.x vs 1.x

Most online tutorials target NGO 1.x. Known differences:

- RPCs: `[Rpc(SendTo.Server)]` is current; `[ServerRpc]` with the mandatory
  `ServerRpc` name suffix is legacy
- `Rigidbody2D.velocity` renamed to `linearVelocity` in Unity 6

## Planned / not yet added

- Unity Relay + Lobby (v2, free tier: ~50 CCU / 6,000 connectivity hours per
  month — far above what this project needs)
- A* or grid-based 2D pathfinding (Unity NavMesh is 3D-first, not ideal for
  2D top-down) — see [[Monster AI]]

## Tools

- Aseprite for pixel art — same workflow as BABEL
- Git, GitHub (no LFS; 2D sprites are small enough)
