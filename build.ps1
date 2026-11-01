$ErrorActionPreference = 'Stop'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Set-Location $PSScriptRoot
& $csc /nologo /optimize+ /target:winexe /codepage:65001 /out:ClaudeTray.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll ClaudeTray.cs
if ($LASTEXITCODE -ne 0) { throw "Build failed: $LASTEXITCODE" }
'Build OK'
