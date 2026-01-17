#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("major", "minor", "patch")]
    [string]$VersionIncrement = "patch",

    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-testbench-dev",
    
    [Parameter(Mandatory=$false)]
    [string]$WebAppName = "app-testbench-dev",
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectPath = "../src/Technosub.CloudService/Technosub.CloudService.Api",
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$false)]
    [string]$PublishProfile
)

# Function to generate version file
function New-VersionFile {
    param([string]$OutputPath)

    $versionPath = Join-Path $OutputPath "version.json"
    $actualVersion = Get-ActualVersion -VersionFilePath $versionPath

    $version = @{
        Version = (Increment-Version -CurrentVersion $actualVersion.Version).ToString()
        BuildDate = Get-Date -Format o
        GitCommit = ""
        GitBranch = ""
        BuildMachine = Get-BuildMachineName
        DeployedBy = Get-User
    }
    
    # Try to get Git information
    try {
        if((git rev-parse HEAD 2>$null) -ne $null){
            $version.GitCommit = (git rev-parse HEAD 2>$null)
        } else {
            $version.GitCommit = "Unknown"
        }

        if((git rev-parse --abbrev-ref HEAD 2>$null) -ne $null){
            $version.GitBranch = (git rev-parse --abbrev-ref HEAD 2>$null)
        } else {
            $version.GitBranch = "Unknown"
        }
    }
    catch {
        Write-Warning "Git information not available"
    }
    
    $versionJson = $version | ConvertTo-Json -Depth 2
    $versionJson | Out-File -FilePath $versionPath -Encoding UTF8
    
    Write-Host "Version file created: $versionPath" -ForegroundColor Green
    Write-Host "Version Info:" -ForegroundColor Cyan
    Write-Host $versionJson -ForegroundColor Gray
    
    return $versionPath
}

function Get-ActualVersion{
    param([string]$VersionFilePath)

    if (Test-Path -Path $VersionFilePath -PathType Leaf) {
        return Get-Content -Path $VersionFilePath -Raw | ConvertFrom-Json
    }
    else {
        return @{
            Version = ([version]"1.0.0").ToString()
            BuildDate = ""
            GitCommit = ""
            GitBranch = ""
            BuildMachine = ""
            DeployedBy = ""
        }
    }
}

function Increment-Version{
    param([version]$CurrentVersion)

    if($VersionIncrement -eq "patch"){
        return [version]::new($CurrentVersion.Major, $CurrentVersion.Minor, $CurrentVersion.Build + 1)
    } elseif ($VersionIncrement -eq "minor"){
        return [version]::new($CurrentVersion.Major, $CurrentVersion.Minor + 1, 0)
    } else {
        return [version]::new($CurrentVersion.Major + 1, 0, 0)
    }
}

function Get-BuildMachineName{
    if($env:COMPUTERNAME -ne $null){
        return $env:COMPUTERNAME
    } elseif($env:HOSTNAME -ne $null) {
        return $env:HOSTNAME
    } else {
        return "Unknown"
    }
}

function Get-User{
    if($env:USER -ne $null){
        return $env:USER
    } elseif($env:USERNAME -ne $null) {
        return $env:USERNAME
    } else {
        return "Unknown"
    }
}


# Function to build and publish
function Build-Application {
    param([string]$ProjectPath, [string]$Configuration, [string]$OutputPath)
    
    Write-Host "Building application..." -ForegroundColor Yellow
    
    # Restore packages
    dotnet restore $ProjectPath
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore packages"
    }
    
    # Build and publish
    dotnet publish $ProjectPath -c $Configuration -o $OutputPath --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build/publish application"
    }
    
    Write-Host "Application built successfully" -ForegroundColor Green
}

# Function to create zip package
function New-DeploymentPackage {
    param([string]$SourcePath, [string]$ZipPath)
    
    Write-Host "Creating deployment package..." -ForegroundColor Yellow
    
    if (Test-Path $ZipPath) {
        Remove-Item $ZipPath -Force
    }
    
    # Create zip file
    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        Compress-Archive -Path "$SourcePath/*" -DestinationPath $ZipPath -Force
    } else {
        # Use system zip on macOS/Linux
        Push-Location $SourcePath
        zip -r $ZipPath . -x "*.DS_Store*" "*.git*"
        Pop-Location
    }
    
    Write-Host "Package created: $ZipPath" -ForegroundColor Green
}

# Function to deploy to Azure
function Deploy-ToAzure {
    param([string]$ZipPath, [string]$WebAppName, [string]$ResourceGroupName, [string]$SubscriptionId)
    
    Write-Host "Deploying to Azure Web App..." -ForegroundColor Yellow
    
    # Set subscription if provided
    if ($SubscriptionId) {
        az account set --subscription $SubscriptionId
    }
    
    # Deploy using Azure CLI
    az webapp deployment source config-zip --resource-group $ResourceGroupName --name $WebAppName --src $ZipPath
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to deploy to Azure"
    }
    
    Write-Host "Deployment completed successfully!" -ForegroundColor Green
    Write-Host "Your app should be available at: https://$WebAppName.azurewebsites.net" -ForegroundColor Cyan
}

# Main execution
try {
    Write-Host "Starting Azure Web App deployment..." -ForegroundColor Magenta
    
    # Validate prerequisites
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw ".NET CLI not found. Please install .NET SDK"
    }
    
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        throw "Azure CLI not found. Please install Azure CLI"
    }
    
    # Check if logged in to Azure
    $azAccount = az account show 2>$null | ConvertFrom-Json
    if (-not $azAccount) {
        Write-Host "Please login to Azure CLI first:" -ForegroundColor Yellow
        Write-Host "az login" -ForegroundColor Cyan
        exit 1
    }
    
    # Setup paths
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $tempDir = Join-Path $env:TEMP "webapp_deploy_$timestamp"
    $publishDir = Join-Path $tempDir "publish"
    $zipPath = Join-Path $tempDir "deploy.zip"
    
    # Create temp directory
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
    
    # Generate version file
    New-VersionFile -OutputPath $ProjectPath

    # Build application
    Build-Application -ProjectPath $ProjectPath -Configuration $Configuration -OutputPath $publishDir
    
    # Create deployment package
    New-DeploymentPackage -SourcePath $publishDir -ZipPath $zipPath
    
    # Deploy to Azure
    Deploy-ToAzure -ZipPath $zipPath -WebAppName $WebAppName -ResourceGroupName $ResourceGroupName -SubscriptionId $SubscriptionId
    
    # Cleanup
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    
    Write-Host "Deployment completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}