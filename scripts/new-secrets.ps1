<#
.SYNOPSIS
    Generates random passwords for docker-compose.prod.yaml and docker-compose.monitoring.yaml into secrets/.
.DESCRIPTION
    Existing files are kept unless -Force is given: changing the PostgreSQL password does not
    change it inside an already initialized postgres_data volume.
    Works in Windows PowerShell 5.1 and PowerShell 7.
#>
param(
    [ValidateRange(16, 128)] [int] $Length = 32,
    [switch] $Force
)

$ErrorActionPreference = 'Stop'

$secretsDir = Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..')).Path 'secrets'
New-Item -ItemType Directory -Force -Path $secretsDir | Out-Null

function New-Password([int] $length) {
    # No look-alike characters (0/O, 1/l/I) and nothing that needs escaping in connection strings or shells.
    $alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789'
    # Bytes above the largest multiple of the alphabet size are rejected, so every character is equally likely.
    $limit = 256 - (256 % $alphabet.Length)
    $password = New-Object System.Text.StringBuilder
    $buffer = New-Object byte[] 64
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        while ($password.Length -lt $length) {
            $rng.GetBytes($buffer)
            foreach ($byte in $buffer) {
                if ($byte -lt $limit -and $password.Length -lt $length) {
                    [void] $password.Append($alphabet[$byte % $alphabet.Length])
                }
            }
        }
    }
    finally {
        $rng.Dispose()
    }

    return $password.ToString()
}

foreach ($name in 'postgres_password', 'pgadmin_password', 'grafana_admin_password', 'grafana_db_password') {
    $path = Join-Path $secretsDir "$name.txt"
    if ((Test-Path -LiteralPath $path) -and -not $Force) {
        Write-Output "kept     $path"
        continue
    }

    # No trailing newline: the files are read verbatim by PostgreSQL, pgAdmin and Grafana.
    [System.IO.File]::WriteAllText($path, (New-Password $Length))
    Write-Output "created  $path"
}
