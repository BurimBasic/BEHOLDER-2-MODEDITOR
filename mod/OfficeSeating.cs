using System.Linq;
using UnityEngine;
static class OfficeSeating {
 internal static bool Active {get{return seated!=null;}} internal static float SeatDistance {get{return hips==null||chair==null?float.PositiveInfinity:Vector3.Distance(hips.position,chair.position);}} internal static Vector3 Center {get{return hips==null?Vector3.zero:hips.position+Vector3.up*.45f;}} static SkinnedMeshRenderer[] meshes;static bool[] meshUpdates; static AnimatorCullingMode previousCulling;static Animator seated;static Transform chair,hips;static Vector3 originalPosition;static Quaternion originalRotation;static float exitingUntil;
 internal static void End(){exitingUntil=Time.realtimeSinceStartup+3;if(seated!=null){seated.SetBool("work",false);if(meshes!=null)for(int i=0;i<meshes.Length;i++)if(meshes[i]!=null)meshes[i].updateWhenOffscreen=meshUpdates[i];seated.cullingMode=previousCulling;seated.transform.localPosition=originalPosition;seated.transform.localRotation=originalRotation;seated=null;}if(chair!=null)Object.Destroy(chair.GetComponentInParent<Animator>().gameObject);chair=null;hips=null;if(PlayerActionsSystem.Instance!=null)PlayerActionsSystem.Instance.Navigation.transform.localScale=Vector3.one;}
 internal static void Tick(){
  if(PlayerActionsSystem.Instance==null)return;
  var desk=Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t=>t.gameObject.scene.name=="BossRoom"&&FloorAdmin.PathOf(t)=="Active/FurnitureActions/WorkplaceBoss");
  var terminal=desk==null?null:desk.Find("Terminal/TerminalFreeTime");var point=desk==null?null:desk.Find("Anim");
  bool active=Time.realtimeSinceStartup>=exitingUntil&&terminal!=null&&terminal.gameObject.activeInHierarchy&&point!=null;
  if(active&&seated==null){seated=PlayerActionsSystem.Instance.Navigation.GetComponentsInChildren<Animator>().FirstOrDefault(a=>a.isHuman&&a.runtimeAnimatorController!=null&&a.runtimeAnimatorController.animationClips.Any(c=>c.name=="PlayerSitWork"));if(seated!=null){previousCulling=seated.cullingMode;seated.cullingMode=AnimatorCullingMode.AlwaysAnimate;seated.Update(0);meshes=seated.GetComponentsInChildren<SkinnedMeshRenderer>(true);meshUpdates=meshes.Select(m=>m.updateWhenOffscreen).ToArray();foreach(var mesh in meshes)mesh.updateWhenOffscreen=true;originalPosition=seated.transform.localPosition;originalRotation=seated.transform.localRotation;var originalBoss=Resources.FindObjectsOfTypeAll<Animator>().FirstOrDefault(a=>a.gameObject.scene.name=="BossRoom"&&FloorAdmin.PathOf(a.transform)=="Active/NpcActions/Boss/Ferguson"&&a.isHuman);if(originalBoss!=null){var pose=Object.Instantiate(originalBoss.gameObject);pose.name="OfficeSeatPose";pose.SetActive(false);foreach(var renderer in pose.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;foreach(var behaviour in pose.GetComponentsInChildren<Behaviour>(true))behaviour.enabled=false;pose.transform.SetPositionAndRotation(originalBoss.transform.position,originalBoss.transform.rotation);pose.transform.localScale=originalBoss.transform.lossyScale;var clip=originalBoss.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name=="StandartNormalSitWork");if(clip!=null)clip.SampleAnimation(pose,.2f);pose.SetActive(false);foreach(var renderer in pose.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;chair=pose.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips);}hips=seated.isHuman?seated.GetBoneTransform(HumanBodyBones.Hips):seated.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="BnHips");seated.SetBool("work",true);PlayerActionsSystem.Instance.Navigation.InstantLookAt(point.position+point.forward);FloorAdmin.Log("DESK seated chair="+(chair==null?"missing":chair.position.ToString()));}}
  else if(!active&&seated!=null)End();
 }
 internal static void Align(){if(ModEditorWindow.Instance!=null&&ModEditorWindow.Instance.Open)return;if(seated==null||chair==null||hips==null)return;var boss=chair.GetComponentInParent<Animator>();if(boss!=null){var forward=boss.transform.forward;forward.y=0;if(forward.sqrMagnitude>.01f)seated.transform.rotation=Quaternion.LookRotation(forward,Vector3.up);}seated.transform.position+=chair.position-hips.position;}
}







