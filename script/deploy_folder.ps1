#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory=$true)]
    [string]$OutputFolder,

    [Parameter(Mandatory=$false)]
    [ValidateSet("major", "minor", "patch")]
    [string]$VersionIncrement = "patch",
    
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath,

    [Parameter(Mandatory=$true)]
    [string]$CsProjFileName,
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",

    [Parameter(Mandatory=$false)]
    [switch]$GenerateVersionFile = $false
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

function Set-CsprojVersion{
    param(
        [string]$CsProjFilePath,
        [version]$Version
    )

    if (Test-Path -Path $CsProjFilePath) {
        [xml]$csProjXml = Get-Content -Path $CsProjFilePath

        $propertyGroupElement = $csProjXml.Project.PropertyGroup

        if ($propertyGroupElement.VersionPrefix -eq $null) {
            # If it doesn't exist, create it
            $newElement = $csProjXml.CreateElement("VersionPrefix")
            $newElement.InnerText = $Version.ToString()
            
            # Add it to the parent
            $propertyGroupElement.AppendChild($newElement)
        }
        else {
            # If it exists, just update the value
            $propertyGroupElement.VersionPrefix = $Version.ToString()
        }

        $csProjXml.Save((Convert-Path $CsProjFilePath))
    }
    else{
        Write-Host "The csproj file was not found, version could not be incremented" -ForegroundColor Yellow
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
    $csProjFilePath = Join-Path $ProjectPath $CsProjFileName
    
    # Create temp directory
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
    
    # Get new version
    $NewVersion = Get-NewVersion -VersionFileFolder $ProjectPath -VersionIncrement $VersionIncrement

    # Update csproj version
    Set-CsprojVersion -CsProjFilePath $csProjFilePath -Version $NewVersion

    # Generate version file
    if($GenerateVersionFile){
        Generate-NewVersionFile -VersionFilePath $ProjectPath -Version $NewVersion
    }

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