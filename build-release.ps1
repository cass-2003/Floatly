# Floatly v2.0.10 Build Script
# PowerShell 5.1+

$ErrorActionPreference = "Stop"

$Version = "2.0.10"
$RootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $RootDir "DeskLite"
$OutputDir = Join-Path $RootDir "release\Floatly"
$InstallerScript = Join-Path $RootDir "installer\Floatly.iss"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  Floatly v$Version Release Build" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Prepare release directory
Write-Host "[1/4] Preparing release folder..." -ForegroundColor Yellow
if (Test-Path $OutputDir) {
    Write-Host "    Cleaning $OutputDir..." -ForegroundColor Gray
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Build .NET project
Write-Host "[2/4] Building .NET 8 project (win-x64)..." -ForegroundColor Yellow
Push-Location $RootDir
dotnet publish $ProjectDir `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $OutputDir

Pop-Location

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Copy app icon
Write-Host "[3/4] Copying icon..." -ForegroundColor Yellow
Copy-Item (Join-Path $ProjectDir "app.ico") $OutputDir -Force

# Build installer
Write-Host "[4/4] Building Inno Setup installer..." -ForegroundColor Yellow
$Iscc = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $Iscc) {
    Write-Host "[warn] Inno Setup not found. Skipping installer build." -ForegroundColor Yellow
    Write-Host "    Download: https://jrsoftware.org/isdl.php" -ForegroundColor Gray
} else {
    & $Iscc "/DMyAppVersion=$Version" $InstallerScript
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[ok] Installer created: release\Floatly-Setup-$Version.exe" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""
Write-Host "Output: $OutputDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Test the build: $OutputDir\Floatly.exe"
Write-Host "  2. Create git tag: git tag v$Version"
Write-Host "  3. Push tag: git push origin v$Version"
Write-Host "  4. Upload installer to GitHub Releases"
Write-Host ""
