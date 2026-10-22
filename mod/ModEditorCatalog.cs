using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

partial class ModEditorWindow {
 class CatalogItem {internal string file,id,type,name;internal int scene;}
 bool fullCatalog=true,catalogLoading;int catalogObjects,catalogPage;string catalogQuery=null;List<CatalogItem> catalog=new List<CatalogItem>(),catalogMatches=new List<CatalogItem>();Vector2 catalogScroll;
 string CatalogPath {get{return Path.Combine(Path.GetDirectoryName(Application.dataPath),"asset-catalog.tsv");}}
 IEnumerator ReadCatalog(){catalogLoading=true;catalog.Clear();catalogObjects=0;catalogQuery=null;using(var reader=new StreamReader(CatalogPath)){reader.ReadLine();string line;while((line=reader.ReadLine())!=null){var fields=line.Split('\t');catalogObjects++;if(fields.Length==5&&fields[3].Length>0){int scene;int.TryParse(fields[4],out scene);catalog.Add(new CatalogItem{file=fields[0],id=fields[1],type=fields[2],name=fields[3],scene=scene});}if(catalogObjects%5000==0)yield return null;}}catalogLoading=false;catalogQuery=null;FloorAdmin.Log("EDITOR catalog objects="+catalogObjects+" named="+catalog.Count);}
 void DrawCatalog(Rect rect){GUILayout.BeginArea(rect,GUI.skin.box);GUILayout.BeginHorizontal();GUILayout.Label("Библиотека всех файлов",heading);assetSearch=GUILayout.TextField(assetSearch,GUILayout.Width(160));if(GUILayout.Button("Предметы сцены")){fullCatalog=false;RefreshProps();}if(GUILayout.Button("Перечитать")&&!catalogLoading&&File.Exists(CatalogPath))StartCoroutine(ReadCatalog());GUILayout.EndHorizontal();
  if(!File.Exists(CatalogPath)){GUILayout.Label("Запустите IndexAssets.cmd рядом с модом: он прочитает каталог вашей установленной игры.",small);GUILayout.EndArea();return;}
  if(catalogObjects==0&&!catalogLoading)StartCoroutine(ReadCatalog());
  if(catalogLoading){GUILayout.Label("Читается каталог: "+catalogObjects+" записей…");GUILayout.EndArea();return;}
  if(catalogQuery!=assetSearch){catalogQuery=assetSearch;catalogPage=0;catalogMatches=catalog.Where(a=>assetSearch.Length==0||(a.name+" "+a.type+" "+a.file).IndexOf(assetSearch,StringComparison.OrdinalIgnoreCase)>=0).ToList();}
  GUILayout.Label(catalogObjects+" записей в файлах; "+catalog.Count+" именованных ресурсов. Поиск по имени, типу или файлу.",small);catalogScroll=GUILayout.BeginScrollView(catalogScroll);
  foreach(var item in catalogMatches.Skip(catalogPage*30).Take(30)){GUILayout.BeginHorizontal();GUILayout.Label(item.name+"  ·  "+item.type+"  ·  "+item.file+" #"+item.id,small);if(item.scene>=0&&item.type=="GameObject"&&GUILayout.Button("Найти в сцене",GUILayout.Width(120)))StartCoroutine(FindCatalogObject(item));GUILayout.EndHorizontal();}
  GUILayout.EndScrollView();GUILayout.BeginHorizontal();if(GUILayout.Button("←"))catalogPage=Mathf.Max(0,catalogPage-1);GUILayout.Label((catalogPage+1)+" / "+Mathf.Max(1,(catalogMatches.Count+29)/30));if(GUILayout.Button("→"))catalogPage=Mathf.Min(Mathf.Max(0,(catalogMatches.Count-1)/30),catalogPage+1);GUILayout.EndHorizontal();GUILayout.EndArea();
 }
 IEnumerator FindCatalogObject(CatalogItem item){if(item.scene>=SceneManager.sceneCountInBuildSettings){ModEditorStore.Status="Сцена не найдена в этой версии игры";yield break;}string name=Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(item.scene));yield return OpenEditorScene(name);var matches=Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.gameObject.scene.name==name&&t.name==item.name).ToArray();if(matches.Length==1){Select(matches[0]);Frame();fullCatalog=false;RefreshProps();assetSearch=item.name;ModEditorStore.Status="Объект найден. Для копии используйте «Поставить» в библиотеке сцены.";}else{search=item.name;ModEditorStore.Status="Совпадений в дереве: "+matches.Length+". Выберите нужный объект по пути.";}}
}
