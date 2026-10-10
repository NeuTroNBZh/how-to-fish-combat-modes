# Résultats du prototype — 2026-10-10

Test en solo (hôte seul, pas de second joueur).

## Armes (clés de GameInfo.GetSpawnable)
- pistol, shotgun, smg, assaultrifle, sniperrifle, bow, knife, brassknuckles, dynamite
- Aucun objet `arrow` ni chargeur (`pistolmag`…) n'est spawnable.
- `Item.Weapon` vaut `null` sur les prefabs (assigné à l'exécution) : `type`/`weapon` du dump ne permettent pas de filtrer les armes.

## Kits retenus pour DefaultKits
pistol+knife;shotgun+knife;smg+knife;assaultrifle+knife;sniperrifle+pistol;bow+knife

## Îles
- total : 11 (0 à 5 principales, 6 à 10 mini-îles), île de départ d'une nouvelle partie : 0
- île retenue pour IslandIndex : 0 (chargée d'office, centre `Island.IslandPos` = (-200, 0, 600), `Island.IslandSize` = 55)

## Téléportation hôte -> invité (Server.TeleportPlayer)
- non testée (pas de second joueur)

## Réapparition (Server.RespawnPlayer) et don d'arme
- don d'arme (Instantiate + SetSyncedHolder + ServerManager.Spawn + _items[slot]) : fonctionne, arme reçue
- `ServerResetVitals` remet la vie à `_startHealth` = **50** (valeur du prefab, pas 100). Le client détecte une réapparition quand la vie revient exactement à cette valeur : le soin à 100 est donc fait au début du combat (`Heal(100)`), pas pendant la préparation.
- réanimation d'un joueur mort : non testée

## Plan B à activer
- aucun pour l'instant ; téléportation d'un invité à vérifier avec un second joueur
