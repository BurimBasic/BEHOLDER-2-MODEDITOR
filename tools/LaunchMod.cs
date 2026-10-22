using System;
using System.Diagnostics;
using System.IO;
class LaunchMod {
 [STAThread] static void Main() {
  string root=AppDomain.CurrentDomain.BaseDirectory;
  string game=Path.Combine(root,"prototype");
  Process.Start(new ProcessStartInfo {
   FileName=Path.Combine(game,"Beholder2.exe"), WorkingDirectory=game,
   Arguments="-screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile \""+Path.Combine(game,"unity.log")+"\" \"--save-path="+Path.Combine(root,"fresh-profile").Replace('\\','/')+"/\"",
   UseShellExecute=true, WindowStyle=ProcessWindowStyle.Normal
  });
 }
}
