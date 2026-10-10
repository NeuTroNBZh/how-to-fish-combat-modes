# Duel 1v1 — mod pour How to Fish

Ajoute un mode duel 1v1 en manches, lançable depuis le menu principal.

## Installation (chaque joueur)
1. Installer BepInEx 5 (x64) dans le dossier du jeu.
2. Copier `HowToFish.Duel1v1.dll` et `HowToFish.Duel1v1.Core.dll` dans `BepInEx\plugins\HowToFish.Duel1v1\`.

## Jouer
Menu principal → **Duel 1v1** → **Inviter un ami** ou **Partie rapide**. Les deux joueurs doivent avoir le mod.

## Réglages
`BepInEx\config\htf.duel1v1.cfg` : manches à gagner, durée des manches, compte à rebours, île, rayon de l'arène, kits d'armes, spawns, mode debug solo.
Touche de position (défaut F9) pour relever des coordonnées de spawn. En `DebugSolo`, F10 simule la mort de l'adversaire.

## Développement
- `dotnet test HowToFish.Duel1v1.slnx` : tests du cœur (règles, config, messages).
- `dotnet build HowToFish.Duel1v1.slnx` : compile et copie le plugin dans le jeu.
- Code décompilé de référence : `..\source\Assembly-CSharp\`.
