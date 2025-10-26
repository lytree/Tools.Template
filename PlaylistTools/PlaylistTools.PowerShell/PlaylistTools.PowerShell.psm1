
$moduleRoot = Split-Path -Parent $PSCommandPath

# 动态版本号（可从 psd1 读取）
$ManifestPath = Join-Path $ModuleRoot "PlaylistTools.PowerShell.psd1"
if (Test-Path $ManifestPath) {
    $Manifest = Import-PowerShellDataFile $ManifestPath
    $ModuleVersion = $Manifest.ModuleVersion
}
else {
    $ModuleVersion = "0.0.0"
}
$CoreDll = Join-Path $ModuleRoot "\$ModuleVersion\PlaylistTools.Core.dll"

if (Test-Path $CoreDll) { Add-Type -Path $CoreDll }

function Create-Playlist {
    param(
        [string]$path = "$(Get-Location)",
        [string]$outputFile = "playlist.m3u",
        [string]$extensions = ".mp4,.mkv,.avi",
        [bool]$recursive = $True
    )
    return [PlaylistTools.Core.PlaylistService]::Generate($path, $outputFile, $extensions, $recursive)
}
Export-ModuleMember -Function Create-Playlist