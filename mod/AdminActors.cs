using System;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using AudioComposerPlugin;
static class AdminActors {
 internal static bool Female(AdminEmployee e){return e.number>100&&e.number<=105;}
 static bool IsFemale(Animator a){var g=a.GetComponentInParent<GenerateNpc.GenerateClothes>()??a.GetComponentInChildren<GenerateNpc.GenerateClothes>(true);if(g!=null)return (GenerateNpc.Genders)AccessTools.Field(typeof(GenerateNpc.GenerateClothes),"m_Gender").GetValue(g)==GenerateNpc.Genders.Female;return a.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r=>r.gameObject.activeSelf&&(r.name.IndexOf("female",StringComparison.OrdinalIgnoreCase)>=0||r.name.IndexOf("woman",StringComparison.OrdinalIgnoreCase)>=0));}
 internal static Animator Model(AdminEmployee e,Animator[] models){var pool=models.Where(a=>IsFemale(a)==Female(e)).ToArray();if(pool.Length==0)return null;var source=pool.FirstOrDefault(a=>FloorAdmin.PathOf(a.transform)==e.model)??pool[(e.number*97+31)%pool.Length];e.model=FloorAdmin.PathOf(source.transform);return source;}
 internal static void Voice(GameObject holder,AdminEmployee e){
  string pattern=Female(e)?"vo_npc_handling_woman_":"vo_npc_handling_man_";
  var source=Resources.FindObjectsOfTypeAll<AudioComponent>().FirstOrDefault(a=>a.PathsToClips!=null&&a.PathsToClips.Any(p=>p.path!=null&&p.path.Contains(pattern)&&p.clip!=null));if(source==null){FloorAdmin.Log("VISITOR voice not loaded "+pattern);return;}
  var audio=holder.AddComponent<AudioComponent>();var clips=source.PathsToClips.Where(p=>p.path!=null&&p.path.Contains(pattern)&&p.clip!=null).ToArray();audio.PathsToClips=clips;
  var data=JsonUtility.FromJson<SoundData>(JsonUtility.ToJson(source.SoundData));data.KeysClips.Clear();foreach(var clip in clips)data.KeysClips.Add(new ClipPath{Key="Dialogue_Talk",Path=clip.path});AccessTools.Field(typeof(AudioComponent),"SoundData").SetValue(audio,data);JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source.SourceParams),audio.SourceParams);audio.Type=source.Type;audio.AudioType=source.AudioType;
 }
 internal static string SmallTalk(AdminEmployee e){string[] lines={"В архиве опять перепутали описи. До конца смены приведём бумаги в порядок.","Сегодня обращений больше обычного. Работаем по очереди, господин Редгрейв.","Коллеги спорят, кто оставил свет в канцелярии. Я предпочитаю заниматься делом.","Получили новые бланки. Половина без номера, но мы уже сообщили на склад."};return lines[(e.number+e.chats)%lines.Length];}
}
