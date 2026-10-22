$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Speech
$voice=New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
 $voice.SelectVoice('Microsoft Irina Desktop')
 $voice.Rate=-1
 $root=Split-Path $PSScriptRoot -Parent
 $namesText=[regex]::Match((Get-Content (Join-Path $root 'mod/AdminManagement.cs') -Raw -Encoding UTF8),'Names=\{([^}]+)\}').Groups[1].Value
 $names=[regex]::Matches($namesText,'"([^"]+)"') | ForEach-Object {$_.Groups[1].Value}
 $out=Join-Path $root 'prototype/AdminAnnouncements'
 for($i=0;$i -lt $names.Count;$i++){
  $voice.SetOutputToWaveFile((Join-Path $out (($i+1).ToString()+'.wav')))
  $voice.Speak(('Товарищ '+$names[$i]+', номер '+($i+1)+'. Пройдите в кабинет господина Редгрейва.'))
 }
 "Announcements generated: $($names.Count)"
} finally {$voice.Dispose()}

