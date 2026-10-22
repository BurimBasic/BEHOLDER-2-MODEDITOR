using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// The donor scene is disabled before Start: no work, quests or scene switch run.
static class AdminWorkplaceAssets {
 internal static GameObject Template;
 internal static bool Loading;
 internal static IEnumerator Load(){
  if(Template!=null)yield break;
  if(Loading){while(Loading)yield return null;yield break;}
  Loading=true;string error=null;
  var oldAmbient=RenderSettings.ambientLight;var oldSky=RenderSettings.skybox;var oldFog=RenderSettings.fog;var oldAmbientMode=RenderSettings.ambientMode;float oldIntensity=RenderSettings.ambientIntensity;
  var oldMaps=LightmapSettings.lightmaps;var oldMapMode=LightmapSettings.lightmapsMode;
  var presets=new System.Collections.Generic.List<IComponentPreset>();var camera=Camera.main;
  if(camera!=null)foreach(var type in typeof(ScenePreset).Assembly.GetTypes().Where(t=>typeof(IComponentPreset).IsAssignableFrom(t)&&!t.IsAbstract&&!t.IsInterface)){try{var preset=(IComponentPreset)Activator.CreateInstance(type);preset.Capture(camera.gameObject);presets.Add(preset);}catch{}}
  UnityEngine.Events.UnityAction<Scene,LoadSceneMode> capture=(scene,mode)=>{
   if(scene.name!="2ndFloorOffice")return;
   foreach(var root in scene.GetRootGameObjects())root.SetActive(false);
   try{
    var source=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Minigames.ProcessingMinigame>(true)).First();
    var root=source.transform;while(root.parent!=null&&root.name!="PlayerWorkplace")root=root.parent;
    var container=new GameObject("AdminImport");container.SetActive(false);
    Template=UnityEngine.Object.Instantiate(root.gameObject,container.transform);Template.name="AdminWorkplaceTemplate";Template.SetActive(false);Template.transform.SetParent(null,true);UnityEngine.Object.Destroy(container);UnityEngine.Object.DontDestroyOnLoad(Template);
    foreach(var script in Template.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
    FloorAdmin.Log("PAPERS native workstation cached");
   }catch(Exception ex){error=ex.Message;}
  };
  var existing=SceneManager.GetSceneByName("2ndFloorOffice");
  if(existing.IsValid()&&existing.isLoaded){Loading=false;throw new InvalidOperationException("Вернитесь в кабинет первого этажа.");}
  SceneManager.sceneLoaded+=capture;
  yield return SceneManager.LoadSceneAsync("2ndFloorOffice",LoadSceneMode.Additive);
  SceneManager.sceneLoaded-=capture;
  yield return SceneManager.UnloadSceneAsync("2ndFloorOffice");
  RenderSettings.ambientLight=oldAmbient;RenderSettings.skybox=oldSky;RenderSettings.fog=oldFog;RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientIntensity=oldIntensity;LightmapSettings.lightmaps=oldMaps;LightmapSettings.lightmapsMode=oldMapMode;
  if(camera!=null)foreach(var preset in presets)try{preset.Apply(camera.gameObject);}catch{}
  Loading=false;
  if(error!=null)FloorAdmin.Log("PAPERS import failed: "+error);
 }
}
