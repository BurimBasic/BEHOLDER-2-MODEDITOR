param([Parameter(Mandatory=$true)][string]$GamePath,[Parameter(Mandatory=$true)][string]$Dependencies)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$managed=Join-Path $GamePath 'Beholder2_Data\Managed'
$output=Join-Path $root 'dist\payload'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$dependencyNames=@('0Harmony.dll','Mono.Cecil.dll','MonoMod.RuntimeDetour.dll','MonoMod.Utils.dll')
foreach($name in $dependencyNames){Copy-Item -LiteralPath (Join-Path $Dependencies $name) -Destination $output -Force}
$refs=@(Get-ChildItem $managed -Filter *.dll | Where-Object {$_.Name -ne 'FloorAdmin.dll' -and $_.Name -notin $dependencyNames} | ForEach-Object {'/reference:'+ $_.FullName})
$refs+=Get-ChildItem $output -Filter *.dll | Where-Object Name -ne FloorAdmin.dll | ForEach-Object {'/reference:'+ $_.FullName}
$sdk=(& dotnet --list-sdks | Select-Object -Last 1)
if($sdk -notmatch '^(\S+) \[(.*)\]$'){throw 'Install the .NET SDK to build from source.'}
$compiler=Join-Path (Join-Path $Matches[2] $Matches[1]) 'Roslyn\bincore\csc.dll'
& dotnet $compiler /nologo /noconfig /nostdlib /target:library /out:"$output\FloorAdmin.dll" $refs (Get-ChildItem "$root\mod" -Filter *.cs | ForEach-Object FullName)
if($LASTEXITCODE -ne 0){throw 'Mod compilation failed'}
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $framework /nologo /target:winexe /out:"$root\dist\LaunchMod.exe" "$root\tools\LaunchMod.cs"
if($LASTEXITCODE -ne 0){throw 'Launcher compilation failed'}
& $framework /nologo /target:exe /out:"$root\dist\AssetIndex.exe" "$root\tools\AssetIndex.cs"
if($LASTEXITCODE -ne 0){throw 'Indexer compilation failed'}
Write-Output 'Build complete. No original game assembly is distributed.'
