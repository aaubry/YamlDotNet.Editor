. .\BuildUtils\build_utils.ps1

$buildNumber = ($env:APPVEYOR_BUILD_NUMBER, "0" -ne $null)[0]
$version = Get-VersionFromTag

Patch-Xml "YamlDotNetEditor\source.extension.vsixmanifest" $version $buildNumber "/vsx:PackageManifest/vsx:Metadata/vsx:Identity/@Version" @{ vsx = "http://schemas.microsoft.com/developer/vsx-schema/2011" }
Patch-AssemblyInfo "YamlDotNetEditor\Properties\AssemblyInfo.cs" $version $buildNumber
