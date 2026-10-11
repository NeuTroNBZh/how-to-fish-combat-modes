# Generates the mod icon (Thunderstore, 256x256) and a banner (Nexus Mods, 1280x720) into art/.
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'art'
New-Item -ItemType Directory -Force $out | Out-Null

function New-Art([int]$width, [int]$height, [string]$path, [bool]$withText) {
    $bmp = New-Object System.Drawing.Bitmap $width, $height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $rect = New-Object System.Drawing.Rectangle 0, 0, $width, $height
    $top = [System.Drawing.Color]::FromArgb(255, 18, 92, 150)
    $bottom = [System.Drawing.Color]::FromArgb(255, 6, 28, 58)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, $top, $bottom, 90
    $g.FillRectangle($brush, $rect)

    $size = [Math]::Min($width, $height) * $(if ($withText) { 0.5 } else { 0.62 })
    $cx = if ($withText) { $width * 0.2 } else { $width / 2 }
    $cy = $height / 2
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(235, 255, 255, 255)), ($size * 0.06)
    $g.DrawEllipse($pen, [float]($cx - $size / 2), [float]($cy - $size / 2), [float]$size, [float]$size)
    $inner = $size * 0.28
    $g.DrawEllipse($pen, [float]($cx - $inner / 2), [float]($cy - $inner / 2), [float]$inner, [float]$inner)
    $gap = $size * 0.18
    $len = $size * 0.62
    $g.DrawLine($pen, [float]($cx - $len), [float]$cy, [float]($cx - $gap), [float]$cy)
    $g.DrawLine($pen, [float]($cx + $gap), [float]$cy, [float]($cx + $len), [float]$cy)
    $g.DrawLine($pen, [float]$cx, [float]($cy - $len), [float]$cx, [float]($cy - $gap))
    $g.DrawLine($pen, [float]$cx, [float]($cy + $gap), [float]$cx, [float]($cy + $len))
    $accent = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 170, 60))
    $dot = $size * 0.08
    $g.FillEllipse($accent, [float]($cx - $dot / 2), [float]($cy - $dot / 2), [float]$dot, [float]$dot)

    if ($withText) {
        $white = [System.Drawing.Brushes]::White
        $title = New-Object System.Drawing.Font 'Segoe UI', ([float]($height * 0.085)), ([System.Drawing.FontStyle]::Bold)
        $sub = New-Object System.Drawing.Font 'Segoe UI', ([float]($height * 0.04))
        $x = [float]($width * 0.41)
        $g.DrawString('COMBAT MODES', $title, $white, $x, [float]($height * 0.30))
        $g.DrawString('for How to Fish', $sub, $accent, $x, [float]($height * 0.44))
        $g.DrawString('1v1 Duel  |  Free-for-all  |  Gun Game', $sub, $white, $x, [float]($height * 0.55))
        $g.DrawString('2 to 8 players  |  Quick match', $sub, $white, $x, [float]($height * 0.63))
    }
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
}

New-Art 256 256 (Join-Path $out 'icon.png') $false
New-Art 1280 720 (Join-Path $out 'banner.png') $true
Write-Output $out
