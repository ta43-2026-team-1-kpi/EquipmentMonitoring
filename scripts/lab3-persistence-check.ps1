$ErrorActionPreference = 'Stop'
$base = 'http://localhost:8080'
$statePath = Join-Path $env:TEMP 'equipmentmonitoring-lab3-state.json'
$state = Get-Content $statePath | ConvertFrom-Json

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)] [string] $Method,
        [Parameter(Mandatory = $true)] [string] $Url
    )

    $tmp = [System.IO.Path]::GetTempFileName()
    try {
        $status = curl.exe -s -o $tmp -w "%{http_code}" -X $Method $Url
        $body = Get-Content -Path $tmp -Raw

        return [pscustomobject]@{
            StatusCode = [int]$status
            Body = $body
        }
    }
    finally {
        Remove-Item -Path $tmp -ErrorAction SilentlyContinue
    }
}

function Wait-ApiReady {
    param([int] $TimeoutSeconds = 180)

    $start = Get-Date
    do {
        try {
            $probe = Invoke-WebRequest -UseBasicParsing -Uri "$base/openapi/v1.json" -TimeoutSec 10
            if ($probe.StatusCode -eq 200) {
                return
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }

        Start-Sleep -Milliseconds 500
    } while (((Get-Date) - $start).TotalSeconds -lt $TimeoutSeconds)

    throw "API did not become ready within $TimeoutSeconds seconds."
}

function Check-Data {
    param([string] $Phase)

    $eq = Invoke-Api -Method GET -Url "$base/api/equipment"
    if ($eq.StatusCode -ne 200) { throw "[$Phase] Equipment list failed: $($eq.StatusCode) $($eq.Body)" }
    $eqItems = $eq.Body | ConvertFrom-Json
    $foundEq = $eqItems | Where-Object { $_.id -eq $state.EquipmentId }
    if (-not $foundEq) { throw "[$Phase] Equipment ID not found: $($state.EquipmentId)" }

    $hist = Invoke-Api -Method GET -Url "$base/api/equipment/$($state.EquipmentId)/measurements?type=temperature&from=2026-11-19T18:00:00Z&to=2026-11-19T18:20:00Z"
    if ($hist.StatusCode -ne 200) { throw "[$Phase] History failed: $($hist.StatusCode) $($hist.Body)" }
    $histItems = $hist.Body | ConvertFrom-Json
    $foundM1 = $histItems | Where-Object { $_.id -eq $state.Measurement1Id }
    $foundM2 = $histItems | Where-Object { $_.id -eq $state.Measurement2Id }
    if (-not $foundM1 -or -not $foundM2) { throw "[$Phase] Measurement IDs not found in history." }

    $warn = Invoke-Api -Method GET -Url "$base/api/warnings/$($state.WarningId)"
    if ($warn.StatusCode -ne 200) { throw "[$Phase] Warning fetch failed: $($warn.StatusCode) $($warn.Body)" }
    $warnJson = $warn.Body | ConvertFrom-Json

    $plans = Invoke-Api -Method GET -Url "$base/api/maintenance-plans?equipmentId=$($state.EquipmentId)"
    if ($plans.StatusCode -ne 200) { throw "[$Phase] Plans list failed: $($plans.StatusCode) $($plans.Body)" }
    $plansItems = $plans.Body | ConvertFrom-Json
    $foundPlan = $plansItems | Where-Object { $_.id -eq $state.MaintenancePlanId }
    if (-not $foundPlan) { throw "[$Phase] Maintenance plan ID not found." }

    return [pscustomobject]@{
        Phase = $Phase
        EquipmentFound = $true
        SerialNumber = $foundEq.serialNumber
        Measurement1Found = $true
        Measurement2Found = $true
        WarningStatus = $warnJson.status
        PlanStatus = $foundPlan.status
    }
}

Wait-ApiReady
$before = Check-Data -Phase 'before-restart'

Write-Output '--- docker compose restart ---'
docker compose -f "C:\visual project\EquipmentMonitoring\docker-compose.yaml" restart | Out-Null
Wait-ApiReady
$afterRestart = Check-Data -Phase 'after-restart'

Write-Output '--- docker compose down/up (without -v) ---'
docker compose -f "C:\visual project\EquipmentMonitoring\docker-compose.yaml" down | Out-Null
docker compose -f "C:\visual project\EquipmentMonitoring\docker-compose.yaml" up -d | Out-Null
Wait-ApiReady
$afterDownUp = Check-Data -Phase 'after-down-up'

$result = [ordered]@{
    StatePath = $statePath
    EquipmentId = $state.EquipmentId
    Measurement1Id = $state.Measurement1Id
    Measurement2Id = $state.Measurement2Id
    WarningId = $state.WarningId
    MaintenancePlanId = $state.MaintenancePlanId
    Checks = @($before, $afterRestart, $afterDownUp)
}

$result | ConvertTo-Json -Depth 6
