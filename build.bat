echo off

rmdir /s /q bin 2>NUL
rmdir /s /q src\DspAdpcm\bin 2>NUL
rmdir /s /q src\DspAdpcm\obj 2>NUL
rmdir /s /q src\DspAdpcm.Cli\bin 2>NUL
rmdir /s /q src\DspAdpcm.Cli\obj 2>NUL

del /s *.lock.json 2>NUL
del /s *.nuget.targets 2>NUL

dotnet restore

dotnet build src\DspAdpcm -c release -f net45 -o bin\DspAdpcm\net45
dotnet build src\DspAdpcm -c release -f netstandard1.1 -o bin\DspAdpcm\netstandard1.1
dotnet build src\DspAdpcm.Cli -c release -f net45 -o bin\Cli\net45
dotnet build src\DspAdpcm.Cli -c release -f netcoreapp1.0 -o bin\Cli\netcoreapp1.0

dotnet pack src\DspAdpcm -c release -o bin\NuGet