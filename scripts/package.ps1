# Construit dist/HowToFish-CombatModes-<version>.zip : BepInEx + le mod, à extraire dans le dossier du jeu.
param(
    [string]$GameDir = 'E:\SteamLibrary\steamapps\common\How to Fish\How to Fish'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$version = (Select-String -Path "$root\src\HowToFish.CombatModes\CombatModesPlugin.cs" -Pattern 'PluginVersion = "(.+)"').Matches[0].Groups[1].Value

dotnet build "$root\HowToFish.CombatModes.slnx" -c Release | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Build en échec' }

$staging = "$root\dist\staging"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Force "$staging\BepInEx\plugins" | Out-Null

foreach ($file in '.doorstop_version', 'doorstop_config.ini', 'winhttp.dll', 'changelog.txt') {
    Copy-Item "$GameDir\$file" $staging
}
Copy-Item "$GameDir\BepInEx\core" "$staging\BepInEx\core" -Recurse
Copy-Item "$GameDir\BepInEx\plugins\HowToFish.CombatModes" "$staging\BepInEx\plugins\HowToFish.CombatModes" -Recurse
Copy-Item "$root\docs\LISEZMOI-ami.txt" "$staging\LISEZMOI-Duel1v1.txt"

$zip = "$root\dist\HowToFish-CombatModes-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$staging\*" -DestinationPath $zip
Remove-Item $staging -Recurse -Force
Write-Output $zip
