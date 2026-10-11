# Mods de combat — mod pour How to Fish

Ajoute trois modes de combat lançables depuis le menu principal : duel 1v1 en manches, mêlée générale et course à l'armement (2 à 8 joueurs).

## Installation (chaque joueur)
1. Installer BepInEx 5 (x64) dans le dossier du jeu.
2. Copier `HowToFish.Duel1v1.dll` et `HowToFish.Duel1v1.Core.dll` dans `BepInEx\plugins\HowToFish.Duel1v1\`.

## Jouer
Menu principal → **Mods de combat** → mode → **Inviter des amis** ou **Partie rapide**. Tous les joueurs doivent avoir le mod.

## Modes arène
- Duel 1v1 : premier à 5 manches, 90 s par manche, kit et île aléatoires.
- Mêlée générale : premier à 20 kills (ou meilleur score après 10 min), kit commun changé toutes les 2 min, réapparition après 3 s.
- Course à l'armement : chaque kill donne l'arme suivante ; tué au couteau = un niveau en moins ; le premier kill au couteau au dernier niveau gagne.

Section `[Arena]` de la config : `KillTarget`, `TimeLimitSeconds`, `KitRotationSeconds`, `RespawnSeconds`, `SpawnProtectionSeconds`, `Ladder`.

## Réglages
`BepInEx\config\htf.duel1v1.cfg` : manches à gagner, durée des manches, compte à rebours, île, rayon de l'arène, kits d'armes, spawns, mode debug solo.
Touche de position (défaut F9) pour relever des coordonnées de spawn. En `DebugSolo` : F10 simule la mort de l'adversaire (duel) ou un kill sur le bot (arène), F11 ajoute un bot dans le salon arène. `ForceImguiFallback` (section Debug) réaffiche l'ancienne interface IMGUI pour diagnostic.

## Développement
- `dotnet test HowToFish.Duel1v1.slnx` : tests du cœur (règles, config, messages).
- `dotnet build HowToFish.Duel1v1.slnx` : compile et copie le plugin dans le jeu.
- Code décompilé de référence : `..\source\Assembly-CSharp\`.
