$feedName = "LocalDevFeed"
$feedPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\NuGetLocalDevFeed"))

if (-not (dotnet nuget list source | Select-String $feedName)) {
    dotnet nuget add source $feedPath -n $feedName
}

./build-and-pack.ps1 -OutputDir $feedPath -Version "0.0.0-dev-$(Get-Date -Format 'yyyyMMddHHmmss')"