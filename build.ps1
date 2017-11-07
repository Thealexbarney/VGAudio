<#
.SYNOPSIS
This is a Powershell script to bootstrap a Cake build.
.DESCRIPTION
This Powershell script will the .NET Core CLI if missing,
and execute your Cake build script with the parameters you provide.
.PARAMETER Target
The build script target to run.
.PARAMETER Configuration
The build configuration to use.
.PARAMETER Verbosity
Specifies the amount of information to be displayed.
.PARAMETER WhatIf
Performs a dry run of the build script.
No tasks will be executed.
.PARAMETER ScriptArgs
Remaining arguments are added here.
.LINK
http://cakebuild.net
#>

[CmdletBinding()]
Param(
    [string]$Target = "Default",
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity = "Normal",
    [switch]$WhatIf,
    [switch]$NetCore,
    [switch]$NetFramework,
    [switch]$Uwp,
    [switch]$Build,
    [switch]$Test,
    [switch]$Clean,
    [switch]$CleanAll,
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$ScriptArgs
)

$dotnetCliVersion = Get-Content DotnetCLIVersion.txt

# Define directories.
$basePath = Split-Path $MyInvocation.MyCommand.Path -Parent
$buildPath = Join-Path $basePath "build"
$dotnetPath = Join-Path $basePath "tools/dotnet"
$dotnetCliPath = Join-Path $dotnetPath "cli"
$globalJsonFile = Join-Path $basePath global.json

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

function SetupDotnetCli() {
    $json = "{`"projects`":[],`"sdk`":{`"version`":`"$dotnetCliVersion`"}}"
    Out-File -FilePath $globalJsonFile -Encoding utf8 -InputObject $json

    if ((Get-Command "dotnet" -errorAction SilentlyContinue) -and (& dotnet --version) -eq $dotnetCliVersion) {  
        return
    }

    $appDataInstallPath = Join-Path $env:LocalAppData "Microsoft/dotnet"
    $path = Join-Path $appDataInstallPath dotnet.exe
    if ((Test-Path $path) -and (& $path --version) -eq $dotnetCliVersion) {
        $env:Path = "$appDataInstallPath;" + $env:path      
        return
    }
    
    $path = Join-Path $dotnetCliPath dotnet.exe
    if ((Test-Path $path) -and (& $path --version) -eq $dotnetCliVersion) {
        $env:Path = "$dotnetCliPath;" + $env:path     
        return
    }
    
    Write-Output "Downloading .NET Core CLI..."
    try {
        & (Join-Path $dotnetPath dotnet-install.ps1) -InstallDir $dotnetCliPath -Version $dotnetCliVersion -NoPath
    } catch {
        Write-Output "Error downloading .NET Core CLI"
    }
    if ((Test-Path $path) -and (& $path --version) -eq $dotnetCliVersion) {
        $env:Path = "$dotnetCliPath;" + $env:path 
        return
    }

    Write-Output "Unable to locate or download .NET Core CLI version $dotnetCliVersion"
    exit 1
}

# Make sure .NET Core CLI exists.
try {
    Push-Location $buildPath
    $originalEnvPath = $env:Path
    SetupDotnetCli

    # HP sets the "Platform" environment variable with some of their software
    # Clear that to allow the build to work
    $env:Platform = ""

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

    # Build the argument list.
    $Arguments = @{
        target=$Target;
        configuration=$Configuration;
        verbosity=$Verbosity;
        dryrun=$WhatIf;
        core=$NetCore;
        full=$NetFramework;
        uwp=$Uwp;
        build=$Build;
        test=$Test;
        clean=$Clean;
        cleanall=$CleanAll;
    }.GetEnumerator() | ForEach-Object {"--{0}=`"{1}`"" -f $_.key, $_.value };

    # Start Cake
    Write-Output "Running build..."
	& dotnet publish /v:q /nologo
    & dotnet bin/Debug/netcoreapp2.0/publish/Build.dll $Arguments
    exit $LASTEXITCODE;

} finally {
    Pop-Location;
    Remove-Item $globalJsonFile
    $env:Path = $originalEnvPath
}
