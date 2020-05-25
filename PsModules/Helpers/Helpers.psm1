<#
 .Synopsis
  Test module powershell
#>

function Show-LogSeparator()
{
    Write-Host "=================================================================" -ForegroundColor Green
}

function Show-Infos($message, $path)
{
    Show-LogSeparator
    $file = Split-Path -Path $path -leaf
    Write-Host " $message $file" -ForegroundColor Green
    Show-LogSeparator   
}

function Show-Header()
{
    Show-Infos -message "Start" -path $MyInvocation.PSCommandPath
}

function Show-Trailer()
{
    Show-Infos -message "End" -path $MyInvocation.PSCommandPath
}

Export-ModuleMember -Function Show-Header
Export-ModuleMember -Function Show-Trailer