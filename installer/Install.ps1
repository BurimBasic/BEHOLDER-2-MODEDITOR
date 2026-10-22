param([string]$GamePath,[switch]$ValidateOnly)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
if(!$GamePath){
 Add-Type -AssemblyName System.Windows.Forms
 $picker=New-Object System.Windows.Forms.FolderBrowserDialog
 $picker.Description='Choose your original Beholder 2 game folder / Выберите папку оригинальной игры Beholder 2'
 if($picker.ShowDialog() -ne 'OK'){exit 1}
 $GamePath=$picker.SelectedPath
}
$game=[IO.Path]::GetFullPath($GamePath).TrimEnd('\')
$target=Join-Path $root 'prototype'
if(!(Test-Path -LiteralPath (Join-Path $game 'Beholder2.exe'))){throw 'Beholder2.exe was not found in the selected folder.'}
if($root.StartsWith($game+'\',[StringComparison]::OrdinalIgnoreCase) -or $root -eq $game){throw 'Extract this addon OUTSIDE the original game folder.'}
$sourceAssembly=Join-Path $game 'Beholder2_Data\Managed\Assembly-CSharp.dll'
$expected='A290DCE4A52C17158CF10B88BA72B8F1713C76E52E08B58C88C9AE7647B0C322'
if((Get-FileHash -LiteralPath $sourceAssembly -Algorithm SHA256).Hash -ne $expected){throw 'Unsupported or modified game build. This alpha requires the tested Windows Unity 2018.4.4f1 build. Original files were not changed.'}
foreach($name in @('FloorAdmin.dll','0Harmony.dll','Mono.Cecil.dll','MonoMod.RuntimeDetour.dll','MonoMod.Utils.dll')){if(!(Test-Path -LiteralPath (Join-Path $root "payload\$name"))){throw "Incomplete addon: $name"}}
if($ValidateOnly){Write-Output 'Compatible game and complete addon payload.';exit 0}
if(Get-Process Beholder2 -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq (Join-Path $target 'Beholder2.exe')}){throw 'Close the addon game before installing/updating.'}
Write-Output 'Copying your game into the isolated prototype folder...'
& robocopy $game $target /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP
if($LASTEXITCODE -ge 8){throw "Game copy failed: robocopy $LASTEXITCODE"}
$managed=Join-Path $target 'Beholder2_Data\Managed'
Copy-Item -Path (Join-Path $root 'payload\*.dll') -Destination $managed -Force
Add-Type -Path (Join-Path $root 'payload\Mono.Cecil.dll')
$resolver=New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory($managed)
$parameters=New-Object Mono.Cecil.ReaderParameters
$parameters.AssemblyResolver=$resolver
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($sourceAssembly,$parameters)
$mod=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed 'FloorAdmin.dll'))
try{
 $boot=($mod.MainModule.Types | Where-Object Name -eq FloorAdmin).Methods | Where-Object Name -eq Boot
 $method=($assembly.MainModule.Types | Where-Object Name -eq SaveLoadManager).Methods | Where-Object Name -eq Start
 if(!$boot -or !$method -or !$method.HasBody){throw 'Expected bootstrap methods are missing.'}
 $il=$method.Body.GetILProcessor()
 $il.InsertBefore($method.Body.Instructions[0],$il.Create([Mono.Cecil.Cil.OpCodes]::Call,$assembly.MainModule.ImportReference($boot)))
 $assembly.Write((Join-Path $managed 'Assembly-CSharp.dll'))
}finally{$assembly.Dispose();$mod.Dispose();$resolver.Dispose()}
Write-Output 'Indexing local game asset metadata...'
& (Join-Path $root 'AssetIndex.exe') (Join-Path $target 'Beholder2_Data') (Join-Path $target 'asset-catalog.tsv')
if($LASTEXITCODE -ne 0){Write-Warning 'Asset indexing was incomplete. Run IndexAssets.cmd to retry.'}
Write-Output 'Installed. Run LaunchMod.exe, then press F8 for the editor. / Готово: LaunchMod.exe, редактор — F8.'

