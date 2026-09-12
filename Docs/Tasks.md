# Tasks

Not yet built. Planned for v1 — see [[Scope Plan]].

## v1 requirements

Small **fixed** task list. Randomization is v3, after procedural generation.

Each task needs:

- A networked object players can interact with
- Completion state replicated to all clients (NetworkVariable)
- Server-authoritative validation — the host decides a task is complete, not
  the client claiming it

## Design questions

- Interaction input: proximity + keypress, or click?
- Does a task take time (hold to complete) or complete instantly?
- Shared progress bar, or per-task indicators?
- Win condition: all tasks done = escape?

## Why server authority matters here

A client that can self-report "task complete" can trivially cheat. Same
reasoning as movement in [[Player]] — validate on the host.
