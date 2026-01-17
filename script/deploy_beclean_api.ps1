param(
    [Parameter(Mandatory=$false)]
    [string]$OutputFolder = "../PackageOutput",

    [Parameter(Mandatory=$false)]
    [ValidateSet("major", "minor", "patch")]
    [string]$VersionIncrement = "patch",
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectPath = "../BeClean/BeClean.Api",
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release"
)

$deployScriptPath = Join-Path $PSScriptRoot "deploy_folder.ps1"
& $deployScriptPath `
    -OutputFolder $OutputFolder `
    -VersionIncrement $VersionIncrement `
    -ProjectPath $ProjectPath `
    -Configuration $Configuration