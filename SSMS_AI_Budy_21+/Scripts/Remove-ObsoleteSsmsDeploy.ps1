#Requires -Version 5.1
# Removes the old Program Files\Extensions\SsmsAIBuddy deploy that SSMS 22 never loads.
$legacy = @(
    "${env:ProgramFiles}\Microsoft SQL Server Management Studio 22\Release\Common7\IDE\Extensions\SsmsAIBuddy",
    "${env:ProgramFiles}\Microsoft SQL Server Management Studio 21\Release\Common7\IDE\Extensions\SsmsAIBuddy"
)
foreach ($path in $legacy) {
    if (Test-Path -LiteralPath $path) {
        Write-Host "Removing $path"
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}
