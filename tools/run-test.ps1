param([switch]$Headless, [string]$Profile='revision-test-profile')
$root=Split-Path $PSScriptRoot -Parent
$argsList=@('-batchmode','-screen-fullscreen','0','-screen-width','960','-screen-height','540','-logFile',"`"$root\prototype\unity.log`"","`"--save-path=$root/$Profile/`"")
if($Headless){$argsList+='-nographics'}
$p=Start-Process -FilePath "$root\prototype\Beholder2.exe" -WorkingDirectory "$root\prototype" -ArgumentList $argsList -WindowStyle Hidden -PassThru
$p.WaitForExit(); "EXIT=$($p.ExitCode)"

