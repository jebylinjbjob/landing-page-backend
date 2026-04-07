[CmdletBinding()]
param(
    [string]$EnvironmentName = "Staging",
    [string]$Configuration = "Release",
    [string]$DeployPath = "\\Vjbdev03\jb\landing_page_backend",
    # 對應公開 URL 中路徑前綴，例如 https://rd.jbhr.com.tw/m/api/landing_page_backend → /m/api/landing_page_backend。sensitive.json 該環境的 PathBase 若存在則覆寫此預設
    [string]$ApplicationPathBase = "/m/api/landing_page_backend"
)

$ErrorActionPreference = "Stop"

$settingsFileName = "appsettings.json"
$sensitiveRootName = "landing_page_backend"
$projectRoot = $PSScriptRoot
$projectFile = Join-Path $projectRoot "landing-page-backend.csproj"
$sourcePath = Join-Path $projectRoot "bin\$Configuration\net8.0\publish"
$sensitiveFilePath = Join-Path $projectRoot "..\..\sensitive.json"
$offlineFilePath = Join-Path $sourcePath "app_offline.htm"

Push-Location $projectRoot
try {
    dotnet publish $projectFile -c $Configuration "/p:EnvironmentName=$EnvironmentName"
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed (exit code: $LASTEXITCODE)"
    }

    if (-not (Test-Path $sourcePath)) {
        throw "Publish output path not found: $sourcePath"
    }

    $sensitiveRaw = Get-Content $sensitiveFilePath -Raw -Encoding UTF8
    # Normalize hidden characters that can break ConvertFrom-Json
    $sensitiveRaw = $sensitiveRaw.TrimStart([char]0xFEFF).Replace([string][char]0, "")
    try {
        $sensitiveData = $sensitiveRaw | ConvertFrom-Json
    }
    catch {
        throw "Invalid JSON in sensitive file: $sensitiveFilePath. Please fix JSON format first."
    }
    $appsettingsPath = Join-Path $sourcePath $settingsFileName
    $appsettingsData = Get-Content $appsettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json

    $envSecrets = $sensitiveData.$sensitiveRootName.$EnvironmentName
    if (-not $envSecrets) {
        throw "Cannot find secrets for environment: $EnvironmentName"
    }

    # Support both structures:
    # 1) { SqlServer: "..." } (current landing_page_backend)
    # 2) { OthersDefaultConnection, JobHereDefaultConnection, DefaultConnection } (legacy projects)
    if ($envSecrets.SqlServer) {
        $appsettingsData.ConnectionStrings.SqlServer = $envSecrets.SqlServer
    }
    else {
        $requiredKeys = @("OthersDefaultConnection", "JobHereDefaultConnection", "DefaultConnection")
        $missingKeys = @()
        foreach ($key in $requiredKeys) {
            if (-not $envSecrets.$key) {
                $missingKeys += $key
            }
        }

        if ($missingKeys.Count -gt 0) {
            throw "Secrets are missing required keys for environment '$EnvironmentName': $($missingKeys -join ', ')"
        }

        $appsettingsData.ConnectionStrings.OthersDefaultConnection = $envSecrets.OthersDefaultConnection
        $appsettingsData.ConnectionStrings.JobHereDefaultConnection = $envSecrets.JobHereDefaultConnection
        $appsettingsData.ConnectionStrings.DefaultConnection = $envSecrets.DefaultConnection
    }

    $valuePathBase = if ($null -eq $ApplicationPathBase) { "" } else { $ApplicationPathBase.Trim() }
    if ($envSecrets.PSObject.Properties.Name -contains 'PathBase' -and -not [string]::IsNullOrWhiteSpace([string]$envSecrets.PathBase)) {
        $valuePathBase = [string]$envSecrets.PathBase.Trim()
    }

    if (-not ($appsettingsData.PSObject.Properties.Name -contains 'Application')) {
        $appsettingsData | Add-Member -MemberType NoteProperty -Name Application -Value ([pscustomobject]@{ PathBase = $valuePathBase })
    }
    elseif (-not ($appsettingsData.Application.PSObject.Properties.Name -contains 'PathBase')) {
        $appsettingsData.Application | Add-Member -MemberType NoteProperty -Name PathBase -Value $valuePathBase
    }
    else {
        $appsettingsData.Application.PathBase = $valuePathBase
    }
    Write-Host "Application:PathBase -> $valuePathBase"

    Write-Host "-------- appsettings after injection --------"
    $appsettingsData | Format-Custom
    $appsettingsData | ConvertTo-Json -Depth 20 | Out-File -FilePath $appsettingsPath -Force -Encoding UTF8

    Get-ChildItem (Join-Path $sourcePath "appsettings.*.json") -ErrorAction SilentlyContinue | Remove-Item -Force

    if (-not (Test-Path $offlineFilePath)) {
        Set-Content -Path $offlineFilePath -Value "<html><body><h1>Maintenance</h1></body></html>" -Encoding UTF8
    }

    Copy-Item -Path $offlineFilePath -Destination $DeployPath -Force

    $secs = 5
    Write-Host "start countdown"
    do {
        Write-Host $secs
        Start-Sleep -Seconds 1
        $secs--
    } while ($secs -gt 0)

    robocopy $sourcePath $DeployPath /MIR /XD "logs" "uploads"
    $robocopyExitCode = $LASTEXITCODE
    if ($robocopyExitCode -gt 7) {
        throw "robocopy failed (exit code: $robocopyExitCode)"
    }
}
finally {
    Remove-Item -Path (Join-Path $DeployPath "app_offline.htm") -ErrorAction SilentlyContinue
    Pop-Location
}

