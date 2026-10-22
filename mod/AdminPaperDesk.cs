using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using Minigames;

[DefaultExecutionOrder(10000)]
class AdminPaperDesk : MonoBehaviour {
 internal static AdminPaperDesk Current;
 ProcessingMinigame native;Transform document;Vector3 documentPosition;Quaternion documentRotation;
 ProcessingMinigame.Stamp[] stamps;ProcessingMinigame.Stamp selected;
 Transform stampParent;Vector3 stampPosition;Quaternion stampRotation;
 LocalizedText body;bool busy,closing;Action finish;GameObject[] pile; Renderer[] playerMeshes;bool[] meshStates;Canvas[] screens;bool[] screenStates;
 string verdict;
 Transform upperArm,forearm,hand;bool movingHand;Vector3 handTarget;float armWeight;
 UnityStandardAssets.ImageEffects.DepthOfField focus;Transform oldFocus;bool oldFocusEnabled;
 Behaviour[] effects;bool[] effectStates;
 internal static IEnumerator Open(Action finished){
  if(Current!=null)yield break;yield return AdminWorkplaceAssets.Load();
  if(AdminWorkplaceAssets.Template==null){finished();yield break;}
  var holder=new GameObject("AdminPaperDesk");holder.SetActive(false);
  var root=Instantiate(AdminWorkplaceAssets.Template,holder.transform);root.name="PetitionsWorkplace";
  Current=holder.AddComponent<AdminPaperDesk>();Current.finish=finished;
  Current.native=root.GetComponentInChildren<ProcessingMinigame>(true);
  try{Current.Prepare(root);}catch(Exception e){FloorAdmin.Log("PAPERS setup failed "+e);Destroy(holder);Current=null;finished();yield break;}
  holder.SetActive(true);root.SetActive(true);Current.StartCoroutine(Current.Begin());
 }
 void Prepare(GameObject root){
  // Retain the game's graphics, hand rig, IK and animation event receiver only.
  foreach(var s in root.GetComponentsInChildren<MonoBehaviour>(true)){
   var ns=s.GetType().Namespace??"";
   s.enabled=s is UnityEngine.EventSystems.UIBehaviour||s is LocalizedText||s is AnimatorCallback||s is MeshHandler||s is AudioComposerPlugin.AudioComponent||ns.StartsWith("RootMotion");
  }
  foreach(var camera in root.GetComponentsInChildren<Camera>(true))camera.enabled=false;
  foreach(var vcam in root.GetComponentsInChildren<CinemachineVirtualCameraBase>(true))vcam.enabled=false;
  foreach(var action in root.GetComponentsInChildren<ActionsList>(true))action.Disabled=true;
  foreach(var button in root.GetComponentsInChildren<Button>(true))button.onClick=new Button.ButtonClickedEvent();
  foreach(var a in root.GetComponentsInChildren<Animator>(true))a.enabled=true;
  native.enabled=false;
  ActivatePath(native.transform,root.transform);ActivatePath(native.HandsAnimator.transform,root.transform);
  foreach(var mask in root.GetComponentsInChildren<Mask>(true))mask.enabled=false;foreach(var mask in root.GetComponentsInChildren<RectMask2D>(true))mask.enabled=false;native.MainCanvas.alpha=1;native.MainCanvas.interactable=true;native.MainCanvas.blocksRaycasts=true;
  foreach(var canvas in native.GetComponentsInChildren<Canvas>(true)){canvas.enabled=true;canvas.worldCamera=Camera.main;}
  native.Instructions.gameObject.SetActive(false);native.InstructionsScroll.gameObject.SetActive(false);
  native.PhaseName.gameObject.SetActive(true);native.PhaseDescription.gameObject.SetActive(true);
  native.PhaseName.text="Обращения первого этажа";
  native.ActionButton.gameObject.SetActive(false);native.ClearButton.gameObject.SetActive(false);
  native.HelpButton.gameObject.SetActive(true);SetButton(native.HelpButton,"Вернуться",Close);
  foreach(var canvas in root.GetComponentsInChildren<Canvas>(true))if(canvas.name=="OverlayCanvas"||canvas.name=="WorldCanvas")canvas.gameObject.SetActive(false);
  stamps=native.Phases.SelectMany(p=>p.Stamps).Where(s=>s.Object.name=="3PhaseStampApprove"||s.Object.name=="3PhaseStampReject"||s.Object.name=="3PhaseStampRevision").ToArray();
  foreach(var s in native.Phases.SelectMany(p=>p.Stamps)){s.Object.SetActive(stamps.Contains(s));s.BlankStamp.gameObject.SetActive(false);}
  foreach(var collider in root.GetComponentsInChildren<Collider>(true))if(!stamps.Any(s=>collider.transform.IsChildOf(s.Object.transform)))collider.enabled=false;foreach(var s in stamps){var stamp=s;ActivatePath(s.Object.transform,root.transform);s.Object.GetComponent<Button>().onClick.AddListener(()=>Choose(stamp));foreach(var collider in s.Object.GetComponentsInChildren<Collider>(true)){var click=collider.gameObject.AddComponent<AdminPaperStampClick>();click.Click=()=>Choose(stamp);}}
  body=Instantiate(native.MinistryText,native.MinistryText.transform.parent);body.name="PetitionBody";body.text="";body.TextMesh.enableAutoSizing=true;body.TextMesh.fontSizeMin=12;body.TextMesh.fontSizeMax=22;body.TextMesh.alignment=TMPro.TextAlignmentOptions.TopLeft;body.TextMesh.raycastTarget=false;
  var rect=body.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.08f,.2f);rect.anchorMax=new Vector2(.92f,.83f);rect.offsetMin=rect.offsetMax=Vector2.zero;
  native.MinistryText.gameObject.SetActive(false);
  foreach(var image in native.BlankBack.GetComponentsInChildren<Image>(true))if(image!=native.BlankBack&&!stamps.Any(s=>s.BlankStamp==image))image.gameObject.SetActive(false);foreach(var label in native.BlankBack.GetComponentsInChildren<LocalizedText>(true))if(label!=body)label.gameObject.SetActive(false);
  document=native.BlankBack.transform.parent;
  var ready=native.WorkplaceAnimator.runtimeAnimatorController.animationClips.First(c=>c.name=="WorkplaceDocument");ready.SampleAnimation(native.WorkplaceAnimator.gameObject,ready.length);native.WorkplaceAnimator.enabled=false;
  documentPosition=document.position;documentRotation=document.rotation;
  var receiver=native.HandsAnimator.GetComponent<AnimatorCallback>();if(receiver==null)receiver=native.HandsAnimator.gameObject.AddComponent<AnimatorCallback>();receiver.Callback+=AnimationEvent;
  var bones=native.HandsAnimator.GetComponentsInChildren<Transform>(true);upperArm=bones.FirstOrDefault(t=>t.name=="BnRUpperArm");forearm=bones.FirstOrDefault(t=>t.name=="BnRLowerArm");hand=bones.FirstOrDefault(t=>t.name=="BnRHand");
  var sourceCamera=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="MiniGameCamera");
  if(sourceCamera==null)throw new Exception("Native desk camera missing");
  var cameraObject=new GameObject("PetitionsCamera");cameraObject.transform.SetParent(root.transform,false);cameraObject.transform.SetPositionAndRotation(sourceCamera.position,sourceCamera.rotation);
  var cameraView=cameraObject.AddComponent<CinemachineVirtualCamera>();cameraView.Priority=11000;var lens=sourceCamera.GetComponent<CinemachineVirtualCamera>();if(lens!=null)cameraView.m_Lens=lens.m_Lens;
  pile=new GameObject[6];for(int i=0;i<pile.Length;i++){var leaf=Instantiate(native.BlankBack.gameObject,document.parent);leaf.name="PendingPetition";var leafRect=leaf.GetComponent<RectTransform>();leafRect.anchorMin=leafRect.anchorMax=new Vector2(.5f,.5f);leafRect.pivot=new Vector2(.5f,.5f);leafRect.sizeDelta=native.BlankBack.rectTransform.rect.size;leaf.GetComponent<Image>().color=Color.white;leaf.GetComponent<Image>().raycastTarget=true;var worldScale=native.BlankBack.transform.lossyScale;var parentScale=document.parent.lossyScale;leaf.transform.localScale=new Vector3(worldScale.x/parentScale.x,worldScale.y/parentScale.y,worldScale.z/parentScale.z);foreach(Transform child in leaf.transform)Destroy(child.gameObject);leaf.transform.position=documentPosition+document.right*.34f+document.forward*.003f*i;leaf.transform.rotation=native.BlankBack.transform.rotation;pile[i]=leaf;var take=leaf.AddComponent<Button>();take.targetGraphic=leaf.GetComponent<Image>();take.onClick.AddListener(TakePaper);}
  // Move the native close-up as a unit; nothing is added over the computer screen.
  var chair=Resources.FindObjectsOfTypeAll<Transform>().First(t=>t.gameObject.scene.name=="BossRoom"&&FloorAdmin.PathOf(t)=="Active/FurnitureActions/WorkplaceBoss/Anim");
  var rotation=Quaternion.LookRotation(Vector3.down,chair.forward)*Quaternion.Inverse(native.BlankBack.transform.rotation);root.transform.rotation=rotation*root.transform.rotation;
  root.transform.position+=chair.position+chair.forward*.65f+Vector3.up*.86f-document.position;
  documentPosition=document.position;documentRotation=document.rotation;
  var paperCenter=native.BlankBack.transform.position;
  cameraObject.transform.position=paperCenter+Vector3.up*1.1f-chair.forward*.25f+chair.right*.1f;cameraObject.transform.rotation=Quaternion.LookRotation(paperCenter+chair.right*.1f-cameraObject.transform.position,chair.forward);cameraView.m_Lens.FieldOfView=55;cameraView.m_Lens.NearClipPlane=.01f;cameraView.m_Lens.FarClipPlane=100;
  foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))renderer.enabled=renderer.transform.IsChildOf(native.HandsAnimator.transform)||stamps.Any(s=>renderer.transform.IsChildOf(s.Object.transform));
  foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))graphic.enabled=graphic.transform.IsChildOf(document)||pile.Any(p=>graphic.transform==p.transform||graphic.transform.IsChildOf(p.transform))||graphic.transform.IsChildOf(native.HelpButton.transform)||stamps.Any(s=>graphic.transform.IsChildOf(s.Object.transform));
  var oldBack=native.HelpButton;var backObject=new GameObject("PetitionsBack",typeof(RectTransform),typeof(Image),typeof(Button));backObject.transform.SetParent(document.parent,false);var backRect=backObject.GetComponent<RectTransform>();backRect.sizeDelta=new Vector2(240,70);backRect.localScale=pile[0].transform.localScale;backRect.rotation=native.BlankBack.transform.rotation;backRect.position=paperCenter-native.BlankBack.transform.right*.38f-native.BlankBack.transform.up*.34f;var backImage=backObject.GetComponent<Image>();backImage.color=new Color(.14f,.10f,.06f,.95f);var backLabel=Instantiate(oldBack.GetComponentInChildren<LocalizedText>(true),backRect);var labelRect=backLabel.GetComponent<RectTransform>();labelRect.localRotation=Quaternion.identity;labelRect.localScale=Vector3.one;labelRect.anchorMin=Vector2.zero;labelRect.anchorMax=Vector2.one;labelRect.offsetMin=labelRect.offsetMax=Vector2.zero;backLabel.TextMesh.alignment=TMPro.TextAlignmentOptions.Center;backLabel.TextMesh.raycastTarget=false;backLabel.gameObject.SetActive(true);native.HelpButton=backObject.GetComponent<Button>();native.HelpButton.targetGraphic=backImage;SetButton(native.HelpButton,"Вернуться",Close);oldBack.gameObject.SetActive(false);for(int i=0;i<pile.Length;i++)pile[i].transform.position=paperCenter+native.BlankBack.transform.right*(.61f-i*.007f)+native.BlankBack.transform.up*(.20f-i*.005f)+Vector3.up*.003f*i;
  for(int i=0;i<stamps.Length;i++)stamps[i].Object.transform.position=paperCenter+native.BlankBack.transform.right*(.38f+i*.17f)-native.BlankBack.transform.up*.32f;
  var tabletop=GameObject.CreatePrimitive(PrimitiveType.Cube);tabletop.name="PetitionsTabletop";tabletop.transform.SetParent(root.transform,true);tabletop.transform.position=paperCenter+Vector3.down*.045f;tabletop.transform.rotation=Quaternion.LookRotation(chair.forward,Vector3.up);tabletop.transform.localScale=new Vector3(2.6f,.04f,1.8f);Destroy(tabletop.GetComponent<Collider>());var tabletopMaterial=new Material(Shader.Find("Standard"));tabletopMaterial.color=new Color(.24f,.135f,.075f);tabletopMaterial.SetFloat("_Glossiness",.15f);tabletop.GetComponent<Renderer>().material=tabletopMaterial;AdminTableSurface.Apply(tabletop);  var lamp=new GameObject("PaperDeskLight");lamp.transform.SetParent(root.transform,true);lamp.transform.position=documentPosition+Vector3.up;var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=5;light.intensity=2f;light.color=new Color(1,.93f,.8f);
  if(Camera.main!=null){focus=Camera.main.GetComponent<UnityStandardAssets.ImageEffects.DepthOfField>();if(focus!=null){oldFocus=focus.focalTransform;oldFocusEnabled=focus.enabled;focus.focalTransform=document;focus.enabled=false;}effects=Camera.main.GetComponents<Behaviour>().Where(b=>HarmonyLib.AccessTools.Method(b.GetType(),"OnRenderImage")!=null).ToArray();effectStates=effects.Select(b=>b.enabled).ToArray();foreach(var effect in effects)effect.enabled=false;}
  playerMeshes=PlayerActionsSystem.Instance.GetComponentsInChildren<Renderer>(true);meshStates=playerMeshes.Select(r=>r.enabled).ToArray();foreach(var mesh in playerMeshes)mesh.enabled=false; screens=Resources.FindObjectsOfTypeAll<Canvas>().Where(c=>OfficeComputer.IsDesk(c.transform)).ToArray();screenStates=screens.Select(c=>c.enabled).ToArray();foreach(var screen in screens)screen.enabled=false;
 }
 static void ActivatePath(Transform t,Transform stop){while(t!=null){t.gameObject.SetActive(true);if(t==stop)break;t=t.parent;}}
 static void SetButton(Button button,string label,UnityEngine.Events.UnityAction click){foreach(var key in button.GetComponentsInChildren<ButtonHotkey>(true))key.enabled=false;var text=button.GetComponentInChildren<LocalizedText>(true);if(text!=null)text.text=label;button.onClick=new Button.ButtonClickedEvent();button.onClick.AddListener(click);button.interactable=true;}
 IEnumerator Begin(){yield return null;native.HandsAnimator.Update(0);Next();FloorAdmin.Log("PAPERS opened native desk");}
 void Next(){
  var paper=AdminPetitions.Current;int count=AdminManagement.Data.petitions.Count(p=>string.IsNullOrEmpty(p.decision));
  for(int i=0;i<pile.Length;i++)pile[i].SetActive(i<count);
  foreach(var s in stamps)s.BlankStamp.gameObject.SetActive(false);
  if(paper==null){busy=false;Close();return;}
  body.text=paper.text;native.PhaseDescription.text="Осталось: "+count+"\nОдобрить · Отклонить · На рассмотрение";
  document.gameObject.SetActive(false);document.position=documentPosition;document.rotation=documentRotation;
  foreach(var s in stamps)s.Object.GetComponent<Button>().interactable=false;busy=false;
 }
 void TakePaper(){if(busy||document.gameObject.activeSelf||AdminPetitions.Current==null)return;StartCoroutine(TakeFromPile());}
 IEnumerator TakeFromPile(){busy=true;native.HelpButton.interactable=false;var top=pile.LastOrDefault(p=>p.activeSelf);if(top==null){busy=false;yield break;}var origin=top.transform.position;var start=hand==null?origin:hand.position;movingHand=hand!=null;
 for(float t=0;t<1;t+=Time.deltaTime/.45f){armWeight=Mathf.SmoothStep(0,1,t);handTarget=Vector3.Lerp(start,origin-document.up*.12f,armWeight);yield return null;}armWeight=1;top.SetActive(false);document.position=origin;document.gameObject.SetActive(true);
 for(float t=0;t<1;t+=Time.deltaTime/.65f){document.position=Vector3.Lerp(origin,documentPosition,Mathf.SmoothStep(0,1,t))+Vector3.up*Mathf.Sin(t*Mathf.PI)*.06f;handTarget=document.position-document.up*.12f;yield return null;}document.position=documentPosition;
 for(float t=0;t<1;t+=Time.deltaTime/.3f){armWeight=1-Mathf.SmoothStep(0,1,t);yield return null;}movingHand=false;busy=false;native.HelpButton.interactable=true;foreach(var stamp in stamps)stamp.Object.GetComponent<Button>().interactable=true;
 }
 void Choose(ProcessingMinigame.Stamp stamp){
  if(busy||!document.gameObject.activeSelf||AdminPetitions.Current==null)return;verdict=stamp.Object.name.Contains("Approve")?"Одобрено":stamp.Object.name.Contains("Reject")?"Отклонено":"На рассмотрение";
  if(verdict=="Одобрено"&&PlayerGameController.Money<AdminPetitions.Current.cost){body.text=AdminPetitions.Current.text+"\n\nНедостаточно средств для одобрения.";return;}
  busy=true;selected=stamp;stampParent=stamp.Object.transform.parent;stampPosition=stamp.Object.transform.localPosition;stampRotation=stamp.Object.transform.localRotation;
  foreach(var s in stamps)s.Object.GetComponent<Button>().interactable=false;native.HelpButton.interactable=false;
  native.HandsAnimator.SetTrigger("stamp");FloorAdmin.Log("PAPERS stamp started "+verdict);
 }
 void AnimationEvent(MonoBehaviour sender,string key){
  if(selected==null)return;
  if(key=="stampBegin"){
   native.RightHandTarget.position=selected.Object.transform.position;var animator=selected.Object.GetComponent<Animator>();if(animator!=null)animator.SetTrigger("Use");
   native.HandsAnimator.GetComponent<MeshHandler>().AddBone(selected.Object.transform,native.RightHandAnchor,false,false,false,true,"RightHand");
  }else if(key=="toStamp")native.RightHandTarget.position=selected.BlankStamp.transform.position;
  else if(key=="Stamp"){
   var error=AdminPetitions.Decide(verdict);if(error!=null){native.PhaseDescription.text=error;return;}
   selected.BlankStamp.gameObject.SetActive(true);var color=selected.BlankStamp.color;color.a=1;selected.BlankStamp.color=color;native.AudioComponent.PlaySound("stamp");FloorAdmin.Log("PAPERS decision committed "+verdict);
  }else if(key=="toStash")native.RightHandTarget.position=stampParent.TransformPoint(stampPosition);
  else if(key=="stampEnd"){selected.Object.transform.SetParent(stampParent,false);selected.Object.transform.localPosition=stampPosition;selected.Object.transform.localRotation=stampRotation;selected=null;StartCoroutine(RemovePaper());}
 }
 IEnumerator RemovePaper(){
  yield return new WaitForSeconds(.35f);
  var from=document.position;var grip=from-document.up*.12f;var to=from-native.BlankBack.transform.up*1.25f-native.BlankBack.transform.right*.18f;
  if(hand!=null){movingHand=true;var start=hand.position;for(float t=0;t<1;t+=Time.deltaTime/.45f){armWeight=Mathf.SmoothStep(0,1,t);handTarget=Vector3.Lerp(start,grip,armWeight);yield return null;}armWeight=1;}
  for(float t=0;t<1;t+=Time.deltaTime/.65f){var amount=Mathf.SmoothStep(0,1,t);document.position=Vector3.Lerp(from,to,amount)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.06f;handTarget=document.position-document.up*.12f;yield return null;}
  document.gameObject.SetActive(false);
  for(float t=0;t<1;t+=Time.deltaTime/.3f){armWeight=1-Mathf.SmoothStep(0,1,t);yield return null;}movingHand=false;
  native.HelpButton.interactable=true;Next();
 }
 void LateUpdate(){if(!movingHand||upperArm==null||forearm==null||hand==null)return;
  var a=upperArm.position;var b=forearm.position;var c=hand.position;float l1=Vector3.Distance(a,b),l2=Vector3.Distance(b,c);if(l1<.001f||l2<.001f)return;
  var target=Vector3.Lerp(c,handTarget,armWeight);var ray=target-a;float distance=Mathf.Clamp(ray.magnitude,Mathf.Abs(l1-l2)+.001f,l1+l2-.001f);var axis=ray.normalized;var bend=Vector3.ProjectOnPlane(b-a,axis).normalized;if(bend.sqrMagnitude<.01f)bend=Vector3.Cross(axis,document.right).normalized;
  float x=(l1*l1-l2*l2+distance*distance)/(2*distance);var elbow=a+axis*x+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-x*x));upperArm.rotation=Quaternion.FromToRotation(b-a,elbow-a)*upperArm.rotation;forearm.rotation=Quaternion.FromToRotation(hand.position-forearm.position,target-forearm.position)*forearm.rotation;
 }
 void Close(){if(busy||closing)return;closing=true;finish();Destroy(gameObject);Current=null;}
 internal void ForceClose(){busy=false;Close();}
 void OnDestroy(){if(playerMeshes!=null)for(int i=0;i<playerMeshes.Length;i++)if(playerMeshes[i]!=null)playerMeshes[i].enabled=meshStates[i];if(screens!=null)for(int i=0;i<screens.Length;i++)if(screens[i]!=null)screens[i].enabled=screenStates[i];if(focus!=null){focus.focalTransform=oldFocus;focus.enabled=oldFocusEnabled;}if(effects!=null)for(int i=0;i<effects.Length;i++)if(effects[i]!=null)effects[i].enabled=effectStates[i];if(Current==this)Current=null;}
}



class AdminPaperStampClick : MonoBehaviour,UnityEngine.EventSystems.IPointerClickHandler {internal Action Click;public void OnPointerClick(UnityEngine.EventSystems.PointerEventData data){if(data.button==UnityEngine.EventSystems.PointerEventData.InputButton.Left&&Click!=null)Click();}}
