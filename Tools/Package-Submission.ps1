$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$requiredItems = @('Assets', 'Packages', 'ProjectSettings', '.gitignore', '.gitattributes', 'README.md', 'Evidence', 'Tools')
foreach ($item in $requiredItems) {
    if (-not (Test-Path -LiteralPath (Join-Path $projectPath $item))) { throw ('Missing submission item: ' + $item) }
}
if (-not (Test-Path -LiteralPath (Join-Path $projectPath 'Assets\Scenes\CrystalArena.unity'))) { throw 'Generate the Unity arena first.' }
& (Join-Path $PSScriptRoot 'Verify-Evidence.ps1')
$archiveDirectory = Join-Path $projectPath 'Submission'
New-Item -ItemType Directory -Path $archiveDirectory -Force | Out-Null
$archivePath = Join-Path $archiveDirectory ('CrystalRush-UnityProject-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.zip')
$includePaths = $requiredItems | ForEach-Object { Join-Path $projectPath $_ }
Compress-Archive -LiteralPath $includePaths -DestinationPath $archivePath -CompressionLevel Optimal
Write-Output $archivePath
