$ErrorActionPreference = 'SilentlyContinue'
$installDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$startMenu = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Wird.lnk'
$startup = Join-Path ([Environment]::GetFolderPath('Startup')) 'Wird.lnk'
$desktop = Join-Path ([Environment]::GetFolderPath('Desktop')) 'وِرد.lnk'
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\WirdTasbeeh'
Get-Process -Name Wird -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-Item -LiteralPath $startMenu,$startup,$desktop -Force -ErrorAction SilentlyContinue
Remove-Item -Path $uninstallKey -Recurse -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500
Remove-Item -LiteralPath $installDir -Recurse -Force -ErrorAction SilentlyContinue

