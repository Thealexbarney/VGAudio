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

    $StoreCertThumbprint = "EB553661E803DE06AA93B72183F93CA767804F1E"
    $ReleaseCertThumbprint = "2043012AE523F7FA0F77A537387633BEB7A9F4DD"

    $libraryBuilds = @(
        @{ Name = "netstandard1.1"; CliFramework = "netcoreapp1.0"; Success = "false"; TestsFramework = "netcoreapp1.0" },
        @{ Name = "netstandard1.0"; CliFramework = "netcoreapp1.0"; Success = "false" },
        @{ Name = "net45"; CliFramework = "net45"; Success = "false"; TestsFramework = "net46" },
        @{ Name = "net40"; CliFramework = "net40"; Success = "false" },
        @{ Name = "net35"; CliFramework = "net35"; Success = "false" },
        @{ Name = "net20"; CliFramework = "net20"; Success = "false" }
    )

    $signReleaseBuild = $true
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
            RemovePath -Path $path -Verbose
        }
    }
}

task CleanPublish {
    RemovePath -Path $publishDir -Verbose
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
    $thumbprint = SetupUwpSigningCertificate
    if ($thumbprint) {
        $thumbprint = "/p:PackageCertificateThumbprint=" + $thumbprint
    }

    NetCliRestore -paths $libraryDir,$uwpDir

    $csproj = "$uwpDir\DspAdpcm.Uwp.csproj"
    & msbuild $csproj /p:AppxBundle=Always`;AppxBundlePlatforms=x86`|x64`|ARM`;UapAppxPackageBuildMode=StoreUpload`;Configuration=Release /v:m $thumbprint
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
    $buildDir = Join-Path $uwpDir AppPackages
    $appxName = "DspAdpcm.Uwp_$version`_x86_x64_ARM"
    $bundleDir = Join-Path $buildDir "DspAdpcm.Uwp_$version`_Test"

    $appxUpload = Join-Path $buildDir "$appxName`_bundle.appxupload"
    $appxBundle = Join-Path $bundleDir "$appxName`.appxbundle"
    $appxCer = Join-Path $bundleDir "$appxName`.cer"

    CopyItemToDirectory -Path $appxUpload,$appxBundle -Destination $uwpPublishdir

    if (($signReleaseBuild -eq $true) -and (Get-ChildItem -Path cert: -Recurse -CodeSigningCert | Where { $_.Thumbprint -eq $ReleaseCertThumbprint }).Count -gt 0)
    {
        $publisher = (Get-ChildItem -Path cert: -Recurse -CodeSigningCert | Where { $_.Thumbprint -eq $ReleaseCertThumbprint}).Subject

        $appxBundlePublish = Join-Path $uwpPublishdir "$appxName`.appxbundle"

        ChangeAppxBundlePublisher -Path $appxBundlePublish -Publisher $publisher
        SignAppx -Path $appxBundlePublish -Thumbprint $ReleaseCertThumbprint
    } else {
        CopyItemToDirectory -Path $appxCer -Destination $uwpPublishdir
    }
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

    if ((Get-ChildItem -Path cert: -Recurse -CodeSigningCert | Where { $_.Thumbprint -eq $StoreCertThumbprint }).Count -gt 0)
    {
        Write-Host "Using store code signing certificate with thumbprint $StoreCertThumbprint in certificate store."
        return $StoreCertThumbprint
    }

    if ((Get-ChildItem -Path cert: -Recurse -CodeSigningCert | Where { $_.Thumbprint -eq $ReleaseCertThumbprint }).Count -gt 0)
    {
        Write-Host "Using release code signing certificate with thumbprint $ReleaseCertThumbprint in certificate store."
        return $ReleaseCertThumbprint
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

function ChangeAppxBundlePublisher([string]$Path, [string]$Publisher)
{
    Write-Host Changing publisher of $Path
    $dirBundle = Join-Path ([System.IO.Path]::GetDirectoryName($Path)) ([System.IO.Path]::GetFileNameWithoutExtension($Path))

    RemovePath $dirBundle
    exec { makeappx unbundle /p $Path /d $dirBundle | Out-Null }

    Get-ChildItem $dirBundle -Filter *.appx | ForEach-Object { ChangeAppxPublisher -Path $_.FullName -Publisher $Publisher }    

    $manifestPath = Join-Path $dirBundle "AppxMetadata\AppxBundleManifest.xml"
    [xml]$manifestXml = Get-Content -Path $manifestPath
        
    $manifestXml.Bundle.Identity.Publisher = $Publisher
    $manifestXml.Save($manifestPath)

    RemovePath $Path
    exec { makeappx bundle /d $dirBundle /p $Path | Out-Null }

    RemovePath $dirBundle
}

function ChangeAppxPublisher([string]$Path, [string]$Publisher)
{
    Write-Host Changing publisher of $Path
    $dirAppx = Join-Path ([System.IO.Path]::GetDirectoryName($Path)) ([System.IO.Path]::GetFileNameWithoutExtension($Path))

    RemovePath $dirAppx
    exec { makeappx unpack /l /p $Path /d $dirAppx | Out-Null }      

    $manifestPath = Join-Path $dirAppx AppxManifest.xml
    [xml]$manifestXml = Get-Content -Path $manifestPath
        
    $manifestXml.Package.Identity.Publisher = $Publisher
    $manifestXml.Save($manifestPath)

    RemovePath $Path
    exec { makeappx pack /l /d $dirAppx /p $Path | Out-Null }    

    RemovePath $dirAppx
}

function SignAppx([string]$Path, [string]$Thumbprint)
{
    $timestampServers =
    "http://timestamp.geotrust.com/tsa",
    "http://sha256timestamp.ws.symantec.com/sha256/timestamp",
    "http://timestamp.comodoca.com/authenticode",
    "http://time.certum.pl"

    foreach($server in $timestampServers)
    {
        for($i = 1; $i -le 4; $i++)
        {
            Write-Host -ForegroundColor Green "Signing $Path"
            $global:lastexitcode = 0
            signtool sign /fd SHA256 /a /sha1 $Thumbprint /tr $server $Path
            if ($lastexitcode -eq 0)
            {
                Write-Host -ForegroundColor Green "Success"
                return
            }
            Write-Host -ForegroundColor Red "Retrying..."
            Start-Sleep -Seconds 3
        }
    }
}

function RemovePath([string]$Path, [switch]$Verbose)
{
    if (Test-Path $Path)
    {
        if ($Verbose) {
            Write-Host Cleaning $Path
        }
        Remove-Item $Path -Recurse -Force
    }
}

function CopyItemToDirectory([string[]]$Path, [string]$Destination)
{
    if (-not (Test-Path $Destination))
    {
        mkdir -Path $Destination | Out-Null
    }
    Copy-Item -Path $Path -Destination $Destination
}
