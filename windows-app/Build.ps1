$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$root = $PSScriptRoot
$build = Join-Path $root 'build'
$out = Join-Path (Split-Path -Parent $root) 'Wird-Setup.exe'
New-Item -ItemType Directory -Force -Path $build | Out-Null

& $compiler /nologo /target:winexe /optimize+ /out:(Join-Path $build 'Wird.exe') /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll (Join-Path $root 'WirdDesktop.cs')
if ($LASTEXITCODE -ne 0) { throw 'Failed to build the desktop app.' }

$resource = '/resource:' + (Join-Path $build 'Wird.exe') + ',WirdDesktopPayload.exe'
& $compiler /nologo /target:winexe /optimize+ /out:$out /reference:System.dll /reference:System.Windows.Forms.dll $resource (Join-Path $root 'WirdInstaller.cs')
if ($LASTEXITCODE -ne 0) { throw 'Failed to build the installer.' }
Get-Item -LiteralPath $out

