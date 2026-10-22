using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using UnityEngine;
using HarmonyLib;

[Serializable] class EditorChange {
 public string scene,path,kind,action,label,source;public int component,price;public float duration;
 public float x,y,z,rx,ry,rz,sx=1,sy=1,sz=1;public bool active=true,disabled;
}
[Serializable] class EditorProject {
 public int version=1;public string name="MyMod",unity;public List<EditorChange> changes=new List<EditorChange>();
}
static class ModEditorStore {
 internal static EditorProject Project=new EditorProject();
 internal static string Status="Готово";internal static bool Dirty;
 internal static readonly string Folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"ModEditorProjects");
 static readonly Dictionary<string,GameObject> spawned=new Dictionary<string,GameObject>();
 static readonly Stack<string> undo=new Stack<string>(),redo=new Stack<string>();
 static readonly Dictionary<int,EditorChange> transforms=new Dictionary<int,EditorChange>();
 static readonly Dictionary<int,Transform> targets=new Dictionary<int,Transform>();
 static readonly Dictionary<int,PlayerAction[]> originalActions=new Dictionary<int,PlayerAction[]>(),editedActions=new Dictionary<int,PlayerAction[]>();
 static readonly Dictionary<int,bool> originalDisabled=new Dictionary<int,bool>();
 static readonly Dictionary<int,ActionsList> lists=new Dictionary<int,ActionsList>();
 static readonly List<PlayerAction> clones=new List<PlayerAction>();static readonly HashSet<int> transformed=new HashSet<int>();
 internal static string Encode(EditorProject value){using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(EditorProject)).WriteObject(stream,value);return Encoding.UTF8.GetString(stream.ToArray());}}
 internal static EditorProject Decode(string value){using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(value)))return (EditorProject)new DataContractJsonSerializer(typeof(EditorProject)).ReadObject(stream);}
 internal static string Key(Transform t){var placed=t.GetComponent<EditorPlacedObject>();if(placed!=null)return placed.id;var parts=new List<string>();while(t!=null){parts.Add(Uri.EscapeDataString(t.name)+"@"+t.GetSiblingIndex());t=t.parent;}parts.Reverse();return string.Join("/",parts.ToArray());}
 internal static string SafeName(string name){var value=new string((name??"").Where(c=>!Path.GetInvalidFileNameChars().Contains(c)).ToArray()).Trim('.',' ');if(value.Length==0)throw new Exception("Укажите имя проекта");return value;}
 internal static void Initialize(){Directory.CreateDirectory(Folder);var last=Path.Combine(Folder,"active.txt");if(File.Exists(last))try{Load(File.ReadAllText(last));}catch(Exception e){Status=e.Message;FloorAdmin.Log("EDITOR load: "+e.Message);}}
 internal static void New(string name){Restore();Project=new EditorProject{name=SafeName(name),unity=Application.unityVersion};undo.Clear();redo.Clear();Dirty=true;Status="Новый проект";}
 internal static void Save(){Project.name=SafeName(Project.name);Project.unity=Application.unityVersion;Directory.CreateDirectory(Folder);var path=Path.Combine(Folder,Project.name+".json");var temp=path+".tmp";File.WriteAllText(temp,Encode(Project),Encoding.UTF8);if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);File.WriteAllText(Path.Combine(Folder,"active.txt"),Project.name);Dirty=false;Status="Сохранено: "+path;}
 internal static void Load(string name){var loaded=Decode(File.ReadAllText(Path.Combine(Folder,SafeName(name)+".json")));if(loaded.version!=1||loaded.changes==null)throw new Exception("Неподдерживаемая версия проекта");if(!string.IsNullOrEmpty(loaded.unity)&&loaded.unity!=Application.unityVersion)throw new Exception("Проект для другой версии Unity: "+loaded.unity);Validate(loaded);Restore();Project=loaded;File.WriteAllText(Path.Combine(Folder,"active.txt"),Project.name);undo.Clear();redo.Clear();Dirty=false;Apply();Status="Загружено: "+Project.name;}
 static void Validate(EditorProject project){project.name=SafeName(project.name);foreach(var p in project.changes){if(p==null||string.IsNullOrEmpty(p.scene)||string.IsNullOrEmpty(p.path)||(p.kind!="transform"&&p.kind!="interaction"&&p.kind!="spawn"))throw new Exception("Некорректное изменение проекта");if(new[]{p.x,p.y,p.z,p.rx,p.ry,p.rz,p.sx,p.sy,p.sz,p.duration}.Any(v=>float.IsNaN(v)||float.IsInfinity(v))||p.sx<=0||p.sy<=0||p.sz<=0||p.price<0||p.duration<0||p.component<0)throw new Exception("Некорректные значения проекта");}}
 internal static void Export(){Save();var folder=Path.Combine(Folder,"Exports");Directory.CreateDirectory(folder);File.Copy(Path.Combine(Folder,Project.name+".json"),Path.Combine(folder,Project.name+".json"),true);Status="Экспорт: "+Path.Combine(folder,Project.name+".json");}
 internal static EditorChange ReadTransform(Transform t){var p=t.localPosition;var r=t.localEulerAngles;var s=t.localScale;return new EditorChange{scene=t.gameObject.scene.name,path=Key(t),kind=t.GetComponent<EditorPlacedObject>()==null?"transform":"spawn",source=t.GetComponent<EditorPlacedObject>()==null?null:t.GetComponent<EditorPlacedObject>().source,x=p.x,y=p.y,z=p.z,rx=r.x,ry=r.y,rz=r.z,sx=s.x,sy=s.y,sz=s.z,active=t.gameObject.activeSelf};}
 internal static void Change(EditorChange change){undo.Push(Encode(Project));redo.Clear();Restore();Project.changes.RemoveAll(p=>p.scene==change.scene&&p.path==change.path&&p.kind==change.kind&&p.component==change.component&&p.action==change.action);Project.changes.Add(change);Dirty=true;Apply();Status="Изменение применено. Сохраните проект для перезапуска.";}
 internal static void Undo(){if(undo.Count==0)return;redo.Push(Encode(Project));Restore();Project=Decode(undo.Pop());Dirty=true;Apply();Status="Отменено";}
 internal static void Redo(){if(redo.Count==0)return;undo.Push(Encode(Project));Restore();Project=Decode(redo.Pop());Dirty=true;Apply();Status="Восстановлено";}
 internal static void Restore(){foreach(var obj in spawned.Values)if(obj!=null){obj.SetActive(false);UnityEngine.Object.Destroy(obj);}spawned.Clear();foreach(var pair in transforms){Transform t;if(targets.TryGetValue(pair.Key,out t)&&t!=null)SetTransform(t,pair.Value);}foreach(var pair in originalActions){ActionsList list;if(lists.TryGetValue(pair.Key,out list)&&list!=null){list.Actions=pair.Value;list.Disabled=originalDisabled[pair.Key];}}foreach(var action in clones)if(action!=null)UnityEngine.Object.Destroy(action);transforms.Clear();targets.Clear();originalActions.Clear();editedActions.Clear();originalDisabled.Clear();lists.Clear();clones.Clear();transformed.Clear();}
 static void SetTransform(Transform t,EditorChange p){t.localPosition=new Vector3(p.x,p.y,p.z);t.localEulerAngles=new Vector3(p.rx,p.ry,p.rz);t.localScale=new Vector3(p.sx,p.sy,p.sz);t.gameObject.SetActive(p.active);}
 internal static void Apply(){
  if(Project.changes.Count==0)return;
  var all=Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.gameObject.scene.IsValid()&&t.gameObject.scene.isLoaded).ToArray();int missing=0;foreach(var placement in Project.changes.Where(p=>p.kind=="spawn"))if(!EnsureSpawn(placement,all))missing++;
  foreach(var group in Project.changes.Where(p=>p.kind!="spawn").GroupBy(p=>p.scene+"|"+p.path)){
   var first=group.First();var matches=all.Where(t=>t.gameObject.scene.name==first.scene&&Key(t)==first.path).ToArray();if(matches.Length!=1){missing++;continue;}var target=matches[0];int id=target.GetInstanceID();
   foreach(var change in group.Where(p=>p.kind=="transform"))if(!transformed.Contains(id)){transforms[id]=ReadTransform(target);targets[id]=target;SetTransform(target,change);transformed.Add(id);}
   foreach(var componentGroup in group.Where(p=>p.kind=="interaction").GroupBy(p=>p.component)){
    var components=target.GetComponents<ActionsList>();if(componentGroup.Key<0||componentGroup.Key>=components.Length){missing++;continue;}var list=components[componentGroup.Key];int key=list.GetInstanceID();
    if(!originalActions.ContainsKey(key)){originalActions[key]=list.Actions;originalDisabled[key]=list.Disabled;lists[key]=list;var values=(list.Actions??new PlayerAction[0]).ToArray();bool valid=true;foreach(var change in componentGroup){int index=Array.FindIndex(values,a=>a!=null&&a.name==change.action);if(index<0){missing++;valid=false;continue;}var clone=UnityEngine.Object.Instantiate(values[index]);clone.name=values[index].name;AccessTools.Field(typeof(PlayerAction),"ActionName").SetValue(clone,change.label);clone.Price=change.price;clone.Duration=change.duration;clones.Add(clone);values[index]=clone;}if(valid)editedActions[key]=values;}
    PlayerAction[] edited;if(editedActions.TryGetValue(key,out edited)){if(!ReferenceEquals(list.Actions,edited))list.Actions=edited;list.Disabled=componentGroup.Last().disabled;}
   }
  }
  if(missing>0)Status="Ожидают объекты/сцены или несовместимы ссылки: "+missing;
 }
 internal static void RemoveSpawn(string key){undo.Push(Encode(Project));redo.Clear();Restore();Project.changes.RemoveAll(p=>p.path==key);Dirty=true;Apply();Status="Предмет удалён";}
 internal static GameObject FindSpawn(string key){GameObject obj;return spawned.TryGetValue(key,out obj)?obj:null;}
 static bool EnsureSpawn(EditorChange p,Transform[] all){
  GameObject existing;if(spawned.TryGetValue(p.path,out existing)&&existing!=null)return true;
  var source=all.FirstOrDefault(t=>t.gameObject.scene.name==p.scene&&Key(t)==p.source);if(source==null)return false;
  var filter=source.GetComponent<MeshFilter>();var renderer=source.GetComponent<MeshRenderer>();if(filter==null||renderer==null||filter.sharedMesh==null)return false;
  var obj=new GameObject("Placed_"+source.name);obj.SetActive(false);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj,source.gameObject.scene);obj.layer=source.gameObject.layer;
  var marker=obj.AddComponent<EditorPlacedObject>();marker.id=p.path;marker.source=p.source;
  if(renderer.isPartOfStaticBatch){marker.mesh=filter.sharedMesh;marker.materials=renderer.sharedMaterials;marker.firstSubMesh=(int)AccessTools.Property(typeof(Renderer),"staticBatchIndex").GetValue(renderer,null);var batchRoot=(Transform)AccessTools.Property(typeof(Renderer),"staticBatchRootTransform").GetValue(renderer,null);marker.origin=source.worldToLocalMatrix*(batchRoot==null?Matrix4x4.identity:batchRoot.localToWorldMatrix);}
  else{obj.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;var visual=obj.AddComponent<MeshRenderer>();visual.sharedMaterials=renderer.sharedMaterials;visual.shadowCastingMode=renderer.shadowCastingMode;visual.receiveShadows=renderer.receiveShadows;}
  var box=obj.AddComponent<BoxCollider>();box.center=source.InverseTransformPoint(renderer.bounds.center);var ext=renderer.bounds.extents;var a=source.InverseTransformVector(new Vector3(ext.x,0,0));var b=source.InverseTransformVector(new Vector3(0,ext.y,0));var c=source.InverseTransformVector(new Vector3(0,0,ext.z));box.size=new Vector3(Mathf.Abs(a.x)+Mathf.Abs(b.x)+Mathf.Abs(c.x),Mathf.Abs(a.y)+Mathf.Abs(b.y)+Mathf.Abs(c.y),Mathf.Abs(a.z)+Mathf.Abs(b.z)+Mathf.Abs(c.z))*2;
  SetTransform(obj.transform,p);spawned[p.path]=obj;return true;
 }
 internal static string Describe(Transform t){return "Scene: "+t.gameObject.scene.name+"\nID: "+Key(t)+"\nPath: "+FloorAdmin.PathOf(t)+"\nPosition: "+t.position+"\nComponents:\n"+string.Join("\n",t.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().FullName).ToArray());}
}




