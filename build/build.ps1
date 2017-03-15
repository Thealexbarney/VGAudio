<#
.SYNOPSIS
This is a Powershell script to bootstrap a Cake build.
.DESCRIPTION
This Powershell script will download NuGet if missing, restore NuGet tools (including Cake)
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
    [string]$Verbosity = "Verbose",
    [switch]$WhatIf,
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$ScriptArgs
)

$cakeVersion = "0.18.0"
$dotnetCliVersion = "1.0.1"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

# Define directories.
$basePath = Split-Path (Split-Path $MyInvocation.MyCommand.Path -Parent) -Parent
$buildPath = Join-Path $basePath "build"
$cakePath = Join-Path $basePath "tools\cake"
$dotnetPath = Join-Path $basePath "tools\dotnet"
$dotnetCliPath = Join-Path $dotnetPath "cli"
$scriptPath = Join-Path $buildPath "build.cake"

$originalEnvPath = $env:Path

# Make sure cake folder exists
if (!(Test-Path $cakePath)) {
    New-Item -Path $cakePath -Type directory | out-null
}

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

function SetupDotnetCli()
{
    $json = "{`"projects`":[],`"sdk`":{`"version`":`"$dotnetCliVersion`"}}"
    Out-File -FilePath (Join-Path $buildPath global.json) -Encoding utf8 -InputObject $json

    if ((Get-Command "dotnet" -errorAction SilentlyContinue) -and (& dotnet --version) -eq $dotnetCliVersion) {  
        return
    }

    $appDataInstallPath = Join-Path $env:LocalAppData "Microsoft\dotnet"
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
    Pop-Location
    exit 1
}

# Make sure .NET Core CLI exists.
Push-Location $buildPath
SetupDotnetCli
Pop-Location

###########################################################################
# INSTALL NUGET
###########################################################################

# Download NuGet if it does not exist.
$nugetPath = Join-Path $cakePath "nuget.exe"
if (!(Test-Path $nugetPath)) {
    Write-Output "Downloading nuget.exe..."
    try {
        (New-Object System.Net.WebClient).DownloadFile($nugetUrl, $nugetPath);
    } catch {
        Throw "An error occured while downloading nuget.exe."
    }
}

###########################################################################
# INSTALL CAKE
###########################################################################

$cakeExePath = Join-Path $cakePath "Cake.$cakeVersion/Cake.exe"
if (!(Test-Path $cakeExePath)) {
    Write-Output "Installing Cake..."
    & "$nugetPath" install Cake -Version $cakeVersion -OutputDirectory "$cakePath" | Out-Null;
    if ($LASTEXITCODE -ne 0) {
        Throw "An error occured while restoring Cake from NuGet."
    }
}

# Make sure Cake has been installed.
if (!(Test-Path $cakeExePath)) {
    Throw "Could not find Cake.exe at '$cakeExePath'"
}

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

# Build the argument list.
$Arguments = @{
    target=$Target;
    configuration=$Configuration;
    verbosity=$Verbosity;
    dryrun=$WhatIf;
}.GetEnumerator() | %{"--{0}=`"{1}`"" -f $_.key, $_.value };

# Start Cake
Write-Output "Running build script..."
& "$cakeExePath" "$scriptPath" $Arguments $ScriptArgs
$env:Path = $originalEnvPath
exit $LASTEXITCODE
