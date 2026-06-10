$ErrorActionPreference = "Continue"
$seeds = @(8, 11, 27)
$config = "myModels/SnakeVisual_fair.yaml"
$env = "Project/Builds/Snake/Visual/SnakeTrainigEnv.exe"
$timeScale = 30
$basePort = 5008

Write-Host "=== Snake Visual training started ===" -ForegroundColor Cyan

$i = 0
foreach ($s in $seeds) {
    $port = $basePort + $i

    Write-Host ""
    Write-Host "--- Snake Visual seed=$s (port $port) ---" -ForegroundColor Yellow

    # Kill any leftover environment from a previous run so it can't keep holding
    # the port or its log file (orphaned envs were causing connection timeouts).
    Get-Process SnakeTrainigEnv -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 5

    mlagents-learn $config --run-id=snake_visual_s$s --seed=$s --env=$env --base-port=$port --time-scale=$timeScale --force

    $i++
}

# Final cleanup so no environment lingers after the script ends.
Get-Process SnakeTrainigEnv -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host ""
Write-Host "=== Snake Visual training DONE ===" -ForegroundColor Green
