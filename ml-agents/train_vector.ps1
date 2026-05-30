$ErrorActionPreference = "Continue"
$seeds = @(8, 11, 27)
$config = "myModels/FlappyBirdVector_fair.yaml"
$env = "builds/FlappyVector/FlappyBirdTrainingEnv.exe"
$timeScale = 30

Write-Host "=== Vector training started ===" -ForegroundColor Cyan

foreach ($s in $seeds) {
    Write-Host ""
    Write-Host "--- Vector seed=$s ---" -ForegroundColor Yellow

    mlagents-learn $config --run-id=vector_s$s --seed=$s --env=$env --no-graphics --time-scale=$timeScale --force
}

Write-Host ""
Write-Host "=== Vector training DONE ===" -ForegroundColor Green
