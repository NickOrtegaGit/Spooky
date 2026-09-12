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

## Next

- [ ] Spawn points — players currently overlap at `0,0,0`
- [ ] Walls + a hand-built room ([[House Layout]])
- [ ] Monster patrol -> chase ([[Monster AI]])
- [ ] Task objects with networked completion ([[Tasks]])
