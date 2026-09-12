param([string]$OutputFolder = 'Evidence')
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$evidencePath = Join-Path $projectPath $OutputFolder
Add-Type -AssemblyName System.Drawing
$stages = @('01-connected', '02-host-moving', '03-host-collected', '04-client-moving', '05-client-collected', '06-no-repeat')
foreach ($stage in $stages) {
    $hostState = Get-Content -LiteralPath (Join-Path $evidencePath ('host-' + $stage + '.json')) -Raw | ConvertFrom-Json
    $clientState = Get-Content -LiteralPath (Join-Path $evidencePath ('client-' + $stage + '.json')) -Raw | ConvertFrom-Json
    foreach ($role in @('host', 'client')) {
        $capture = Join-Path $evidencePath ($role + '-' + $stage + '.png')
        if (-not (Test-Path -LiteralPath $capture)) { throw ('Missing screenshot: ' + $capture) }
        $bitmap = [System.Drawing.Bitmap]::new($capture)
        try {
            $colors = [System.Collections.Generic.HashSet[int]]::new()
            for ($x = 0; $x -lt $bitmap.Width; $x += 25) {
                for ($y = 0; $y -lt $bitmap.Height; $y += 25) { [void]$colors.Add($bitmap.GetPixel($x, $y).ToArgb()) }
            }
            if ($colors.Count -lt 10) { throw ('Blank screenshot: ' + $capture + '. Rerun with -ShowWindows.') }
        } finally { $bitmap.Dispose() }
    }
    if (-not $hostState.host -or $clientState.host -or -not $hostState.connected -or -not $clientState.connected) { throw ('Connection failure: ' + $stage) }
    if ($hostState.players.Count -ne 2 -or $clientState.players.Count -ne 2) { throw ('Player count failure: ' + $stage) }
    if (-not $hostState.players[0].owner -or $hostState.players[1].owner -or $clientState.players[0].owner -or -not $clientState.players[1].owner) { throw 'Player ownership mismatch' }
    if ($stage -in @('02-host-moving', '04-client-moving')) { continue }
    $expectedHost = if ($stage -eq '01-connected') { 0 } else { 25 }
    $expectedClient = if ($stage -in @('05-client-collected', '06-no-repeat')) { 25 } else { 0 }
    foreach ($snapshot in @($hostState, $clientState)) {
        if ($snapshot.players[0].score -ne $expectedHost -or $snapshot.players[1].score -ne $expectedClient) { throw ('Score mismatch: ' + $stage) }
        if ($snapshot.crystals -ne (15 - ($expectedHost + $expectedClient) / 25)) { throw ('Despawn/count mismatch: ' + $stage) }
    }
    for ($i = 0; $i -lt 2; $i++) {
        if ([Math]::Abs($hostState.players[$i].position.z - $clientState.players[$i].position.z) -gt 0.15) { throw ('Position synchronization mismatch: ' + $stage) }
    }
    Write-Output ('PASS ' + $stage + ': both participants agree on ownership, scores, positions and collectible count.')
}
$initial = Get-Content -LiteralPath (Join-Path $evidencePath 'host-01-connected.json') -Raw | ConvertFrom-Json
$hostMoved = Get-Content -LiteralPath (Join-Path $evidencePath 'host-03-host-collected.json') -Raw | ConvertFrom-Json
$bothMoved = Get-Content -LiteralPath (Join-Path $evidencePath 'host-05-client-collected.json') -Raw | ConvertFrom-Json
if ($hostMoved.players[0].position.z -le $initial.players[0].position.z + 1 -or
    $bothMoved.players[1].position.z -le $initial.players[1].position.z + 1) { throw 'Both players must demonstrate movement' }
if ([Math]::Abs($hostMoved.players[1].position.z - $initial.players[1].position.z) -gt .1 -or
    [Math]::Abs($bothMoved.players[0].position.z - $hostMoved.players[0].position.z) -gt .1) { throw 'Remote player moved during the other player input phase' }
Write-Output 'PASS: each participant moved independently through the normal RPC movement and collection flow.'
