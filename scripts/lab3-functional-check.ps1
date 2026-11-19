param(
    [int] $ApiPort = 0,
    [string] $StatePath = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$composePath = Join-Path $repoRoot 'docker-compose.yaml'
$envPath = Join-Path $repoRoot '.env'

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

    $output = & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }

    return $output
}

function Get-CollectionItems {
    param($Value)

    if ($null -eq $Value) {
        return @()
    }

    if ($Value -is [System.Array]) {
        return @($Value)
    }

    if ($Value.PSObject.Properties.Name -contains 'items') {
        return @($Value.items)
    }

    return @($Value)
}

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)] [string] $Method,
        [Parameter(Mandatory = $true)] [string] $Url,
        [string] $JsonBody = ''
    )

    $tmp = [System.IO.Path]::GetTempFileName()
    $bodyFile = $null
    try {
        if ([string]::IsNullOrEmpty($JsonBody)) {
            $status = Invoke-CheckedExternal -Name "curl $Method $Url" -Command {
                curl.exe -sS -o $tmp -w "%{http_code}" -X $Method "$Url"
            }
        }
        else {
            $bodyFile = [System.IO.Path]::GetTempFileName()
            Set-Content -LiteralPath $bodyFile -Value $JsonBody -Encoding UTF8
            $status = Invoke-CheckedExternal -Name "curl $Method $Url" -Command {
                curl.exe -sS -o $tmp -w "%{http_code}" -X $Method -H "Content-Type: application/json" --data-binary "@$bodyFile" "$Url"
            }
        }

        $body = Get-Content -LiteralPath $tmp -Raw

        return [pscustomobject]@{
            StatusCode = [int]$status
            Body = $body
        }
    }
    finally {
        Remove-Item -LiteralPath $tmp -ErrorAction SilentlyContinue
        if ($bodyFile) {
            Remove-Item -LiteralPath $bodyFile -ErrorAction SilentlyContinue
        }
    }
}

