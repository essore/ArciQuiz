[CmdletBinding()]
param(
    [ValidateSet("win-x64", "win-arm64")]
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputDirectory = "artifacts\\ArciQuiz-windows"
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$publishDirectory = if ([System.IO.Path]::IsPathRooted($OutputDirectory))
{
    $OutputDirectory
}
else
{
    Join-Path $repositoryRoot $OutputDirectory
}

$publishDirectory = [System.IO.Path]::GetFullPath($publishDirectory)

dotnet publish (Join-Path $repositoryRoot "Web\\Web.csproj") `
    --configuration Release `
    --runtime $RuntimeIdentifier `
    --self-contained true `
    --output $publishDirectory

if ($LASTEXITCODE -ne 0)
{
    throw "La pubblicazione Windows non è riuscita."
}

Copy-Item (Join-Path $PSScriptRoot "Avvia-ArciQuiz.cmd") (Join-Path $publishDirectory "Avvia-ArciQuiz.cmd") -Force
Copy-Item (Join-Path $repositoryRoot "docs\\DISTRIBUZIONE_WINDOWS.md") (Join-Path $publishDirectory "LEGGIMI-WINDOWS.md") -Force

$applicationPath = Join-Path $publishDirectory "ArciQuiz.exe"
if (-not (Test-Path -LiteralPath $applicationPath))
{
    throw "La pubblicazione non contiene ArciQuiz.exe."
}

Write-Host "Pacchetto creato in $publishDirectory"
Write-Host "Copia l'intera cartella e avvia Avvia-ArciQuiz.cmd dopo aver configurato appsettings.Local.json."
