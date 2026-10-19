[CmdletBinding()]
param([ValidateSet('WrathCombo','Accord')][string[]]$Plugins=@('WrathCombo','Accord'))
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
    $projects = @()
    if ($Plugins -contains 'WrathCombo') { $projects += 'WrathCombo/WrathCombo.csproj' }
    if ($Plugins -contains 'Accord') { $projects += 'Accord.Plugin/Accord.csproj' }
    foreach ($project in $projects) {
        & dotnet build $project -c Release -p:UseSharedCompilation=false --nologo -v quiet
        if ($LASTEXITCODE -ne 0) { throw "Build failed: $project" }
    }
    if (-not (Test-Path -LiteralPath 'WrathCombo/bin/WrathCombo/Release/WrathCombo.dll')) { throw 'Build the compatible Wrath Combo engine before running schema verification.' }
    & dotnet run --project tests/Accord.Tests.csproj -c Release -p:UseSharedCompilation=false -- WrathCombo/bin/WrathCombo/Release/WrathCombo.dll
    if ($LASTEXITCODE -ne 0) { throw 'Separation tests failed.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $tag = 'v0.2.15-preview'
    $out = Join-Path $repoRoot "artifacts/$tag"
    New-Item -ItemType Directory -Force -Path $out | Out-Null
    $hashes = @()
    $feed = @()
    foreach ($name in $Plugins) {
        $relative = if ($name -eq 'WrathCombo') { 'WrathCombo/bin/WrathCombo/Release/WrathCombo' } else { 'Accord.Plugin/bin/Release/Accord' }
        $build = Join-Path $repoRoot $relative
        $manifest = Get-Content -LiteralPath (Join-Path $build "$name.json") -Raw | ConvertFrom-Json
        $expectedVersion = if ($name -eq 'Accord') { '0.2.15.0' } else { '1.0.4.28' }
        if ($manifest.InternalName -ne $name -or $manifest.Name -ne $(if ($name -eq 'WrathCombo') { 'Wrath Combo' } else { 'Accord' }) -or $manifest.DalamudApiLevel -ne 15 -or [version]$manifest.AssemblyVersion -ne [version]$expectedVersion) {
            throw "Incorrect package identity/version: $name"
        }
        $package = Join-Path $out "$name.zip"
        Copy-Item -LiteralPath (Join-Path $build 'latest.zip') -Destination $package -Force
        $archive = [IO.Compression.ZipFile]::Open($package, [IO.Compression.ZipArchiveMode]::Update)
        try {
            $paths = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
            if ($paths -match '(?i)(^|/)WrathFork\.(dll|json)$|\.zip$|(^|/)(configuration|configuration-backups)/') { throw 'Invalid or private package content.' }
            $required = @("$name.dll", "$name.json", 'ECommons.dll')
            if ($name -eq 'WrathCombo') { $required += @('PunishLib.dll', 'WrathCombo.API.dll') }
            foreach ($file in $required) { if ($paths -notcontains $file) { throw "Package missing $file" } }
            if ($name -eq 'Accord' -and ($paths -contains 'WrathCombo.dll' -or $paths -contains 'WrathCombo.API.dll')) { throw 'Accord contains an engine dependency.' }
            if ($name -eq 'WrathCombo' -and $paths -contains 'Accord.dll') { throw 'Fork contains Accord.' }
            $notices = @(@('LICENSE', 'LICENSE'), @('ECommons/LICENSE.md', 'licenses/ECommons.md'), @('docs/distribution.md', 'README.md'))
            if ($name -eq 'WrathCombo') { $notices += ,@('PunishLib/LICENCE.md', 'licenses/PunishLib.md') }
            foreach ($notice in $notices) {
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $repoRoot $notice[0]), $notice[1]) | Out-Null
            }
        } finally { $archive.Dispose() }
        $url = "https://github.com/kaldigo/Accord/releases/download/$tag/$name.zip"
        foreach ($field in @('DownloadLinkInstall', 'DownloadLinkUpdate', 'DownloadLinkTesting')) {
            $manifest | Add-Member -NotePropertyName $field -NotePropertyValue $url -Force
        }
        $changelog = if ($name -eq 'Accord') { '0.2.15: Compact PvE/PvP layouts and restores work without Hotbar Bridge. Uses Accord layouts directly; no Bridge migration required. Converts Sprint across every active regular and cross hotbar, including utility bars, in static and live modes.' } else { 'Restore WrathCombo identity and migrate current WrathFork settings once with backups. Accord detects the compatible WrathCombo build. Remove legacy WrathFork before loading.' }
        $publicManifest = $manifest | ConvertTo-Json -Depth 8 | ConvertFrom-Json
        $publicManifest | Add-Member -NotePropertyName Changelog -NotePropertyValue $changelog -Force
        $feed += $publicManifest
        $manifest | Add-Member -NotePropertyName IsTestingExclusive -NotePropertyValue $true -Force
        $manifest | Add-Member -NotePropertyName TestingAssemblyVersion -NotePropertyValue $manifest.AssemblyVersion -Force
        $manifest | Add-Member -NotePropertyName Changelog -NotePropertyValue $changelog -Force
        ConvertTo-Json -InputObject @($manifest) -Depth 8 | Set-Content -LiteralPath (Join-Path $out "$name.pluginmaster.testing.json") -Encoding utf8NoBOM
        $hashes += (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant() + "  $name.zip"
    }
    ConvertTo-Json -InputObject $feed -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'pluginmaster.json') -Encoding utf8NoBOM
    $hashes | Set-Content -LiteralPath (Join-Path $out 'SHA256SUMS.txt') -Encoding utf8NoBOM
    Write-Output "Prepared $($Plugins.Count) local previews in $out. Nothing installed or published."
} finally { Pop-Location }
