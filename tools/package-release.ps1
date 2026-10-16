[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
    foreach ($project in @('WrathCombo/WrathCombo.csproj', 'Accord.Plugin/Accord.csproj')) {
        & dotnet build $project -c Release --nologo -v quiet
        if ($LASTEXITCODE -ne 0) { throw "Build failed: $project" }
    }
    & dotnet run --project tests/Accord.Tests.csproj -c Release -- WrathCombo/bin/WrathFork/Release/WrathFork.dll
    if ($LASTEXITCODE -ne 0) { throw 'Separation tests failed.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $tag = 'v0.2.0-preview'
    $out = Join-Path $repoRoot "artifacts/$tag"
    New-Item -ItemType Directory -Force -Path $out | Out-Null
    $hashes = @()
    foreach ($name in @('WrathFork', 'Accord')) {
        $relative = if ($name -eq 'WrathFork') { 'WrathCombo/bin/WrathFork/Release/WrathFork' } else { 'Accord.Plugin/bin/Release/Accord' }
        $build = Join-Path $repoRoot $relative
        $manifest = Get-Content -LiteralPath (Join-Path $build "$name.json") -Raw | ConvertFrom-Json
        if ($manifest.InternalName -ne $name -or $manifest.Name -ne $name -or $manifest.DalamudApiLevel -ne 15 -or [version]$manifest.AssemblyVersion -ne [version]'0.2.0.0') {
            throw "Incorrect package identity/version: $name"
        }
        $package = Join-Path $out "$name.zip"
        Copy-Item -LiteralPath (Join-Path $build 'latest.zip') -Destination $package -Force
        $archive = [IO.Compression.ZipFile]::Open($package, [IO.Compression.ZipArchiveMode]::Update)
        try {
            $paths = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
            if ($paths -match '(?i)(^|/)WrathCombo\.(dll|json)$|\.zip$|(^|/)(configuration|configuration-backups)/') { throw 'Invalid or private package content.' }
            $required = @("$name.dll", "$name.json", 'ECommons.dll')
            if ($name -eq 'WrathFork') { $required += @('PunishLib.dll', 'WrathCombo.API.dll') }
            foreach ($file in $required) { if ($paths -notcontains $file) { throw "Package missing $file" } }
            if ($name -eq 'Accord' -and ($paths -contains 'WrathFork.dll' -or $paths -contains 'WrathCombo.API.dll')) { throw 'Accord contains an engine dependency.' }
            if ($name -eq 'WrathFork' -and $paths -contains 'Accord.dll') { throw 'Fork contains Accord.' }
            $notices = @(@('LICENSE', 'LICENSE'), @('ECommons/LICENSE.md', 'licenses/ECommons.md'), @('docs/distribution.md', 'README.md'))
            if ($name -eq 'WrathFork') { $notices += ,@('PunishLib/LICENCE.md', 'licenses/PunishLib.md') }
            foreach ($notice in $notices) {
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $repoRoot $notice[0]), $notice[1]) | Out-Null
            }
        } finally { $archive.Dispose() }
        $url = "https://github.com/kaldigo/Accord/releases/download/$tag/$name.zip"
        foreach ($field in @('DownloadLinkInstall', 'DownloadLinkUpdate', 'DownloadLinkTesting')) {
            $manifest | Add-Member -NotePropertyName $field -NotePropertyValue $url -Force
        }
        $manifest | Add-Member -NotePropertyName IsTestingExclusive -NotePropertyValue $true -Force
        $manifest | Add-Member -NotePropertyName TestingAssemblyVersion -NotePropertyValue $manifest.AssemblyVersion -Force
        $manifest | Add-Member -NotePropertyName Changelog -NotePropertyValue 'Separate Accord client and WrathFork engine/API. Native acceptance pending.' -Force
        ConvertTo-Json -InputObject @($manifest) -Depth 8 | Set-Content -LiteralPath (Join-Path $out "$name.pluginmaster.testing.json") -Encoding utf8NoBOM
        $hashes += (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant() + "  $name.zip"
    }
    $hashes | Set-Content -LiteralPath (Join-Path $out 'SHA256SUMS.txt') -Encoding utf8NoBOM
    Write-Output "Prepared two local previews in $out. Nothing installed or published."
} finally { Pop-Location }
