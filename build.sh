#!/usr/bin/env bash

rm -r bin 2> /dev/null
rm -r src/DspAdpcm/bin 2> /dev/null
rm -r src/DspAdpcm/obj 2> /dev/null
rm -r src/DspAdpcm.Cli/bin 2> /dev/null
rm -r src/DspAdpcm.Cli/obj 2> /dev/null
rm -r src/DspAdpcm.Tests/bin 2> /dev/null
rm -r src/DspAdpcm.Tests/obj 2> /dev/null

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