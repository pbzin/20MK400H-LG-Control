$ErrorActionPreference = 'Stop'

$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source = Join-Path $PSScriptRoot 'LGMonitorControl.cs'
$output = Join-Path $PSScriptRoot 'LGMonitorControl.exe'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw ".NET Framework C# compiler not found: $compiler"
}

& $compiler `
    /nologo `
    /target:winexe `
    /optimize+ `
    /platform:anycpu `
    /reference:System.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    "/out:$output" `
    $source

if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE."
}

Write-Host "Built: $output"
