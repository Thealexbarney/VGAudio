#!/usr/bin/env bash

rm -r bin
rm -r src/DspAdpcm/bin
rm -r src/DspAdpcm/obj
rm -r src/DspAdpcm.Cli/bin
rm -r src/DspAdpcm.Cli/obj

find . -type f -name "*.lock.json" -delete
find . -type f -name "*.nuget.targets" -delete

dotnet restore

dotnet build src/DspAdpcm -c release -f net45 -o bin/DspAdpcm/net45
dotnet build src/DspAdpcm -c release -f netstandard1.1 -o bin/DspAdpcm/netstandard1.1
dotnet build src/DspAdpcm.Cli -c release -f net45 -o bin/Cli/net45
dotnet build src/DspAdpcm.Cli -c release -f netcoreapp1.0 -o bin/Cli/netcoreapp1.0

dotnet test src/DspAdpcm.Tests -c release -f netcoreapp1.0

rc=$?; if [[ $rc != 0 ]]; then exit $rc; fi

dotnet pack src/DspAdpcm -c release -o bin/NuGet