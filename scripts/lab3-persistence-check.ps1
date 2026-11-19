param(
    [int] $ApiPort = 0,
    [string] $StatePath = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$composePath = Join-Path $repoRoot 'docker-compose.yaml'
$envPath = Join-Path $repoRoot '.env'

if (-not (Test-Path -LiteralPath $composePath)) {
    throw "Compose file not found: $composePath"
}

function Get-EnvMap {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $map = @{}
    if (-not (Test-Path -LiteralPath $Path)) {
        return $map
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -match '^(?<k>[^#=]+)=(?<v>.*)$') {
            $map[$matches.k.Trim()] = $matches.v.Trim()
        }
    }

    return $map
}

function Invoke-CheckedExternal {
    param(
        [Parameter(Mandatory = $true)] [scriptblock] $Command,
        [Parameter(Mandatory = $true)] [string] $Name
    )

    $result = & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }

    return $result
}

function Get-CollectionItems {
    param($Value)

    if ($null -eq $Value) { return @() }
    if ($Value -is [System.Array]) { return @($Value) }
    if ($Value.PSObject.Properties.Name -contains 'items') { return @($Value.items) }
    return @($Value)
}

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)] [string] $Method,
        [Parameter(Mandatory = $true)] [string] $Url
    )

    $tmp = [System.IO.Path]::GetTempFileName()
    try {
        $status = Invoke-CheckedExternal -Name "curl $Method $Url" -Command {
            curl.exe -sS -o $tmp -w "%{http_code}" -X $Method "$Url"
        }

        $body = Get-Content -LiteralPath $tmp -Raw
        return [pscustomobject]@{
            StatusCode = [int]$status
            Body = $body
        }
    }
    finally {
        Remove-Item -LiteralPath $tmp -ErrorAction SilentlyContinue
    }
}

