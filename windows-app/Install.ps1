$ErrorActionPreference = 'Stop'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\Wird'
$startMenu = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$startup = [Environment]::GetFolderPath('Startup')
$desktop = [Environment]::GetFolderPath('Desktop')
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\WirdTasbeeh'

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Wird.exe') -Destination (Join-Path $installDir 'Wird.exe') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Uninstall.ps1') -Destination (Join-Path $installDir 'Uninstall.ps1') -Force

$shell = New-Object -ComObject WScript.Shell
foreach ($shortcutPath in @(
  (Join-Path $startMenu 'Wird.lnk'),
  (Join-Path $desktop 'وِرد.lnk'),
  (Join-Path $startup 'Wird.lnk')
)) {
  $shortcut = $shell.CreateShortcut($shortcutPath)
  $shortcut.TargetPath = Join-Path $installDir 'Wird.exe'
  $shortcut.WorkingDirectory = $installDir
  $shortcut.Description = 'وِرد — تذكير التسبيح'
  $shortcut.IconLocation = (Join-Path $installDir 'Wird.exe') + ',0'
  $shortcut.Save()
}

New-Item -Path $uninstallKey -Force | Out-Null
Set-ItemProperty -Path $uninstallKey -Name DisplayName -Value 'وِرد — تذكير التسبيح'
Set-ItemProperty -Path $uninstallKey -Name DisplayVersion -Value '1.0.0'
Set-ItemProperty -Path $uninstallKey -Name Publisher -Value 'Wird'
Set-ItemProperty -Path $uninstallKey -Name InstallLocation -Value $installDir
Set-ItemProperty -Path $uninstallKey -Name UninstallString -Value ('powershell.exe -NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $installDir 'Uninstall.ps1') + '"')
Start-Process -FilePath (Join-Path $installDir 'Wird.exe')

