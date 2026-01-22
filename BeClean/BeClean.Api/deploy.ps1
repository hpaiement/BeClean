param(
    [Parameter(Mandatory=$false)]
    [string]$OutputFolder = "../../PackageOutput",

    [Parameter(Mandatory=$false)]
    [ValidateSet("major", "minor", "patch")]
    [string]$VersionIncrement = "patch",
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release"
)

$deployScriptPath = Join-Path $PSScriptRoot "../../script/deploy_folder.ps1"
& $deployScriptPath `
    -OutputFolder $OutputFolder `
    -VersionIncrement $VersionIncrement `
    -ProjectPath "./" `
    -CsProjFileName "BeClean.Api.csproj" `
    -Configuration $Configuration `
    -GenerateVersionFile