function Wait-ApiReady {
    param([int] $TimeoutSeconds = 180)

    $start = Get-Date
    do {
        try {
            $probe = Invoke-Api -Method GET -Url "$base/openapi/v1.json"
            if ($probe.StatusCode -eq 200) {
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 2
    } while (((Get-Date) - $start).TotalSeconds -lt $TimeoutSeconds)

    throw "API did not become ready within $TimeoutSeconds seconds."
}

function Check-Data {
    param([string] $Phase)

    $eq = Invoke-Api -Method GET -Url "$base/api/equipment"
    if ($eq.StatusCode -ne 200) { throw "[$Phase] Equipment list failed: $($eq.StatusCode) $($eq.Body)" }
    $eqItems = Get-CollectionItems -Value ($eq.Body | ConvertFrom-Json)
    $foundEq = $eqItems | Where-Object { $_.id -eq $state.EquipmentId } | Select-Object -First 1
    if (-not $foundEq) { throw "[$Phase] Equipment ID not found: $($state.EquipmentId)" }

    $hist = Invoke-Api -Method GET -Url "$base/api/equipment/$($state.EquipmentId)/measurements?type=temperature&from=2026-11-19T18:00:00Z&to=2026-11-19T18:20:00Z"
    if ($hist.StatusCode -ne 200) { throw "[$Phase] History failed: $($hist.StatusCode) $($hist.Body)" }
    $histItems = Get-CollectionItems -Value ($hist.Body | ConvertFrom-Json)
    $foundM1 = $histItems | Where-Object { $_.id -eq $state.Measurement1Id } | Select-Object -First 1
    $foundM2 = $histItems | Where-Object { $_.id -eq $state.Measurement2Id } | Select-Object -First 1
    if (-not $foundM1 -or -not $foundM2) { throw "[$Phase] Measurement IDs not found in history." }

    $warn = Invoke-Api -Method GET -Url "$base/api/warnings/$($state.WarningId)"
    if ($warn.StatusCode -ne 200) { throw "[$Phase] Warning fetch failed: $($warn.StatusCode) $($warn.Body)" }
    $warnJson = $warn.Body | ConvertFrom-Json
    if ($warnJson.equipmentId -ne $state.EquipmentId) {
        throw "[$Phase] Warning equipmentId mismatch."
    }
    if ($warnJson.measurementId -ne $state.WarningMeasurementId) {
        throw "[$Phase] Warning measurementId mismatch."
    }

    $plans = Invoke-Api -Method GET -Url "$base/api/maintenance-plans?equipmentId=$($state.EquipmentId)"
    if ($plans.StatusCode -ne 200) { throw "[$Phase] Plans list failed: $($plans.StatusCode) $($plans.Body)" }
    $plansItems = Get-CollectionItems -Value ($plans.Body | ConvertFrom-Json)
    $foundPlan = $plansItems | Where-Object { $_.id -eq $state.MaintenancePlanId } | Select-Object -First 1
    if (-not $foundPlan) { throw "[$Phase] Maintenance plan ID not found." }

    return [pscustomobject]@{
        Phase = $Phase
        EquipmentId = $foundEq.id
        SerialNumber = $foundEq.serialNumber
        Measurement1Id = $foundM1.id
        Measurement1Value = [double]$foundM1.value
        Measurement2Id = $foundM2.id
        Measurement2Value = [double]$foundM2.value
        WarningId = $warnJson.id
        WarningStatus = $warnJson.status
        PlanId = $foundPlan.id
        PlanStatus = $foundPlan.status
    }
}

function Assert-SnapshotEquals {
    param(
        [Parameter(Mandatory = $true)] $Left,
        [Parameter(Mandatory = $true)] $Right,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    if ($Left.EquipmentId -ne $Right.EquipmentId) { throw "[$Label] EquipmentId changed." }
    if ($Left.SerialNumber -ne $Right.SerialNumber) { throw "[$Label] SerialNumber changed." }
    if ($Left.Measurement1Id -ne $Right.Measurement1Id -or $Left.Measurement2Id -ne $Right.Measurement2Id) { throw "[$Label] Measurement IDs changed." }
    if ($Left.Measurement1Value -ne $Right.Measurement1Value -or $Left.Measurement2Value -ne $Right.Measurement2Value) { throw "[$Label] Measurement values changed." }
    if ($Left.WarningId -ne $Right.WarningId) { throw "[$Label] Warning ID changed." }
    if ($Left.WarningStatus -ne $Right.WarningStatus) { throw "[$Label] Warning status changed." }
    if ($Left.PlanId -ne $Right.PlanId) { throw "[$Label] Maintenance plan ID changed." }
    if ($Left.PlanStatus -ne $Right.PlanStatus) { throw "[$Label] Maintenance plan status changed." }
}

$envMap = Get-EnvMap -Path $envPath
if ($ApiPort -le 0) {
    if ($envMap.ContainsKey('API_PORT')) {
        $ApiPort = [int]$envMap['API_PORT']
    }
    else {
        $ApiPort = 8080
    }
}

if ([string]::IsNullOrWhiteSpace($StatePath)) {
    $StatePath = Join-Path $env:TEMP 'equipmentmonitoring-lab3-state.json'
}

if (-not (Test-Path -LiteralPath $StatePath)) {
    throw "State file not found. Run scripts/lab3-functional-check.ps1 first: $StatePath"
}

$state = Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json
$base = "http://localhost:$ApiPort"

Wait-ApiReady
$before = Check-Data -Phase 'before-restart'

Write-Output '--- docker compose restart ---'
Invoke-CheckedExternal -Name 'docker compose restart' -Command {
    docker compose -f "$composePath" restart
} | Out-Null
Wait-ApiReady
$afterRestart = Check-Data -Phase 'after-restart'
Assert-SnapshotEquals -Left $before -Right $afterRestart -Label 'after-restart'

Write-Output '--- docker compose down/up (without -v) ---'
Invoke-CheckedExternal -Name 'docker compose down' -Command {
    docker compose -f "$composePath" down
} | Out-Null
Invoke-CheckedExternal -Name 'docker compose up -d' -Command {
    docker compose -f "$composePath" up -d
} | Out-Null
Wait-ApiReady
$afterDownUp = Check-Data -Phase 'after-down-up'
Assert-SnapshotEquals -Left $before -Right $afterDownUp -Label 'after-down-up'

$result = [ordered]@{
    RepoRoot = $repoRoot
    ComposePath = $composePath
    BaseUrl = $base
    StatePath = $StatePath
    EquipmentId = $state.EquipmentId
    Measurement1Id = $state.Measurement1Id
    Measurement2Id = $state.Measurement2Id
    WarningId = $state.WarningId
    MaintenancePlanId = $state.MaintenancePlanId
    Checks = @($before, $afterRestart, $afterDownUp)
}

$result | ConvertTo-Json -Depth 6
