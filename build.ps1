$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "Source/SelectiveReTend.csproj"
dotnet build $project -c Release

$assembly = Join-Path $PSScriptRoot "1.6/Assemblies/SelectiveReTend.dll"
if (-not (Test-Path $assembly)) {
    throw "The build completed without producing $assembly"
}

Write-Host "Built: $assembly"
