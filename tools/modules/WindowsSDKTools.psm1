function ConfigureSdkBuildEnvironment
{
    if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots") {
        $sdkPath = Get-ItemPropertyValue -Path "HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots" -Name KitsRoot10
        $sdkBinPath = Join-Path $sdkPath bin\x86
        $env:path = $sdkBinPath + ";$env:path"
    }
}

ConfigureSdkBuildEnvironment