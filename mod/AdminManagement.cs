using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;

[Serializable] public class AdminEmployee {
 public int number, salary=50, loyalty, collections, interviews, reportLoyalty=-1;
 public int chats;
 public string surname, role, model;
 public bool reported, pending;
}
[Serializable] public class AdminLetter {
 public int id, employee; public long date; public string sender, recipient, subject, body; public bool outgoing, read;
}
[Serializable] public class AdminJob {public string kind;public int employee;public double due;}
[Serializable] public class AdminData {
 public int version=2, nextMail=1, collections, fines, summoned;
 public string secretary="";public double clock,summonDue;public bool dead,announced;
 public List<AdminEmployee> staff=new List<AdminEmployee>();
 public List<AdminLetter> mail=new List<AdminLetter>();
 public List<AdminJob> jobs=new List<AdminJob>();
 public List<double> traffic=new List<double>();
 public long petitionDay;
 public List<AdminPetition> petitions=new List<AdminPetition>();
 public List<AdminFine> debts=new List<AdminFine>();
 public List<double> collectionTraffic=new List<double>();
 public List<AdminJob> visitorCalls=new List<AdminJob>(); public List<int> visitorQueue=new List<int>();public bool receptionOpen=true;public int receptionVersion=1;
}
static class AdminManagement {
 internal static AdminData Data=new AdminData();
 internal static float last=-1;
 static bool deathApplied;
 static readonly string[] Surnames={"Восс","Кесслер","Штайнер","Крюгер","Брандт","Вебер","Гримм","Вольф","Краус","Фальк","Хартман","Адлер","Беккер","Рихтер","Вальтер","Фогель","Шульц","Келлер","Бауэр","Дорн","Рот","Фишер","Шварц","Энгель","Кляйн","Винтер","Бергер"};
 static readonly string[] Given={"Эдгар","Вальтер","Герхард","Оскар","Альберт","Курт","Феликс","Эрих","Артур","Отто","Густав","Эмиль"};
 static string Surname(int id){return Surnames[(id-1)%Surnames.Length];}
 internal static void Init(){if(Data.staff.Count==0)for(int i=0;i<108;i++)Data.staff.Add(new AdminEmployee{number=i+1,surname=Surname(i+1),role=i<100?"Клерк":i<105?"Уборщица":i==105?"Курьер":i==106?"Архивариус":"Техник",loyalty=35+(i*37%56),salary=i<100?50:0});foreach(var e in Data.staff)e.surname=Surname(e.number);} internal static void Reset(){Data=new AdminData();last=-1;deathApplied=false;Init();}
 internal static string FullName(AdminEmployee e){string given=e.number>100&&e.number<=105?new[]{"Грета","Марта","Хильда","Эльза","Ингрид"}[e.number-101]:Given[((e.number-1)%27+(e.number-1)/27*3)%Given.Length];return e.surname+" "+given;} internal static string Save(){Init();Data.secretary=FloorAdmin.Secretary;using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(AdminData)).WriteObject(stream,Data);return "FloorAdmin/v2:"+Encoding.UTF8.GetString(stream.ToArray());}}
 internal static AdminData Decode(string json){using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json)))return (AdminData)new DataContractJsonSerializer(typeof(AdminData)).ReadObject(stream);}
 internal static void Load(string value){Reset();if(value!=null&&value.StartsWith("FloorAdmin/v2:")){Data=Decode(value.Substring(14));Data.staff=Data.staff??new List<AdminEmployee>();Data.mail=Data.mail??new List<AdminLetter>();Data.jobs=Data.jobs??new List<AdminJob>();Data.traffic=Data.traffic??new List<double>();Init();FloorAdmin.Secretary=Data.secretary??"";}else FloorAdmin.Secretary=value!=null&&value.StartsWith("FloorAdmin/v1:")?value.Substring(14):"";last=-1;if(PlayerGameController.Instance!=null)AdminBills.Sync();AdminDesktop.RefreshNeeded=true;}
 internal static AdminEmployee Employee(int id){return Data.staff.First(e=>e.number==id);}
 internal static string Attitude(int value){return value>=65?"хорошо":value>=40?"средне":"плохо";}
 internal static bool ShouldInvite(AdminEmployee e){return e.reported&&e.reportLoyalty<40;}
 internal static void Letter(string sender,string recipient,string subject,string body,bool outgoing=false,int employee=0){Data.mail.Add(new AdminLetter{id=Data.nextMail++,sender=sender,recipient=recipient,subject=subject,body=body,outgoing=outgoing,employee=employee,date=PlayerGameController.GameDate.Ticks});AdminDesktop.RefreshNeeded=true;}
 internal static string Report(int id){Init();var e=Employee(id);if(e.pending)return "Запрос уже отправлен. Ответ ожидается.";if(PlayerActionsSystem.Stamina<10)return "Для отчёта нужны 10 минут рабочего времени.";PlayerActionsSystem.Stamina-=10;e.pending=true;Data.jobs.Add(new AdminJob{kind="report",employee=id,due=Data.clock+60});Letter("Начальник этажа Эван Редгрейв","Магда Ракович","Запрос отчёта: №"+id,"Прошу подготовить отчёт о сотруднике "+e.surname+", №"+id+".",true,id);Traffic(false);return "Запрос отправлен Ракович. Списано 10 минут; ответ через 60 секунд.";}
 internal static string Collect(){Init();if(Data.jobs.Any(j=>j.kind=="collection"))return "Предыдущий сбор ещё не завершён.";Data.jobs.Add(new AdminJob{kind="collection",due=Data.clock+60});Letter("Начальник этажа Эван Редгрейв","Магда Ракович","Сбор наличных","Собрать с каждого сотрудника по 10 и передать мне. Официально сбор не оформлять.",true);Traffic(true);return "Поручение отправлено. Сбор незаконный; результат придёт через минуту.";}
 static void Traffic(bool collection){if(!collection)return;if(Data.collectionTraffic==null)Data.collectionTraffic=new List<double>();Data.collectionTraffic.RemoveAll(t=>Data.clock-t>600);Data.collectionTraffic.Add(Data.clock);if(Data.collectionTraffic.Count>=6){Data.collectionTraffic.Clear();Data.fines++;AdminBills.Add();Letter("Государственная инспекция","Эван Редгрейв","Штраф за незаконные сборы","Обнаружены неоднократные сборы денег с сотрудников. Штраф 5000 добавлен к домашним расходам. Оплатите его через список счетов.");}} internal static bool ComplaintDue(){return Data.collections>=2&&Data.staff.Any(e=>e.loyalty<30); }
 internal static string Summon(int id){AdminReception.Init();var e=Employee(id);if(Data.summoned==id||Data.visitorQueue.Contains(id))return "Сотрудник уже вызван.";if(Data.summoned!=0||!Data.receptionOpen){Data.visitorQueue.Add(id);Letter("Эван Редгрейв","Охрана","Вызов сотрудника №"+id,"Пригласить "+FullName(e)+". Ожидать приёма у кабинета.",true,id);AdminReception.Init();AdminVisitor.Announce(e);return "Вызов поставлен в очередь.";}Data.summoned=id;Data.announced=false;Data.summonDue=0;Letter("Начальник этажа Эван Редгрейв","Охрана","Вызов сотрудника №"+id,"Вызвать сотрудника "+e.surname+", №"+id+", в мой кабинет.",true,id);Letter("Охрана","Эван Редгрейв","Вызов принят","Товарищ "+e.surname+", номер "+id+", пройдите в кабинет господина Редгрейва.",false,id);AdminReception.Init();AdminVisitor.Announce(e);return "Охрана объявила вызов. Сотрудник направляется к кабинету.";}
 internal static void Tick(){Init();float now=Time.realtimeSinceStartup;if(last>=0)Data.clock+=Math.Max(0,now-last);last=now;if(Data.dead){if(!deathApplied){deathApplied=true;NodeCanvas.Tasks.Actions.GameOver.Run();}return;}foreach(var job in Data.jobs.Where(j=>j.due<=Data.clock).ToArray()){
 Data.jobs.Remove(job);
 if(job.kind=="report") {var e=Employee(job.employee);e.pending=false;e.reported=true;e.reportLoyalty=e.loyalty;Letter("Магда Ракович","Эван Редгрейв","Отчёт: "+e.surname+", №"+e.number,"Должность: "+e.role+". Оплата за обращение: "+e.salary+". Лояльность: "+e.loyalty+"/100. Отношение к начальству: "+Attitude(e.loyalty)+". Вызвать в кабинет: "+(e.loyalty<40?"да":"нет")+". Факты: принудительных сборов — "+e.collections+", проведённых бесед — "+e.interviews+". Отчёт составлен по текущей карточке сотрудника.",false,e.number);}
 else {Data.collections++;int sum=Data.staff.Count*10;foreach(var e in Data.staff){e.collections++;e.loyalty=Math.Max(0,e.loyalty-10);}PlayerGameController.Money+=sum;Letter("Магда Ракович","Эван Редгрейв","Сбор завершён","Собрано "+sum+" с "+Data.staff.Count+" сотрудников. Лояльность каждого снизилась на 10.");if(ComplaintDue()){Data.dead=true;Letter("Служба безопасности","Эван Редгрейв","Приговор","Сотрудники с низкой лояльностью подали жалобу на повторные незаконные поборы. Приговор: смертная казнь.");deathApplied=true;NodeCanvas.Tasks.Actions.GameOver.Run();}}
 } }
}
[HarmonyPatch(typeof(PlayerGameController),"Awake")]
class ManagementNewSession {static void Postfix(PlayerGameController __instance){if(PlayerGameController.Instance==__instance)AdminManagement.Reset();}}







