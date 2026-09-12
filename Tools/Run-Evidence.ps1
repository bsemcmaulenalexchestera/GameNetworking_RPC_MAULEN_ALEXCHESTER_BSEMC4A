param([string]$OutputFolder = 'Evidence', [switch]$ShowWindows, [switch]$ReplaceCapture)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$gamePath = Join-Path $projectPath 'Builds\CrystalRush\CrystalRush.exe'
if (-not (Test-Path -LiteralPath $gamePath)) { throw 'Build first: Crystal Rush > Build Windows Test Player in Unity.' }
$evidencePath = [System.IO.Path]::GetFullPath((Join-Path $projectPath $OutputFolder))
if (-not $evidencePath.StartsWith($projectPath + '\', [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Output must be inside this project.' }
if ((Test-Path -LiteralPath (Join-Path $evidencePath 'host-01-connected.png')) -and -not $ReplaceCapture) { throw 'Evidence already exists. Choose a different -OutputFolder to preserve it.' }
New-Item -ItemType Directory -Force -Path $evidencePath | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $projectPath 'Logs') | Out-Null
$startTicks = [DateTime]::UtcNow.AddSeconds(35).Ticks
$sharedArgs = @('-force-d3d11', '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '800', ('--cr-start=' + $startTicks), ('"--cr-output=' + $evidencePath + '"'))
$hostArgs = $sharedArgs + @('--cr-evidence=host', '-logFile', ('"' + (Join-Path $projectPath 'Logs\evidence-host.log') + '"'))
$windowStyle = if ($ShowWindows) { 'Normal' } else { 'Hidden' }
$hostProcess = Start-Process -FilePath $gamePath -ArgumentList $hostArgs -WorkingDirectory $projectPath -WindowStyle $windowStyle -PassThru
Start-Sleep -Seconds 5
$clientArgs = $sharedArgs + @('--cr-evidence=client', '-logFile', ('"' + (Join-Path $projectPath 'Logs\evidence-client.log') + '"'))
$clientProcess = Start-Process -FilePath $gamePath -ArgumentList $clientArgs -WorkingDirectory $projectPath -WindowStyle $windowStyle -PassThru
Write-Output ('Host PID: ' + $hostProcess.Id + '; client PID: ' + $clientProcess.Id)
Write-Output ('Captures will be saved in ' + $evidencePath + '. Both test players exit automatically.')
