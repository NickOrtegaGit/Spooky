# Items

Things the player picks up, carries, and uses. **Not built yet.**

Items are deliberately **simple**. They are not an inventory system — they are
one object in one slot with one action.

## Rules

- **One item at a time.** No inventory, no slots plural. The constraint is the
  design — it forces choices about what to carry and what to leave.
- Items on the ground behave exactly like any other [[Tasks|interactable]]:
  walk into range, the prompt appears above the player's head, press E.
- **Swap on pickup.** Picking up a new item while holding one **drops the old
  one** and equips the new. No "inventory full" state to communicate.
- A **caught** player drops what they are carrying. See [[Player]].
- Items can be dropped and **picked up by anyone**, including teammates.

## Controls (placeholder bindings)

| Key | Action |
|---|---|
| `E` | Pick up (drops current item first if holding one) |
| `Q` | Drop |
| `Space` | Use |

All three are placeholders until there is a real input map.

## Worked example: the flashlight

1. Flashlight sits on the floor
2. Player walks into interact range -> prompt appears above their head
3. Press `E` -> flashlight is now in the player's item slot
4. `Space` uses it (toggles the light)
5. `Q` drops it where the player stands

The flashlight is the obvious first item because [[Shaders]] lighting already
works — a held Light 2D is most of the implementation.

## Items and tasks — undecided

Whether tasks require items ("find the key for the cabinet") is **not
decided.** Fetch objectives would promote exploring the map, which fits
[[Gameplay Loop]]'s find-it-yourself design.

Not hard to add later, so it stays open. Build items as a standalone system
first; wire them to [[Tasks]] only if that turns out to be wanted.

## Networking notes

Carried items are the first system here that needs **ownership transfer** —
everything so far has been server-owned or purely local.

Things to get right:

- Who holds what must replicate; every client has to see the flashlight in
  the right player's hands
- Drop position is **server-authoritative**, like everything else. A client
  does not get to decide where an item lands.
- The held/dropped transition must be atomic. Two players pressing E on the
  same item in the same frame must not both get it. **Decided: first request
  the server processes wins**; later requests see the item already held and
  are rejected. RPCs arrive in order, so this needs only an explicit
  already-held check, not a queue.
- Swap-on-pickup is two operations (drop old, take new) that should resolve
  as one on the server, or a caught edge case could leave a player holding
  nothing while the old item vanishes.

## Open questions

- Does using an item consume it, or is use repeatable?
- Do items have durability / battery? (A flashlight that dies is very
  on-theme, and very annoying — needs playtesting, not deciding now.)
- Can items be thrown, or only dropped at the player's feet?
