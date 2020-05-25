param ($GitSourceDirectory, $PropsDir)
# ============================================================
# Rem relatives path are from powershell script directory
# ============================================================

$ErrorActionPreference = "Stop"

Import-Module -Name $PSScriptRoot\PsModules\Helpers #-Verbose
#Show-Header

Push-Location $PSScriptRoot

$GitPath = "C:\Program Files\Git\cmd\git.exe"
$NewFile = New-TemporaryFile
$TmpFile = $Newfile.FullName

if ( $null -eq $PropsDir )
{
    $PropsDir = (Get-Location).Path
}
$PropsFile = "${PropsDir}\Directory.Build.props"
$TemplateFile = "$PropsFile.template.xml"

#echo "GitSource = $GitSourceDirectory"
#echo "PropsFile = $PropsFile"
#echo "TemplateFile = $TemplateFile"

$GitHash = &$GitPath log -n 1 --full-history --pretty=format:%h $GitSourceDirectory
Write-Output "Githash on '$GitSourceDirectory' = '$GitHash' for target '$PropsFile'"

$Content = (Get-Content -path $TemplateFile -Raw) -replace "{GIT}","$GitHash"

Set-Content -Path $TmpFile -Value $Content

if ( -not (Test-Path $PropsFile -PathType leaf) -or  ((Get-FileHash $TmpFile).Hash -ne (Get-FileHash $PropsFile).Hash) )
{
    Write-Output "Updating $PropsFile"
    Set-Content -Path $PropsFile -Value $Content
}

Remove-Item $TmpFile

Pop-Location

#Show-Trailer