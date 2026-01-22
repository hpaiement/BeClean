#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("major", "minor", "patch")]
    [string]$VersionIncrement = "patch",

    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-testbench-dev",
    
    [Parameter(Mandatory=$false)]
    [string]$WebAppName = "app-testbench-dev",
    
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath,
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$false)]
    [string]$PublishProfile
)

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

# Import functions
$commonDeployFunctions = Join-Path $PSScriptRoot "deploy_common.ps1"
. $commonDeployFunctions

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
    
    # Get new version
    $NewVersion = Get-NewVersion -VersionFileFolder $ProjectPath -VersionIncrement $VersionIncrement

    # Update csproj version
    Set-CsprojVersion -CsProjFilePath $csProjFilePath -Version $NewVersion

    # Generate version file
    Generate-NewVersionFile -VersionFilePath $ProjectPath -Version $NewVersion

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