$ErrorActionPreference = "Continue"
$seeds = @(8, 11, 27)
$config = "myModels/FlappyBirdVisual_fair.yaml"
$env = "builds/FlappyVisual/FlappyBirdTrainingEnv.exe"
$timeScale = 30

Write-Host "=== Visual training started ===" -ForegroundColor Cyan

foreach ($s in $seeds) {
    Write-Host ""
    Write-Host "--- Visual seed=$s ---" -ForegroundColor Yellow

    mlagents-learn $config --run-id=vector_s$s --seed=$s --env=$env --time-scale=$timeScale --force
}

Write-Host ""
Write-Host "=== Visual training DONE ===" -ForegroundColor Green
