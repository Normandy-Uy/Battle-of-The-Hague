$target = 'C:\Users\admin\Free Dutz 2025\New Unity Project\Builds\Dutz_Paid.aab'
$log = Join-Path $env:LOCALAPPDATA 'Unity\Editor\Editor.log'
$patterns = @(
  'buildAppBundle=True'
  'NOT a real Android App Bundle'
  'Paid AAB built successfully (real App Bundle'
  'build FAILED'
)
$maxMinutes = 50
$start = Get-Date
$lastLogPos = 0
if (Test-Path $log) { $lastLogPos = (Get-Item $log).Length }
$iteration = 0

function Check-Aab([string]$path) {
  if (-not (Test-Path $path)) { return @{ status = 'MISSING' } }
  $fi = Get-Item $path
  try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($path)
    $names = @($zip.Entries | ForEach-Object { $_.FullName })
    $hasBundle = ($names | Where-Object { $_ -eq 'BundleConfig.pb' }).Count -gt 0
    $topLevel = $names | ForEach-Object { if ($_ -match '/') { ($_ -split '/')[0] } else { $_ } } | Select-Object -Unique
    $zip.Dispose()
    if ($hasBundle) {
      return @{ status = 'SUCCESS'; size = $fi.Length; lwt = $fi.LastWriteTime; top = $topLevel }
    }
    return @{ status = 'FAIL_FAKE'; size = $fi.Length; lwt = $fi.LastWriteTime; top = $topLevel }
  }
  catch {
    return @{ status = 'ERROR'; msg = $_.Exception.Message; size = $fi.Length; lwt = $fi.LastWriteTime }
  }
}

function Scan-Log([string]$logPath, [ref]$pos) {
  if (-not (Test-Path $logPath)) { return @() }
  $stream = [System.IO.File]::Open($logPath, 'Open', 'Read', 'ReadWrite')
  $reader = $null
  try {
    if ($stream.Length -lt $pos.Value) { $pos.Value = 0 }
    $stream.Seek($pos.Value, 'Begin') | Out-Null
    $reader = New-Object System.IO.StreamReader($stream)
    $text = $reader.ReadToEnd()
    $pos.Value = $stream.Position
  }
  finally {
    if ($reader) { $reader.Close() }
    $stream.Close()
  }
  $hits = [System.Collections.Generic.List[string]]::new()
  foreach ($line in ($text -split "`n")) {
    foreach ($p in $patterns) {
      if ($line -like "*$p*") { $hits.Add($line.Trim()) }
    }
  }
  return $hits.ToArray()
}

while (((Get-Date) - $start).TotalMinutes -lt $maxMinutes) {
  $iteration++
  $ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
  $r = Check-Aab $target
  $hits = Scan-Log $log ([ref]$lastLogPos)
  Write-Output "[$ts] iter=$iteration status=$($r.status)"
  if ($r.status -eq 'SUCCESS') {
    Write-Output 'VERDICT=SUCCESS'
    Write-Output "LastWriteTime=$($r.lwt)"
    Write-Output "SizeBytes=$($r.size)"
    Write-Output "TopLevel=$($r.top -join ', ')"
    if ($hits.Count -gt 0) { Write-Output 'LogHits:'; $hits | ForEach-Object { Write-Output $_ } }
    exit 0
  }
  if ($r.status -eq 'FAIL_FAKE') {
    Write-Output "FAIL_FAKE: missing BundleConfig.pb size=$($r.size) lwt=$($r.lwt)"
    Write-Output "TopLevel=$($r.top -join ', ')"
  }
  if ($r.status -eq 'ERROR') { Write-Output "Zip error: $($r.msg)" }
  if ($hits.Count -gt 0) {
    Write-Output 'LogHits:'
    $hits | ForEach-Object { Write-Output $_ }
    foreach ($h in $hits) {
      if ($h -like '*build FAILED*') {
        Write-Output 'VERDICT=BUILD_FAILED'
        exit 2
      }
    }
  }
  $sleep = Get-Random -Minimum 60 -Maximum 91
  Start-Sleep -Seconds $sleep
}
Write-Output 'VERDICT=TIMEOUT'
exit 3
