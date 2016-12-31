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

    $storeCertThumbprint = "EB553661E803DE06AA93B72183F93CA767804F1E"
    $releaseCertThumbprint = "2043012AE523F7FA0F77A537387633BEB7A9F4DD"

    $dotnetToolsDir = Join-Path $toolsDir dotnet
    $dotnetSdkDir = Join-Path $dotnetToolsDir sdk
    $dotnetCliVersion = "1.0.0-preview2-003133"

    $libraryBuilds = @(
        @{ Name = "netstandard1.1"; LibSuccess = $null; CliFramework = "netcoreapp1.0"; CliSuccess = $null; TestFramework = "netcoreapp1.0"; TestSuccess = $null },
        @{ Name = "netstandard1.0"; LibSuccess = $null },
        @{ Name = "net45"; LibSuccess = $null; CliFramework = "net45"; CliSuccess = $null; TestFramework = "net46"; TestSuccess = $null },
        @{ Name = "net40"; LibSuccess = $null; CliFramework = "net40"; CliSuccess = $null },
        @{ Name = "net35"; LibSuccess = $null; CliFramework = "net35"; CliSuccess = $null },
        @{ Name = "net20"; LibSuccess = $null; CliFramework = "net20"; CliSuccess = $null }
    )

    $otherBuilds = @{
        "Uwp" = @{ Name = "UWP App"; Success = $null }
    }

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
    SetupDotnetCli
    
    NetCliRestore -Path $libraryDir

    foreach ($build in $libraryBuilds)
    {
        Write-Host -ForegroundColor Green Building $libraryDir $build.Name
        try {
            NetCliBuild $libraryDir $build.Name
        }
        catch {
            $build.LibSuccess = $false
            continue
        }
        $build.LibSuccess = $true
    }
}

task BuildCli -depends BuildLib {
    SetupDotnetCli

    NetCliRestore -Path $cliDir
    foreach ($build in $libraryBuilds | Where { $_.LibSuccess -ne $false -and $_.CliFramework })
    {
        Write-Host -ForegroundColor Green Building $cliDir $build.CliFramework
        try {
            NetCliBuild $cliDir $build.CliFramework
        }
        catch {
            $build.CliSuccess = $false
            continue
        }
        $build.CliSuccess = $true
    }
}

