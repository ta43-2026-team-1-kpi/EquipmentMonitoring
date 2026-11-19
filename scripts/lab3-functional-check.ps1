$ErrorActionPreference = 'Stop'
$base = 'http://localhost:8080'
$statePath = Join-Path $env:TEMP 'equipmentmonitoring-lab3-state.json'

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
            $status = curl.exe -s -o $tmp -w "%{http_code}" -X $Method $Url
        }
        else {
            $bodyFile = [System.IO.Path]::GetTempFileName()
            Set-Content -Path $bodyFile -Value $JsonBody -Encoding UTF8
            $status = curl.exe -s -o $tmp -w "%{http_code}" -X $Method -H "Content-Type: application/json" --data-binary "@$bodyFile" $Url
        }

        $body = Get-Content -Path $tmp -Raw

        return [pscustomobject]@{
            StatusCode = [int]$status
            Body = $body
        }
    }
    finally {
        Remove-Item -Path $tmp -ErrorAction SilentlyContinue
        if ($bodyFile) {
            Remove-Item -Path $bodyFile -ErrorAction SilentlyContinue
        }
    }
}

$serialCore = "SN-LAB3-$([Guid]::NewGuid().ToString('N').Substring(0,8).ToUpperInvariant())"
$serialWithSpaces = "  $serialCore  "

$createBody = @{ name='Lab3 Equipment'; type='Compressor'; serialNumber=$serialWithSpaces; location='Lab Zone'; status='Working' } | ConvertTo-Json -Compress
$createResp = Invoke-Api -Method POST -Url "$base/api/equipment" -JsonBody $createBody
if ($createResp.StatusCode -ne 201) { throw "Create equipment failed: $($createResp.StatusCode) $($createResp.Body)" }
$createJson = $createResp.Body | ConvertFrom-Json

$dupBody = @{ name='Lab3 Equipment Duplicate'; type='Compressor'; serialNumber=$serialWithSpaces; location='Lab Zone 2'; status='Working' } | ConvertTo-Json -Compress
$dupResp = Invoke-Api -Method POST -Url "$base/api/equipment" -JsonBody $dupBody

$listResp = Invoke-Api -Method GET -Url "$base/api/equipment"
if ($listResp.StatusCode -ne 200) { throw "Get equipment list failed: $($listResp.StatusCode) $($listResp.Body)" }
$list = $listResp.Body | ConvertFrom-Json

$filterResp = Invoke-Api -Method GET -Url "$base/api/equipment?status=working"
if ($filterResp.StatusCode -ne 200) { throw "Get equipment filter failed: $($filterResp.StatusCode) $($filterResp.Body)" }
$filter = $filterResp.Body | ConvertFrom-Json

$thrBody = @{ minValue=0; maxValue=80 } | ConvertTo-Json -Compress
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
$hist = $histResp.Body | ConvertFrom-Json

$warnResp = Invoke-Api -Method GET -Url "$base/api/warnings"
if ($warnResp.StatusCode -ne 200) { throw "Warnings failed: $($warnResp.StatusCode) $($warnResp.Body)" }
$warnJson = $warnResp.Body | ConvertFrom-Json

$summaryResp = Invoke-Api -Method GET -Url "$base/api/warnings/summary"
if ($summaryResp.StatusCode -ne 200) { throw "Warnings summary failed: $($summaryResp.StatusCode) $($summaryResp.Body)" }
$summaryJson = $summaryResp.Body | ConvertFrom-Json

$warningId = $null
if ($warnJson.items -and $warnJson.items.Count -gt 0) {
    $warningId = $warnJson.items[0].id
}

$ackStatus = $null
if ($warningId) {
    $ackResp = Invoke-Api -Method POST -Url "$base/api/warnings/$warningId/acknowledge"
    $ackStatus = $ackResp.StatusCode
}

$planDate = (Get-Date).ToUniversalTime().AddDays(7).ToString('yyyy-MM-dd')
$planBody = @{ equipmentId=$eqId; plannedDate=$planDate; workType='Inspection'; responsibleEmployee='Lab Engineer'; note='Lab3 demo plan' } | ConvertTo-Json -Compress
$planResp = Invoke-Api -Method POST -Url "$base/api/maintenance-plans" -JsonBody $planBody
if ($planResp.StatusCode -ne 201) { throw "Plan create failed: $($planResp.StatusCode) $($planResp.Body)" }
$planJson = $planResp.Body | ConvertFrom-Json

$patchBody = @{ status='Completed' } | ConvertTo-Json -Compress
$patchResp = Invoke-Api -Method PATCH -Url "$base/api/maintenance-plans/$($planJson.id)/status" -JsonBody $patchBody
if ($patchResp.StatusCode -ne 200) { throw "Plan patch failed: $($patchResp.StatusCode) $($patchResp.Body)" }
$patchJson = $patchResp.Body | ConvertFrom-Json

$state = [ordered]@{
    EquipmentId = $eqId
    EquipmentSerialOriginal = $serialWithSpaces
    EquipmentSerialExpectedNormalized = $serialCore
    EquipmentSerialStored = $createJson.serialNumber
    Measurement1Id = $m1Json.id
    Measurement2Id = $m2Json.id
    WarningId = $warningId
    MaintenancePlanId = $planJson.id
    ThresholdId = $thrJson.id
}
$state | ConvertTo-Json | Set-Content -Path $statePath -Encoding UTF8

$result = [ordered]@{
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
    ActiveWarningsCount = $warnJson.totalCount
    SummaryStatus = $summaryResp.StatusCode
    SummaryTotalActive = $summaryJson.totalActiveWarnings
    AcknowledgeStatus = $ackStatus
    PlanCreateStatus = $planResp.StatusCode
    PlanStatusUpdateStatus = $patchResp.StatusCode
    PlanFinalStatus = $patchJson.status
    StatePath = $statePath
    IDs = $state
}

$result | ConvertTo-Json -Depth 6
