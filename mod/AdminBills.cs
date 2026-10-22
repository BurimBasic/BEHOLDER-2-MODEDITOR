using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
[Serializable] public class AdminFine {public int id,amount;public long date;public bool paid;}
static class AdminBills {
 const string Prefix="FloorAdminFine_";
 internal static bool IsOurs(BillMetadata b){return b.Ref!=null&&b.Ref.name.StartsWith(Prefix);}
 internal static void Add(){
  Ensure();var fine=new AdminFine{id=AdminManagement.Data.fines,amount=5000,date=PlayerGameController.GameDate.ToUtcBinary()};AdminManagement.Data.debts.Add(fine);Install(fine);PlayerGameController.Instance.HasNewBills=true;
 }
 static void Ensure(){if(AdminManagement.Data.debts==null)AdminManagement.Data.debts=new List<AdminFine>();}
 static void Install(AdminFine fine){var bill=ScriptableObject.CreateInstance<Bill>();bill.name=Prefix+fine.id;AccessTools.Field(typeof(Bill),"Description").SetValue(bill,"Штраф за незаконные сборы с сотрудников");AccessTools.Field(typeof(Bill),"Money").SetValue(bill,fine.amount);AccessTools.Field(typeof(Bill),"Days").SetValue(bill,3);PlayerGameController.Instance.BillList.Add(new BillMetadata(bill){Date=fine.date});}
 internal static void Sync(){Ensure();var list=PlayerGameController.Instance.BillList;foreach(var fine in AdminManagement.Data.debts.Where(d=>!d.paid))if(!list.Any(b=>b.Ref!=null&&b.Ref.name==Prefix+fine.id))Install(fine);}
 internal static void Paid(){Ensure();foreach(var fine in AdminManagement.Data.debts.Where(d=>!d.paid))if(!PlayerGameController.Instance.BillList.Any(b=>b.Ref!=null&&b.Ref.name==Prefix+fine.id))fine.paid=true;}
}
[HarmonyPatch(typeof(BillWindow),"OnEnable")]
class AdminBillsShow {static void Prefix(){AdminBills.Sync();}}
[HarmonyPatch(typeof(BillWindow),"OnPay")]
class AdminBillsPay {static void Postfix(){AdminBills.Paid();}}
[HarmonyPatch(typeof(PlayerGameController),"OnSave")]
class AdminBillsSave {
 static void Prefix(PlayerGameController __instance,out BillMetadata[] __state){__state=__instance.BillList.Where(AdminBills.IsOurs).ToArray();__instance.BillList.RemoveAll(AdminBills.IsOurs);}
 static void Postfix(PlayerGameController __instance,BillMetadata[] __state){__instance.BillList.AddRange(__state);}
}
