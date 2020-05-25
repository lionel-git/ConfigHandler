# ============================================================
# Deploy nugets
# ============================================================

$ErrorActionPreference = "Stop"

Import-Module -Name $PSScriptRoot\PsModules\Helpers #-Verbose
Show-Header

Push-Location $PSScriptRoot

$VersionDll = ".\ConfigHandler\bin\Release\netstandard2.0\ConfigHandler.dll"
$ProductVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($VersionDll).ProductVersion
$NugetVersion = $ProductVersion.Split("+")[0]
$NugetFile = "ConfigHandler.${NugetVersion}.nupkg"
$SourceFile = ".\ConfigHandler\bin\Release\$NugetFile"
$TargetFile = "\\freebox\maxtor\MyNugets\$NugetFile"

if (Test-Path $SourceFile -PathType leaf)
{
    Write-Output "Checking file '$NugetFile'"
    if (-not (Test-Path $TargetFile -PathType leaf))
    {
        Copy-Item .\ConfigHandler\bin\Release\$NugetFile -Destination $TargetFile
        Write-Output "Copied to $TargetFile."
    }
    else
    {
        Write-Output "Nuget alreday deployed, dot not copy."
    }
}
else
{
    Write-Output "Source file $SourceFile does not exists?"
}

Pop-Location

Show-Trailer