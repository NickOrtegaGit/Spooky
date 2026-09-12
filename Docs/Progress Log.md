# Progress Log

Newest first.

## 2026-09-11 — v1 networking foundation working

Two local instances, host-authoritative movement, verified end to end.

- Unity 6000.3.7f1, URP 2D template
- NGO 2.13.2 + Transport 2.6.0 installed
- NetworkManager + UnityTransport wired (127.0.0.1:7777)
- Player prefab with NetworkObject + NetworkTransform + PlayerMovement
- Server-authoritative movement: client input -> RPC -> host physics ->
  replicated position
- `NetworkHUD` for Host/Client/Server buttons
- Git repo initialized, first commit `646c210`

Detail in [[Networking]] and [[Player]].

### Problems hit, and fixes

- **Project created with the plain 2D template**, not URP. Deleted and
  recreated with `Universal 2D`.
- **`NetworkTransport` field silently null** — it does not auto-populate.
  Assign it manually or `StartHost()` fails at runtime.
- **Scripts written outside the Editor aren't noticed** until Unity regains
  focus (or Assets -> Refresh).
- **`Input.GetAxisRaw` throws** — project is new Input System only. See
  [[Tech Stack]].
- **Stale build holding UDP 7777** caused `transport start failure` on the
  next host. Always fully quit the build first. See [[Networking]].

## 2026-09-11 (later) — spawn points + first room

- `SpawnManager` assigns positions round-robin; server-only, set in
  `OnNetworkSpawn` before `NetworkTransform`'s first update so clients never
  see a snap from origin. Four points at (+/-2, +/-2).
- First tilemap room painted, 16 PPU. Wall collision verified on **both**
  host and client.

Gotcha: Unity 6 replaced `Used By Composite` with a **Composite Operation**
dropdown — see [[House Layout]].

## Next

- [ ] Monster patrol -> chase ([[Monster AI]])
- [ ] Monster patrol -> chase ([[Monster AI]])
- [ ] Task objects with networked completion ([[Tasks]])