task BuildUwp {
    if (-not (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots")) {
        Write-Host "Windows 10 SDK not detected. Skipping UWP build."
        return
    }

    SetupDotnetCli

    try {
        $thumbprint = SetupUwpSigningCertificate
        if ($thumbprint) {
            $thumbprint = "/p:PackageCertificateThumbprint=" + $thumbprint
        }

        NetCliRestore -Path $libraryDir,$uwpDir

        $csproj = "$uwpDir\DspAdpcm.Uwp.csproj"
        exec { msbuild $csproj /p:AppxBundle=Always`;AppxBundlePlatforms=x86`|x64`|ARM`;UapAppxPackageBuildMode=StoreUpload`;Configuration=Release /v:m $thumbprint }
    }
    catch {
        $otherBuilds.Uwp.Success = $false
        continue
    }
    $otherBuilds.Uwp.Success = $true
}

task PublishLib -depends BuildLib {
    dotnet pack --no-build $libraryDir -c release -o "$publishDir\NuGet"
}

task PublishCli -depends BuildCli {
    foreach ($build in $libraryBuilds | Where { $_.CliSuccess -ne $false -and $_.LibSuccess -ne $false -and $_.CliFramework })
    {
        $framework = $build.CliFramework
        Write-Host -ForegroundColor Green "Publishing CLI project $framework"
        NetCliPublish $cliDir "$publishDir\cli\$framework" $framework
    }
}

task PublishUwp -depends BuildUwp {
    if (-not (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots")) {
        return
    }

    if ($otherBuilds.Uwp.Success -eq $false) {
        Write-Host -ForegroundColor Red "UWP project was not successfully built. Skipping..."
        return
    }

    $version = GetVersionFromAppxManifest $uwpDir\Package.appxmanifest
    $buildDir = Join-Path $uwpDir AppPackages
    $appxName = "DspAdpcm.Uwp_$version`_x86_x64_ARM"
    $bundleDir = Join-Path $buildDir "DspAdpcm.Uwp_$version`_Test"

    $appxUpload = Join-Path $buildDir "$appxName`_bundle.appxupload"
    $appxBundle = Join-Path $bundleDir "$appxName`.appxbundle"
    $appxCer = Join-Path $bundleDir "$appxName`.cer"

    CopyItemToDirectory -Path $appxUpload,$appxBundle -Destination $uwpPublishdir

    if (($signReleaseBuild -eq $true) -and (Get-ChildItem -Path cert: -Recurse -CodeSigningCert | Where { $_.Thumbprint -eq $releaseCertThumbprint }).Count -gt 0)
    {
        $publisher = (Get-ChildItem -Path cert: -Recurse -CodeSigningCert | Where { $_.Thumbprint -eq $releaseCertThumbprint}).Subject

        $appxBundlePublish = Join-Path $uwpPublishdir "$appxName`.appxbundle"

        ChangeAppxBundlePublisher -Path $appxBundlePublish -Publisher $publisher
        SignAppx -Path $appxBundlePublish -Thumbprint $releaseCertThumbprint
    } else {
        CopyItemToDirectory -Path $appxCer -Destination $uwpPublishdir
    }
}

task TestLib -depends BuildLib {
    NetCliRestore -Path $testsDir
    foreach ($build in $libraryBuilds | Where { $_.LibSuccess -ne $false -and $_.TestFramework })
    {
        try {
            $path = "$sourceDir\" + $build.TestDir
            exec { dotnet test $testsDir -c release -f $build.TestFramework }
        }
        catch {
            $build.TestSuccess = $false
            continue
        }
        $build.TestSuccess = $true
    }
}

task RebuildAll -depends Clean, BuildAll
task BuildAll -depends CleanPublish, PublishCli, PublishLib, PublishUwp, TestLib {
    Write-Host $("-" * 70)
    Write-Host "Build Report"
    Write-Host $("-" * 70)

    Write-Host `n
    Write-Host "Library Builds"
    Write-Host $("-" * 35)
    $list = @()

    foreach ($build in $libraryBuilds) {
        $status = ""
        switch ($build.LibSuccess)
        {
            $true { $status = "Success" }
            $false { $status = "Failure" }
            $null { $status = "Not Built" }
        }

        $list += new-object PSObject -property @{
            Name = $build.Name;
            Status = $status
        }
    }
    $list | format-table -autoSize -property Name,Status | out-string -stream | where-object { $_ }
    
    Write-Host `n
    Write-Host "CLI Builds"
    Write-Host $("-" * 35)
    $list = @()

    foreach ($build in $libraryBuilds | Where { $_.CliFramework }) {
        $status = ""
        switch ($build.CliSuccess)
        {
            $true { $status = "Success" }
            $false { $status = "Failure" }
            $null { $status = "Not Built" }
        }

        $list += new-object PSObject -property @{
            Name = $build.CliFramework;
            Status = $status
        }
    }
    $list | format-table -autoSize -property Name,Status | out-string -stream | where-object { $_ }

    Write-Host `n
    Write-Host "Other Builds"
    Write-Host $("-" * 35)
    $list = @()

    foreach ($build in $otherBuilds.Values) {
        $status = ""
        switch ($build.Success)
        {
            $true { $status = "Success" }
            $false { $status = "Failure" }
            $null { $status = "Not Built" }
        }

        $list += new-object PSObject -property @{
            Name = $build.Name;
            Status = $status
        }
    }
    $list | format-table -autoSize -property Name,Status | out-string -stream | where-object { $_ }

    Write-Host `n
    Write-Host "Tests"
    Write-Host $("-" * 35)
    $list = @()

    foreach ($build in $libraryBuilds | Where { $_.TestFramework }) {
        $status = ""
        switch ($build.TestSuccess)
        {
            $true { $status = "Success" }
            $false { $status = "Failure" }
            $null { $status = "Not Tested" }
        }

        $list += new-object PSObject -property @{
            Name = $build.CliFramework;
            Status = $status
        }
    }
    $list | format-table -autoSize -property Name,Status | out-string -stream | where-object { $_ }
}

function SetupDotnetCli()
{
    CreateBuildGlobalJson -Path (Join-Path $buildDir global.json) -Version $dotnetCliVersion
    if ((Get-Command "dotnet" -errorAction SilentlyContinue) -and (& dotnet --version) -eq $dotnetCliVersion)
    {
        return
    }

    Write-Host "Searching for Dotnet CLI version $dotnetCliVersion..."    
    Write-Host "Checking Local AppData default install path..."
    $appDataInstallPath = Join-Path $env:LocalAppData "Microsoft\dotnet"
    $path = Join-Path $appDataInstallPath dotnet.exe
    if ((Test-Path $path) -and (& $path --version) -eq $dotnetCliVersion)
    {
        $env:Path = "$appDataInstallPath;" + $env:path
        Write-Host "Found Dotnet CLI version $dotnetCliVersion"        
        return
    }
    
    Write-Host "Checking project tools path..."
    $path = Join-Path $dotnetSdkDir dotnet.exe
    if ((Test-Path $path) -and (& $path --version) -eq $dotnetCliVersion)
    {
        $env:Path = "$dotnetSdkDir;" + $env:path
        Write-Host "Found Dotnet CLI version $dotnetCliVersion"        
        return
    }
    
    Write-Host "Downloading Dotnet CLI..."
    & (Join-Path $dotnetToolsDir dotnet-install.ps1) -InstallDir $dotnetSdkDir -Version $dotnetCliVersion -NoPath
    if ((Test-Path $path) -and (& $path --version) -eq $dotnetCliVersion)
    {
        $env:Path = "$dotnetSdkDir;" + $env:path
        Write-Host "Found Dotnet CLI version $dotnetCliVersion"        
        return
    }

    Write-Host "Unable to find Dotnet CLI version $dotnetCliVersion"
    exit
}

function CreateBuildGlobalJson([string] $Path, [string]$Version)
{
    $json = "{`"projects`":[],`"sdk`":{`"version`":`"$Version`"}}"
    Out-File -FilePath $Path -Encoding utf8 -InputObject $json
}

function NetCliBuild([string]$path, [string]$framework)
{
    exec { dotnet build $path -f $framework -c Release }
}

function NetCliPublish([string]$srcPath, [string]$outPath, [string]$framework)
{
    exec { dotnet publish --no-build $srcPath -f $framework -c Release -o $outPath }
}

function NetCliRestore([string[]]$Path)
{
    foreach ($singlePath in $Path)
    {
        Write-Host -ForegroundColor Green "Restoring $singlePath"
        exec { dotnet restore $singlePath | Out-Default }
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

    if ((Get-ChildItem -Path cert: -Recurse -CodeSigningCert | Where { $_.Thumbprint -eq $storeCertThumbprint }).Count -gt 0)
    {
        Write-Host "Using store code signing certificate with thumbprint $storeCertThumbprint in certificate store."
        return $storeCertThumbprint
    }

    if ((Get-ChildItem -Path cert: -Recurse -CodeSigningCert | Where { $_.Thumbprint -eq $releaseCertThumbprint }).Count -gt 0)
    {
        Write-Host "Using release code signing certificate with thumbprint $releaseCertThumbprint in certificate store."
        return $releaseCertThumbprint
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
