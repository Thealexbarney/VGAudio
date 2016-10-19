#!/usr/bin/env bash

sourceDir="../src"
outDir="../bin"

rm -rf $outDir 2> /dev/null
rm -rf $sourceDir/DspAdpcm/bin 2> /dev/null
rm -rf $sourceDir/DspAdpcm/obj 2> /dev/null
rm -rf $sourceDir/DspAdpcm.Cli/bin 2> /dev/null
rm -rf $sourceDir/DspAdpcm.Cli/obj 2> /dev/null
rm -rf $sourceDir/DspAdpcm.Tests/bin 2> /dev/null
rm -rf $sourceDir/DspAdpcm.Tests/obj 2> /dev/null

find $sourceDir -type f -name "*.lock.json" -delete
find $sourceDir -type f -name "*.nuget.targets" -delete

dotnet restore $sourceDir

dotnet pack $sourceDir/DspAdpcm -c release -o $outDir/NuGet

dotnet publish $sourceDir/DspAdpcm.Cli -c release -f net45 -o $outDir/Cli/net45
dotnet publish $sourceDir/DspAdpcm.Cli -c release -f net40 -o $outDir/Cli/net40
dotnet publish $sourceDir/DspAdpcm.Cli -c release -f net35 -o $outDir/Cli/net35
dotnet publish $sourceDir/DspAdpcm.Cli -c release -f net20 -o $outDir/Cli/net20
dotnet publish $sourceDir/DspAdpcm.Cli -c release -f netcoreapp1.0 -o $outDir/Cli/netcoreapp1.0

dotnet test $sourceDir/DspAdpcm.Tests -c release -f netcoreapp1.0
