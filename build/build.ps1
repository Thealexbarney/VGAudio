properties {
    $baseDir = Resolve-Path ..
    $buildDir = "$baseDir\build"
    $sourceDir = "$baseDir\src"
    $toolsDir = "$baseDir\tools"
    $publishDir = "$baseDir\publish"

    $libraryDir = "$sourceDir\DspAdpcm"
    $cliDir = "$sourceDir\DspAdpcm.Cli"
    $uwpDir = "$sourceDir\DspAdpcm.Uwp"
    $testsDir = "$sourceDir\DspAdpcm.Tests"

    $libraryPublishDir = "$publishDir\NuGet"
    $cliPublishDir = "$publishDir\cli"
    $uwpPublishDir = "$publishDir\uwp"

    $libraryBuilds = @(
        @{ Name = "netstandard1.1"; CliFramework = "netcoreapp1.0"; Success = "false"; TestsFramework = "netcoreapp1.0" },
        @{ Name = "netstandard1.0"; CliFramework = "netcoreapp1.0"; Success = "false" },
        @{ Name = "net45"; CliFramework = "net45"; Success = "false"; TestsFramework = "net46" },
        @{ Name = "net40"; CliFramework = "net40"; Success = "false" },
        @{ Name = "net35"; CliFramework = "net35"; Success = "false" },
        @{ Name = "net20"; CliFramework = "net20"; Success = "false" }
    )
}

framework '4.6'

task default -depends RebuildAll

task Clean -depends CleanBuild, CleanPublish

task CleanBuild {
    $toDelete = "bin", "obj", "*.lock.json", "*.nuget.targets", "AppPackages", "BundleArtifacts", "*.appx", "_pkginfo.txt"

    Get-ChildItem $sourceDir | ? { $_.PSIsContainer } |
    ForEach-Object {
        foreach ($file in $toDelete)
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
    NetCliRestore -paths $libraryDir

    foreach ($build in $libraryBuilds)
    {
        NetCliBuild $build $libraryDir $build.Name
    }
}

task BuildCli -depends BuildLib {
    NetCliRestore -paths $cliDir
    foreach ($build in $libraryBuilds | Where { $_.Success -eq "true" })
    {
        NetCliBuild $build $cliDir $build.CliFramework
    }
}

task BuildUwp {
    SetupUwpSigningCertificate
    NetCliRestore -paths $libraryDir,$uwpDir

    $csproj = "$uwpDir\DspAdpcm.Uwp.csproj"
    & msbuild $csproj /p:AppxBundle=Always`;AppxBundlePlatforms=x86`|x64`|ARM`;UapAppxPackageBuildMode=StoreUpload`;Configuration=Release /v:m
}

task PublishLib -depends BuildLib {
    dotnet pack --no-build $libraryDir -c release -o "$publishDir\NuGet"
}

task PublishCli -depends BuildCli {
    foreach ($build in $libraryBuilds)
    {
        $framework = $build.CliFramework
        Write-Host -ForegroundColor Green "Publishing CLI project $framework"
        NetCliPublish $build $cliDir "$publishDir\cli\$framework" $framework
    }
}

task PublishUwp -depends BuildUwp {
    $version = GetVersionFromAppxManifest $uwpDir\Package.appxmanifest
    $buildDir = "$uwpDir\AppPackages"
    $appxName = "DspAdpcm.Uwp_$version`_x86_x64_ARM"
    $bundleDir = "$buildDir\DspAdpcm.Uwp_$version`_Test"

    $appxUpload = "$buildDir\$appxName`_bundle.appxupload"
    $appxBundle = "$bundleDir\$appxName`.appxbundle"
    $appxCer = "$bundleDir\$appxName`.cer"

    CopyItemToDirectory -Path $appxUpload -Destination $uwpPublishdir
    CopyItemToDirectory -Path $appxBundle -Destination $uwpPublishdir
    CopyItemToDirectory -Path $appxCer -Destination $uwpPublishdir
}

task TestLib -depends BuildLib {
    NetCliRestore -paths $testsDir
    foreach ($build in $libraryBuilds | Where { ($_.ContainsKey("TestsFramework")) })
    {
        $path = "$sourceDir\" + $build.TestDir
        exec { dotnet test $testsDir -c release -f $build.TestsFramework }
    }
}

task RebuildAll -depends Clean, PublishCli, PublishLib, PublishUwp, TestLib
task BuildAll -depends CleanPublish, PublishCli, PublishLib, PublishUwp, TestLib

function NetCliBuild($build, [string]$path, [string]$framework)
{
    Write-Host -ForegroundColor Green "Building $path $framework"
    & dotnet build $path -f $framework -c Release
    if ($lastexitcode -eq 0)
    {
        $build.Success = "true"
    }
    else
    {
        $build.Success = "false"
    }
}

function NetCliPublish($build, [string]$srcPath, [string]$outPath, [string]$framework)
{
    Write-Host -ForegroundColor Green "Building $srcPath $framework"
    & dotnet publish --no-build $srcPath -f $framework -c Release -o $outPath
    if ($lastexitcode -ne 0)
    {
        RemovePath $outPath
    }
}

function NetCliRestore([string[]]$Paths)
{
    foreach ($path in $Paths)
    {
        Write-Host -ForegroundColor Green "Restoring $path"
        exec { dotnet restore $path | Out-Default }
    }
}

function GetVersionFromAppxManifest([string]$manifestPath)
{
    [xml]$manifestXml = Get-Content -Path $manifestPath
    $manifestXml.Package.Identity.Version
}

function SetupUwpSigningCertificate()
{
    $csprojPath = "$uwpDir\DspAdpcm.Uwp.csproj"
    [xml]$csprojXml = Get-Content -Path $csprojPath
    $thumbprint = $csprojXml.Project.PropertyGroup[0].PackageCertificateThumbprint
    $keyFile = "$uwpDir\" + $csprojXml.Project.PropertyGroup[0].PackageCertificateKeyFile

    $certCount = (Get-ChildItem -Path cert: -Recurse -CodeSigningCert | Where { $_.Thumbprint -eq $thumbprint }).Count
    if ($certCount -gt 0)
    {
        Write-Host "Using code signing certificate with thumbprint $thumbprint in certificate store."
        return
    }

    if (Test-Path $keyFile)
    {
        Write-Host "Using code signing certificate at $keyFile"
        return
    }

    $cert = New-SelfSignedCertificate -Subject CN=$env:username -Type CodeSigningCert -TextExtension @("2.5.29.19={text}") -CertStoreLocation cert:\currentuser\my
    Remove-Item $cert.PSPath
    Export-PfxCertificate -Cert $cert -FilePath $keyFile -Password (New-Object System.Security.SecureString) | Out-Null

    Write-Host "Created self-signed test certificate at $keyFile"
}

function RemovePath([string]$path)
{
    if (Test-Path $path)
    {
        Write-Host Cleaning $path
        Remove-Item $path -Recurse -Force
    }
}

function CopyItemToDirectory([string]$Path, [string]$Destination)
{
    if (-not (Test-Path $Destination))
    {
        mkdir -Path $Destination
    }
    Copy-Item -Path $Path -Destination $Destination
}
