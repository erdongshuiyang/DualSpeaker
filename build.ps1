param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot 'dist\DualSpeaker' }
& (Join-Path $projectRoot 'scripts\restore-dependencies.ps1')
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x x64 compiler was not found.' }
$refs = @(
    ('/r:' + (Join-Path $framework 'netstandard.dll')),
    ('/r:' + (Join-Path $framework 'System.Runtime.dll')),
    ('/r:' + (Join-Path $framework 'System.Runtime.InteropServices.WindowsRuntime.dll')),
    '/r:System.Web.Extensions.dll','/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Core.dll'
)
foreach ($dll in @('NAudio.Core.dll','NAudio.Wasapi.dll','NAudio.WinMM.dll')) {
    $refs += '/r:' + (Join-Path $projectRoot ('.deps\lib\' + $dll))
}
foreach ($metadata in @('Windows.Foundation','Windows.Media','Windows.Storage')) {
    $path = Join-Path $env:WINDIR ('System32\WinMetadata\' + $metadata + '.winmd')
    if (-not (Test-Path -LiteralPath $path)) { throw ('Windows metadata was not found: ' + $path + '. Build on Windows 11.') }
    $refs += '/r:' + $path
}
$sources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
& $compiler /nologo /utf8output /optimize+ /platform:x64 /target:exe /main:DualSpeaker.CommandLine ('/out:' + (Join-Path $OutputDirectory 'DualSpeaker.Cli.exe')) @refs @sources
if ($LASTEXITCODE -ne 0) { throw 'CLI compilation failed.' }
& $compiler /nologo /utf8output /optimize+ /platform:x64 /target:winexe /main:DualSpeaker.Program ('/win32manifest:' + (Join-Path $projectRoot 'app.manifest')) ('/win32icon:' + (Join-Path $projectRoot 'app.ico')) ('/out:' + (Join-Path $OutputDirectory 'DualSpeaker.exe')) @refs @sources
if ($LASTEXITCODE -ne 0) { throw 'GUI compilation failed.' }
Get-ChildItem -LiteralPath (Join-Path $projectRoot '.deps\lib') -Filter '*.dll' | Copy-Item -Destination $OutputDirectory -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'DualSpeaker.exe.config'),(Join-Path $projectRoot 'LICENSE'),(Join-Path $projectRoot 'README.md'),(Join-Path $projectRoot 'THIRD-PARTY-NOTICES.md') -Destination $OutputDirectory -Force
foreach ($folder in @('fonts','licenses','docs')) { Copy-Item -LiteralPath (Join-Path $projectRoot $folder) -Destination $OutputDirectory -Recurse -Force }
Copy-Item -LiteralPath (Join-Path $projectRoot 'scripts\launch.cmd'),(Join-Path $projectRoot 'scripts\recover-audio.cmd') -Destination $OutputDirectory -Force
Write-Host ('Build complete: ' + $OutputDirectory)