if (-not (Test-Path -LiteralPath $composePath)) {
    throw "Compose file not found: $composePath"
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

$base = "http://localhost:$ApiPort"

$serialCore = "SN-LAB3-$([Guid]::NewGuid().ToString('N').Substring(0,8).ToUpperInvariant())"
$serialWithSpaces = "  $serialCore  "

$createBody = @{ name = 'Lab3 Equipment'; type = 'Compressor'; serialNumber = $serialWithSpaces; location = 'Lab Zone'; status = 'Working' } | ConvertTo-Json -Compress
$createResp = Invoke-Api -Method POST -Url "$base/api/equipment" -JsonBody $createBody
if ($createResp.StatusCode -ne 201) { throw "Create equipment failed: $($createResp.StatusCode) $($createResp.Body)" }
$createJson = $createResp.Body | ConvertFrom-Json
if ($createJson.serialNumber -ne $serialCore) {
    throw "Serial normalization mismatch. Expected '$serialCore', got '$($createJson.serialNumber)'."
}

$dupBody = @{ name = 'Lab3 Equipment Duplicate'; type = 'Compressor'; serialNumber = $serialWithSpaces; location = 'Lab Zone 2'; status = 'Working' } | ConvertTo-Json -Compress
$dupResp = Invoke-Api -Method POST -Url "$base/api/equipment" -JsonBody $dupBody
if ($dupResp.StatusCode -ne 400) { throw "Duplicate equipment expected 400, got $($dupResp.StatusCode). Body: $($dupResp.Body)" }
$dupJson = $dupResp.Body | ConvertFrom-Json
if (-not $dupJson.errors -or -not $dupJson.errors.serialNumber) {
    throw "Duplicate response does not contain serialNumber validation details."
}

$listResp = Invoke-Api -Method GET -Url "$base/api/equipment"
if ($listResp.StatusCode -ne 200) { throw "Get equipment list failed: $($listResp.StatusCode) $($listResp.Body)" }
$list = Get-CollectionItems -Value ($listResp.Body | ConvertFrom-Json)
if (-not ($list | Where-Object { $_.id -eq $createJson.id })) {
    throw "Created equipment id '$($createJson.id)' not found in full list."
}

$filterResp = Invoke-Api -Method GET -Url "$base/api/equipment?status=working"
if ($filterResp.StatusCode -ne 200) { throw "Get equipment filter failed: $($filterResp.StatusCode) $($filterResp.Body)" }
$filter = Get-CollectionItems -Value ($filterResp.Body | ConvertFrom-Json)
if (-not ($filter | Where-Object { $_.id -eq $createJson.id })) {
    throw "Created equipment id '$($createJson.id)' not found in status filter list."
}
if ($filter | Where-Object { $_.status -ne 'Working' }) {
    throw "Status filter contains records with status other than 'Working'."
}

$thrBody = @{ minValue = 0; maxValue = 80 } | ConvertTo-Json -Compress
$thrResp = Invoke-Api -Method PUT -Url "$base/api/thresholds/Temperature" -JsonBody $thrBody
if ($thrResp.StatusCode -ne 201 -and $thrResp.StatusCode -ne 200) { throw "Threshold upsert failed: $($thrResp.StatusCode) $($thrResp.Body)" }
$thrJson = $thrResp.Body | ConvertFrom-Json

$eqId = $createJson.id
$m1Body = '{"type":"Temperature","value":50,"unit":"\u00B0C","measuredAt":"2026-11-19T18:00:00Z"}'
$m1Resp = Invoke-Api -Method POST -Url "$base/api/equipment/$eqId/measurements" -JsonBody $m1Body
if ($m1Resp.StatusCode -ne 201) { throw "Measurement1 failed: $($m1Resp.StatusCode) $($m1Resp.Body)" }
$m1Json = $m1Resp.Body | ConvertFrom-Json

$m2Body = '{"type":"Temperature","value":90,"unit":"\u00B0C","measuredAt":"2026-11-19T18:10:00Z"}'
$m2Resp = Invoke-Api -Method POST -Url "$base/api/equipment/$eqId/measurements" -JsonBody $m2Body
if ($m2Resp.StatusCode -ne 201) { throw "Measurement2 failed: $($m2Resp.StatusCode) $($m2Resp.Body)" }
$m2Json = $m2Resp.Body | ConvertFrom-Json

$histResp = Invoke-Api -Method GET -Url "$base/api/equipment/$eqId/measurements?type=temperature&from=2026-11-19T18:00:00Z&to=2026-11-19T18:20:00Z"
if ($histResp.StatusCode -ne 200) { throw "History failed: $($histResp.StatusCode) $($histResp.Body)" }
$hist = Get-CollectionItems -Value ($histResp.Body | ConvertFrom-Json)

$histM1 = $hist | Where-Object { $_.id -eq $m1Json.id } | Select-Object -First 1
$histM2 = $hist | Where-Object { $_.id -eq $m2Json.id } | Select-Object -First 1
if (-not $histM1 -or -not $histM2) {
    throw "Measurement ids are missing in history response."
}
if ([double]$histM1.value -ne 50 -or [double]$histM2.value -ne 90) {
    throw "Unexpected measurement values in history response."
}

$warnResp = Invoke-Api -Method GET -Url "$base/api/warnings"
if ($warnResp.StatusCode -ne 200) { throw "Warnings failed: $($warnResp.StatusCode) $($warnResp.Body)" }
$warnJson = $warnResp.Body | ConvertFrom-Json
$warnItems = Get-CollectionItems -Value $warnJson
$warning = $warnItems | Where-Object { $_.equipmentId -eq $eqId -and $_.measurementId -eq $m2Json.id } | Select-Object -First 1
if (-not $warning) {
    throw "Expected warning for EquipmentId=$eqId and MeasurementId=$($m2Json.id) not found."
}
if ($warning.status -ne 'Active') {
    throw "Expected warning status 'Active', got '$($warning.status)'."
}

$summaryResp = Invoke-Api -Method GET -Url "$base/api/warnings/summary"
if ($summaryResp.StatusCode -ne 200) { throw "Warnings summary failed: $($summaryResp.StatusCode) $($summaryResp.Body)" }
$summaryJson = $summaryResp.Body | ConvertFrom-Json
$summaryTotalActive = if ($summaryJson.PSObject.Properties.Name -contains 'totalActiveWarnings') { [int]$summaryJson.totalActiveWarnings } else { [int]$summaryJson.totalActive }
if ($summaryTotalActive -lt 1) {
    throw "Warnings summary total active must be >= 1 before acknowledge."
}

$warningId = $warning.id
$ackResp = Invoke-Api -Method POST -Url "$base/api/warnings/$warningId/acknowledge"
if ($ackResp.StatusCode -ne 200) { throw "Acknowledge failed: $($ackResp.StatusCode) $($ackResp.Body)" }
$ackJson = $ackResp.Body | ConvertFrom-Json
if ($ackJson.status -ne 'Acknowledged') {
    throw "Expected acknowledged warning status, got '$($ackJson.status)'."
}

$planDate = (Get-Date).ToUniversalTime().AddDays(7).ToString('yyyy-MM-dd')
$planBody = @{ equipmentId = $eqId; plannedDate = $planDate; workType = 'Inspection'; responsibleEmployee = 'Lab Engineer'; note = 'Lab3 demo plan' } | ConvertTo-Json -Compress
$planResp = Invoke-Api -Method POST -Url "$base/api/maintenance-plans" -JsonBody $planBody
if ($planResp.StatusCode -ne 201) { throw "Plan create failed: $($planResp.StatusCode) $($planResp.Body)" }
$planJson = $planResp.Body | ConvertFrom-Json
if ($planJson.status -ne 'Planned') {
    throw "Expected plan initial status 'Planned', got '$($planJson.status)'."
}

$patchBody = @{ status = 'Completed' } | ConvertTo-Json -Compress
$patchResp = Invoke-Api -Method PATCH -Url "$base/api/maintenance-plans/$($planJson.id)/status" -JsonBody $patchBody
if ($patchResp.StatusCode -ne 200) { throw "Plan patch failed: $($patchResp.StatusCode) $($patchResp.Body)" }
$patchJson = $patchResp.Body | ConvertFrom-Json
if ($patchJson.status -ne 'Completed') {
    throw "Expected plan final status 'Completed', got '$($patchJson.status)'."
}

$state = [ordered]@{
    RepoRoot = $repoRoot
    ComposePath = $composePath
    EnvPath = $envPath
    ApiPort = $ApiPort
    BaseUrl = $base
    EquipmentId = $eqId
    EquipmentSerialOriginal = $serialWithSpaces
    EquipmentSerialExpectedNormalized = $serialCore
    EquipmentSerialStored = $createJson.serialNumber
    Measurement1Id = $m1Json.id
    Measurement1Value = 50
    Measurement2Id = $m2Json.id
    Measurement2Value = 90
    WarningId = $warningId
    WarningMeasurementId = $m2Json.id
    WarningStatusAfterAcknowledge = $ackJson.status
    MaintenancePlanId = $planJson.id
    MaintenancePlanInitialStatus = $planJson.status
    MaintenancePlanFinalStatus = $patchJson.status
    ThresholdId = $thrJson.id
    ThresholdMaxValue = 80
}
$state | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $StatePath -Encoding UTF8

$activeWarningsCount = if ($warnJson.PSObject.Properties.Name -contains 'totalCount') { [int]$warnJson.totalCount } else { $warnItems.Count }

$result = [ordered]@{
    ComposePath = $composePath
    ApiPort = $ApiPort
    BaseUrl = $base
    CreateEquipmentStatus = $createResp.StatusCode
    CreateEquipmentSerialReturned = $createJson.serialNumber
    DuplicateStatus = $dupResp.StatusCode
    ListStatus = $listResp.StatusCode
    ListCount = $list.Count
    FilterStatus = $filterResp.StatusCode
    FilterCount = $filter.Count
    ThresholdUpsertStatus = $thrResp.StatusCode
    Measurement1Status = $m1Resp.StatusCode
    Measurement2Status = $m2Resp.StatusCode
    HistoryStatus = $histResp.StatusCode
    HistoryCount = $hist.Count
    WarningsStatus = $warnResp.StatusCode
    ActiveWarningsCount = $activeWarningsCount
    MatchingWarningId = $warningId
    SummaryStatus = $summaryResp.StatusCode
    SummaryTotalActive = $summaryTotalActive
    AcknowledgeStatus = $ackResp.StatusCode
    PlanCreateStatus = $planResp.StatusCode
    PlanStatusUpdateStatus = $patchResp.StatusCode
    PlanFinalStatus = $patchJson.status
    StatePath = $StatePath
    IDs = $state
}

$result | ConvertTo-Json -Depth 6
