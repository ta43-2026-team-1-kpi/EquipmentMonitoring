<#
.SYNOPSIS
    End-to-end verification of the production stack (docker-compose.yaml + docker-compose.prod.yaml).
.DESCRIPTION
    Starts the stack and checks, in order:
      1. availability: health endpoints, Swagger, pgAdmin;
      2. functionality: equipment -> threshold -> measurement -> warning -> summary;
      3. security: no passwords in container environment, read-only non-root API,
         PostgreSQL unreachable outside the backend network, pgAdmin server pre-registered;
      4. persistence: data survives "restart" and "down" + "up" (volumes kept);
      5. backup: a fresh backup restores data deleted after it was taken.
    Exits with code 1 if any check fails. Works in Windows PowerShell 5.1 and PowerShell 7.

    The stack is restarted and recreated during the run (volumes are kept unless -Down is given).
    Rows created by the run are removed at the end, so it is safe against a stack with real data.
.EXAMPLE
    ./scripts/verify-stack.ps1
.EXAMPLE
    ./scripts/verify-stack.ps1 -ProjectName verify -Down
#>
param(
    [string[]] $ComposeFile = @('docker-compose.yaml', 'docker-compose.prod.yaml'),
    [string] $ProjectName = '',
    [switch] $SkipBuild,
    [switch] $Down
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $repoRoot

$composeArgs = @()
if ($ProjectName) { $composeArgs += @('-p', $ProjectName) }
foreach ($file in $ComposeFile) { $composeArgs += @('-f', $file) }

$results = New-Object System.Collections.Generic.List[object]

function Invoke-Native {
    param([scriptblock] $Command)

    # docker writes progress to stderr; in Windows PowerShell 5.1 that must not become a terminating error.
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $output = & $Command 2>&1 | ForEach-Object { "$_" } }
    finally { $ErrorActionPreference = $previous }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
}

function Invoke-Compose {
    param([Parameter(ValueFromRemainingArguments = $true)] [string[]] $Arguments)

    $result = Invoke-Native { docker compose @composeArgs @Arguments }
    $output = $result.Output
    if ($result.ExitCode -ne 0) {
        throw "docker compose $($Arguments -join ' ') failed:`n$($output | Out-String)"
    }
    return $output
}

function Invoke-ComposeInput {
    param([string] $InputText, [Parameter(ValueFromRemainingArguments = $true)] [string[]] $Arguments)

    # Passing SQL/Python through stdin avoids Windows PowerShell 5.1 quoting of native arguments.
    $result = Invoke-Native { $InputText | docker compose @composeArgs @Arguments }
    $output = $result.Output
    if ($result.ExitCode -ne 0) {
        throw "docker compose $($Arguments -join ' ') failed:`n$($output | Out-String)"
    }
    return $output
}

function Invoke-Http {
    param([string] $Method = 'GET', [string] $Url, $Body = $null, [hashtable] $Headers = @{})

    $request = @{ Method = $Method; Uri = $Url; UseBasicParsing = $true; TimeoutSec = 30; Headers = $Headers }
    if ($null -ne $Body) {
        $request.ContentType = 'application/json'
        $request.Body = ($Body | ConvertTo-Json -Depth 5)
    }

    try {
        $response = Invoke-WebRequest @request
        return [pscustomobject]@{ Status = [int]$response.StatusCode; Content = $response.Content }
    }
    catch {
        if ($null -eq $_.Exception.PSObject.Properties['Response'] -or $null -eq $_.Exception.Response) { throw }
        return [pscustomobject]@{ Status = [int]$_.Exception.Response.StatusCode; Content = '' }
    }
}

function Test-Check {
    param([string] $Group, [string] $Name, [scriptblock] $Check)

    try {
        $detail = & $Check
        $results.Add([pscustomobject]@{ Group = $Group; Check = $Name; Result = 'PASS'; Detail = "$detail" })
        Write-Output ("PASS  [{0}] {1} {2}" -f $Group, $Name, $detail)
    }
    catch {
        $results.Add([pscustomobject]@{ Group = $Group; Check = $Name; Result = 'FAIL'; Detail = $_.Exception.Message })
        Write-Output ("FAIL  [{0}] {1}: {2}" -f $Group, $Name, $_.Exception.Message)
    }
}

