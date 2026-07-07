$files = @('GameOntologyTests.cs','RulesEngineIntegrationTests.cs','ResourceManagerTests.cs','RulesEngineTests.cs','RuleConflictResolverTests.cs','EconomicBalancerTests.cs','RuleTimingSystemTests.cs')
$root = 'd:\dev\SuperHexLink\Assets\Tests\Editor'
$pattern = 'new ActionLogSettings\s*\{\s*IsEnabled = \(category, severity\) => true,\s*LogLevel = ActionLogSeverity\.Info\s*\}'
$replacement = 'ScriptableObject.CreateInstance<ActionLogSettings>()'
foreach ($f in $files) {
    $p = Join-Path $root $f
    $c = [IO.File]::ReadAllText($p)
    $u = [Regex]::Replace($c, $pattern, $replacement)
    if ($u -ne $c) { [IO.File]::WriteAllText($p, $u); Write-Host "Fixed $f" }
    else { Write-Host "No match in $f" }
}
