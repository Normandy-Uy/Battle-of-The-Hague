# DEPLOY-1: Upload Unity WebGL build to psr.ovh/play/ and configure nginx.
# Run from project root: .\deploy-1\deploy.ps1

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$ConfigFile = Join-Path $ScriptDir "config.env"

$DeployHost = "15.235.206.171"
$DeployUser = "ubuntu"
$DeployPath = "/var/www/psr/play"
$PlayUrl = "https://psr.ovh/play/"

if (Test-Path $ConfigFile) {
    Get-Content $ConfigFile | ForEach-Object {
        if ($_ -match '^\s*([^#=]+)=(.*)$') {
            $name = $matches[1].Trim()
            $value = $matches[2].Trim()
            switch ($name) {
                "DEPLOY_HOST" { $DeployHost = $value }
                "DEPLOY_USER" { $DeployUser = $value }
                "DEPLOY_PATH" { $DeployPath = $value }
                "PLAY_URL" { $PlayUrl = $value }
            }
        }
    }
}

$BuildDir = Join-Path $ProjectRoot "Builds\WebGL"
$Remote = "${DeployUser}@${DeployHost}"
$RemoteTarget = "${Remote}:${DeployPath}/"

Write-Host "[DEPLOY-1] Project: $ProjectRoot"
Write-Host "[DEPLOY-1] Build:   $BuildDir"
Write-Host "[DEPLOY-1] Target:  $RemoteTarget"
Write-Host "[DEPLOY-1] URL:     $PlayUrl"

if (-not (Test-Path (Join-Path $BuildDir "index.html"))) {
    Write-Error "[DEPLOY-1] Missing Builds/WebGL/index.html - run Unity WebGL Level 00 build first (DutzWebGlBuildPrepare)."
}

if (-not (Test-Path (Join-Path $BuildDir "Build"))) {
    Write-Error "[DEPLOY-1] Missing Builds/WebGL/Build/ folder."
}

Write-Host "[DEPLOY-1] Creating remote directory..."
ssh $Remote "mkdir -p $DeployPath"

Write-Host "[DEPLOY-1] Uploading WebGL build (may take several minutes)..."
scp -r "$BuildDir\*" "${RemoteTarget}"

Write-Host "[DEPLOY-1] Uploading remote-setup.sh and nginx snippet..."
scp (Join-Path $ScriptDir "remote-setup.sh") "${Remote}:~/deploy-1-remote-setup.sh"
scp (Join-Path $ScriptDir "nginx-play.snippet") "${Remote}:~/deploy-1-nginx-play.snippet"

Write-Host "[DEPLOY-1] Running remote nginx setup..."
ssh $Remote "chmod +x ~/deploy-1-remote-setup.sh; DEPLOY_PATH='$DeployPath' bash ~/deploy-1-remote-setup.sh"

Write-Host "[DEPLOY-1] Smoke test: $PlayUrl"
try {
    $index = Invoke-WebRequest -Uri $PlayUrl -UseBasicParsing -TimeoutSec 60
    Write-Host "[DEPLOY-1] GET /play/ => $($index.StatusCode)"
} catch {
    Write-Warning "[DEPLOY-1] GET /play/ failed: $_"
}

$wasmFiles = @(
    Get-ChildItem -Path (Join-Path $BuildDir "Build") -Filter "*.wasm" -ErrorAction SilentlyContinue
    Get-ChildItem -Path (Join-Path $BuildDir "Build") -Filter "*.wasm.unityweb" -ErrorAction SilentlyContinue
) | Select-Object -First 1
if ($wasmFiles) {
    $wasmName = $wasmFiles.Name
    $wasmUrl = ($PlayUrl.TrimEnd('/') + "/Build/" + $wasmName)
    try {
        $wasm = Invoke-WebRequest -Uri $wasmUrl -Method Head -UseBasicParsing -TimeoutSec 60
        $ctype = $wasm.Headers["Content-Type"]
        Write-Host "[DEPLOY-1] HEAD $wasmUrl => $($wasm.StatusCode) Content-Type: $ctype"
        if ($wasmName -match '\.unityweb$') {
            if ($ctype -notmatch 'octet-stream') {
                Write-Warning "[DEPLOY-1] Expected application/octet-stream for .unityweb build"
            }
        } elseif ($ctype -notmatch "wasm") {
            Write-Warning "[DEPLOY-1] Expected application/wasm Content-Type - check nginx mime.types"
        }
    } catch {
        Write-Warning "[DEPLOY-1] WASM head check failed: $_"
    }
}

Write-Host ""
Write-Host "[DEPLOY-1] DONE. Share: $PlayUrl"
