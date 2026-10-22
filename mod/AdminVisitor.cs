using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using HarmonyLib;

class AdminVisitor : MonoBehaviour {
 internal static AdminVisitor Current; static NpcPage crowdPage; internal static void CachePage(){if(crowdPage==null)crowdPage=Resources.FindObjectsOfTypeAll<NpcPage>().FirstOrDefault(p=>p.name=="ManFromCrowd");} static NpcPage CreateVisitorPage(){CachePage();return crowdPage!=null?UnityEngine.Object.Instantiate(crowdPage):ScriptableObject.CreateInstance<NpcPage>();}
 NavMeshAgent agent;Animator animator;Vector3 destination,look,exitPoint;int number;bool arrived,talking,discussed,leaving;float nextAutoTalk;Ink.Runtime.Story story;
 internal static void TickSpawn(){AdminReception.Init();var pendingCall=AdminManagement.Data.visitorCalls.FirstOrDefault(c=>c.due<0&&(c.employee==AdminManagement.Data.summoned||AdminManagement.Data.visitorQueue.Contains(c.employee)));if(pendingCall!=null)Announce(AdminManagement.Employee(pendingCall.employee));
  if(!AdminManagement.Data.receptionOpen||AdminManagement.Data.summoned==0||Current!=null)return;
  if(!AdminManagement.Data.announced){Announce(AdminManagement.Employee(AdminManagement.Data.summoned));return;}if(AdminManagement.Data.clock<AdminManagement.Data.summonDue)return;
  var desk=UnityEngine.Object.FindObjectsOfType<Transform>().FirstOrDefault(t=>OfficeComputer.IsDesk(t)&&t.name=="WorkplaceBoss");if(desk==null)return;
  var entry=UnityEngine.Object.FindObjectsOfType<Transform>().FirstOrDefault(t=>t.gameObject.scene.name=="BossRoom"&&FloorAdmin.PathOf(t)=="Active/FurnitureActions/ClosedDoor/AnimExit");
  var safe=UnityEngine.Object.FindObjectsOfType<Transform>().FirstOrDefault(t=>t.gameObject.scene.name=="BossRoom"&&FloorAdmin.PathOf(t)=="Active/FurnitureActions/Safe_01");if(entry==null||safe==null)return;
  var models=Resources.FindObjectsOfTypeAll<Animator>().Where(a=>a.gameObject.scene.name=="BossRoom"&&FloorAdmin.PathOf(a.transform).StartsWith("Active/Lines/")&&a.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length>0).OrderBy(a=>FloorAdmin.PathOf(a.transform)).ToArray();if(models.Length==0)return;
  var e=AdminManagement.Employee(AdminManagement.Data.summoned);var source=AdminActors.Model(e,models);if(source==null)return;
  Vector3 waitingPosition;bool wasWaiting=AdminReception.WaitingPosition(e.number,out waitingPosition);
  NavMeshHit start,end;var goal=(desk.position+safe.position)*.5f;if(!NavMesh.SamplePosition(wasWaiting?waitingPosition:entry.position,out start,wasWaiting?.25f:3f,NavMesh.AllAreas)||!NavMesh.SamplePosition(goal,out end,4,NavMesh.AllAreas))return;
  var path=new NavMeshPath();if(!NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)return;
  var holder=new GameObject("AdminVisitor_"+e.number);holder.SetActive(false);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder,desk.gameObject.scene);
  var visual=UnityEngine.Object.Instantiate(source.gameObject,holder.transform);visual.name=e.surname;visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=source.transform.lossyScale;
  foreach(var script in visual.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=script is AudioComposerPlugin.AudioComponent;
  foreach(var collider in visual.GetComponentsInChildren<Collider>(true))collider.enabled=false;
  foreach(var rigidbody in visual.GetComponentsInChildren<Rigidbody>(true)){rigidbody.isKinematic=true;rigidbody.detectCollisions=false;}
  foreach(var nav in visual.GetComponentsInChildren<NavMeshAgent>(true))nav.enabled=false;
  var npc=holder.AddComponent<NpcSettings>();var originalPage=source.GetComponentInParent<NpcSettings>();npc.Page=CreateVisitorPage();npc.Page.name="AdminEmployee_"+e.number;AccessTools.Field(typeof(NpcPage),"Id").SetValue(npc.Page,npc.Page.name);npc.enabled=false;
  AdminActors.Voice(holder,e);holder.transform.position=start.position;var capsule=holder.AddComponent<CapsuleCollider>();capsule.center=new Vector3(0,1,0);capsule.height=2;capsule.radius=.35f;
  Current=holder.AddComponent<AdminVisitor>();Current.number=e.number;Current.destination=end.position;Current.exitPoint=start.position;Current.look=desk.position;Current.animator=visual.GetComponent<Animator>();Current.animator.applyRootMotion=false;
  Current.agent=holder.AddComponent<NavMeshAgent>();Current.agent.speed=1.5f;Current.agent.angularSpeed=180;Current.agent.radius=.3f;Current.agent.stoppingDistance=.2f;
  var talk=Resources.FindObjectsOfTypeAll<PlayerAction>().FirstOrDefault(a=>a.name=="Talk");if(talk!=null){var actions=holder.AddComponent<ActionsList>();actions.Actions=new[]{talk};}
  holder.AddComponent<MeshSelection>();holder.SetActive(true);visual.SetActive(true);Current.agent.SetDestination(end.position);AdminReception.Admit(e.number);FloorAdmin.Log("VISITOR walking "+e.number+" from "+start.position+" to "+end.position);
 }
 void Update(){if(agent==null)return;if(leaving&&!agent.pathPending&&agent.remainingDistance<=.3f){AdminManagement.Data.visitorCalls.RemoveAll(c=>c.employee==number);AdminManagement.Data.summoned=0;Destroy(gameObject);return;}if(!leaving&&!arrived&&!agent.pathPending&&agent.remainingDistance<=.3f){arrived=true;agent.isStopped=true;FloorAdmin.Log("VISITOR arrived "+number);}if(arrived&&!talking&&Time.realtimeSinceStartup>=nextAutoTalk){nextAutoTalk=Time.realtimeSinceStartup+2;Talk();}if(arrived){var dir=look-transform.position;dir.y=0;if(dir.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(dir),Time.deltaTime*4);}
  foreach(var p in animator.parameters)if(p.type==AnimatorControllerParameterType.Float&&p.name.IndexOf("speed",StringComparison.OrdinalIgnoreCase)>=0)animator.SetFloat(p.name,arrived?0:agent.velocity.magnitude);
  if(talking){var ink=Resources.FindObjectsOfTypeAll<InkController>().FirstOrDefault(c=>c.name=="InkControllerC");if(ink!=null){var current=AccessTools.Field(typeof(InkController),"m_Story").GetValue(ink) as Ink.Runtime.Story;if(current!=null){story=current;if(AdminManagement.ShouldInvite(AdminManagement.Employee(number))&&story.state.VisitCountAtPathString("loyal")>0)discussed=true;}}}
 }
 void OnMouseDown(){Talk();}
 internal void Talk(){if(AdminPaperDesk.Current!=null||AdminWorkplaceAssets.Loading)return;if(!OfficeSeating.Active&&Vector3.Distance(transform.position,PlayerActionsSystem.Instance.transform.position)>5)return;if(!arrived||talking||UnityEngine.Object.FindObjectsOfType<InkController>().Any(c=>c.InProgress))return;var ink=Resources.FindObjectsOfTypeAll<InkController>().FirstOrDefault(c=>c.name=="InkControllerC");if(ink==null)return;var e=AdminManagement.Employee(number);bool serious=AdminManagement.ShouldInvite(e);
  string json="{\"inkVersion\":17,\"root\":["+L(e.surname+", №"+e.number+": Вы вызывали, господин Редгрейв?",1)+",\"\\n\",\"ev\",\"str\","+Q("^Спросить, как идут дела")+",\"/str\",\"/ev\",{\"*\":\"small\",\"flg\":20},";
  if(serious)json+="\"ev\",\"str\","+Q("^Обсудить трудности на работе")+",\"/str\",\"/ev\",{\"*\":\"loyal\",\"flg\":20},";
  json+="\"done\",{\"small\":["+L(e.surname+": "+AdminActors.SmallTalk(e),1)+",\"\\n\",\"end\",{\"#f\":1}],\"loyal\":["+L("Редгрейв: По отчёту вижу, что на участке есть трудности. Что мешает вам работать спокойно?",0)+",\"\\n\","+L(e.surname+": Людей тревожат поборы и непонятные распоряжения. Хотелось бы ясных правил и уважения к нашей работе.",1)+",\"\\n\","+L("Редгрейв: Я услышал вас. Разберём замечания и договоримся о понятном порядке работы.",0)+",\"\\n\",\"end\",{\"#f\":1}] }]}";
  new Ink.Runtime.Story(json);var shot=AdminSceneShot.Begin(transform,false);var file=new TextAsset(json);file.name="FloorAdminConversation";talking=true;PlayerActionsSystem.Instance.Navigation.Lock=true;
  ink.LoadStory(file,transform,new List<Transform>(),()=>{e.chats++;if(discussed){e.interviews++;e.loyalty=Math.Min(100,e.loyalty+15);e.reported=false;AdminManagement.Letter("Канцелярия","Эван Редгрейв","Итог беседы: №"+e.number,"Обсуждены трудности сотрудника. Лояльность выросла на 15. Для следующей предметной беседы нужен свежий отчёт.",false,e.number);}shot.Close();PlayerActionsSystem.Instance.Navigation.Lock=false;talking=false;arrived=false;leaving=true;agent.isStopped=false;agent.SetDestination(exitPoint);Destroy(file);FloorAdmin.Log("VISITOR leaving "+number);},null);
 }
 static string L(string s,int speaker){return Q("^"+s)+",{\"#\":\"bubble: "+speaker+"\"}";}
 static string Q(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n","\\n")+"\"";}
 static bool announcing;
 static bool ExitComputer(){
  var screen=UnityEngine.Object.FindObjectsOfType<Transform>().FirstOrDefault(t=>OfficeComputer.IsDesk(t)&&t.name=="TerminalFreeTime"&&t.gameObject.activeInHierarchy);
  if(screen==null)return false;
  var exit=Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>().FirstOrDefault(b=>FloorAdmin.PathOf(b.transform)=="OverlayGUI/Gameplay/ExitButton/ExitButton");
  if(exit!=null)exit.onClick.Invoke();return true;
 }
 internal static void Announce(AdminEmployee e){if(!announcing)FloorAdmin.Instance.StartCoroutine(NativeAnnouncement(e));}
 static IEnumerator NativeAnnouncement(AdminEmployee e){
  announcing=true;
  while(UnityEngine.Object.FindObjectsOfType<InkController>().Any(c=>c.InProgress))yield return null;
  var controller=Resources.FindObjectsOfTypeAll<InkController>().FirstOrDefault(c=>c.name=="InkControllerC");
  var horn=AdminSceneShot.Horn;
  if(controller==null||horn==null){FloorAdmin.Log("PA unavailable: native Horn/controller not loaded");announcing=false;yield break;}
  var text=File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"AdminNativeAnnouncement.json"));
  text=text.Replace("\"^^$10400 \"",Q("^Внимание! Объявление охраны первого этажа."));
  text=text.Replace("\"^^$10401 \"",Q("^Товарищ "+e.surname+", номер "+e.number+", пройдите в кабинет господина Редгрейва."));
  text=text.Replace(",{\"#\":\"start_quest: QuestDongMotherBoss\"}","");
  var file=new TextAsset(text);file.name="AdminNativeCall";
  var target=new GameObject("AdminHorn");target.SetActive(false);target.transform.position=PlayerActionsSystem.Instance.transform.position;target.AddComponent<NpcSettings>().Page=horn;AdminSceneShot.AddHornAudio(target);target.SetActive(true);
  var shot=AdminSceneShot.Begin(target.transform,true);bool done=false;controller.LoadStory(file,target.transform,new List<Transform>(),()=>{done=true;},null);
  while(!done)yield return null;
  var call=AdminManagement.Data.visitorCalls.FirstOrDefault(c=>c.employee==e.number);if(call!=null){call.due=AdminManagement.Data.clock+8+(e.number%8);if(AdminManagement.Data.summoned==e.number){AdminManagement.Data.announced=true;AdminManagement.Data.summonDue=call.due;}}
  shot.Close();Destroy(target);Destroy(file);announcing=false;FloorAdmin.Log("PA native Horn completed "+e.number);
 }
}
[HarmonyPatch(typeof(PlayerActionsSystem),"Run")]
class AdminVisitorTalk {static bool Prefix(Transform __1){var visitor=__1==null?null:__1.GetComponentInParent<AdminVisitor>();if(visitor==null)return true;visitor.Talk();return false;}}










