param(
    [string]$Profile1Time = '08:00',
    [string]$Profile2Time = '13:00',
    [string]$Profile3Time = '17:00',
    [string]$Profile4Time = '22:00',
    [int]$Profile1Enabled = 1,
    [int]$Profile2Enabled = 1,
    [int]$Profile3Enabled = 0,
    [int]$Profile4Enabled = 0
)

$ErrorActionPreference = 'Stop'
$controller = Join-Path $PSScriptRoot 'LGMonitorControl.exe'
if (-not (Test-Path -LiteralPath $controller)) {
    throw "LGMonitorControl.exe não foi encontrado em $PSScriptRoot"
}

$times = @($Profile1Time, $Profile2Time, $Profile3Time, $Profile4Time)
$enabled = @($Profile1Enabled, $Profile2Enabled, $Profile3Enabled, $Profile4Enabled)
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable

# Remove as tarefas antigas para não interferirem nos quatro perfis novos.
Unregister-ScheduledTask -TaskName 'LG Monitor - Dia' -Confirm:$false -ErrorAction SilentlyContinue
Unregister-ScheduledTask -TaskName 'LG Monitor - Noite' -Confirm:$false -ErrorAction SilentlyContinue

for ($i = 1; $i -le 4; $i++) {
    $taskName = "LG Monitor - Perfil $i"
    if ($enabled[$i - 1] -eq 1) {
        $action = New-ScheduledTaskAction -Execute $controller -Argument "--profile Profile$i"
        $trigger = New-ScheduledTaskTrigger -Daily -At $times[$i - 1]
        Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Description "Ativa o perfil $i do LG 20MK400H." -Force | Out-Null
    } else {
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
    }
}

Get-ScheduledTask | Where-Object TaskName -Like 'LG Monitor - Perfil *' | Select-Object TaskName,State
