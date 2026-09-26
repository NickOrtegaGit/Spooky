# Session Flow

How a player gets from launching the game to standing in the house.

```
Host:  MainMenu -> Character -> Van -> House
Join:  MainMenu (enter code) -> Character -> Van -> House
```

## The fiction

You work for **Oddjob Inc**, a company that does literally anything a
customer calls about — babysitting, moving, finding lost things, the dishes.
That is why you are in a stranger's house doing chores. Your boss, **Mr.
Magee**, drives the van to each job.

The lobby *is* the van ride. See [[#Van]].

## MainMenu

Two buttons: **Host** and **Join**. No single/multiplayer split — playing solo
is hosting and pressing Continue at 1/4. (That still goes through Relay, so it
needs internet. Offline play can be added later without touching the rest.)

- Host goes straight to Character. Nothing connects.
- Join opens a code box. The code is **checked with Relay** before moving on,
  so a typo is caught before you pick a name. Wrong code stays on the menu
  with "No job found for that code."

Anything that sends a player back here (refused join, host left) leaves a
message in `PlayerProfile.MenuMessage` for the menu to show.

## Character

Name field at the top, skin preview in the middle with arrows either side,
Continue in the bottom right. Visual reference: a single warm spotlight cone
on the character against black, pixel arrows.

- Arrows can be clicked, or cycled with ←/→ and A/D. **Keys are ignored while
  the name field is focused** — otherwise typing "Dave" cycles skins twice.
- The skin showing *is* the selection. No confirm step.
- Name is capped at 12 characters (it has to fit over a van seat) and
  remembered between launches, with the skin, via PlayerPrefs.
- Esc goes back to the menu.
- Skins live in a `SkinCatalog` ScriptableObject (Create > Spooky > Skin
  Catalog). One entry today; skins 2–4 are just more entries. **Order
  matters** — the index is what gets sent over the network.

### Why nothing connects until Continue

With scene management on, NGO syncs a client into the host's active scene
the moment it connects. Connecting at the menu would drag a joining client
past this screen straight into the van. So Character is a plain local scene,
and Continue is where the session starts:

- Host: create Relay allocation -> `StartHost()` -> load Van
- Join: join Relay **again** (the menu's check allocation may have expired
  while picking a skin) -> `StartClient()` -> NGO loads the host's van

A side effect: the join code only exists once the host reaches the van.
Nobody can join a host still picking a skin, which is correct.

## Van

A see-through van driving, Mr. Magee at the wheel, four seats in the back.
The join code is painted on the side as if it were a slogan:
"ODDJOB INC — CALL US — CODE: XXXXXX".

- **Same seating on every screen.** Host is seat 1, joiners fill the lowest
  free seat, and you see everyone. Your own seat is marked.
- Players pop into their seat on join; leaving frees it.
- Continue (bottom right) is **host-only**, with an "x/4 joined" count.
  Clients see the count and a waiting message.
- "Driving" is faked: the van stays put while the road and background scroll,
  wheels spin, slight bob. Loops forever, costs nothing.

Continue loads the house for everyone. A future scene of arriving at the
house goes between the two.

## House

The server spawns each player object only once every client has the house
loaded (`OnLoadEventCompleted`), then fires `SessionManager.HouseReady` —
which is what `MonsterSpawner` and `DoorSpawner` listen for. They used to
hook `OnServerStarted`, which now fires back in the menu, before the house
exists.

Joins are refused once the house loads.

## Pieces

| Piece | Where | Job |
|---|---|---|
| `SessionManager` | NetworkManager object, MainMenu | Approval, scene flow, player spawning, return to menu |
| `RelayConnectionManager` | NetworkManager object, MainMenu | Code check, host, join |
| `PlayerProfile` | static | Intent, pending code, name, skin, menu message |
| `SkinCatalog` | asset | The selectable looks |
| `MainMenuUI` / `CharacterSelectUI` | their scenes | The two local screens |
| `VanPlaceholderUI` | Van | Code, count, host Continue — until real seats |
| `PlayFromMainMenu` | Editor | Play always starts from MainMenu (Tools > Spooky) |

`SessionManager` also destroys the duplicate NetworkManager object that
MainMenu brings back on every return trip; NGO keeps the original alive
across scenes on its own.

## Build order

1. **Flow** — menu, working character screen, placeholder van, spawner fixes
2. **Seats** — replicated seat list, name + skin sent in the connection
   request (`ConnectionData`), host-only Continue, refused joins
3. **Art** — the van, Mr. Magee, the scroll; character screen polish
