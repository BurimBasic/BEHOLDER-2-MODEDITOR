using System.Linq;
using UnityEngine;

static class AdminTableSurface {
 internal static bool Apply(GameObject target){
  var source=Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m=>m.name=="BossTable"&&m.mainTexture!=null);
  if(source==null)return false;
  // The original atlas contains furniture sides too; use its uninterrupted marble surface.
  var material=target.GetComponent<Renderer>().material;
  material.mainTexture=source.mainTexture;
  material.mainTextureScale=new Vector2(.20f,.42f);
  material.mainTextureOffset=new Vector2(.02f,.53f);
  material.color=Color.white;
  material.SetFloat("_Glossiness",.3f);
  FloorAdmin.Log("PAPERS original BossTable texture applied");return true;
 }
}
