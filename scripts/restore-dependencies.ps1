param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$cache = Join-Path $projectRoot '.deps\packages'
$library = Join-Path $projectRoot '.deps\lib'
New-Item -ItemType Directory -Path $cache,$library -Force | Out-Null
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Add-Type -AssemblyName System.IO.Compression.FileSystem
$packages = @(
    @{ Id='naudio.core'; Dll='NAudio.Core.dll'; Hash='794645DBFD30E4880663D52D9D9224D55C301FA54228FF623C95D572C7887347' },
    @{ Id='naudio.wasapi'; Dll='NAudio.Wasapi.dll'; Hash='C8396B8DBAB86A2619EDFD9158BEAF18C21CB86677099B5594950FD8651B9D1A' },
    @{ Id='naudio.winmm'; Dll='NAudio.WinMM.dll'; Hash='48DA978D715F219489C050EE8C32C64F80FF73718EBA237EBCBC636BA5902BE3' }
)
foreach ($package in $packages) {
    $archivePath = Join-Path $cache ($package.Id + '.2.2.1.nupkg')
    if (-not (Test-Path -LiteralPath $archivePath)) {
        $uri = 'https://api.nuget.org/v3-flatcontainer/' + $package.Id + '/2.2.1/' + $package.Id + '.2.2.1.nupkg'
        $downloadPath = $archivePath + '.download'
        for ($attempt = 1; $attempt -le 3; $attempt++) {
            try {
                Invoke-WebRequest -UseBasicParsing -Uri $uri -OutFile $downloadPath -TimeoutSec 30
                Move-Item -LiteralPath $downloadPath -Destination $archivePath -Force
                break
            } catch {
                if ($attempt -eq 3) { throw }
                Write-Warning ('Retrying package download: ' + $package.Id)
            }
        }
    }
    if ((Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash -ne $package.Hash) {
        throw ('Package checksum mismatch: ' + $package.Id + '. Delete the cached package and retry.')
    }
    $archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $entry = $archive.GetEntry('lib/netstandard2.0/' + $package.Dll)
        if (-not $entry) { throw ('Missing dependency: ' + $package.Dll) }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $library $package.Dll), $true)
    } finally { $archive.Dispose() }
}
Write-Host 'Restored NAudio 2.2.1 (SHA-256 verified).'
