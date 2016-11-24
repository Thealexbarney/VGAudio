properties { 
  $baseDir  = resolve-path ..
  $buildDir = "$baseDir\build"
  $sourceDir = "$baseDir\src"
  $toolsDir = "$baseDir\tools"
  $publishDir = "$baseDir\publish"

  $libraryDir = "$sourceDir\DspAdpcm"
  $cliDir = "$sourceDir\DspAdpcm.Cli"
  $testsDir = "$sourceDir\DspAdpcm.Tests"
  
  $libraryBuilds = @(
    @{Name = "netstandard1.1"; CliFramework = "netcoreapp1.0"; Success = "false"; TestsFramework = "netcoreapp1.0" },
    @{Name = "netstandard1.0"; CliFramework = "netcoreapp1.0"; Success = "false" },
    @{Name = "net45"; CliFramework = "net45"; Success = "false"; TestsFramework = "net46" },
    @{Name = "net40"; CliFramework = "net40"; Success = "false" },
    @{Name = "net35"; CliFramework = "net35"; Success = "false" },
    @{Name = "net20"; CliFramework = "net20"; Success = "false" }
  )
}

framework '4.6x86'

task default -depends RebuildAll

task Clean -depends CleanBuild, CleanPublish

task CleanBuild {
  $toDelete = "bin", "obj", "*.lock.json", "*.nuget.targets"

  Get-ChildItem $sourceDir | ?{ $_.PSIsContainer } | 
  Foreach-Object {
    foreach($file in $toDelete)
    {
      $path = $_.FullName + "\" + $file
      RemovePath $path
    }
  }  
}

task CleanPublish {
  RemovePath $publishDir
}

task BuildLib {
  Write-Host -ForegroundColor Green "Restoring packages for library"
  Write-Host
  exec { dotnet restore $libraryDir | Out-Default }
  foreach($build in $libraryBuilds)
  {
    NetCliBuild $build $libraryDir $build.Name
  }
}

task BuildCli -depends BuildLib {
  Write-Host -ForegroundColor Green "Restoring packages for CLI"
  Write-Host
  exec { dotnet restore $cliDir | Out-Default }
  foreach($build in $libraryBuilds | Where {$_.Success -eq "true" })
  {
    NetCliBuild $build $cliDir $build.CliFramework
  }
}

task PublishLib -depends BuildLib {
  dotnet pack --no-build $libraryDir -c release -o "$publishDir\NuGet"
}

task PublishCli -depends BuildCli {
  foreach($build in $libraryBuilds )
  {
    $framework = $build.CliFramework
    Write-Host -ForegroundColor Green "Publishing CLI project $framework"
    NetCliPublish $build $cliDir "$publishDir\cli\$framework" $framework
  }
}

task TestLib -depends BuildLib {
  Write-Host -ForegroundColor Green "Restoring packages for Tests"
  Write-Host
  exec { dotnet restore $testsDir | Out-Default }
  foreach($build in $libraryBuilds | Where {($_.ContainsKey("TestsFramework"))})
  {
    $path = "$sourceDir\" + $build.TestDir
    exec {dotnet test $testsDir -c release -f $build.TestsFramework}
  }
}

task RebuildAll -depends Clean, PublishCli, PublishLib, Test
task BuildAll -depends CleanPublish, PublishCli, PublishLib

function NetCliBuild($build, $path, $framework)
{
  Write-Host -ForegroundColor Green "Building $path $framework"
  & dotnet build $path -f $framework -c Release
  If ($lastexitcode -eq 0) 
  { 
    $build.Success = "true"
  } 
  Else 
  {
    $build.Success = "false"
  }
}

function NetCliPublish($build, $srcPath, $outPath, $framework)
{
  Write-Host -ForegroundColor Green "Building $srcPath $framework"
  & dotnet publish --no-build $srcPath -f $framework -c Release -o $outPath
  If ($lastexitcode -ne 0) 
  { 
    RemovePath $outPath
  } 
}

function RemovePath($path)
{
  If (Test-Path $path)
  {
    Write-Host Cleaning $path
    Remove-Item $path -Recurse -Force
  }
}