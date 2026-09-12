# Networking

Model: **listen-server**. One player's instance is host (server + client).
See [[Architecture]].

## Current setup (v1)

- `NetworkManager` GameObject carries `NetworkManager` + `UnityTransport`
- Transport: `127.0.0.1:7777` — localhost only, correct for local testing
- `NetworkHUD.cs` — throwaway OnGUI Host/Client/Server buttons, replaced by
  a real menu in v2

## Testing two clients locally

Unity can only Play one instance, so: **standalone build = host, Editor =
client**.

1. Quit any running build (see gotcha below)
2. Build to a path **outside** the project folder
3. Launch build -> Host
4. Unity -> Play -> Client

## Gotcha: port already in use

`[Netcode] Host is shutting down due to network transport start failure of
UnityTransport!` almost always means a previous build instance is still
running and holding the port.

```bash
lsof -nP -iUDP:7777
```

Always fully quit the build before rebuilding or restarting a host.

## v2 — Relay + Lobby

Replaces direct IP connection with a join code. Does not change gameplay
logic. See [[Scope Plan]].
