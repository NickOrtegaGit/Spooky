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

## v2 — Relay (built)

`Assets/Scripts/Networking/RelayConnectionManager.cs`

Host creates a Relay allocation and gets a 6-character join code; clients
join with that code. No port forwarding, works across the internet.

Flow: `UnityServices.InitializeAsync()` -> `SignInAnonymouslyAsync()` ->
`CreateAllocationAsync(maxPlayers - 1)` -> `GetJoinCodeAsync()` ->
`SetRelayServerData()` -> `StartHost()`.

### WebSocket-ready from the start

```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
    const string ConnectionType = "wss";   // browsers cannot do raw UDP
#else
    const string ConnectionType = "dtls";  // encrypted UDP, lower latency
#endif
```

A WebGL page served over HTTPS is **not allowed** to open an insecure
connection, so `wss` is mandatory for v4, not optional. Building this in now
means the browser target needs no connection-layer rework.

### Package gotchas (Unity 6)

- The standalone `com.unity.services.relay` / `com.unity.services.lobby`
  packages are **deprecated**. Use `com.unity.services.multiplayer` — having
  both installed is a hard conflict.
- The unified package defines its own `Allocation` types, so
  `new RelayServerData(allocation, type)` does **not** compile. Use the
  extension method: `allocation.ToRelayServerData(connectionType)`.
- `CreateAllocationAsync(maxPlayers - 1)` — the host does not consume a
  Relay connection slot.
- Relay and Lobby must be **enabled in the Unity Cloud dashboard**, or calls
  fail at runtime with an authorization error.

### Still to do

- Lobby (player list, names, ready state) — package installed, not used yet
- A real menu; `RelayHUD` is throwaway OnGUI
- ~~Test across two machines~~ — **done 2026-09-13, worked first try.**
  Two Macs on different networks, connected by join code, no port forwarding.
