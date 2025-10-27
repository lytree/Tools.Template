param([string]$ProjectName)

Write-Host "Renaming subfolders to: $ProjectName"

# 获取模板生成输出目录
$root = Get-Location

$folders = @(
    "PlaylistTools.Cli",
    "PlaylistTools.Core",
    "PlaylistTools.PowerShell"
)

foreach ($f in $folders) {
    $src = Join-Path $root $f
    if (Test-Path $src) {
        $dest = $f -replace "^PlaylistTools", $ProjectName
        Write-Host "Renaming $f -> $dest"
        Rename-Item $src $dest -Force
    }
}
