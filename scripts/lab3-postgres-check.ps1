$ErrorActionPreference = 'Stop'
$state = Get-Content "$env:TEMP\equipmentmonitoring-lab3-state.json" | ConvertFrom-Json

$envData = @{}
Get-Content "C:\visual project\EquipmentMonitoring\.env" | ForEach-Object {
    if ($_ -match '^(?<k>[^#=]+)=(?<v>.*)$') {
        $envData[$matches.k] = $matches.v
    }
}

$db = $envData['POSTGRES_DB']
$user = $envData['POSTGRES_USER']

$q1 = 'select "Id", "SerialNumber", "Status" from "Equipment" where "Id" = ''' + $state.EquipmentId + ''';'
$q2 = 'select "Id", "EquipmentId", "Type", "Value", "MeasuredAtUtc" from "Measurements" where "Id" in (''' + $state.Measurement1Id + ''',''' + $state.Measurement2Id + ''') order by "MeasuredAtUtc";'
$q3 = 'select "Id", "Status", "AcknowledgedAtUtc" from "Warnings" where "Id" = ''' + $state.WarningId + ''';'
$q4 = 'select "Id", "EquipmentId", "PlannedDate", "Status" from "MaintenancePlans" where "Id" = ''' + $state.MaintenancePlanId + ''';'

$queries = @(
    $q1,
    $q2,
    $q3,
    $q4
)

foreach ($q in $queries) {
    $sqlFile = [System.IO.Path]::GetTempFileName()
    try {
        Set-Content -Path $sqlFile -Value $q -Encoding UTF8
        docker cp $sqlFile equipment-monitoring-db:/tmp/lab3-check.sql | Out-Null
        docker exec equipment-monitoring-db psql -U $user -d $db -f /tmp/lab3-check.sql
    }
    finally {
        Remove-Item -Path $sqlFile -ErrorAction SilentlyContinue
    }
}
