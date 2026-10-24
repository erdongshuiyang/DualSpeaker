param([string]$Version = '0.1.0')
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.]+)?$') { throw 'Version must use semantic versioning, e.g. 0.1.0.' }
$projectRoot = Split-Path -Parent $PSScriptRoot
$inputDirectory = Join-Path $projectRoot 'dist\DualSpeaker'
if (-not (Test-Path -LiteralPath (Join-Path $inputDirectory 'DualSpeaker.exe'))) { throw 'Run build.ps1 first.' }
$artifacts = Join-Path $projectRoot 'artifacts'
$stage = Join-Path $artifacts ('stage-' + [Guid]::NewGuid().ToString('N') + '\DualSpeaker')
New-Item -ItemType Directory -Path $stage -Force | Out-Null
# Deliberate allowlist: never package saved endpoints, logs or microphone recordings.
foreach ($file in @('DualSpeaker.exe','DualSpeaker.Cli.exe','DualSpeaker.exe.config','NAudio.Core.dll','NAudio.Wasapi.dll','NAudio.WinMM.dll','LICENSE','README.md','THIRD-PARTY-NOTICES.md','launch.cmd','recover-audio.cmd')) {
    Copy-Item -LiteralPath (Join-Path $inputDirectory $file) -Destination $stage
}
foreach ($folder in @('fonts','licenses','docs')) { Copy-Item -LiteralPath (Join-Path $inputDirectory $folder) -Destination $stage -Recurse }
$zipPath = Join-Path $artifacts ('DualSpeaker-v' + $Version + '-win-x64.zip')
Compress-Archive -LiteralPath $stage -DestinationPath $zipPath -Force
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksum = $hash + '  ' + [IO.Path]::GetFileName($zipPath)
[IO.File]::WriteAllText(($zipPath + '.sha256'), ($checksum + [Environment]::NewLine), (New-Object Text.UTF8Encoding($false)))
Write-Host $checksum
