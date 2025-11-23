Set-Location -Path $PSScriptRoot\..\
git switch feature/hex-state-refactor
try { git commit --allow-empty -m 'ci: trigger unity self-hosted tests' } catch { Write-Host 'No changes to commit (empty commit may have been refused)'}
git push origin feature/hex-state-refactor
Write-Host 'Pushed branch to origin'
