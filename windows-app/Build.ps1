$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$root = $PSScriptRoot
$build = Join-Path $root 'build'
$out = Join-Path (Split-Path -Parent $root) 'Wird-Setup.exe'
$appOut = Join-Path $build 'Wird.exe'
New-Item -ItemType Directory -Force -Path $build | Out-Null

& $compiler /nologo /target:winexe /optimize+ ('/out:' + $appOut) /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll /reference:Microsoft.CSharp.dll (Join-Path $root 'WirdDesktop.cs')
if ($LASTEXITCODE -ne 0) { throw 'Failed to build the desktop app.' }

$resource = '/resource:' + $appOut + ',WirdDesktopPayload.exe'
& $compiler /nologo /target:winexe /optimize+ ('/out:' + $out) /reference:System.dll /reference:System.Windows.Forms.dll /reference:Microsoft.CSharp.dll $resource (Join-Path $root 'WirdInstaller.cs')
if ($LASTEXITCODE -ne 0) { throw 'Failed to build the installer.' }
Get-Item -LiteralPath $out