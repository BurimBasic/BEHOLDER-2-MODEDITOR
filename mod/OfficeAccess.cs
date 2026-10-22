using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using NodeCanvas.Tasks.Conditions;

static class OfficeAccess {
 internal static Transform NormalDoor(){
  var all=Resources.FindObjectsOfTypeAll<Transform>();
  var door=all.FirstOrDefault(t=>t.gameObject.scene.name=="BossRoom"&&FloorAdmin.PathOf(t)=="Active/FurnitureActions/ClosedDoor");
  if(door==null)return null;
  var confiscated=all.FirstOrDefault(t=>t.gameObject.scene.name=="BossRoom"&&FloorAdmin.PathOf(t)=="Active/FurnitureActions/ClosedDoorFergussonArrested");
  if(confiscated!=null&&confiscated.gameObject.activeSelf)confiscated.gameObject.SetActive(false);
  if(!door.gameObject.activeSelf)door.gameObject.SetActive(true);
  return door;
 }
 internal static void Ensure(){
  NormalDoor();
  var player=PlayerGameController.Instance;
  if(player==null)return;
  var page=Resources.FindObjectsOfTypeAll<NpcPage>().FirstOrDefault(p=>p.name=="Ferguson");
  if(page==null)return;
  var meta=player.GetNpcPageMetadata(page);
  if(!player.ContainsNpcPage(page))player.AddNpcPage(page);
  var arrested=Resources.FindObjectsOfTypeAll<NpcInfo>().FirstOrDefault(i=>i.name=="info_arrested_male");
  if(arrested!=null&&!player.GetNpcPageMetadata(page).Unlocked.Contains(arrested))player.UnlockNpcInfo(page,arrested);
  if(!meta.Deactivated){
   NodeCanvas.Tasks.Actions.DeactivateNpc.Run(page,null,false);
   FloorAdmin.Log("FERGUSON native deactivation applied");
  }
  // The original Boss FSM references this exact actor transform.
  foreach(var actor in UnityEngine.Object.FindObjectsOfType<Transform>().Where(t=>t.name=="Ferguson"&&t.gameObject.scene.name=="BossRoom"))
   NodeCanvas.Tasks.Actions.DeactivateNpc.Run(page,actor,false);
  // Replace every original outside-door action variant, including the day-one
  // KnockDoorOnly and the business-trip variant selected by the native graphs.
  var open=Resources.FindObjectsOfTypeAll<PlayerAction>().FirstOrDefault(a=>a.name=="OpenDoor");
  if(open==null)return;
  foreach(var list in Resources.FindObjectsOfTypeAll<ActionsList>()){
   if(list.gameObject.scene.name!="BossRoom")continue;
   var path=FloorAdmin.PathOf(list.transform);
   if(!path.StartsWith("Active/FurnitureActions/ClosedDoor/"))continue;
   if(list.Actions==null||!list.Actions.Any(a=>a!=null&&(a.name=="KnockDoorOnly"||a.name=="KnockDoor"||a.name=="WaitBossLeave"||a.name=="OpenDoor")))continue;
   list.Actions=new[]{open}; list.Disabled=false;
  }
 }
}

