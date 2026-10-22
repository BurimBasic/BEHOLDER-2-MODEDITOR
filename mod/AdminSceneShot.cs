using System;
using System.Linq;
using UnityEngine;
using Cinemachine;
using AudioComposerPlugin;
using HarmonyLib;

class AdminSceneShot : MonoBehaviour {
 static Vector3 roomView;
 internal static void CaptureRoomView(){if(Camera.main!=null){roomView=-Camera.main.transform.forward;roomView.y=0;roomView.Normalize();}}
 static AudioComponent hornTemplate;
 internal static NpcPage Horn;
 internal static void CacheHorn(){
  if(Horn==null)Horn=Resources.FindObjectsOfTypeAll<NpcPage>().FirstOrDefault(p=>p.name=="Horn");
  if(hornTemplate!=null)return;
  var source=Resources.FindObjectsOfTypeAll<AudioComponent>().FirstOrDefault(a=>a.PathsToClips!=null&&a.PathsToClips.Any(p=>p.clip!=null&&p.path!=null&&p.path.Contains("vo_npc_guard_2")));
  if(source==null)return;
  var holder=new GameObject("AdminHornAudioTemplate");holder.SetActive(false);DontDestroyOnLoad(holder);hornTemplate=holder.AddComponent<AudioComponent>();
  var clip=source.PathsToClips.First(p=>p.clip!=null&&p.path!=null&&p.path.Contains("vo_npc_guard_2"));hornTemplate.PathsToClips=new[]{clip};AccessTools.Field(typeof(AudioComponent),"SoundData").SetValue(hornTemplate,JsonUtility.FromJson<SoundData>(JsonUtility.ToJson(source.SoundData)));hornTemplate.SoundData.KeysClips.Clear();hornTemplate.SoundData.KeysClips.Add(new ClipPath{Key="Dialogue_Talk",Path=clip.path});JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source.SourceParams),hornTemplate.SourceParams);hornTemplate.Type=source.Type;hornTemplate.AudioType=source.AudioType;
 }
 CinemachineVirtualCamera shot;
 Canvas[] screens;bool[] enabledScreens;bool closed;
 internal static AdminSceneShot Begin(Transform visitor,bool announcement){
  var scope=new GameObject("AdminConversationShot").AddComponent<AdminSceneShot>();
  
  scope.shot=scope.gameObject.AddComponent<CinemachineVirtualCamera>();scope.shot.Priority=10000;
  var camera=Camera.main;
  if(camera!=null){scope.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);scope.shot.m_Lens.FieldOfView=camera.fieldOfView;}
  if(!announcement){
   var player=PlayerActionsSystem.Instance.transform;var playerPoint=VisualCenter(player);var visitorPoint=VisualCenter(visitor);var center=(playerPoint+visitorPoint)*.5f;
   var axis=visitorPoint-playerPoint;axis.y=0;var direction=roomView.sqrMagnitude>.1f?roomView:Vector3.Cross(axis,Vector3.up).normalized;if(direction.sqrMagnitude<.1f)direction=-player.forward;
   float distance=Mathf.Max(4.5f,Vector3.Distance(playerPoint,visitorPoint)*1.3f);scope.transform.position=center+direction*distance+Vector3.up*1.6f;scope.transform.LookAt(center);scope.shot.m_Lens.FieldOfView=55;FloorAdmin.Log("CAMERA conversation player="+playerPoint+" visitor="+visitorPoint+" shot="+scope.transform.position);
   scope.screens=Resources.FindObjectsOfTypeAll<Canvas>().Where(c=>OfficeComputer.IsDesk(c.transform)).ToArray();scope.enabledScreens=scope.screens.Select(c=>c.enabled).ToArray();foreach(var screen in scope.screens)screen.enabled=false;
  }
  return scope;
 }
 static Vector3 VisualCenter(Transform target){if(OfficeSeating.Active&&PlayerActionsSystem.Instance!=null&&target==PlayerActionsSystem.Instance.transform)return OfficeSeating.Center;var renderers=target.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();if(renderers.Length==0)return target.position+Vector3.up;var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);return bounds.center;}
 internal void Close(){if(closed)return;closed=true;if(screens!=null)for(int i=0;i<screens.Length;i++)if(screens[i]!=null)screens[i].enabled=enabledScreens[i];Destroy(gameObject);}
 internal static void AddHornAudio(GameObject target){
  CacheHorn();var source=hornTemplate;
  if(source==null){FloorAdmin.Log("PA voice unavailable: guard clip not loaded");return;}
  var clip=source.PathsToClips[0];var audio=target.AddComponent<AudioComponent>();audio.PathsToClips=new[]{clip};AccessTools.Field(typeof(AudioComponent),"SoundData").SetValue(audio,JsonUtility.FromJson<SoundData>(JsonUtility.ToJson(source.SoundData)));audio.SoundData.KeysClips.Clear();audio.SoundData.KeysClips.Add(new ClipPath{Key="Dialogue_Talk",Path=clip.path});JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source.SourceParams),audio.SourceParams);audio.Type=source.Type;audio.AudioType=source.AudioType;
 }
}
[HarmonyPatch(typeof(NpcPage),"get_DisplayName")]
class AdminEmployeeName {static bool Prefix(NpcPage __instance,ref string __result){if(!__instance.name.StartsWith("AdminEmployee_"))return true;int id;if(!int.TryParse(__instance.name.Substring(14),out id))return true;__result=AdminManagement.FullName(AdminManagement.Employee(id));return false;}}





