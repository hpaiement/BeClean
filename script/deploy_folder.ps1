#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory=$true)]
    [string]$OutputFolder,

    [Parameter(Mandatory=$false)]
    [ValidateSet("major", "minor", "patch")]
    [string]$VersionIncrement = "patch",
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectPath = "../src/Technosub.CloudService/Technosub.CloudService.Api",
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release"
)

# Function to deploy to Azure
function Deploy-ToFolder {
    param([string]$ProjectPath, [string]$Configuration, [string]$OutputFolder)
    
    Write-Host "Deploying to folder as nuget package..." -ForegroundColor Yellow
    
    dotnet pack $ProjectPath --configuration $Configuration --output $OutputFolder

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to deploy"
    }
}

# Import functions
$commonDeployFunctions = Join-Path $PSScriptRoot "deploy_common.ps1"
. $commonDeployFunctions

# Main execution
try {
    Write-Host "Starting deployment..." -ForegroundColor Magenta
    
    # Validate prerequisites
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw ".NET CLI not found. Please install .NET SDK"
    }
    
    # Setup paths
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $tempDir = Join-Path $env:TEMP "webapp_deploy_$timestamp"
    $publishDir = Join-Path $tempDir "publish"
    
    # Create temp directory
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
    
    # Generate version file
    New-VersionFile -OutputPath $ProjectPath

    # Build application
    Build-Application -ProjectPath $ProjectPath -Configuration $Configuration -OutputPath $publishDir

    # Copy to package folder 
    Deploy-ToFolder -ProjectPath $ProjectPath -Configuration $Configuration -OutputFolder $OutputFolder

    # Cleanup
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    
    Write-Host "Deployment completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}