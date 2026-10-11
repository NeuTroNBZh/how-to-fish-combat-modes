# Game internals

Notes about How to Fish (1.1.3) that the mod depends on. They are useful when the game updates and something stops working.

## Items

- Spawnable item keys (`GameInfo.GetSpawnable(name)`) are prefab names without spaces, in lower case: `pistol`, `shotgun`, `smg`, `assaultrifle`, `sniperrifle`, `bow`, `knife`, `brassknuckles`, `dynamite`.
- Arrows and magazines are not spawnable items.
- `Item.Weapon` is `null` on prefabs; it is assigned at runtime.
- Giving a weapon: `Instantiate` the prefab, `SetSyncedHolder(player)`, `ServerManager.Spawn`, then put it in `PlayerInventory._items[slot]`.
- Attachments are FishNet `SyncVar`s on `Attachments` (sight, barrel, bullet level, quiver, extended magazine, laser) and on `Melee` (sharpness). Setting them on the server right after spawning is enough to sync them.

## Islands

- 11 islands: 0–5 are main islands, 6–10 are mini islands. A new save starts on island 0.
- `OnlineIslandManager.TpToSpecificIsland(index)` loads an island and teleports everyone; it is ready when `IslandManager.IsLoading` and `OnlineIslandManager.TeleportPlayers` are false and the scene of `Island.CurIsland` has build index `index + 1`.
- `Island.IslandPos` is the island center (island 0: `(-200, 0, 600)`).
- Islands are loaded additively; the main scene is never reloaded.

## Health, death and respawn

- `PlayerVitals.ServerResetVitals` resets health to the prefab start health, which is **50**, not 100. Clients detect a respawn when health goes back to exactly that value, so the mod heals players to 100 slightly later.
- The game's `Server.RespawnPlayer` RPC runs on the next tick and drops the whole inventory on the ground. The mod revives players itself (destroy the corpse, reset vitals, teleport, give the kit) and blocks the game's respawn and corpse revive during matches.
- `PlayerDying.ServerDie` has no killer information. The attacker is only known in the server logic of the `HitPlayer` RPC (`Server.RpcLogic___HitPlayer___2449261505`, last argument). The mod remembers the last attacker of each victim and credits the kill if the hit happened in the last 10 seconds.

## Interface

- Main menu sub-panels are listed in `CanvasManager._allMainMenuButtons`; `[0]` is the root panel and `ToggleMenuUI` re-enables only that one.
- Menu buttons use `UnityEngine.UI.Button` plus the game's `UIButton` component (hover and click sounds, scale animation). The mod clones the main menu *Quit* button to build its own buttons.
- Tab is the `PlayerTab` input action and opens the journal (`PlayerThinking.ThinkingInput`).
- `Player.BlockInputs` (getter) disables movement, aiming and shooting; the mod forces it on lobby and end screens.
- `PauseManager._serverButtons` holds the host-only pause buttons (save, server settings).

## Networking

- Lobbies are Steam lobbies; the game publishes the `version` lobby key. The mod adds `htf_combat`, `htf_combat_ver` and `htf_mode`.
- Mod messages travel through the Steam lobby chat with the `HTFCM|` prefix; the host is the only authority.
