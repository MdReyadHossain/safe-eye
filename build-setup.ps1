<#
.SYNOPSIS
    Builds the Safe Eye Windows desktop application and generates the setup installer packages.
    Author: squadrosoft
    Publisher: squadrosoft
#>

[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$installerDir = Join-Path $root "installer"
$publishDir = Join-Path $installerDir "publish_single"
$logoPath = Join-Path $root "WpfApp1\sage-eyes_logo.png"
$icoPath = Join-Path $installerDir "app.ico"
$resIcoPath = Join-Path $root "WpfApp1\Resources\app.ico"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Safe Eye Windows Setup Installer Build" -ForegroundColor Cyan
Write-Host " Author / Publisher: squadrosoft" -ForegroundColor Cyan
Write-Host " Version: $Version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Step 1: Ensure directories
if (!(Test-Path $installerDir)) { New-Item -ItemType Directory -Path $installerDir -Force | Out-Null }
if (!(Test-Path (Join-Path $root "WpfApp1\Resources"))) { New-Item -ItemType Directory -Path (Join-Path $root "WpfApp1\Resources") -Force | Out-Null }

# Step 2: Generate ICO from Logo PNG if needed
if ((Test-Path $logoPath) -and (!(Test-Path $icoPath) -or ((Get-Item $logoPath).LastWriteTime -gt (Get-Item $icoPath).LastWriteTime))) {
    Write-Host "[1/4] Generating multi-resolution icon from sage-eyes_logo.png..." -ForegroundColor Yellow
    Add-Type -AssemblyName System.Drawing

    $srcImage = [System.Drawing.Image]::FromFile($logoPath)
    $sizes = @(256, 128, 64, 48, 32, 16)
    $pngStreams = [System.Collections.Generic.List[byte[]]]::new()
    $validSizes = [System.Collections.Generic.List[int]]::new()

    foreach ($sz in $sizes) {
        $bmp = New-Object System.Drawing.Bitmap $sz, $sz, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.Clear([System.Drawing.Color]::Transparent)
        $g.DrawImage($srcImage, 0, 0, $sz, $sz)
        $g.Dispose()

        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        $pngStreams.Add($ms.ToArray())
        $validSizes.Add($sz)
        $ms.Dispose()
    }
    $srcImage.Dispose()

    $fs = [System.IO.File]::Create($icoPath)
    $bw = New-Object System.IO.BinaryWriter $fs
    $bw.Write([uint16]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]$validSizes.Count)

    $offset = 6 + (16 * $validSizes.Count)
    for ($i = 0; $i -lt $validSizes.Count; $i++) {
        $sz = $validSizes[$i]
        $bytes = $pngStreams[$i]
        $w = if ($sz -ge 256) { 0 } else { [byte]$sz }
        $h = if ($sz -ge 256) { 0 } else { [byte]$sz }
        $bw.Write([byte]$w)
        $bw.Write([byte]$h)
        $bw.Write([byte]0)
        $bw.Write([byte]0)
        $bw.Write([uint16]1)
        $bw.Write([uint16]32)
        $bw.Write([uint32]$bytes.Length)
        $bw.Write([uint32]$offset)
        $offset += $bytes.Length
    }
    for ($i = 0; $i -lt $validSizes.Count; $i++) {
        $bw.Write($pngStreams[$i])
    }
    $bw.Flush()
    $bw.Dispose()
    $fs.Dispose()

    Copy-Item $icoPath $resIcoPath -Force
    Write-Host "   Created: $icoPath" -ForegroundColor Green
}

# Step 3: Publish standalone single-file binary
Write-Host "[2/4] Publishing self-contained single-file binary..." -ForegroundColor Yellow
$projPath = Join-Path $root "WpfApp1\WpfApp1.csproj"
dotnet publish $projPath -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $publishDir | Out-Null
Write-Host "   Published to: $publishDir" -ForegroundColor Green

# Step 4: Build MSI installer using WiX 5
Write-Host "[3/4] Building Windows Installer (.msi) with WiX Toolset..." -ForegroundColor Yellow
$msiOut = Join-Path $installerDir "SafeEye-Setup-$Version.msi"
$wxsPath = Join-Path $installerDir "SafeEye.wxs"
if (!(Test-Path $wxsPath)) {
    $legacyWxs = Join-Path $installerDir "SafeEyes.wxs"
    if (Test-Path $legacyWxs) { $wxsPath = $legacyWxs }
}
if (Test-Path $wxsPath) {
    Push-Location $installerDir
    try {
        wix build $wxsPath -arch x64 -out $msiOut
    } finally {
        Pop-Location
    }
    Write-Host "   Created MSI Setup: $msiOut" -ForegroundColor Green
} else {
    Write-Host "   (WiX source file not present, skipping MSI generation)" -ForegroundColor Gray
}

# Step 5: Check Inno Setup compiler if available
Write-Host "[4/4] Checking for Inno Setup compiler (ISCC.exe)..." -ForegroundColor Yellow
$isccPaths = @(
    "ISCC.exe",
    "$env:LOCALAPPDATA\Programs\InnoSetup\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$foundIscc = $null
foreach ($p in $isccPaths) {
    if (Get-Command $p -ErrorAction SilentlyContinue) {
        $foundIscc = $p
        break
    }
    elseif (Test-Path $p) {
        $foundIscc = $p
        break
    }
}

if ($foundIscc) {
    Write-Host "   Found Inno Setup at: $foundIscc" -ForegroundColor Green
    $issPath = Join-Path $installerDir "SafeEye-Setup.iss"
    if (!(Test-Path $issPath)) {
        $legacyIss = Join-Path $installerDir "SafeEyes-Setup.iss"
        if (Test-Path $legacyIss) { $issPath = $legacyIss }
    }
    if (Test-Path $issPath) {
        & $foundIscc $issPath | Out-Null
        Write-Host "   Created EXE Setup: SafeEye-Setup-$Version.exe" -ForegroundColor Green
    }
} else {
    Write-Host "   (Inno Setup compiler not found in standard paths; Inno Setup script ready at installer\SafeEye-Setup.iss)" -ForegroundColor Gray
}

Write-Host "`nSetup Build Completed Successfully!" -ForegroundColor Green
if (Test-Path $msiOut) {
    $msiItem = Get-Item $msiOut
    Write-Host "MSI Installer: $($msiItem.FullName) ($([math]::Round($msiItem.Length / 1MB, 2)) MB)" -ForegroundColor Cyan
}
