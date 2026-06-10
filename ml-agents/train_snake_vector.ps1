$ErrorActionPreference = "Continue"
$seeds = @(8, 11, 27)
$config = "myModels/SnakeVector_fair.yaml"
$env = "Project/Builds/Snake/Vector/SnakeTrainigEnv.exe"
$timeScale = 30
$basePort = 5005

Write-Host "=== Snake Vector training started ===" -ForegroundColor Cyan

$i = 0
foreach ($s in $seeds) {
    $port = $basePort + $i

    Write-Host ""
    Write-Host "--- Snake Vector seed=$s (port $port) ---" -ForegroundColor Yellow

    # Kill any leftover environment from a previous run so it can't keep holding
    # the port or its log file (orphaned envs were causing connection timeouts).
    Get-Process SnakeTrainigEnv -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 5

    mlagents-learn $config --run-id=snake_vector_s$s --seed=$s --env=$env --base-port=$port --no-graphics --time-scale=$timeScale --force

    $i++
}

# Final cleanup so no environment lingers after the script ends.
Get-Process SnakeTrainigEnv -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host ""
Write-Host "=== Snake Vector training DONE ===" -ForegroundColor Green
