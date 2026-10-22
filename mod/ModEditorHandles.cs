using System;
using UnityEngine;

partial class ModEditorWindow {
 int transformTool,dragAxis=-1;bool localAxes=true,snap;
 Vector2 dragStart,dragDirection;float dragUnits;
 Vector3 dragPosition,dragScale,dragWorldAxis;Quaternion dragRotation;
 EditorChange dragOriginal;Transform dragTarget;
 static readonly Color[] axisColors={new Color(1,.3f,.3f),new Color(.3f,1,.4f),new Color(.35f,.65f,1)};
 void NavigateScene(Rect view){sceneRect=view;var e=Event.current;if(!view.Contains(e.mousePosition)||dragAxis>=0||e.type!=EventType.MouseDrag)return;if(e.button==1){GUIUtility.keyboardControl=0;viewCamera.transform.Rotate(Vector3.up,e.delta.x*.3f,Space.World);viewCamera.transform.Rotate(Vector3.right,e.delta.y*.3f,Space.Self);e.Use();}else if(e.button==2){viewCamera.transform.position+=(-viewCamera.transform.right*e.delta.x+viewCamera.transform.up*e.delta.y)*.02f;e.Use();}}
 void DrawTransformToolbar(){
  bool enabled=GUI.enabled;GUI.enabled=enabled&&dragAxis<0;
  transformTool=GUILayout.Toolbar(transformTool,new[]{"W: сдвиг","E: поворот","R: размер"});
  localAxes=GUILayout.Toggle(localAxes,"Local",GUI.skin.button,GUILayout.Width(48));
  snap=GUILayout.Toggle(snap,"Шаг",GUI.skin.button,GUILayout.Width(42));
  GUI.enabled=enabled;
 }
 Rect ImageRect(Rect view){float width=Mathf.Min(view.width,view.height*preview.width/preview.height);return new Rect(view.center.x-width/2,view.center.y-width*preview.height/preview.width/2,width,width*preview.height/preview.width);}
 Vector2 ProjectHandle(Vector3 world,Rect rect){var p=viewCamera.WorldToViewportPoint(world);return new Vector2(rect.x+p.x*rect.width,rect.y+(1-p.y)*rect.height);}
 static float SegmentDistance(Vector2 point,Vector2 a,Vector2 b){var delta=b-a;return Vector2.Distance(point,a+delta*Mathf.Clamp01(Vector2.Dot(point-a,delta)/Mathf.Max(.001f,delta.sqrMagnitude)));}
 static void HandleLine(Vector2 a,Vector2 b,Color color,float thickness=2){var saved=GUI.matrix;var tint=GUI.color;GUI.color=color;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y-thickness/2,(b-a).magnitude,thickness),Texture2D.whiteTexture);GUI.matrix=saved;GUI.color=tint;}
 void DrawTransformHandles(Rect view){
  var e=Event.current;
  if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape&&dragAxis>=0){CancelTransformDrag();e.Use();return;}
  if(e.type==EventType.KeyDown&&!Input.GetMouseButton(1)&&GUIUtility.keyboardControl==0&&dragAxis<0){if(e.keyCode==KeyCode.W)transformTool=0;else if(e.keyCode==KeyCode.E)transformTool=1;else if(e.keyCode==KeyCode.R)transformTool=2;else if(e.keyCode==KeyCode.F)Frame();}
  if(dragAxis>=0){if(e.type==EventType.MouseDrag){UpdateTransformDrag(e.mousePosition);e.Use();}else if(e.type==EventType.MouseUp&&e.button==0){CommitTransformDrag();e.Use();}}
  if(selected==null||viewCamera.WorldToViewportPoint(selected.position).z<=viewCamera.nearClipPlane)return;
  var rect=ImageRect(view);var pivot=ProjectHandle(selected.position,rect);if(!rect.Contains(pivot))return;
  float size=viewCamera.orthographic?viewCamera.orthographicSize*.3f:Vector3.Distance(viewCamera.transform.position,selected.position)*Mathf.Tan(viewCamera.fieldOfView*Mathf.Deg2Rad*.5f)*.22f;
  int hit=-1;float closest=9;Vector2 hitDirection=Vector2.right;float hitUnits=1;Vector3 hitWorld=Vector3.right;
  for(int axis=0;axis<3;axis++){
   Vector3 unit=axis==0?Vector3.right:axis==1?Vector3.up:Vector3.forward;
   Vector3 world=(localAxes||transformTool==2)?selected.rotation*unit:unit;
   var end=ProjectHandle(selected.position+world*size,rect);var color=axisColors[axis];
   if(transformTool==1){var u=Vector3.Cross(world,Mathf.Abs(world.y)<.9f?Vector3.up:Vector3.right).normalized;var v=Vector3.Cross(world,u);Vector2 previous=ProjectHandle(selected.position+u*size,rect);
    for(int segment=1;segment<=48;segment++){float angle=segment*Mathf.PI*2/48;var point=ProjectHandle(selected.position+(u*Mathf.Cos(angle)+v*Mathf.Sin(angle))*size,rect);if(rect.Contains(previous)&&rect.Contains(point))HandleLine(previous,point,color,axis==dragAxis?4:2);float distance=SegmentDistance(e.mousePosition,previous,point);if(distance<closest){closest=distance;hit=axis;hitDirection=(point-previous).normalized;hitWorld=world;hitUnits=.75f;}previous=point;}
   }else if(rect.Contains(end)){
    HandleLine(pivot,end,color,axis==dragAxis?4:2);var direction=(end-pivot).normalized;
    if(transformTool==0){var side=new Vector2(-direction.y,direction.x);HandleLine(end,end-direction*10+side*5,color);HandleLine(end,end-direction*10-side*5,color);}else{var old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(end.x-4,end.y-4,8,8),Texture2D.whiteTexture);GUI.color=old;}
    GUI.Label(new Rect(end.x+5,end.y-8,25,22),axis==0?"X":axis==1?"Y":"Z");
    float distance=SegmentDistance(e.mousePosition,pivot+direction*14,end);if((end-pivot).magnitude>15&&distance<closest){closest=distance;hit=axis;hitDirection=direction;hitWorld=world;hitUnits=size/Mathf.Max(15,(end-pivot).magnitude);}
   }
  }
  if(e.type==EventType.MouseDown&&e.button==0&&dragAxis<0&&hit>=0&&rect.Contains(e.mousePosition)){BeginTransformDrag(hit,e.mousePosition,hitDirection,hitWorld,hitUnits);GUIUtility.keyboardControl=0;e.Use();}
  if(dragAxis>=0)GUI.Label(new Rect(view.x+8,view.y+8,view.width-16,24),"Тяните ось; Esc — отменить. Шаг: 0.1 / 15° / 0.1.");
 }
 void BeginTransformDrag(int axis,Vector2 mouse,Vector2 direction,Vector3 world,float units){dragAxis=axis;dragTarget=selected;dragStart=mouse;dragDirection=direction;dragUnits=units;dragWorldAxis=world;dragOriginal=ModEditorStore.ReadTransform(selected);dragPosition=selected.position;dragRotation=selected.rotation;dragScale=selected.localScale;}
 void UpdateTransformDrag(Vector2 mouse){if(dragTarget==null){CancelTransformDrag();return;}float pixels=Vector2.Dot(mouse-dragStart,dragDirection);float value=transformTool==2?pixels*.01f:pixels*dragUnits;if(snap){float step=transformTool==1?15:.1f;value=Mathf.Round(value/step)*step;}if(transformTool==0)dragTarget.position=dragPosition+dragWorldAxis*value;else if(transformTool==1)dragTarget.rotation=Quaternion.AngleAxis(value,dragWorldAxis)*dragRotation;else{var scaleValue=dragScale;scaleValue[dragAxis]=Mathf.Max(.01f,dragScale[dragAxis]*(1+value));dragTarget.localScale=scaleValue;}LoadFields();}
 void RestoreDrag(){if(dragTarget==null||dragOriginal==null)return;dragTarget.localPosition=new Vector3(dragOriginal.x,dragOriginal.y,dragOriginal.z);dragTarget.localEulerAngles=new Vector3(dragOriginal.rx,dragOriginal.ry,dragOriginal.rz);dragTarget.localScale=new Vector3(dragOriginal.sx,dragOriginal.sy,dragOriginal.sz);}
 void CancelTransformDrag(){RestoreDrag();dragAxis=-1;dragTarget=null;dragOriginal=null;}
 void CommitTransformDrag(){if(dragTarget==null){CancelTransformDrag();return;}var change=ModEditorStore.ReadTransform(dragTarget);CancelTransformDrag();Try(()=>ModEditorStore.Change(change));LoadFields();}
 internal void VerifyTransforms(Transform target){
  string previous=ModEditorStore.Encode(ModEditorStore.Project);bool dirty=ModEditorStore.Dirty;
  try{ModEditorStore.New("__transform_verification");Show();Select(target);Frame();
   for(int tool=0;tool<3;tool++){transformTool=tool;var beforePosition=target.position;var beforeRotation=target.rotation;var beforeScale=target.localScale;
    BeginTransformDrag(0,Vector2.zero,Vector2.right,Vector3.right,.01f);UpdateTransformDrag(new Vector2(50,0));CommitTransformDrag();
    if(tool==0&&Vector3.Distance(target.position,beforePosition)<.4f||tool==1&&Quaternion.Angle(target.rotation,beforeRotation)<.4f||tool==2&&Vector3.Distance(target.localScale,beforeScale)<.01f)throw new Exception("Transform tool did not change target: "+tool);
    ModEditorStore.Undo();if(Vector3.Distance(target.position,beforePosition)>.001f||Quaternion.Angle(target.rotation,beforeRotation)>.1f||Vector3.Distance(target.localScale,beforeScale)>.001f)throw new Exception("Transform undo failed");
    ModEditorStore.Redo();ModEditorStore.Undo();
   }
   FloorAdmin.Log("PASS editor move/rotate/scale with undo and redo");
  }finally{CancelTransformDrag();ModEditorStore.Restore();ModEditorStore.Project=ModEditorStore.Decode(previous);ModEditorStore.Dirty=dirty;ModEditorStore.Apply();transformTool=0;LoadFields();}
 }
}

