# Builds dist/NeuTroNBZh-CombatModes-<version>.zip for Thunderstore (DLLs at the root, BepInEx as a dependency).
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
dotnet build "$root\HowToFish.CombatModes.slnx" -c Release | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

$manifest = Get-Content "$root\thunderstore\manifest.json" -Raw | ConvertFrom-Json
$bin = "$root\src\HowToFish.CombatModes\bin\Release\netstandard2.1"
$staging = "$root\dist\thunderstore"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Force $staging | Out-Null
Copy-Item "$root\thunderstore\manifest.json", "$root\thunderstore\README.md", "$root\CHANGELOG.md", "$root\art\icon.png" $staging
Copy-Item "$bin\HowToFish.CombatModes.dll", "$bin\HowToFish.CombatModes.Core.dll" $staging

$zip = "$root\dist\NeuTroNBZh-CombatModes-$($manifest.version_number).zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$staging\*" -DestinationPath $zip
Remove-Item $staging -Recurse -Force
Write-Output $zip
