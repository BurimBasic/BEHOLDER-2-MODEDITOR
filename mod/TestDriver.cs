using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using NodeCanvas.Framework;
public partial class FloorAdmin {
 bool autoAdvance; float autoNext;
 void LateUpdate(){OfficeSeating.Align();if(!autoAdvance||Time.realtimeSinceStartup<autoNext)return;autoNext=Time.realtimeSinceStartup+1.2f;foreach(var c in FindObjectsOfType<InkController>())if(c.InProgress){var s=(Ink.Runtime.Story)AccessTools.Field(typeof(InkController),"m_Story").GetValue(c);if(s!=null&&s.currentChoices.Count==0)c.ContinueStory();}}
 bool DriverCommand(string cmd){if(cmd=="editor-library-write"||cmd=="editor-library-read"){StartCoroutine(ModEditorWindow.Instance.VerifyLibrary(cmd.EndsWith("read")));return true;}if(cmd.StartsWith("editor-scene ")){ModEditorWindow.Instance.Show();StartCoroutine(ModEditorWindow.Instance.OpenEditorScene(cmd.Substring(13)));return true;}if(cmd=="editor-transform-test"){ModEditorWindow.Instance.VerifyTransforms(Resources.FindObjectsOfTypeAll<Transform>().First(t=>t.gameObject.scene.name=="BossRoom"&&PathOf(t)=="Enviroment/BossTable"));return true;}if(cmd=="screen-capture"){StartCoroutine(ModVerification.ScreenCapture());return true;}if(cmd=="lock-debug"){var stack=CloseableWindowsStack.instance;var windows=(System.Collections.IEnumerable)AccessTools.Field(typeof(CloseableWindowsStack),"m_Windows").GetValue(stack);foreach(var w in windows)Log("WINDOW "+w);Log("LOADING "+SceneController.Instance.LoadingInProgress);return true;}if(cmd=="fixes-verify"){StartCoroutine(ModVerification.Gameplay());return true;}if(cmd=="editor-write-test"){ModVerification.EditorWrite();return true;}if(cmd=="editor-read-test"){ModVerification.EditorRead();return true;}if(cmd=="editor-open"){ModEditorWindow.Instance.Show();return true;}if(cmd=="editor-close"){ModEditorWindow.Instance.Close();return true;}if(cmd=="office-debug"){foreach(var a in Resources.FindObjectsOfTypeAll<Animator>().Where(a=>a.gameObject.scene.name=="BossRoom"&&PathOf(a.transform).Contains("Ferguson")))Log("SEAT source="+PathOf(a.transform)+" pos="+a.transform.position+" hips="+(a.isHuman&&a.GetBoneTransform(HumanBodyBones.Hips)!=null?a.GetBoneTransform(HumanBodyBones.Hips).position.ToString():"none")+" clips="+(a.runtimeAnimatorController==null?"none":string.Join(",",a.runtimeAnimatorController.animationClips.Select(c=>c.name).Distinct().ToArray())));foreach(var r in Resources.FindObjectsOfTypeAll<Renderer>().Where(r=>r.gameObject.scene.name=="BossRoom"&&r.name=="BossChair"))Log("SEAT chair bounds="+r.bounds);var n=PlayerActionsSystem.Instance.Navigation;Log("OFFICE nav="+n.transform.position+" root="+n.RootTransform.position+" agentType="+n.Agent.agentTypeID+" area="+n.Agent.areaMask+" agent="+n.Agent.transform.position+" lockCount="+AccessTools.Field(typeof(PlayerNavigation),"m_Lock").GetValue(null)+" locked="+n.Lock+" busy="+PlayerActionsSystem.Instance.IsBusy);foreach(var t in Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.gameObject.scene.name=="BossRoom"&&(PathOf(t)=="Enviroment/BossTable/BossChair"||PathOf(t)=="Active/FurnitureActions/WorkplaceBoss/Anim")))Log("OFFICE anchor "+PathOf(t)+" pos="+t.position+" forward="+t.forward);foreach(var a in n.GetComponentsInChildren<Animator>())if(a.isHuman)Log("OFFICE avatar="+PathOf(a.transform)+" pos="+a.transform.position+" hips="+a.GetBoneTransform(HumanBodyBones.Hips).position+" clips="+string.Join(",",a.runtimeAnimatorController.animationClips.Where(c=>System.Text.RegularExpressions.Regex.IsMatch(c.name,"sit|work",System.Text.RegularExpressions.RegexOptions.IgnoreCase)).Select(c=>c.name).Distinct().ToArray()));return true;}if(cmd=="queue-walk-prepare"){var d=AdminManagement.Data;d.receptionOpen=false;d.summoned=1;d.announced=true;d.summonDue=d.clock;AdminReception.Init();var call=d.visitorCalls.First(c=>c.employee==1);call.due=d.clock;AdminReception.Tick();Vector3 pos;Log("QUEUE waiting="+AdminReception.WaitingPosition(1,out pos)+" position="+pos);return true;}if(cmd=="queue-walk-resume"){AdminManagement.Data.receptionOpen=true;AdminReception.Tick();AdminVisitor.TickSpawn();return true;}if(cmd=="queue-walk-position"){Log("QUEUE walking position="+(AdminVisitor.Current==null?"none":AdminVisitor.Current.transform.position.ToString()));return true;}if(cmd=="desk-black-test"){SceneController.Instance.Fade(1,0);Resources.FindObjectsOfTypeAll<Button>().First(b=>PathOf(b.transform)=="OverlayGUI/Gameplay/ExitButton/ExitButton").onClick.Invoke();return true;}if(cmd=="table-materials"){foreach(var r in Resources.FindObjectsOfTypeAll<Renderer>().Where(r=>r.gameObject.scene.name=="BossRoom"&&System.Text.RegularExpressions.Regex.IsMatch(PathOf(r.transform),"desk|table|workplace|furniture",System.Text.RegularExpressions.RegexOptions.IgnoreCase)))Log("TABLE "+PathOf(r.transform)+" materials="+string.Join(",",r.sharedMaterials.Where(m=>m!=null).Select(m=>m.name+":"+(m.mainTexture==null?"none":m.mainTexture.name)).ToArray()));return true;}if(cmd=="guard-capture"){var guard=GameObject.Find("ReceptionGuard");var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;var center=guard.transform.position+Vector3.up;camera.transform.position=center+guard.transform.forward*4+Vector3.up*.3f;camera.transform.LookAt(center);DriverCommand("papers-capture");camera.transform.SetPositionAndRotation(position,rotation);return true;}if(cmd=="reception-close-test"){AdminReception.SetOpen(false);AdminReception.Tick();return true;}if(cmd.StartsWith("paper-pointer ")){var name=cmd.Substring(14);var button=AdminPaperDesk.Current.GetComponentsInChildren<Button>().Last(b=>b.name==name);var graphic=button.targetGraphic;var point=graphic==null?button.transform.position:graphic.rectTransform.TransformPoint(graphic.rectTransform.rect.center);var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=Camera.main.WorldToScreenPoint(point),button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(data,hits);Log("POINTER "+name+" hits="+string.Join(",",hits.Select(h=>h.gameObject.name).ToArray()));if(hits.Count>0)UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);return true;}if(cmd=="guard-layout"){foreach(var a in Resources.FindObjectsOfTypeAll<Animator>().Where(a=>a.gameObject.scene.name=="BossRoom"&&System.Text.RegularExpressions.Regex.IsMatch(PathOf(a.transform),"police|militia|guard",System.Text.RegularExpressions.RegexOptions.IgnoreCase)))Log("GUARD "+PathOf(a.transform)+" clips="+(a.runtimeAnimatorController==null?"none":string.Join(",",a.runtimeAnimatorController.animationClips.Select(c=>c.name).Distinct().ToArray())));return true;}if(cmd=="paper-layout"){var p=AdminPaperDesk.Current;foreach(var r in p.GetComponentsInChildren<Renderer>(true))if(!(r is SkinnedMeshRenderer))Log("LAYOUT mesh "+PathOf(r.transform)+" enabled="+r.enabled+" bounds="+r.bounds);foreach(var g in p.GetComponentsInChildren<Graphic>(true).Where(g=>g.name=="PendingPetition"||g.name=="Back"||g.name=="HelpButton"))Log("LAYOUT graphic "+PathOf(g.transform)+" active="+g.gameObject.activeInHierarchy+" color="+g.color+" scale="+g.transform.lossyScale+" rect="+g.rectTransform.rect+" canvas="+(g.canvas==null?"none":g.canvas.name)+" pos="+g.transform.position);Log("LAYOUT camera "+Camera.main.transform.position+" forward="+Camera.main.transform.forward);return true;} if(cmd=="desk-test-open"){var desk=Resources.FindObjectsOfTypeAll<Transform>().First(t=>t.gameObject.scene.name=="BossRoom"&&PathOf(t)=="Active/FurnitureActions/WorkplaceBoss");AccessTools.Method(typeof(PlayerNavigation),"WarpPosition").Invoke(PlayerActionsSystem.Instance.Navigation,new object[]{desk.Find("Anim").position});var action=desk.GetComponentsInChildren<ActionsList>(true).SelectMany(a=>a.Actions).First(a=>a.name=="HackLvl2");PlayerActionsSystem.Instance.Run(action,desk,null);return true;} if(cmd=="desk-test-walk"){var n=PlayerActionsSystem.Instance.Navigation;Log("DESK walk start="+n.transform.position+" busy="+PlayerActionsSystem.Instance.IsBusy+" locked="+n.Lock);n.SetAdjustedDestination(n.transform.position+n.transform.right*2);return true;} if(cmd=="desk-test-position"){Log("DESK walk position="+PlayerActionsSystem.Instance.Navigation.transform.position);return true;}
  if(cmd=="papers-capture"){var camera=Camera.main;var target=new RenderTexture(1280,720,24);var previous=camera.targetTexture;var active=RenderTexture.active;camera.targetTexture=target;camera.Render();RenderTexture.active=target;var picture=new Texture2D(1280,720,TextureFormat.RGB24,false);picture.ReadPixels(new Rect(0,0,1280,720),0,0);picture.Apply();File.WriteAllBytes(Path.Combine(Root,"papers-check.png"),picture.EncodeToPNG());camera.targetTexture=previous;RenderTexture.active=active;Destroy(picture);Destroy(target);return true;}
  if(cmd=="papers-take"){AccessTools.Method(typeof(AdminPaperDesk),"TakePaper").Invoke(AdminPaperDesk.Current,null);return true;}
  if(cmd=="papers-close"){AccessTools.Method(typeof(AdminPaperDesk),"Close").Invoke(AdminPaperDesk.Current,null);return true;}
  if(cmd=="papers-save-check"){var before=AdminManagement.Save();var decoded=AdminManagement.Decode(before.Substring(14));if(decoded.petitions.Count!=6||decoded.petitions.Count(p=>!string.IsNullOrEmpty(p.decision))!=AdminManagement.Data.petitions.Count(p=>!string.IsNullOrEmpty(p.decision)))throw new Exception("Petitions save mismatch");Log("PASS PAPERS save roundtrip decisions="+decoded.petitions.Count(p=>!string.IsNullOrEmpty(p.decision)));return true;}
  if(cmd=="papers-open"){PlayerGameController.Money=10000;StartCoroutine(AdminPaperDesk.Open(()=>Log("PAPERS closed")));return true;}
  if(cmd.StartsWith("papers-stamp ")){var rig=AdminPaperDesk.Current;if(rig==null)throw new Exception("No paper desk");var suffix=cmd.Substring(13);var button=rig.GetComponentsInChildren<Button>(true).First(b=>b.name=="3PhaseStamp"+suffix);button.onClick.Invoke();Log("PAPERS click "+button.name);return true;}
  if(cmd=="papers-state"){Log("PAPERS state count="+AdminManagement.Data.petitions.Count+" completed="+AdminManagement.Data.petitions.Count(p=>!string.IsNullOrEmpty(p.decision))+" loading="+AdminWorkplaceAssets.Loading);if(AdminPaperDesk.Current!=null){foreach(var a in AdminPaperDesk.Current.GetComponentsInChildren<Animator>(true))if(a.name=="RedgraveHands")Log("PAPERS hands enabled="+a.enabled+" active="+a.gameObject.activeInHierarchy+" params="+string.Join(",",a.parameters.Select(x=>x.name).ToArray()));foreach(var c in AdminPaperDesk.Current.GetComponentsInChildren<Cinemachine.CinemachineVirtualCamera>(true))Log("PAPERS camera "+c.name+" position="+c.transform.position+" active="+c.isActiveAndEnabled);foreach(var c in AdminPaperDesk.Current.GetComponentsInChildren<Canvas>(true))Log("PAPERS canvas "+c.name+" mode="+c.renderMode+" active="+c.isActiveAndEnabled);}return true;}
  if(cmd=="paper-inspect"){
   foreach(var p in Resources.FindObjectsOfTypeAll<Minigames.ProcessingMinigame>())if(p.gameObject.scene.IsValid()){
    Log("PAPER ROOT "+PathOf(p.transform));foreach(var f in typeof(Minigames.ProcessingMinigame).GetFields()) {var v=f.GetValue(p) as Component;if(v!=null)Log("PAPER REF "+f.Name+" "+PathOf(v.transform));}
    foreach(var a in new[]{p.HandsAnimator,p.WorkplaceAnimator}){Log("PAPER ANIM "+PathOf(a.transform)+" params="+string.Join(",",a.parameters.Select(x=>x.name).ToArray()));foreach(var c in a.runtimeAnimatorController.animationClips.Distinct())Log("PAPER CLIP "+c.name+" length="+c.length+" events="+string.Join(",",c.events.Select(e=>e.time+":"+e.functionName+":"+e.stringParameter).ToArray()));}
    foreach(var phase in p.Phases)foreach(var s in phase.Stamps)Log("PAPER STAMP "+s.Object.name+" object="+PathOf(s.Object.transform)+" blank="+PathOf(s.BlankStamp.transform));
   }return true;
  }
  if(cmd=="office-assets"){
   foreach(var a in Resources.FindObjectsOfTypeAll<Animator>())if(a.gameObject.scene.IsValid()&&(OfficeComputer.IsDesk(a.transform)||a.GetComponentInParent<PlayerActionsSystem>()!=null||PathOf(a.transform).Contains("DongAfterSteal"))){Log("ASSET ANIM "+PathOf(a.transform)+" params="+string.Join(",",a.parameters.Select(p=>p.name).ToArray()));if(a.runtimeAnimatorController!=null)Log("ASSET CLIPS "+string.Join(",",a.runtimeAnimatorController.animationClips.Select(c=>c.name).Distinct().ToArray()));}
   foreach(var a in Resources.FindObjectsOfTypeAll<AudioComposerPlugin.AudioComponent>())if(a.gameObject.scene.IsValid()&&(PathOf(a.transform).Contains("DongAfterSteal")||a.GetComponentInParent<PlayerActionsSystem>()!=null||a.name.Contains("Horn")))Log("ASSET AUDIO "+PathOf(a.transform)+" data="+JsonUtility.ToJson(a.SoundData));
   foreach(var t in Resources.FindObjectsOfTypeAll<Transform>())if(t.gameObject.scene.IsValid()&&System.Text.RegularExpressions.Regex.IsMatch(t.name,"cigar|smok|stamp|paper|horn|megaph",System.Text.RegularExpressions.RegexOptions.IgnoreCase))Log("ASSET PROP "+PathOf(t));
   return true;
  }
  if(cmd=="pa-inspect"){
   foreach(var bb in Resources.FindObjectsOfTypeAll<Blackboard>())if(bb.gameObject.scene.IsValid()&&bb.variables.Values.Any(v=>v.value is TextAsset&&((TextAsset)v.value).name=="OfficeDongAfterSteal")){Log("PA BOARD "+PathOf(bb.transform));foreach(var v in bb.variables)Log("PA VAR "+v.Key+"="+(v.Value.value is Component?PathOf(((Component)v.Value.value).transform):Convert.ToString(v.Value.value)));}
   foreach(var a in Resources.FindObjectsOfTypeAll<TextAsset>())if(a.text.Contains("$10400")||a.text.Contains("$11764")||a.text.Contains("$14897")){File.WriteAllText(Path.Combine(Root,a.name+"-announcement.json"),a.text);Log("PA ASSET "+a.name);}
   foreach(var c in Resources.FindObjectsOfTypeAll<InkController>())if(c.gameObject.scene.IsValid())Log("PA INK "+PathOf(c.transform)+" behavior="+(c.Behavior==null?"null":c.Behavior.name));
   foreach(var t in Resources.FindObjectsOfTypeAll<Transform>())if(t.gameObject.scene.IsValid()&&System.Text.RegularExpressions.Regex.IsMatch(t.name,"speaker|radio|megaph|rupor|loud|reproducer",System.Text.RegularExpressions.RegexOptions.IgnoreCase))Log("PA TARGET "+PathOf(t));
   foreach(var f in Resources.FindObjectsOfTypeAll<NodeCanvas.StateMachines.FSMOwner>())if(f.gameObject.scene.IsValid()&&f.behaviour!=null){var refs=new System.Collections.Generic.List<UnityEngine.Object>();var json=f.behaviour.Serialize(true,refs);if(refs.Any(r=>r!=null&&r.name=="OfficeDongAfterSteal")){File.WriteAllText(Path.Combine(Root,"PA-caller.json"),json);File.WriteAllLines(Path.Combine(Root,"PA-caller-refs.txt"),refs.Select((r,i)=>i+" "+r).ToArray());Log("PA CALLER "+PathOf(f.transform));foreach(var v in f.blackboard.variables)if(v.Value.value is Component)Log("PA BB "+v.Key+"="+PathOf(((Component)v.Value.value).transform));}}
   return true;
  }
  if(cmd=="admin-request"){
   if(!Environment.GetCommandLineArgs().Any(a=>a.Contains("revision-test-profile")))throw new Exception("Test profile required");
   AdminManagement.Init();if(AdminManagement.Data.staff.Count!=108)throw new Exception("Roster count");
   PlayerActionsSystem.Stamina=120;Log("ADMIN "+AdminManagement.Report(1));
   if(PlayerActionsSystem.Stamina!=110)throw new Exception("Report cost");
   Log("ADMIN "+AdminManagement.Summon(1));return true;
  }
  if(cmd=="admin-result"){
   var e=AdminManagement.Employee(1);if(!(e.reported&&e.reportLoyalty==35&&AdminManagement.ShouldInvite(e))&&!(e.interviews==1&&e.loyalty==50&&!e.reported))throw new Exception("Report/conversation mismatch");
   var save=(object[])AccessTools.Method(typeof(PlayerGameController),"OnSave").Invoke(PlayerGameController.Instance,null);
   var round=AdminManagement.Decode(((string)save[save.Length-1]).Substring(14));
   Log("ROUND staff="+round.staff.Count+" reported="+(round.staff.Count>0&&round.staff[0].reported)+" mail="+round.mail.Count+" expected="+AdminManagement.Data.mail.Count+" prefix="+((string)save[save.Length-1]).Substring(0,20));
   File.WriteAllText(Path.Combine(Root,"admin-state-test.json"),(string)save[save.Length-1]);
   if(round.staff.Count!=108||round.staff[0].loyalty!=e.loyalty||round.mail.Count!=AdminManagement.Data.mail.Count)throw new Exception("Save roundtrip");
   foreach(var w in Resources.FindObjectsOfTypeAll<MailWindow>().Where(w=>OfficeComputer.IsDesk(w.transform))){w.UpdateMail();if(w.Client.IncomingMail.Count+w.Client.OutgoingMail.Count!=round.mail.Count)throw new Exception("Native mailbox count");}
   Log("PASS ADMIN roster=108 loyalty="+e.loyalty+" interviews="+e.interviews+" native-mail=OK save-envelope=OK visitor="+(AdminVisitor.Current==null?"none":AdminVisitor.Current.transform.position.ToString()));
   return true;
  }
  if(cmd=="admin-rules-test"){
   if(!Environment.GetCommandLineArgs().Any(a=>a.Contains("revision-test-profile")))throw new Exception("Test profile required");
   string saved=AdminManagement.Save();int money=PlayerGameController.Money,stamina=PlayerActionsSystem.Stamina;
   try{
    AdminManagement.Reset();PlayerGameController.Money=10000;PlayerActionsSystem.Stamina=1000;
    foreach(var e in AdminManagement.Data.staff)e.loyalty=90;
    for(int i=1;i<=6;i++)AdminManagement.Report(i);
    if(PlayerGameController.Money!=5000||PlayerActionsSystem.Stamina!=940)throw new Exception("Fine/cost rule");
    AdminManagement.Report(1);if(PlayerActionsSystem.Stamina!=940)throw new Exception("Duplicate report charged");
    string pending=AdminManagement.Save();AdminManagement.Load(pending);if(AdminManagement.Data.jobs.Count!=6||!AdminManagement.Employee(1).pending)throw new Exception("Pending save/load");
    AdminManagement.Collect();AdminManagement.Data.clock+=61;AdminManagement.Tick();
    if(PlayerGameController.Money!=6080||AdminManagement.Employee(1).loyalty!=80||AdminManagement.Data.collections!=1)throw new Exception("Collection rule");
    AdminManagement.Data.collections=2;AdminManagement.Employee(1).loyalty=29;if(!AdminManagement.ComplaintDue())throw new Exception("Complaint rule");
    AdminManagement.Employee(1).loyalty=30;if(AdminManagement.ComplaintDue())throw new Exception("Complaint boundary");
    Log("PASS ADMIN RULES report cost, duplicate guard, pending save/load, fine, collection, complaint threshold");
   }finally{AdminManagement.Load(saved);PlayerGameController.Money=money;PlayerActionsSystem.Stamina=stamina;}
   return true;
  }
  if(cmd=="admin-talk"){if(AdminVisitor.Current==null)throw new Exception("Visitor missing");AdminVisitor.Current.Talk();return true;}
  if(cmd=="admin-fast-forward"){if(!Environment.GetCommandLineArgs().Any(a=>a.Contains("revision-test-profile")))throw new Exception("Test profile required");AdminManagement.Data.clock+=61;AdminManagement.Tick();return true;}
  if(cmd=="admin-choose"){var c=FindObjectsOfType<InkController>().First(x=>x.InProgress);var s=(Ink.Runtime.Story)AccessTools.Field(typeof(InkController),"m_Story").GetValue(c);s.ChooseChoiceIndex(1);c.ContinueStory();return true;}
  if(cmd=="computer-inspect"){
   var desk=Resources.FindObjectsOfTypeAll<NodeCanvas.StateMachines.FSMOwner>().First(f=>f.gameObject.scene.name=="BossRoom"&&FloorAdmin.PathOf(f.transform)=="Active/FurnitureActions/WorkplaceBoss");
   foreach(var v in desk.blackboard.variables){Log("DESKVAR "+v.Key+"="+v.Value.value);var c=v.Value.value as Component;if(c!=null&&v.Key=="Menu")foreach(var t in c.GetComponentsInChildren<Transform>(true))Log("DESKUI "+PathOf(t)+" components="+string.Join(",",t.GetComponents<Component>().Select(x=>x==null?"null":x.GetType().Name).ToArray()));}
   return true;
  }
  if(cmd.StartsWith("select ")){Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.gameObject.scene.IsValid()&&PathOf(b.transform)==cmd.Substring(7)).Select();return true;}
  if(cmd=="access-check"){
   var point=FindObjectsOfType<ActionsList>().FirstOrDefault(a=>a.gameObject.scene.name=="BossRoom"&&a.Actions.Any(x=>x!=null&&x.name=="OpenDoor"));
   if(point==null)throw new Exception("Native OpenDoor unavailable");
   var page=Resources.FindObjectsOfTypeAll<NpcPage>().First(n=>n.name=="Ferguson");
   if(!PlayerGameController.Instance.GetNpcPageMetadata(page).Deactivated)throw new Exception("Ferguson still active");
   var actor=FindObjectsOfType<Transform>().FirstOrDefault(t=>t.name=="Ferguson"&&t.gameObject.scene.name=="BossRoom");
   if(actor!=null)throw new Exception("Ferguson actor still visible");
   Log("PASS ACCESS native OpenDoor="+PathOf(point.transform)+"; Ferguson deactivated and absent");return true;
  }
  if(cmd=="door-state") {var f=FindObjectsOfType<NodeCanvas.StateMachines.FSMOwner>().First(o=>PathOf(o.transform)=="Active/FurnitureActions/ClosedDoor");Log("DOOR state="+f.behaviour.currentState.ID+" time="+Time.timeScale+" player="+PlayerActionsSystem.Instance.transform.position);foreach(var v in f.blackboard.variables)if(v.Key=="IsInside"||v.Key=="_Player"||v.Key=="_Target")Log("DOOR "+v.Key+"="+(v.Value.value is UnityEngine.Object?PathOf(((Component)v.Value.value).transform):Convert.ToString(v.Value.value)));return true;}
  if(cmd=="access-enter"){
   var point=FindObjectsOfType<ActionsList>().First(a=>a.gameObject.scene.name=="BossRoom"&&a.Actions.Any(x=>x!=null&&x.name=="OpenDoor"));
   var target=point.GetComponentInParent<NodeCanvas.StateMachines.FSMOwner>().transform;
   PlayerActionsSystem.Instance.Run(point.Actions.First(a=>a.name=="OpenDoor"),target,null);return true;
  }
  if(cmd=="queue-check"){
   var collider=PlayerActionsSystem.Instance.GetComponent<Collider>();if(collider==null)throw new Exception("No player collider");
   var triggers=Resources.FindObjectsOfTypeAll<TriggerController>().Where(AppointmentQueueBypass.IsAppointmentCheck).ToArray();
   if(triggers.Length!=3)throw new Exception("Expected three original player checks");
   var objects=Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.gameObject.scene.name=="BossRoom"&&PathOf(t).StartsWith("Active/Lines/")).ToArray();
   var active=objects.Select(t=>t.gameObject.activeSelf).ToArray();
   PlayerActionsSystem.Instance.FinishAllActions();
   foreach(var trigger in triggers)AccessTools.Method(typeof(TriggerController),"OnTriggerEnter").Invoke(trigger,new object[]{collider});
   if(PlayerActionsSystem.Instance.IsBusy)throw new Exception("Queue still captured player");
   if(!active.SequenceEqual(objects.Select(t=>t.gameObject.activeSelf)))throw new Exception("Queue objects changed");
   Log("PASS QUEUE all three original triggers ignore Evan; "+objects.Length+" queue objects unchanged");return true;
  }
  if(cmd.StartsWith("assets ")){foreach(var t in Resources.FindObjectsOfTypeAll<TextAsset>())if(System.Text.RegularExpressions.Regex.IsMatch(t.name,cmd.Substring(7),System.Text.RegularExpressions.RegexOptions.IgnoreCase)){File.WriteAllText(Path.Combine(Root,t.name+".json"),t.text);Log("ASSET "+t.name+" length="+t.text.Length);}return true;}
  if(cmd.StartsWith("scene ")){var m=AccessTools.Method(typeof(SceneController),"AsyncFadeAndLoadScene");m.Invoke(m.IsStatic?null:FindObjectOfType<SceneController>(),new object[]{cmd.Substring(6),null,null,null});return true;}
  if(cmd.StartsWith("graph ")){foreach(var f in Resources.FindObjectsOfTypeAll<NodeCanvas.StateMachines.FSMOwner>())if(f.gameObject.scene.IsValid()&&f.behaviour!=null&&System.Text.RegularExpressions.Regex.IsMatch(PathOf(f.transform),cmd.Substring(6),System.Text.RegularExpressions.RegexOptions.IgnoreCase)){var refs=new System.Collections.Generic.List<UnityEngine.Object>();File.WriteAllText(Path.Combine(Root,f.name+"-fsm.json"),f.behaviour.Serialize(true,refs));File.WriteAllLines(Path.Combine(Root,f.name+"-refs.txt"),refs.Select((o,i)=>i+" "+(o==null?"null":o.name+" "+o.GetType().Name)).ToArray());Log("GRAPH "+PathOf(f.transform));}return true;}
  if(cmd=="inspect-scene"){
   foreach(var t in Resources.FindObjectsOfTypeAll<TriggerController>())if(t.gameObject.scene.name=="BossRoom")Log("TRIGGER "+PathOf(t.transform)+" action="+t.Action+" target="+t.Target+" active="+t.gameObject.activeInHierarchy);
   foreach(var w in FindObjectsOfType<WayPoint>())if(PathOf(w.transform).StartsWith("Travel/")||PathOf(w.transform).Contains("ClosedDoor"))Log("WAY "+PathOf(w.transform)+" pos="+w.transform.position);
   var n=PlayerActionsSystem.Instance.Navigation;Log("NAV enabled="+n.enabled+" lock="+n.Lock+" agentEnabled="+n.Agent.enabled+" onMesh="+n.Agent.isOnNavMesh);return true;
  }
  if(cmd.StartsWith("place ")){var t=Resources.FindObjectsOfTypeAll<Transform>().Single(x=>x.gameObject.scene.IsValid()&&PathOf(x)==cmd.Substring(6));PlayerActionsSystem.Instance.FinishAllActions();AccessTools.Method(typeof(PlayerNavigation),"WarpPosition").Invoke(PlayerActionsSystem.Instance.Navigation,new object[]{t.position});return true;}
  if(cmd=="auto"){autoAdvance=!autoAdvance;Log("AUTO "+autoAdvance);return true;}
  if(cmd.StartsWith("act ")){var bits=cmd.Substring(4).Split('|');var p=FindObjectsOfType<ActionsList>().First(x=>PathOf(x.transform)==bits[0]);var a=p.Actions.First(x=>x.name==bits[1]);if(p.Disabled)throw new Exception("Point disabled");PlayerActionsSystem.Instance.Run(a,p.transform,null);return true;}
  if(cmd=="choices"){foreach(var c in FindObjectsOfType<InkController>())if(c.InProgress){var s=(Ink.Runtime.Story)AccessTools.Field(typeof(InkController),"m_Story").GetValue(c);Log("STORY "+c.name+" text="+(s==null?"null":s.currentText));if(s!=null)foreach(var choice in s.currentChoices)Log("CHOICE "+choice.index+" "+choice.text);}return true;}
  if(cmd=="research"){foreach(var t in Resources.FindObjectsOfTypeAll<TextAsset>())if(t.name.IndexOf("Hemnitz",StringComparison.OrdinalIgnoreCase)>=0){File.WriteAllText(Path.Combine(Root,t.name+".json"),t.text);Log("INKFILE "+t.name+" length="+t.text.Length);}return true;}
  if(cmd=="ui-visible"){foreach(var b in FindObjectsOfType<Button>())if(b.IsInteractable()&&b.GetComponentsInParent<UnityEngine.CanvasGroup>().All(g=>g.alpha>.01f))Log("VISIBLE "+PathOf(b.transform)+" text="+string.Join(" ",b.GetComponentsInChildren<LocalizedText>().Select(t=>t.text).ToArray()));return true;}
  return false;
 }
}

// Keep the isolated validation profile out of Steam Cloud.
[HarmonyLib.HarmonyPatch(typeof(SaveLoadManager),"CloudSavesSubscribe")]
class TestCloudSubscribe {static bool Prefix(){return !System.Environment.GetCommandLineArgs().Any(a=>a.StartsWith("--save-path="));}}
[HarmonyLib.HarmonyPatch(typeof(SaveLoadManager),"SaveToCloud")]
class TestCloudSave {static bool Prefix(){return !System.Environment.GetCommandLineArgs().Any(a=>a.StartsWith("--save-path="));}}
[HarmonyLib.HarmonyPatch(typeof(SaveLoadManager),"DeleteFromCloud")]
class TestCloudDelete {static bool Prefix(){return !System.Environment.GetCommandLineArgs().Any(a=>a.StartsWith("--save-path="));}}




