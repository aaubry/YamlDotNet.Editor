. .\BuildUtils\build_utils.ps1

$buildNumber = ($env:APPVEYOR_BUILD_NUMBER, "0" -ne $null)[0]
$version = Get-VersionFromTag

Update-AppveyorBuild -Version "$version.$buildNumber"

Patch-Xml "YamlDotNetEditor\source.extension.vsixmanifest" $version "/vsx:PackageManifest/vsx:Metadata/vsx:Identity/@Version" @{ vsx = "http://schemas.microsoft.com/developer/vsx-schema/2011" }
Patch-AssemblyInfo "YamlDotNetEditor\Properties\AssemblyInfo.cs" $version
