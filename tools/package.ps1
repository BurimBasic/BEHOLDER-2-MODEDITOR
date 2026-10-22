param([string]$Version='0.2.0-alpha.1')
$ErrorActionPreference='Stop'
if($Version -notmatch '^[0-9A-Za-z.-]+$'){throw 'Invalid version'}
$root=Split-Path $PSScriptRoot -Parent
$stage=Join-Path $root "release\Beholder2-ModEditor-v$Version-Windows"
New-Item -ItemType Directory -Force -Path $stage | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'dist\payload') -Destination $stage -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root 'vendor') -Destination $stage -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root 'docs') -Destination $stage -Recurse -Force
foreach($name in @('LaunchMod.exe','AssetIndex.exe')){Copy-Item -LiteralPath (Join-Path $root "dist\$name") -Destination $stage -Force}
Get-ChildItem (Join-Path $root 'installer') -File | Copy-Item -Destination $stage -Force
foreach($name in @('README.md','README.ru.md','LICENSE','THIRD_PARTY_NOTICES.md','CHANGELOG.md')){Copy-Item -LiteralPath (Join-Path $root $name) -Destination $stage -Force}
$allowed=@('payload','vendor','docs','LaunchMod.exe','AssetIndex.exe','Install.cmd','Install.ps1','IndexAssets.cmd','README.md','README.ru.md','LICENSE','THIRD_PARTY_NOTICES.md','CHANGELOG.md')
$files=foreach($name in $allowed){Get-ChildItem -LiteralPath (Join-Path $stage $name) -Recurse -File}
$manifest=foreach($file in $files){(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLower()+'  '+$file.FullName.Substring($stage.Length+1).Replace('\','/')}
$manifest | Set-Content -LiteralPath (Join-Path $stage 'SHA256SUMS.txt') -Encoding ASCII
$zip=Join-Path $root "release\Beholder2-ModEditor-v$Version-Windows.zip"
$paths=@($allowed+@('SHA256SUMS.txt') | ForEach-Object {Join-Path $stage $_})
Compress-Archive -LiteralPath $paths -DestinationPath $zip -Force
Write-Output $zip
Get-FileHash -LiteralPath $zip -Algorithm SHA256
