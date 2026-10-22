param([Parameter(Mandatory=$true)][string]$GamePath,[Parameter(Mandatory=$true)][string]$CecilPath,[string]$Type='', [string]$Method='', [switch]$Names)
Add-Type -Path  $CecilPath
$a=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GamePath 'Beholder2_Data\Managed\Assembly-CSharp.dll'))
foreach($t in $a.MainModule.Types | Where-Object FullName -Match $Type) {
 if($Names){$t.FullName; continue}
 "TYPE $($t.FullName) : $($t.BaseType)"
 if(!$Method){$t.Fields | ForEach-Object {"FIELD $_"}}
 foreach($m in $t.Methods | Where-Object Name -Match $Method){
  "METHOD $m"
  if($Method -and $m.HasBody){$m.Body.Instructions | ForEach-Object {$_.ToString()}}
 }
}


