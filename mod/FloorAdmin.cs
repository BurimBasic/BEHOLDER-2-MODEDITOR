using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class FloorAdmin : MonoBehaviour {
 public static FloorAdmin Instance;
 public static string Secretary="";
 static string Root { get { return Path.GetDirectoryName(Application.dataPath); } }
 public static void Log(string s) { File.AppendAllText(Path.Combine(Root,"admin.log"), DateTime.Now.ToString("HH:mm:ss")+" "+s+"\n"); }
 public static void Boot() {
  if(Instance!=null)return;
  try { new Harmony("local.beholder2.flooradmin").PatchAll(typeof(FloorAdmin).Assembly); var go=new GameObject("FloorAdmin"); DontDestroyOnLoad(go); Instance=go.AddComponent<FloorAdmin>();go.AddComponent<ModEditorWindow>(); Log("BOOT Unity="+Application.unityVersion); }
  catch(Exception e){ Log("BOOT ERROR "+e); }
 }
 float next;
 void Update(){ if(Time.realtimeSinceStartup<next)return; next=Time.realtimeSinceStartup+1;
  try{if(ModEditorWindow.Instance==null||!ModEditorWindow.Instance.Open){AdminSceneShot.CacheHorn();AdminVisitor.CachePage();OfficeAccess.Ensure();OfficeComputer.Ensure();OfficeSession.Ensure();OfficeSeating.Tick();if(PlayerGameController.Instance!=null){AdminManagement.Tick();AdminReception.Tick();AdminVisitor.TickSpawn();}}ModEditorWindow.Tick();}catch(Exception e){Log("APPOINTMENT ERROR "+e.Message);}
  var p=Path.Combine(Root,Environment.GetCommandLineArgs().Any(a=>a.Contains("revision-test-profile"))?"admin-test-command.txt":"admin-command.txt"); if(!File.Exists(p))return;
  var cmd=File.ReadAllText(p).Trim(); File.Delete(p);
  try { Command(cmd); } catch(Exception e){Log("COMMAND ERROR "+e);}
 }
 void Command(string cmd) {
  Log("COMMAND "+cmd);
  if(DriverCommand(cmd))return; if(cmd=="prologue-check"){Prologue.Verify();return;} if(cmd=="quit"){Application.Quit();return;}
  if(cmd=="saves"){ foreach(var f in Directory.GetFiles(((string)AccessTools.Property(typeof(SaveLoadManager),"SavesPath").GetValue(null,null)),"*.bin")){var s=new SaveLoadData(f);Log("SAVE "+Path.GetFileName(f)+" name="+s.Name+" scene="+s.SceneName+" valid="+s.Valid+" type="+s.Type);} return;}
  if(cmd.StartsWith("load ")){SaveLoadManager.Instance.Load(new SaveLoadData(Path.Combine(((string)AccessTools.Property(typeof(SaveLoadManager),"SavesPath").GetValue(null,null)),cmd.Substring(5))));return;}
  if(cmd=="fsm"){foreach(var f in FindObjectsOfType<NodeCanvas.StateMachines.FSMOwner>())Log("FSM "+PathOf(f.transform)+" state="+f.behaviour.currentStateName+" running="+f.behaviour.isRunning);return;}
  if(cmd=="ui"){foreach(var b in Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>()) if(b.gameObject.scene.IsValid()) Log("BUTTON "+PathOf(b.transform)+" interactable="+b.IsInteractable()+" active="+b.gameObject.activeInHierarchy+" listeners="+string.Join(",",Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Select(i=>b.onClick.GetPersistentMethodName(i)).ToArray()));return;}
  if(cmd.StartsWith("click ")){var b=Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>().First(x=>PathOf(x.transform)==cmd.Substring(6)); var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current); b.onClick.Invoke(); Log("CLICK invoked "+PathOf(b.transform)+" interactable="+b.IsInteractable());return;}
  if(cmd=="dump"){
   Log("SCENES "+string.Join(",",Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).name).ToArray()));
   foreach(var n in Resources.FindObjectsOfTypeAll<NpcPage>()) Log("NPC asset="+n.name+" id="+n.Id+" name="+n.Name+" display="+n.DisplayName+" floor="+n.Floor);
   foreach(var a in FindObjectsOfType<ActionsList>()) Log("POINT "+PathOf(a.transform)+" disabled="+a.Disabled+" actions="+string.Join(",",a.Actions.Select(x=>x==null?"null":x.name+":"+x.Name).ToArray()));
   foreach(var q in Resources.FindObjectsOfTypeAll<Quest>()) if(q.QuestGroup.ToString()=="FirstFloor") Log("QUEST "+q.name+" header="+q.Header+" finished="+q.Finished);
   foreach(var n in FindObjectsOfType<NpcSettings>()) if(n.Page!=null) Log("LIVE "+PathOf(n.transform)+" asset="+n.Page.name+" label="+Localization.Get(n.Page.Name));
   Log("PLAYER "+(PlayerActionsSystem.Instance==null?"null":PathOf(PlayerActionsSystem.Instance.transform)));
  }
 }
 public static string PathOf(Transform t){return t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;}
}
[HarmonyPatch(typeof(PlayerGameController),"OnSave")]
class AdminSave {static void Postfix(ref object __result){var a=(object[])__result;Array.Resize(ref a,a.Length+1);a[a.Length-1]=AdminManagement.Save();__result=a;FloorAdmin.Log("SAVE secretary="+FloorAdmin.Secretary);}}
[HarmonyPatch(typeof(PlayerGameController),"OnLoad")]
class AdminLoad {static void Postfix(object __0){var a=(object[])__0;var s=a[a.Length-1] as string;AdminManagement.Load(s);FloorAdmin.Log("LOAD secretary="+FloorAdmin.Secretary);}}

[HarmonyPatch(typeof(Localization),"GetText")]
class AdminText {
 static void Postfix(int __0,ref string __result) {
  string replacement;
  if(Prologue.Text.TryGetValue(__0,out replacement))__result=replacement;
 }
}











