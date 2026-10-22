using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

static class OfficeComputer {
 internal static PlayerAction Access;
 internal static bool IsDesk(Transform t){return t!=null&&t.gameObject.scene.name=="BossRoom"&&(FloorAdmin.PathOf(t)=="Active/FurnitureActions/WorkplaceBoss"||FloorAdmin.PathOf(t).StartsWith("Active/FurnitureActions/WorkplaceBoss/"));}
 internal static void Ensure(){
  foreach(var list in Resources.FindObjectsOfTypeAll<ActionsList>()){
   if(!IsDesk(list.transform)||list.Actions==null)continue;
   var hack=list.Actions.FirstOrDefault(a=>a!=null&&a.name=="HackLvl2");
   if(hack==null)continue;
   if(Access==null){
    Access=UnityEngine.Object.Instantiate(hack); Access.name=hack.name;
    AccessTools.Field(typeof(PlayerAction),"ActionName").SetValue(Access,"Сесть за компьютер");Access.Price=0;Access.Duration=0;
    Access.RequiredItems.Clear();Access.RequiredBuffs.Clear();Access.RequiredItemStrings.Clear();
   }
   list.Actions=new[]{Access};list.Disabled=false;
  }
  foreach(var desk in Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.gameObject.scene.name=="BossRoom"&&FloorAdmin.PathOf(t)=="Active/FurnitureActions/WorkplaceBoss")){
   var screens=desk.Find("Terminal/TerminalFreeTime/Desktops");if(screens==null)continue;
   foreach(var path in new[]{"Desktop_00/Docs","Desktop_00/Photos","Desktop_00/NewMail","DesktopDocs","Doc1","Doc2","Doc3","DesktopPhotos","Photo01","Photo02"}){
    var item=screens.Find(path);if(item==null)continue;
    if(item.GetComponent<OfficePrivateContent>()==null)item.gameObject.AddComponent<OfficePrivateContent>();
    if(item.gameObject.activeSelf)item.gameObject.SetActive(false);
   }
   AdminDesktop.Ensure(screens);
   var title=screens.Find("Desktop_00/Text");
   if(title!=null){var text=title.GetComponent<LocalizedText>();if(text!=null)text.text="Рабочий компьютер руководителя первого этажа";}
  }
 }
}

// Existing FSMs can reopen a saved panel; never expose the previous owner's files.
class OfficePrivateContent : MonoBehaviour {
 void OnEnable(){gameObject.SetActive(false);}
}

[HarmonyPatch(typeof(MailClient),"NewMail")]
class OfficePrivateMail {
 static bool Prefix(MailClient __instance){
  if(!OfficeComputer.IsDesk(__instance.transform))return true;
  AdminMail.Fill(__instance);return false;
 }
}

[HarmonyPatch(typeof(PlayerActionsSystem),"Run")]
class OfficeComputerRun {
 static void Prefix(ref PlayerAction __0,Transform __1){
  if(OfficeComputer.IsDesk(__1)&&__0!=null&&__0.name=="HackLvl2"&&OfficeComputer.Access!=null){AdminSceneShot.CaptureRoomView();__0=OfficeComputer.Access;}
 }
}



