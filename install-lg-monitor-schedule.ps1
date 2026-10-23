param(
    [string]$Profile1Time = '08:00', [string]$Profile2Time = '13:00',
    [string]$Profile3Time = '17:00', [string]$Profile4Time = '22:00',
    [int]$Profile1Enabled = 1, [int]$Profile2Enabled = 1,
    [int]$Profile3Enabled = 0, [int]$Profile4Enabled = 0,
    [string]$DayTime = '', [string]$NightTime = ''
)
$ErrorActionPreference = 'Stop'
$log = Join-Path $PSScriptRoot 'lg-monitor-schedule.log'
function Log($message) { Add-Content -LiteralPath $log -Value ((Get-Date -Format 'yyyy-MM-dd HH:mm:ss') + ' ' + $message) }
Log "INICIO args: P1=$Profile1Time/$Profile1Enabled P2=$Profile2Time/$Profile2Enabled P3=$Profile3Time/$Profile3Enabled P4=$Profile4Time/$Profile4Enabled Day=$DayTime Night=$NightTime"
try {
    $controller = Join-Path $PSScriptRoot 'LGMonitorControl.exe'
    if (-not (Test-Path -LiteralPath $controller -PathType Leaf)) { throw "LGMonitorControl.exe nao foi encontrado em $PSScriptRoot" }
    # Compatibilidade com a tela antiga de dois perfis.
    if ($DayTime) { $Profile1Time = $DayTime; $Profile1Enabled = 1 }
    if ($NightTime) { $Profile3Time = $NightTime; $Profile3Enabled = 1; $Profile2Enabled = 0 }
    $times = @($Profile1Time, $Profile2Time, $Profile3Time, $Profile4Time)
    $enabled = @($Profile1Enabled, $Profile2Enabled, $Profile3Enabled, $Profile4Enabled)
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable
    Unregister-ScheduledTask -TaskName 'LG Monitor - Dia' -Confirm:$false -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName 'LG Monitor - Noite' -Confirm:$false -ErrorAction SilentlyContinue
    for ($i = 1; $i -le 4; $i++) {
        $taskName = "LG Monitor - Perfil $i"
        if ($enabled[$i - 1] -eq 1) {
            Log "Registrando $taskName em $($times[$i - 1])"
            $action = New-ScheduledTaskAction -Execute $controller -Argument "--profile Profile$i"
            $trigger = New-ScheduledTaskTrigger -Daily -At $times[$i - 1]
            Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Description "Ativa o perfil $i do LG Monitor." -Force | Out-Null
        } else { Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue }
    }
    Log 'SUCESSO'
    exit 0
} catch { Log ("ERRO: " + $_.Exception.ToString()); [Console]::Error.WriteLine($_.Exception.Message); exit 1 }
