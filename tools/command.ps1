param([Parameter(Mandatory=$true)][string]$Command,[int]$Wait=2,[switch]$Test)
$root=Split-Path $PSScriptRoot -Parent
$path=Join-Path $root 'prototype/admin-command.txt'
if($Test){$path=Join-Path $root 'prototype/admin-test-command.txt'}
if(Test-Path -LiteralPath $path){throw 'Previous command is still pending'}
[IO.File]::WriteAllText($path,$Command,[Text.UTF8Encoding]::new($false))
$deadline=(Get-Date).AddSeconds(15)
while((Test-Path -LiteralPath $path) -and (Get-Date) -lt $deadline){Start-Sleep -Milliseconds 200}
if(Test-Path -LiteralPath $path){throw 'Game did not consume command'}
Start-Sleep -Seconds $Wait
Get-Content (Join-Path $root 'prototype/admin.log') -Tail 5
