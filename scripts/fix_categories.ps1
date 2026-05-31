$root = "d:\dev\SuperHexLink"
$files = @(
    "Assets\Scripts\Gameplay\DiceRoller.cs",
    "Assets\Scripts\Gameplay\VictoryChecker.cs",
    "Assets\Scripts\Gameplay\ProductionEngine.cs",
    "Assets\Scripts\Gameplay\ResourceManager.cs",
    "Assets\Scripts\Gameplay\PlacementValidator.cs",
    "Assets\Scripts\Rules\EconomicBalancer.cs",
    "Assets\Scripts\Rules\GameOntology.cs",
    "Assets\Scripts\Rules\RuleTimingSystem.cs",
    "Assets\Scripts\Rules\RulesEngine.cs",
    "Assets\Scripts\Rules\RuleConflictResolver.cs",
    "Assets\Scripts\Services\SelectionManager.cs",
    "Assets\Scripts\Performance\PerformanceMetrics.cs",
    "Assets\Scripts\Performance\PerformanceDashboard.cs"
)
foreach ($rel in $files) {
    $p = "$root\$rel"
    $c = [IO.File]::ReadAllText($p)
    $u = $c -replace 'ActionLogCategory\.(Gameplay|HexSelection|Rules|Performance)','ActionLogCategory.General'
    if ($u -ne $c) {
        [IO.File]::WriteAllText($p, $u)
        Write-Host "Updated: $rel"
    } else {
        Write-Host "No change: $rel"
    }
}
Write-Host "Done"