// Build the original action menu at the click boundary. The scene's active
// child ActionsList can disappear after its quest-driven switchers run.
[HarmonyPatch(typeof(PlayerActionsSystem),"OnClickRaycast")]
class AdministratorDoorMenu {
 static bool Prefix(PlayerActionsSystem __instance,RaycastHit __0,Vector3 __1){
  var hit=__0.transform;
  if(hit==null||hit.gameObject.scene.name!="BossRoom")return true;
  while(hit!=null&&hit.name!="ClosedDoor"&&hit.name!="ClosedDoorFergussonArrested")hit=hit.parent;
  if(hit==null)return true;
  var path=FloorAdmin.PathOf(hit);
  if(path!="Active/FurnitureActions/ClosedDoor"&&path!="Active/FurnitureActions/ClosedDoorFergussonArrested")return true;
  if(__instance.IsBusy||UnityEngine.Object.FindObjectsOfType<InkController>().Any(i=>i.InProgress))return false;
  if((bool)AccessTools.Method(typeof(PlayerActionsSystem),"TryHideActionsUi").Invoke(__instance,null))return false;
  var door=OfficeAccess.NormalDoor();if(door==null)return true;
  var outside=door.Find("KnockWp");var inside=door.Find("AnimExit");
  bool leaving=false;
  if(outside!=null&&inside!=null){var axis=inside.position-outside.position;leaving=Vector3.Dot(__instance.transform.position-outside.position,axis)>axis.sqrMagnitude*.5f;}
  var action=Resources.FindObjectsOfTypeAll<PlayerAction>().FirstOrDefault(a=>a.name==(leaving?"ExitDoor":"OpenDoor"));
  if(action==null)return true;
  var owner=door.GetComponent<NodeCanvas.StateMachines.FSMOwner>();
  if(owner!=null&&owner.blackboard.variables.ContainsKey("IsInside"))owner.blackboard.variables["IsInside"].value=leaving;
  __instance.Navigation.Lock=true;
  var ui=(PlayerActionsUI)AccessTools.Field(typeof(PlayerActionsSystem),"Ui").GetValue(__instance);
  ui.Show(__instance,new[]{action},__1,door);
  FloorAdmin.Log("DOOR native menu shown: "+action.name);
  return false;
 }
}

// Both are original player-only checks. Keep the queue, its colliders and NPC FSMs.
[HarmonyPatch(typeof(TriggerController),"OnTriggerEnter")]
class AppointmentQueueBypass {
 internal static bool IsAppointmentCheck(TriggerController trigger){
  if(trigger.gameObject.scene.name!="BossRoom")return false;
  var path=FloorAdmin.PathOf(trigger.transform);
  return path=="Active/Lines/LineAction/Trigger" || path=="Active/FurnitureActions/ClosedDoor/OutsideActions/SecretaryNoKnockTrigger" || path=="Active/NpcActions/Secretary/Trigger";
 }
 static bool Prefix(TriggerController __instance,Collider __0){
  if(__0!=null&&__0.GetComponent<PlayerActionsSystem>()!=null&&IsAppointmentCheck(__instance))return false;
  return true;
 }
}

// Also handle an action menu that was already populated before Ensure ran.
[HarmonyPatch(typeof(PlayerActionsSystem),"Run")]
class AdministratorDoorAction {
 static void Prefix(ref PlayerAction __0,ref Transform __1){
  if(__0==null||__1==null||__1.gameObject.scene.name!="BossRoom")return;
  if(__0.name!="KnockDoorOnly"&&__0.name!="KnockDoor"&&__0.name!="WaitBossLeave"&&__0.name!="OpenDoor")return;
  var door=__1;
  while(door!=null&&door.name!="ClosedDoor"&&door.name!="ClosedDoorFergussonArrested")door=door.parent;
  if(door==null)return;
  var path=FloorAdmin.PathOf(door);
  if(path!="Active/FurnitureActions/ClosedDoor"&&path!="Active/FurnitureActions/ClosedDoorFergussonArrested")return;
  door=OfficeAccess.NormalDoor();if(door==null)return;
  var open=Resources.FindObjectsOfTypeAll<PlayerAction>().FirstOrDefault(a=>a.name=="OpenDoor");
  if(open!=null){
   __0=open;__1=door;
   // Outside entry must not be rejected by a stale saved IsInside flag.
   // Leaving uses ExitDoor and never reaches this prefix branch.
   var owner=door.GetComponent<NodeCanvas.StateMachines.FSMOwner>();
   if(owner!=null&&owner.blackboard.variables.ContainsKey("IsInside"))owner.blackboard.variables["IsInside"].value=false;
  }
 }
}

