using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
static class AdminReception {
 static Dictionary<int,GameObject> waiting=new Dictionary<int,GameObject>();static GameObject guard;
 internal static void Init(){var d=AdminManagement.Data;if(d.visitorQueue==null)d.visitorQueue=new List<int>();if(d.visitorCalls==null)d.visitorCalls=new List<AdminJob>();foreach(var id in d.visitorQueue.Concat(d.summoned==0?new int[0]:new[]{d.summoned}))if(!d.visitorCalls.Any(c=>c.employee==id))d.visitorCalls.Add(new AdminJob{employee=id,due=id==d.summoned&&d.announced?d.summonDue:-1});if(d.receptionVersion==0){d.receptionOpen=true;d.receptionVersion=1;}}
 internal static string SetOpen(bool open){Init();AdminManagement.Data.receptionOpen=open;AdminManagement.Letter("Эван Редгрейв","Магда Ракович",open?"Возобновить приём":"Приостановить приём",open?"Пропускайте вызванных сотрудников по очереди, по одному.":"Никого не впускать. Вызванным сотрудникам ожидать у кабинета.",true);return "Письмо отправлено";}
 internal static void Tick(){
  Init();var d=AdminManagement.Data;
  if(d.summoned==0&&d.receptionOpen&&d.visitorQueue.Count>0){d.summoned=d.visitorQueue[0];d.visitorQueue.RemoveAt(0);var call=d.visitorCalls.First(c=>c.employee==d.summoned);d.announced=call.due>=0;d.summonDue=call.due;}
  var door=Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t=>t.gameObject.scene.name=="BossRoom"&&FloorAdmin.PathOf(t)=="Active/FurnitureActions/ClosedDoor/KnockWp");if(door==null)return;
  var ids=d.visitorQueue.ToList();if(d.summoned!=0&&AdminVisitor.Current==null&&(!d.receptionOpen||waiting.ContainsKey(d.summoned)))ids.Insert(0,d.summoned);
  foreach(var key in waiting.Keys.ToArray())if(!ids.Contains(key)||waiting[key]==null){if(waiting[key]!=null)UnityEngine.Object.Destroy(waiting[key]);waiting.Remove(key);}
  var models=Resources.FindObjectsOfTypeAll<Animator>().Where(a=>a.gameObject.scene.name=="BossRoom"&&FloorAdmin.PathOf(a.transform).StartsWith("Active/Lines/")&&a.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length>0).ToArray();
  for(int i=0;i<ids.Count;i++){int id=ids[i];if(waiting.ContainsKey(id))continue;var source=AdminActors.Model(AdminManagement.Employee(id),models);if(source==null)continue;NavMeshHit hit;var target=door.position-door.forward*(1+i*.75f);if(!NavMesh.SamplePosition(target,out hit,1.5f,NavMesh.AllAreas))continue;waiting[id]=Figure(source,"WaitingEmployee_"+id,hit.position,door.rotation);}
  if(d.receptionOpen&&ids.Count==0){if(guard!=null)UnityEngine.Object.Destroy(guard);guard=null;}else if(guard==null){var source=Resources.FindObjectsOfTypeAll<Animator>().FirstOrDefault(a=>a.gameObject.scene.name=="BossRoom"&&a.runtimeAnimatorController!=null&&a.runtimeAnimatorController.animationClips.Any(c=>c.name=="HighBigIdle_02")&&FloorAdmin.PathOf(a.transform).StartsWith("Active/NpcActions/")&&System.Text.RegularExpressions.Regex.IsMatch(FloorAdmin.PathOf(a.transform),"police|militia|guard",System.Text.RegularExpressions.RegexOptions.IgnoreCase)&&a.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length>0);if(source!=null){Vector3 position;Quaternion rotation;GuardPose(door,out position,out rotation);guard=Figure(source,"ReceptionGuard",position,rotation);var animator=guard.GetComponentInChildren<Animator>();var idle=source.runtimeAnimatorController.animationClips.First(c=>c.name=="HighBigIdle_02");var controller=new AnimatorOverrideController(source.runtimeAnimatorController);controller.ApplyOverrides(source.runtimeAnimatorController.animationClips.Distinct().Select(c=>new KeyValuePair<AnimationClip,AnimationClip>(c,idle)).ToList());animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Update(0);FloorAdmin.Log("RECEPTION guard idle created closed="+!d.receptionOpen);}}
 }
 internal static bool WaitingPosition(int number,out Vector3 position){GameObject figure;if(waiting.TryGetValue(number,out figure)&&figure!=null){position=figure.transform.position;return true;}position=Vector3.zero;return false;}
 internal static void Admit(int number){GameObject figure;if(waiting.TryGetValue(number,out figure)){if(figure!=null)UnityEngine.Object.Destroy(figure);waiting.Remove(number);}}
 static void GuardPose(Transform waypoint,out Vector3 position,out Quaternion rotation){
  var panels=waypoint.parent.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("door02_part_")).ToArray();
  var center=waypoint.parent.position;
  if(panels.Length>0){var bounds=panels[0].bounds;foreach(var panel in panels.Skip(1))bounds.Encapsulate(panel.bounds);center=bounds.center;}
  center.y=waypoint.position.y;
  var outward=Vector3.ProjectOnPlane(waypoint.forward,Vector3.up).normalized;
  if(Vector3.Dot(outward,waypoint.position-center)<=0)outward=-outward;
  rotation=Quaternion.LookRotation(outward,Vector3.up)*Quaternion.Euler(0,90f,0);
  position=center+outward*.80f+(rotation*Vector3.forward)*.50f;
  NavMeshHit hit;if(NavMesh.SamplePosition(position,out hit,.25f,NavMesh.AllAreas))position=hit.position;
 }
 static GameObject Figure(Animator source,string name,Vector3 position,Quaternion rotation){var holder=new GameObject(name);holder.SetActive(false);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder,source.gameObject.scene);holder.transform.SetPositionAndRotation(position,rotation);var model=UnityEngine.Object.Instantiate(source.gameObject,holder.transform);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=source.transform.lossyScale;foreach(var s in model.GetComponentsInChildren<MonoBehaviour>(true))s.enabled=false;foreach(var c in model.GetComponentsInChildren<Collider>(true))c.enabled=false;foreach(var n in model.GetComponentsInChildren<NavMeshAgent>(true))n.enabled=false;foreach(var body in model.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}model.SetActive(true);holder.SetActive(true);return holder;}
}
