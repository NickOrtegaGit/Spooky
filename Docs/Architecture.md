# Architecture

## Netcode from day one — settled, do not relitigate

Build with Netcode for GameObjects (NGO) from the very first player-movement
prototype.

Do **not** build single-player-style scripts and add networking later. That
causes a near-total rewrite of core gameplay logic — movement, task
interaction, and monster AI all need host-authoritative patterns from the
start (NetworkVariables, RPCs, host-authoritative monster AI so all clients
see the same monster state).

## What can safely be deferred

Relay, Lobby, join codes, and internet-facing connection. These affect only
*how players connect*, not how gameplay logic runs once connected. Deferred
to v2 in [[Scope Plan]].

## Server authority

One player's instance acts as host/server (listen-server model). No
dedicated server.

The host simulates; clients send input and receive replicated state. Already
implemented for movement — see [[Player]] and [[Networking]].

## Determinism (v3)

Procedural generation must be seed-driven and shared, never generated
independently per client. See [[House Layout]].
