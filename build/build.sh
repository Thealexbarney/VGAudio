#!/usr/bin/env bash

##########################################################################
# This is the Cake bootstrapper script for Linux and OS X.
# This file was downloaded from https://github.com/cake-build/resources
# Feel free to change this file to fit your needs.
##########################################################################

# Define default arguments.
TARGET="Default"
CONFIGURATION="Release"
VERBOSITY="verbose"
DRYRUN=
SCRIPT_ARGUMENTS=()

# Parse arguments.
while (($#)); do
    case $1 in
        -t|--target) TARGET="$2"; shift ;;
        -c|--configuration) CONFIGURATION="$2"; shift ;;
        -v|--verbosity) VERBOSITY="$2"; shift ;;
        -d|--dryrun) DRYRUN="-dryrun" ;;
        --) shift; SCRIPT_ARGUMENTS+=("$@"); break ;;
        *) SCRIPT_ARGUMENTS+=("$1") ;;
    esac
    shift
done

cakeVersion="0.18.0"
dotnetCliVersion="1.0.1"
nugetUrl="https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

# Define directories.
basePath=$( cd "$( dirname "${BASH_SOURCE[0]}" )/.." && pwd )
buildPath=$basePath/build
cakePath=$basePath/tools/cake
dotnetPath=$basePath/tools/dotnet
dotnetCliPath=$dotnetPath/cli
script=$buildPath/build.cake

# Make sure cake folder exists
if [ ! -d "$cakePath" ]; then
    mkdir "$cakePath"
fi

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

function SetupDotnetCli {
    json="{\"projects\":[],\"sdk\":{\"version\":\"$dotnetCliVersion\"}}"
    echo "$json" > "$buildPath/global.json"

    if command -v dotnet >/dev/null && [ "$(dotnet --version)" = $dotnetCliVersion ]; then
        return
    fi

    homeInstallPath=~/.dotnet
    path=$homeInstallPath/dotnet
    if [ -f "$path" ] && [ "$("$path" --version)" = $dotnetCliVersion ]; then
        export PATH="$homeInstallPath":$PATH
        return
    fi

    path=$dotnetCliPath/dotnet
    if [ -f "$path" ] && [ "$("$path" --version)" = $dotnetCliVersion ]; then
        export PATH="$dotnetCliPath":$PATH
        return
    fi

    echo "Downloading .NET Core CLI..."
    bash "$dotnetPath/dotnet-install.sh" -Version $dotnetCliVersion -InstallDir "$dotnetCliPath" --no-path
    if [ -f "$path" ] && [ "$("$path" --version)" = $dotnetCliVersion ]; then
        export PATH="$dotnetCliPath":$PATH
        return
    fi

    echo "Unable to locate or download .NET Core CLI version $dotnetCliVersion"
    popd > /dev/null
    exit 1
}

# Make sure .NET Core CLI exists.
pushd "$buildPath" > /dev/null
SetupDotnetCli
popd > /dev/null

###########################################################################
# INSTALL NUGET
###########################################################################

# Download NuGet if it does not exist.
nugetPath=$cakePath/nuget.exe
if [ ! -f "$nugetPath" ]; then
    echo "Downloading nuget.exe..."
    if ! curl -Lsfo "$nugetPath" $nugetUrl
    then
        echo "An error occured while downloading nuget.exe."
        exit 1
    fi
fi

###########################################################################
# INSTALL CAKE
###########################################################################

cakeExePath=$cakePath/Cake.$cakeVersion/Cake.exe
if [ ! -f "$cakeExePath" ]; then
    echo "Installing Cake..."
    if ! mono "$nugetPath" install Cake -Version $cakeVersion -OutputDirectory "$cakePath"
    then
        echo "An error occured while restoring Cake from NuGet."
        exit 1
    fi
fi

# Make sure Cake has been installed.
if [ ! -f "$cakeExePath" ]; then
    echo "Could not find Cake.exe at '$cakeExePath'."
    exit 1
fi

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

# Start Cake
echo "Running build script..."
exec mono "$cakeExePath" "$script" --verbosity="$VERBOSITY" --configuration="$CONFIGURATION" --target="$TARGET" $DRYRUN "${SCRIPT_ARGUMENTS[@]}"
