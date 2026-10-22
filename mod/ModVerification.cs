using System;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

static class ModVerification { internal static IEnumerator ScreenCapture(){yield return new WaitForEndOfFrame();var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Application.dataPath),"editor-ui.png"),image.EncodeToPNG());UnityEngine.Object.Destroy(image);FloorAdmin.Log("SCREEN captured");}
 internal static IEnumerator Gameplay(){
  var player=PlayerActionsSystem.Instance;var nav=player.Navigation;
  var count=AccessTools.Field(typeof(PlayerNavigation),"m_Lock");int before=(int)count.GetValue(null);
  var shot=AdminSceneShot.Begin(player.transform,true);shot.Close();shot.Close();
  if((int)count.GetValue(null)!=before)throw new Exception("Camera altered navigation lock count");
  FloorAdmin.Log("PASS camera leaves navigation counter unchanged");
  var desk=Resources.FindObjectsOfTypeAll<Transform>().First(t=>t.gameObject.scene.name=="BossRoom"&&FloorAdmin.PathOf(t)=="Active/FurnitureActions/WorkplaceBoss");
  var terminal=desk.Find("Terminal/TerminalFreeTime");var point=desk.Find("Anim");
  var oldPosition=nav.transform.localPosition;var oldRotation=nav.transform.localRotation;bool wasEnabled=nav.enabled,wasActive=terminal.gameObject.activeSelf;
  nav.enabled=false;nav.transform.position=point.position;var ancestors=new System.Collections.Generic.Dictionary<GameObject,bool>();for(var t=terminal;t!=null&&t!=desk.parent;t=t.parent){ancestors[t.gameObject]=t.gameObject.activeSelf;t.gameObject.SetActive(true);}FloorAdmin.Log("VERIFY terminal="+terminal.gameObject.activeInHierarchy+" animators="+string.Join(";",nav.GetComponentsInChildren<Animator>(true).Select(a=>a.name+":"+string.Join(",",a.parameters.Select(v=>v.name+"="+v.type).ToArray())).ToArray()));OfficeSeating.Tick();
  yield return new WaitForSeconds(.8f);
  var boss=Resources.FindObjectsOfTypeAll<Animator>().First(a=>FloorAdmin.PathOf(a.transform)=="Active/NpcActions/Boss/Ferguson");var avatar=nav.GetComponentsInChildren<Animator>().First(a=>a.isHuman);
  float error=OfficeSeating.SeatDistance;
  FloorAdmin.Log((error<.04f?"PASS":"FAIL")+" seat alignment distance="+error);
  var editor=ModEditorWindow.Instance;editor.Show();var edited=nav.transform;var savedPosition=edited.position;edited.position+=Vector3.right*.5f;var expectedPosition=edited.position;yield return null;yield return null;FloorAdmin.Log((Vector3.Distance(edited.position,expectedPosition)<.01f?"PASS":"FAIL")+" editor Evan remains at edited position");edited.position=savedPosition;editor.Close();var camera=AdminSceneShot.Begin(player.transform,false);yield return new WaitForSeconds(.5f);Capture("seat-check.png");camera.Close();
  OfficeSeating.End();foreach(var pair in ancestors)pair.Key.SetActive(pair.Value);terminal.gameObject.SetActive(wasActive);nav.transform.localPosition=oldPosition;nav.transform.localRotation=oldRotation;nav.enabled=wasEnabled;
  yield return AdminPaperDesk.Open(()=>FloorAdmin.Log("VERIFY paper closed"));yield return new WaitForSeconds(.2f);
  var rig=AdminPaperDesk.Current;if(rig==null)throw new Exception("Paper desk missing");AccessTools.Method(typeof(AdminPaperDesk),"TakePaper").Invoke(rig,null);
  yield return new WaitForSeconds(.75f);
  var native=rig.GetComponentInChildren<Minigames.ProcessingMinigame>(true);var table=rig.GetComponentsInChildren<Renderer>().First(r=>r.name=="PetitionsTabletop");
  float clearance=native.BlankBack.transform.position.y-table.bounds.max.y;
  FloorAdmin.Log((native.BlankBack.gameObject.activeInHierarchy&&clearance>0?"PASS":"FAIL")+" moving paper clearance="+clearance);Capture("paper-transfer-check.png");
  yield return new WaitForSeconds(1);rig.ForceClose();
  count.SetValue(null,3);Resources.FindObjectsOfTypeAll<Button>().First(b=>FloorAdmin.PathOf(b.transform)=="OverlayGUI/Gameplay/ExitButton/ExitButton").onClick.Invoke();
  yield return new WaitForSecondsRealtime(2.6f);
  FloorAdmin.Log(((int)count.GetValue(null)==0&&!nav.Lock?"PASS":"FAIL")+" desk exit lock="+nav.Lock+" counter="+count.GetValue(null));
 }
 static void Capture(string name){AccessTools.Method(typeof(FloorAdmin),"DriverCommand").Invoke(FloorAdmin.Instance,new object[]{"papers-capture"});var folder=Path.GetDirectoryName(Application.dataPath);File.Copy(Path.Combine(folder,"papers-check.png"),Path.Combine(folder,name),true);}
 internal static void EditorWrite(){
  OfficeComputer.Ensure();var list=Resources.FindObjectsOfTypeAll<ActionsList>().First(l=>OfficeComputer.IsDesk(l.transform)&&l.Actions!=null&&l.Actions.Any(a=>a.name=="HackLvl2"));var original=list.Actions.First(a=>a.name=="HackLvl2");var originalName=original.Name;
  ModEditorStore.New("__verification");ModEditorStore.Change(new EditorChange{kind="interaction",scene=list.gameObject.scene.name,path=ModEditorStore.Key(list.transform),component=Array.IndexOf(list.GetComponents<ActionsList>(),list),action=original.name,label="Проверка редактора",price=original.Price,duration=original.Duration});
  if(list.Actions.First(a=>a.name==original.name).Name!="Проверка редактора"||original.Name!=originalName)throw new Exception("Interaction clone verification failed");
  ModEditorStore.Undo();if(!ReferenceEquals(list.Actions.First(a=>a.name==original.name),original))throw new Exception("Undo failed");ModEditorStore.Redo();ModEditorStore.Save();ModEditorStore.Export();FloorAdmin.Log("PASS editor native interaction, original preserved, undo/redo, saved export");
 }
 internal static void EditorRead(){ModEditorStore.Apply();var list=Resources.FindObjectsOfTypeAll<ActionsList>().First(l=>OfficeComputer.IsDesk(l.transform)&&l.Actions!=null&&l.Actions.Any(a=>a.name=="HackLvl2"));if(list.Actions.First(a=>a.name=="HackLvl2").Name!="Проверка редактора")throw new Exception("Interaction did not survive restart");FloorAdmin.Log("PASS editor interaction applied after process restart");ModEditorStore.New("MyMod");ModEditorStore.Save();}
}



