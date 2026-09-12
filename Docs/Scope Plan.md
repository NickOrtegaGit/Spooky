# Scope Plan

Build in this order. Do not skip ahead.

## v1 — local multiplayer (current)

- 1-2 hand-built (non-procedural) house layouts
- One simple monster behavior: patrol -> chase, no complex states yet
- Small fixed task list
- Tested with 2 local clients (same PC/LAN), no internet-facing networking

Status: see [[Progress Log]]

## v2 — internet play

Add Unity Relay + Lobby so players connect over the actual internet with a
join code, instead of local network only.

Only affects *how players connect*, not how gameplay runs once connected —
which is why it is safe to defer. See [[Architecture]].

## v3 — procedural house

Procedural house generation, deterministic across clients.

Host generates from a seed and either shares the seed or the generated
layout data. Layouts **cannot** independently generate per-client or they
will diverge. See [[House Layout]].

## v4 — browser (stretch)

WebGL hosting via itch.io so players start/join from a website without
downloading a build.
