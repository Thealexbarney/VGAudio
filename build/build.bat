echo off

set sourceDir=..\src
set outDir=..\bin

rmdir /s /q %outDir% 2>NUL
rmdir /s /q %sourceDir%\DspAdpcm\bin 2>NUL
rmdir /s /q %sourceDir%\DspAdpcm\obj 2>NUL
rmdir /s /q %sourceDir%\DspAdpcm.Cli\bin 2>NUL
rmdir /s /q %sourceDir%\DspAdpcm.Cli\obj 2>NUL
rmdir /s /q %sourceDir%\DspAdpcm.Tests\bin 2>NUL
rmdir /s /q %sourceDir%\DspAdpcm.Tests\obj 2>NUL

del /s %sourceDir%\*.lock.json 2>NUL
del /s %sourceDir%\*.nuget.targets 2>NUL

dotnet restore %sourceDir%

dotnet pack %sourceDir%\DspAdpcm -c release -o %outDir%\NuGet

dotnet publish %sourceDir%\DspAdpcm.Cli -c release -f net45 -o %outDir%\Cli\net45
dotnet publish %sourceDir%\DspAdpcm.Cli -c release -f net40 -o %outDir%\Cli\net40
dotnet publish %sourceDir%\DspAdpcm.Cli -c release -f net35 -o %outDir%\Cli\net35
dotnet publish %sourceDir%\DspAdpcm.Cli -c release -f net20 -o %outDir%\Cli\net20
dotnet publish %sourceDir%\DspAdpcm.Cli -c release -f netcoreapp1.0 -o %outDir%\Cli\netcoreapp1.0

dotnet test %sourceDir%\DspAdpcm.Tests -c release
