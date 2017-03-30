#!/usr/bin/env bash

##########################################################################
# This is the Cake bootstrapper script for Linux and OS X.
# This file was downloaded from https://github.com/cake-build/resources
# Feel free to change this file to fit your needs.
##########################################################################

# Define default arguments.
TARGET="Default"
CONFIGURATION="Release"
VERBOSITY="normal"
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

dotnetCliVersion="1.0.1"

# Define directories.
basePath=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
buildPath=$basePath/build
dotnetPath=$basePath/tools/dotnet
dotnetCliPath=$dotnetPath/cli
globalJsonFile=$buildPath/global.json

trap "rm -f $globalJsonFile" INT TERM EXIT

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

function SetupDotnetCli {
    json="{\"projects\":[],\"sdk\":{\"version\":\"$dotnetCliVersion\"}}"
    echo "$json" > $globalJsonFile

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
    exit 1
}

# Make sure .NET Core CLI exists.
cd "$buildPath"
SetupDotnetCli

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

# Start Cake
echo "Restoring build packages..."
if ! dotnet msbuild /t:restore /v:q /nologo; then
    echo "Restore for build project failed"
    exit 1
fi

echo "Running build..."
dotnet publish /v:q /nologo
dotnet bin/Debug/netcoreapp1.0/publish/Build.dll --verbosity="$VERBOSITY" --configuration="$CONFIGURATION" --target="$TARGET" $DRYRUN "${SCRIPT_ARGUMENTS[@]}"
exit $?
