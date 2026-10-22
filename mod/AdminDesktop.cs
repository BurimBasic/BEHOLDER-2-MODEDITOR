using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

class AdminDesktop : MonoBehaviour {
 internal static bool RefreshNeeded;
 Transform screens,home,content;GameObject panel;LocalizedText heading;Button rowTemplate,action,back;LocalizedText captionTemplate;
 int selected;string mode="staff",template="";
 readonly Dictionary<string,float> positions=new Dictionary<string,float>();
 ScrollRect listScroll;
 internal static void Ensure(Transform root){if(root.GetComponent<AdminDesktop>()==null)root.gameObject.AddComponent<AdminDesktop>().Build(root);}
 static void StripGuard(GameObject go){foreach(var g in go.GetComponentsInChildren<OfficePrivateContent>(true))DestroyImmediate(g);}
 void Build(Transform root){
  screens=root;home=root.Find("Desktop_00");
  captionTemplate=home.Find("Text").GetComponent<LocalizedText>();
  rowTemplate=root.Find("DesktopMail/Mail/BackElements/Scroll View/Viewport/Content/Mail").GetComponent<Button>();
  panel=Instantiate(root.Find("Doc1").gameObject,root);panel.name="Administration";StripGuard(panel);
  content=panel.transform.Find("Scroll View/Viewport/Content");heading=content.Find("Text").GetComponent<LocalizedText>();heading.TextMesh.fontSize=24;heading.TextMesh.enableAutoSizing=false;
  var layout=heading.GetComponent<LayoutElement>()??heading.gameObject.AddComponent<LayoutElement>();layout.minHeight=50;layout.preferredHeight=-1;
  back=panel.transform.Find("BackDoc").GetComponent<Button>();ConfigureButton(back,"Назад",Back);
  action=Instantiate(back,panel.transform);action.name="AdminSubmit";PlaceFooter(back,0.04f,.46f);PlaceFooter(action,.54f,.96f);
  var scroll=panel.GetComponentInChildren<ScrollRect>(true);var sr=scroll.GetComponent<RectTransform>();sr.offsetMin=new Vector2(sr.offsetMin.x,90);
  listScroll=scroll;
  var bar=Instantiate(root.Find("DesktopMail/Mail/BackElements/Scroll View/Scrollbar Vertical").GetComponent<Scrollbar>(),scroll.transform);bar.name="AdminScrollbar";
  var br=bar.GetComponent<RectTransform>();br.anchorMin=new Vector2(1,0);br.anchorMax=Vector2.one;br.pivot=new Vector2(1,.5f);br.offsetMin=new Vector2(-22,0);br.offsetMax=Vector2.zero;
  bar.onValueChanged=new Scrollbar.ScrollEvent();scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.Permanent;scroll.horizontal=false;bar.gameObject.SetActive(true);
  var viewport=scroll.viewport;if(viewport!=null)viewport.offsetMax=new Vector2(-28,viewport.offsetMax.y);
  ConfigureIcon(home.Find("Docs"),"Сотрудники",()=>Show("staff"));ConfigureIcon(home.Find("NewMail"),"Почта",OpenMail);
  ConfigureIcon(home.Find("Work"),"Обработать обращения",OpenPetitions);
  var mail=root.Find("DesktopMail");StripGuard(mail.gameObject);StripGuard(root.Find("MailClient").gameObject);root.Find("MailClient").gameObject.SetActive(true);
  var mailBack=root.Find("DesktopMail/Mail/BackElements/BackMail").GetComponent<Button>();ConfigureButton(mailBack,"Назад",()=>{mail.gameObject.SetActive(false);home.gameObject.SetActive(true);});PlaceFooter(mailBack,.04f,.46f);
  var compose=Instantiate(mailBack,mailBack.transform.parent);compose.name="AdminCompose";ConfigureButton(compose,"Написать",()=>Show("templates"));PlaceFooter(compose,.54f,.96f);
  panel.SetActive(false);
 }
 void ConfigureIcon(Transform icon,string label,UnityEngine.Events.UnityAction click){
  var original=icon;icon=Instantiate(original.gameObject,original.parent).transform;icon.name="AdminIcon"+label;StripGuard(icon.gameObject);original.gameObject.SetActive(false);var b=icon.GetComponent<Button>();b.onClick=new Button.ButtonClickedEvent();b.onClick.AddListener(click);
  // Separate native icon artwork and its caption: no lettering laid over an image.
  var glyph=Instantiate(icon.gameObject,icon);glyph.name="AdminGlyph";StripGuard(glyph);DestroyImmediate(glyph.GetComponent<Button>());
  foreach(var key in glyph.GetComponents<ButtonHotkey>())DestroyImmediate(key);
  var glyphRect=glyph.GetComponent<RectTransform>();glyphRect.anchorMin=new Vector2(.15f,.28f);glyphRect.anchorMax=new Vector2(.85f,.98f);glyphRect.offsetMin=glyphRect.offsetMax=Vector2.zero;
  var image=icon.GetComponent<Image>();var glyphImage=glyph.GetComponent<Image>();glyphImage.preserveAspect=true;glyph.SetActive(true);image.enabled=false;b.targetGraphic=glyphImage;
  var caption=Instantiate(captionTemplate,icon);caption.name="Caption";caption.text=label;caption.TextMesh.fontSize=22;caption.TextMesh.enableAutoSizing=true;caption.TextMesh.fontSizeMin=14;caption.TextMesh.fontSizeMax=22;caption.TextMesh.alignment=TMPro.TextAlignmentOptions.Center;caption.TextMesh.raycastTarget=false;
  var r=caption.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=new Vector2(1,.26f);r.offsetMin=r.offsetMax=Vector2.zero;
  var le=caption.GetComponent<LayoutElement>();if(le!=null)le.ignoreLayout=true;caption.gameObject.SetActive(true);icon.gameObject.SetActive(true);
 }
 static void PlaceFooter(Button b,float left,float right){var r=b.GetComponent<RectTransform>();r.anchorMin=new Vector2(left,0);r.anchorMax=new Vector2(right,0);r.pivot=new Vector2(.5f,0);r.offsetMin=new Vector2(0,12);r.offsetMax=new Vector2(0,72);}
 static void ConfigureButton(Button b,string label,UnityEngine.Events.UnityAction callback){
  foreach(var hotkey in b.GetComponentsInChildren<ButtonHotkey>(true))hotkey.enabled=false;
  foreach(var input in b.GetComponentsInChildren<InputTypeEnabledObject>(true))input.gameObject.SetActive(false);
  var text=b.GetComponentsInChildren<LocalizedText>(true).FirstOrDefault();if(text!=null){text.text=label;text.gameObject.SetActive(true);}
  b.onClick=new Button.ButtonClickedEvent();if(callback!=null)b.onClick.AddListener(callback);b.interactable=callback!=null;
 }
 void Show(string next){if(panel.activeSelf)positions[mode]=listScroll.verticalNormalizedPosition;mode=next;foreach(Transform t in screens)if(t!=panel.transform&&t.name!="MailClient")t.gameObject.SetActive(false);panel.SetActive(true);Render();}
 void OpenMail(){panel.SetActive(false);home.gameObject.SetActive(false);var m=screens.Find("DesktopMail");m.gameObject.SetActive(true);AccessTools.Method(typeof(MailWindow),"OpenListView").Invoke(m.GetComponent<MailWindow>(),null);m.GetComponent<MailWindow>().UpdateMail();}
 void OpenPetitions(){if(AdminWorkplaceAssets.Loading||AdminPaperDesk.Current!=null)return;Show("papers");if(AdminPetitions.Current==null){heading.text="Все обращения на сегодня обработаны.";return;}heading.text="Подготавливаем стол…";FloorAdmin.Instance.StartCoroutine(AdminPaperDesk.Open(()=>{if(this!=null){panel.SetActive(false);home.gameObject.SetActive(true);}}));}
 void Back(){if(mode=="profile")Show("staff");else if(mode=="recipients"||mode=="collection")Show("templates");else if(mode=="templates")OpenMail();else{panel.SetActive(false);home.gameObject.SetActive(true);}}
 void Row(string number,string name,string detail,UnityEngine.Events.UnityAction callback){
  var row=Instantiate(rowTemplate,content);row.name="AdminRow";row.transform.Find("Icon").gameObject.SetActive(false);
  SetCell(row,"Date",number,.02f,.18f,32);SetCell(row,"From",name,.20f,.70f,24);SetCell(row,"Subject",detail,.72f,.98f,18);
  var le=row.GetComponent<LayoutElement>()??row.gameObject.AddComponent<LayoutElement>();le.minHeight=66;le.preferredHeight=66;
  row.onClick=new Button.ButtonClickedEvent();row.onClick.AddListener(callback);row.gameObject.SetActive(true);
 }
 static void SetCell(Button row,string path,string value,float x1,float x2,float size){var t=row.transform.Find(path);t.gameObject.SetActive(true);var label=t.GetComponent<LocalizedText>();label.text=value;label.TextMesh.fontSize=size;label.TextMesh.enableAutoSizing=true;label.TextMesh.fontSizeMin=14;label.TextMesh.fontSizeMax=size;label.TextMesh.raycastTarget=false;label.TextMesh.alignment=TMPro.TextAlignmentOptions.MidlineLeft;var r=t.GetComponent<RectTransform>();r.anchorMin=new Vector2(x1,0);r.anchorMax=new Vector2(x2,1);r.offsetMin=r.offsetMax=Vector2.zero;}
 void Render(){
  foreach(Transform t in content)if(t.name=="AdminRow"){t.gameObject.SetActive(false);Destroy(t.gameObject);}action.gameObject.SetActive(false);heading.text="";
  if(mode=="staff"||mode=="recipients"){
   heading.text=mode=="staff"?"Сотрудники":"Выберите сотрудника";
   foreach(var e in AdminManagement.Data.staff){int id=e.number;Row(id.ToString("000"),AdminManagement.FullName(e),e.role,()=>{selected=id;if(mode=="staff")Show("profile");else{heading.text="Вызов: №"+id+" · "+AdminManagement.FullName(e);action.gameObject.SetActive(true);ConfigureButton(action,"Отправить",Send);}});}
  }else if(mode=="profile"){
   var e=AdminManagement.Employee(selected);heading.text="№"+e.number.ToString("000")+"  "+AdminManagement.FullName(e)+"\n\n"+e.role+"\nЛояльность: "+e.loyalty+"\nОплата за обращение: "+e.salary+"\nПроведено бесед: "+e.interviews+"\nУчастие в сборах: "+e.collections;
   action.gameObject.SetActive(true);ConfigureButton(action,e.pending?"Запрос обрабатывается":"Составить отчёт",e.pending?(UnityEngine.Events.UnityAction)null:()=>{var message=AdminManagement.Report(selected);if(!e.pending)heading.text+="\n"+message;else Render();});
  }else if(mode=="templates"){
   heading.text="Написать письмо";Row("1","Сбор средств","Ракович",()=>{template="collection";Show("collection");});Row("2","Вызов сотрудника","Охрана",()=>{template="summon";Show("recipients");});Row("3","Приостановить приём","Ракович",()=>{template="pause";Show("reception");});Row("4","Возобновить приём","Ракович",()=>{template="resume";Show("reception");});
  }else if(mode=="reception"){heading.text="Кому: Магда Ракович\n\n"+(template=="pause"?"Никого не впускать. Вызванным сотрудникам ожидать у кабинета.":"Возобновить приём. Впускать сотрудников по одному в порядке очереди.");action.gameObject.SetActive(true);ConfigureButton(action,"Отправить",()=>{heading.text=AdminReception.SetOpen(template=="resume");ConfigureButton(action,"Отправлено",null);});}else if(mode=="collection"){
   heading.text="Кому: Магда Ракович\nОт: Эван Редгрейв\n\nОрганизовать сбор по 10 с каждого сотрудника и передать средства мне.\n\nНеофициальный сбор.";action.gameObject.SetActive(true);ConfigureButton(action,"Отправить",Send);
  }
  Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);float position;listScroll.StopMovement();listScroll.verticalNormalizedPosition=positions.TryGetValue(mode,out position)?position:1;
 }
 void Send(){string status=template=="collection"?AdminManagement.Collect():AdminManagement.Summon(selected);bool sent=template=="collection"?AdminManagement.Data.jobs.Any(j=>j.kind=="collection"):(AdminManagement.Data.summoned==selected||(AdminManagement.Data.visitorQueue!=null&&AdminManagement.Data.visitorQueue.Contains(selected)));if(sent){heading.text="Письмо отправлено";ConfigureButton(action,"Отправлено",null);}else heading.text=status;}
 void Update(){if(!RefreshNeeded)return;RefreshNeeded=false;if(panel!=null&&panel.activeSelf){positions[mode]=listScroll.verticalNormalizedPosition;Render();}foreach(var w in screens.GetComponentsInChildren<MailWindow>(true))if(w.gameObject.activeInHierarchy)w.UpdateMail();}
}
static class AdminMail {
 static Dictionary<int,EmailMetadata> cache=new Dictionary<int,EmailMetadata>();static AdminData owner;
 internal static void Fill(MailClient client){AdminManagement.Init();if(owner!=AdminManagement.Data){foreach(var old in cache.Values)UnityEngine.Object.Destroy(old.Ref);cache.Clear();owner=AdminManagement.Data;}client.IncomingMail.Clear();client.OutgoingMail.Clear();foreach(var letter in AdminManagement.Data.mail){EmailMetadata metadata;if(!cache.TryGetValue(letter.id,out metadata)){
  var email=ScriptableObject.CreateInstance<Email>();email.name="FloorAdminMail_"+letter.id;
  AccessTools.Field(typeof(Email),"SenderType").SetValue(email,(Email.InterlocutorTypes)0);AccessTools.Field(typeof(Email),"RecipientType").SetValue(email,(Email.InterlocutorTypes)0);AccessTools.Field(typeof(Email),"SenderText").SetValue(email,letter.sender);AccessTools.Field(typeof(Email),"RecipientText").SetValue(email,letter.recipient);AccessTools.Field(typeof(Email),"Subject").SetValue(email,letter.subject);AccessTools.Field(typeof(Email),"Text").SetValue(email,letter.body);
  metadata=new EmailMetadata(email);metadata.Date=(letter.date>0?letter.date:PlayerGameController.GameDate.Ticks)+letter.id;cache.Add(letter.id,metadata);
 }if(letter.read)metadata.ReadBy.Add(new KeyValuePair<Email.InterlocutorTypes,NpcPage>(client.Type,client.NpcPage));if(letter.outgoing)client.OutgoingMail.Add(metadata);else client.IncomingMail.Add(metadata);}
 }
}
[HarmonyPatch(typeof(MailWindow),"UpdateMail")]
class AdminMailView {static void Prefix(MailWindow __instance){if(OfficeComputer.IsDesk(__instance.transform))AdminMail.Fill(__instance.Client);}}
[HarmonyPatch(typeof(MailClient),"SetRead")]
class AdminMailRead {static void Postfix(MailClient __instance,EmailMetadata __0){if(!OfficeComputer.IsDesk(__instance.transform)||__0==null||__0.Ref==null)return;var prefix="FloorAdminMail_";if(!__0.Ref.name.StartsWith(prefix))return;int id;if(int.TryParse(__0.Ref.name.Substring(prefix.Length),out id)){var letter=AdminManagement.Data.mail.FirstOrDefault(m=>m.id==id);if(letter!=null)letter.read=true;}}}