function Assert-That([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
}

function Get-DotEnv {
    $map = @{}
    if (Test-Path -LiteralPath '.env') {
        foreach ($line in Get-Content -LiteralPath '.env') {
            if ($line -match '^\s*(?<k>[A-Za-z_][A-Za-z0-9_]*)\s*=(?<v>.*)$') { $map[$matches.k] = $matches.v.Trim() }
        }
    }
    return $map
}

function Get-EquipmentBySerial([string] $SerialNumber) {
    $response = Invoke-Http -Url "$api/api/equipment"
    Assert-That ($response.Status -eq 200) "GET /api/equipment returned $($response.Status)"
    return ($response.Content | ConvertFrom-Json) | Where-Object { $_.serialNumber -eq $SerialNumber }
}

function Wait-Stack {
    Invoke-Compose up -d --wait --wait-timeout 300 | Out-Null
}

$serial = $null
try {
    $envMap = Get-DotEnv
    $apiPort = if ($envMap.ContainsKey('API_PORT')) { $envMap['API_PORT'] } else { '8080' }
    $pgAdminPort = if ($envMap.ContainsKey('PGADMIN_PORT')) { $envMap['PGADMIN_PORT'] } else { '5050' }
    $api = "http://localhost:$apiPort"
    $pgAdmin = "http://localhost:$pgAdminPort"
    $serial = 'SN-VERIFY-' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')

    $requiredSecrets = @('postgres_password', 'pgadmin_password')
    foreach ($secret in $requiredSecrets) {
        Assert-That (Test-Path -LiteralPath "secrets/$secret.txt") "secrets/$secret.txt is missing, run ./scripts/new-secrets.ps1"
    }
    $secrets = @{}
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $repoRoot 'secrets') -Filter '*.txt') {
        $secrets[$file.BaseName] = [System.IO.File]::ReadAllText($file.FullName).Trim()
    }
    $secretValues = @($secrets.Values)

    Write-Output '--- starting stack'
    Invoke-Compose config --quiet | Out-Null
    if ($SkipBuild) { Wait-Stack } else { Invoke-Compose up -d --build --wait --wait-timeout 300 | Out-Null }
    $config = Invoke-Compose config --format json | Out-String | ConvertFrom-Json
    $project = $config.name
    $postgresImage = $config.services.db.image

    Test-Check 'availability' 'all services healthy' {
        # Services without a healthcheck only have to be running.
        $states = @(Invoke-Compose ps --format '{{.Service}}={{.State}}/{{.Health}}')
        $unhealthy = @($states | Where-Object { $_ -notmatch '=running/(healthy)?$' })
        Assert-That ($unhealthy.Count -eq 0) "not healthy: $($unhealthy -join ', ')"
        $states -join ', '
    }
    foreach ($path in '/health/live', '/health/ready', '/openapi/v1.json', '/swagger/index.html') {
        Test-Check 'availability' "GET $path" {
            $status = (Invoke-Http -Url "$api$path").Status
            Assert-That ($status -eq 200) "status $status"
            $status
        }
    }
    Test-Check 'availability' 'pgAdmin' {
        $status = (Invoke-Http -Url "$pgAdmin/misc/ping").Status
        Assert-That ($status -eq 200) "status $status"
        $status
    }

    $script:equipmentId = $null
    Test-Check 'functional' 'register equipment' {
        $response = Invoke-Http -Method POST -Url "$api/api/equipment" -Body @{
            name = 'Verification pump'; type = 'Pump'; serialNumber = $serial; location = 'Workshop V'; status = 'Working'
        }
        Assert-That ($response.Status -eq 201) "status $($response.Status)"
        $script:equipmentId = ($response.Content | ConvertFrom-Json).id
        $script:equipmentId
    }
    Test-Check 'functional' 'threshold breach creates warning' {
        $threshold = Invoke-Http -Method PUT -Url "$api/api/equipment/$script:equipmentId/thresholds/Load" -Body @{ minValue = $null; maxValue = 90 }
        Assert-That ($threshold.Status -in 200, 201) "threshold status $($threshold.Status)"
        $measurement = Invoke-Http -Method POST -Url "$api/api/equipment/$script:equipmentId/measurements" -Body @{
            type = 'Load'; value = 97; unit = '%'; measuredAt = [DateTime]::UtcNow.AddMinutes(-1).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
        }
        Assert-That ($measurement.Status -eq 201) "measurement status $($measurement.Status)"
        $warnings = (Invoke-Http -Url "$api/api/warnings?equipmentId=$script:equipmentId").Content | ConvertFrom-Json
        Assert-That ($warnings.totalCount -eq 1) "expected 1 warning, got $($warnings.totalCount)"
        "warning $($warnings.items[0].id)"
    }
    Test-Check 'functional' 'summary counts the warning' {
        $summary = (Invoke-Http -Url "$api/api/warnings/summary").Content | ConvertFrom-Json
        $entry = @($summary.equipment) | Where-Object { $_.equipmentId -eq $script:equipmentId }
        Assert-That ($null -ne $entry -and $entry.activeCount -eq 1) 'equipment is missing from the summary'
        "totalActive=$($summary.totalActive)"
    }

    Test-Check 'security' 'no passwords in container environment' {
        $containers = @(Invoke-Compose ps -q)
        foreach ($container in $containers) {
            $environment = (Invoke-Native { docker inspect $container --format '{{json .Config.Env}}' }).Output | Out-String
            foreach ($value in $secretValues) {
                Assert-That (-not $environment.Contains($value)) "password found in environment of $container"
            }
        }
        "$($containers.Count) containers"
    }
    Test-Check 'security' 'API runs read-only as non-root' {
        $container = (Invoke-Compose ps -q api | Select-Object -First 1)
        $state = (Invoke-Native { docker inspect $container --format '{{.Config.User}}|{{.HostConfig.ReadonlyRootfs}}' }).Output | Out-String
        $user, $readOnly = $state.Trim().Split('|')
        Assert-That ($user -and $user -ne '0' -and $user -ne 'root') "user '$user'"
        Assert-That ($readOnly -eq 'true') 'root filesystem is writable'
        "user=$user"
    }
    Test-Check 'security' 'PostgreSQL unreachable outside backend network' {
        $db = (Invoke-Compose ps -q db | Select-Object -First 1)
        $bindings = "$((Invoke-Native { docker inspect $db --format '{{json .HostConfig.PortBindings}}' }).Output)".Trim()
        Assert-That ($bindings -eq '{}' -or $bindings -eq 'null') "published ports: $bindings"
        $outside = Invoke-Native { docker run --rm --network "${project}_monitoring_net" $postgresImage pg_isready -h db -t 3 }
        Assert-That ($outside.ExitCode -ne 0) 'db answered from monitoring_net'
        $inside = Invoke-Native { docker run --rm --network "${project}_backend" $postgresImage pg_isready -h db -t 3 }
        Assert-That ($inside.ExitCode -eq 0) 'db did not answer from backend'
        'only backend'
    }
    Test-Check 'security' 'pgAdmin has the server registered' {
        $python = "import sqlite3; print(sqlite3.connect('/var/lib/pgadmin/pgadmin4.db').execute('select host, passexec_cmd from server').fetchall())"
        $servers = (Invoke-ComposeInput $python exec -T pgadmin /venv/bin/python3 -) | Out-String
        Assert-That ($servers -match "'db'" -and $servers -match '/run/secrets/postgres_password') "servers: $servers"
        $servers.Trim()
    }

    Test-Check 'persistence' 'data survives restart' {
        Invoke-Compose restart | Out-Null
        Wait-Stack
        Assert-That ($null -ne (Get-EquipmentBySerial $serial)) 'equipment lost'
        'equipment found'
    }
    Test-Check 'persistence' 'data survives down + up' {
        Invoke-Compose down | Out-Null
        Wait-Stack
        Assert-That ($null -ne (Get-EquipmentBySerial $serial)) 'equipment lost'
        'equipment found'
    }

    Test-Check 'backup' 'backup restores deleted data' {
        $backup = Invoke-Compose exec -T backup /bin/sh /scripts/backup.sh --once
        Invoke-ComposeInput "DELETE FROM ""Equipment"" WHERE ""SerialNumber"" = '$serial';" exec -T db sh -c 'exec psql -v ON_ERROR_STOP=1 -q -U $POSTGRES_USER -d $POSTGRES_DB' | Out-Null
        Assert-That ($null -eq (Get-EquipmentBySerial $serial)) 'equipment was not deleted'

        Invoke-Compose stop api | Out-Null
        Invoke-Compose exec -T backup /bin/sh /scripts/restore.sh | Out-Null
        Wait-Stack

        Assert-That ($null -ne (Get-EquipmentBySerial $serial)) 'equipment not restored'
        $warnings = (Invoke-Http -Url "$api/api/warnings?equipmentId=$script:equipmentId").Content | ConvertFrom-Json
        Assert-That ($warnings.totalCount -eq 1) 'warning not restored'
        ($backup | Select-Object -Last 1)
    }
}
catch {
    $results.Add([pscustomobject]@{ Group = 'setup'; Check = 'stack'; Result = 'FAIL'; Detail = $_.Exception.Message })
    Write-Output "FAIL  [setup] $($_.Exception.Message)"
}
finally {
    if (-not $Down -and $serial) {
        # Measurements, thresholds and warnings of the probe equipment go with it (ON DELETE CASCADE).
        $cleanup = "DELETE FROM ""Equipment"" WHERE ""SerialNumber"" = '$serial';"
        Invoke-Native { $cleanup | docker compose @composeArgs exec -T db sh -c 'exec psql -q -U $POSTGRES_USER -d $POSTGRES_DB' } | Out-Null
    }
    if ($Down) {
        Invoke-Native { docker compose @composeArgs down -v } | Out-Null
    }
    Pop-Location
}

$failed = @($results | Where-Object Result -eq 'FAIL')
Write-Output ''
Write-Output ("{0} checks, {1} passed, {2} failed" -f $results.Count, ($results.Count - $failed.Count), $failed.Count)
if ($failed.Count -gt 0) { exit 1 }
