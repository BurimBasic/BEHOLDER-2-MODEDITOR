using System;
using System.Collections.Generic;
using System.Linq;

[Serializable] public class AdminPetition {
 public int number,employee,cost;public string title,text,decision;
}
static class AdminPetitions {
 internal static void Today(){
  var data=AdminManagement.Data;long day=PlayerGameController.GameDate.Date.Ticks;
  if(data.petitions==null)data.petitions=new List<AdminPetition>();
  if(data.petitionDay==day&&data.petitions.Count>0)return;
  data.petitionDay=day;data.petitions.Clear();
  string[] titles={"Ремонт санитарного узла","Замена настольной лампы","Материальная помощь","Перенос рабочего места","Сводка для Министерства","Жалоба на ночные смены"};
  string[] bodies={"В санитарном узле первого этажа второй день течёт бачок. Вода поступает в помещение архива. Прошу выделить средства на прокладку и работу сантехника.","Лампа над столом мигает. При свете коридора невозможно различить номера дел. Прошу выдать исправную лампу со склада.","В связи с болезнью супруги прошу оказать единовременную материальную помощь. Справка больничной канцелярии приложена.","Рабочее место находится под неисправной вентиляционной решёткой. Прошу разрешить перевод к свободному столу у внутренней стены.","По распоряжению вышестоящей канцелярии прошу согласовать сводку по просроченным обращениям. Материалы для окончательного решения следует направить на верхние этажи.","За последнюю неделю мне назначили три ночных дежурства подряд. Прошу пересмотреть график и распределить дежурства между сотрудниками отдела."};
  int[] costs={30,20,75,0,0,0};int offset=(int)((day/TimeSpan.TicksPerDay)%100);
  for(int i=0;i<titles.Length;i++){int employee=1+(offset+i*17)%100;var e=AdminManagement.Employee(employee);data.petitions.Add(new AdminPetition{number=i+1,employee=employee,cost=costs[i],title=titles[i],text="Начальнику первого этажа\nЭвану Редгрейву\nОт: "+AdminManagement.FullName(e)+", №"+employee.ToString("000")+"\n\n"+titles[i]+"\n\n"+bodies[i]+(costs[i]>0?"\n\nЗапрашиваемая сумма: "+costs[i]+".":"")+"\n\n"+PlayerGameController.GameDate.ToString("dd.MM.yyyy")});}
 }
 internal static AdminPetition Current {get{Today();return AdminManagement.Data.petitions.FirstOrDefault(p=>string.IsNullOrEmpty(p.decision));}}
 internal static string Decide(string verdict){
  var p=Current;if(p==null)return "На сегодня обращений больше нет.";
  if(verdict!="Одобрено"&&verdict!="Отклонено"&&verdict!="На рассмотрение")throw new ArgumentException("Unknown petition decision");
  if(verdict=="Одобрено"&&PlayerGameController.Money<p.cost)return "Недостаточно средств для одобрения.";
  p.decision=verdict;var e=AdminManagement.Employee(p.employee);
  if(verdict=="Одобрено"){PlayerGameController.Money-=p.cost;e.loyalty=Math.Min(100,e.loyalty+3);}
  else if(verdict=="Отклонено")e.loyalty=Math.Max(0,e.loyalty-2);
  else AdminManagement.Letter("Эван Редгрейв","Вышестоящая канцелярия","На рассмотрение: "+p.title,p.text+"\n\nРезолюция: передать на рассмотрение вышестоящему руководству.",true,p.employee);
  if(AdminManagement.Data.petitions.All(x=>!string.IsNullOrEmpty(x.decision)))AdminManagement.Letter("Канцелярия","Эван Редгрейв","Обращения за день обработаны",string.Join("\n",AdminManagement.Data.petitions.Select(x=>"№"+x.number+". "+x.title+" — "+x.decision).ToArray()));
  return null;
 }
}
