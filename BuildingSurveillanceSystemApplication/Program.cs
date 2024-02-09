using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildingSurveillanceSystemApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Clear();

            SecuritySurveillanceHub securitySurveillanceHub = new SecuritySurveillanceHub();

            EmployeeNotify employeeNotify = new EmployeeNotify(new Employee
            {
                Id = 1,
                FirstName = "Bob",
                LastName = "Jones",
                JobTitle = "Development Manager"
            });
            EmployeeNotify employeeNotify2 = new EmployeeNotify(new Employee
            {
                Id = 2,
                FirstName = "Dave",
                LastName = "Kendal",
                JobTitle = "Chief Information Officer"
            });

            SecurityNotify securityNotify = new SecurityNotify();

            employeeNotify.Subscribe(securitySurveillanceHub);
            employeeNotify2.Subscribe(securitySurveillanceHub);
            securityNotify.Subscribe(securitySurveillanceHub);

            securitySurveillanceHub.ConfirmExternalVisitorEntersBuilding(1, "Andrew", "Jackson", "The Company", "Contractor", DateTime.Parse("12 May 2020 11:00"), 1);
            securitySurveillanceHub.ConfirmExternalVisitorEntersBuilding(2, "Jane", "Davidson", "Another Company", "Lawyer", DateTime.Parse("12 May 2020 12:00"), 2);

           // employeeNotify.UnSubscribe();

            securitySurveillanceHub.ConfirmExternalVisitorExitsBuilding(1, DateTime.Parse("12 May 2020 13:00"));
            securitySurveillanceHub.ConfirmExternalVisitorExitsBuilding(2, DateTime.Parse("12 May 2020 15:00"));

            securitySurveillanceHub.BuildingEntryCutOffTimeReached();

            Console.ReadKey();
        }
    }

    public class Employee : IEmployee  //定义了一个员工信息的实体类
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string JobTitle { get; set; }
    }
    public interface IEmployee //员工接口
    { 
        int Id { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string JobTitle { get; set; }
    
    }

    public abstract class Observer : IObserver<ExternalVisitor>
    {
        IDisposable _cancellation;
        protected List<ExternalVisitor> _externalVisitors = new List<ExternalVisitor>();

        public abstract void OnCompleted();

        public abstract void OnError(Exception error);

        public abstract void OnNext(ExternalVisitor value);

        public void Subscribe(IObservable<ExternalVisitor> provider)
        {
            _cancellation = provider.Subscribe(this);
        }

        public void UnSubscribe()
        {
            _cancellation.Dispose();
            _externalVisitors.Clear();
        }

    }


    public class EmployeeNotify : Observer
    {
        IEmployee _employee = null; //私有字段
        public EmployeeNotify(IEmployee employee) //构造函数
        { 
            _employee = employee;   //再将参数进行传递  
        }
        //OnNext OnCompleted Onerror
        public override void OnCompleted()
        {
        //这个方法会输出每日访问报告，包括访客的相关信息 
        //，它首先构造了报告的标题，
        //然后遍历 _externalVisitors 列表中的访客信息，
        //并输出每个访客的 ID、姓名、进入时间和离开时间。最后，输出空行作为报告的分隔符。
            string heading = $"{_employee.FirstName + " " + _employee.LastName} Daily Visitor's Report"; //这里形成了一个heading标题
            Console.WriteLine(); //空行

            Console.WriteLine(heading);
            Console.WriteLine(new string('-', heading.Length));
            Console.WriteLine();

            foreach (var externalVisitor in _externalVisitors)
            {
                externalVisitor.InBuilding = false; //这里的属性表示外部访客是否还在建筑中 如果等于false则表示已经不在建筑中了
                //-6：表示字段的最小宽度为 6 个字符。如果字段的实际宽度不足 6 个字符，将会在字段的左侧添加空格，
                //以使得整个字段的宽度达到 6 个字符。如果字段的实际宽度超过 6 个字符，则不进行截断。
                Console.WriteLine($"{externalVisitor.Id,-6}{externalVisitor.FirstName,-15}
                {externalVisitor.LastName,-15}{externalVisitor.EntryDateTime.ToString("dd MMM yyyy hh:mm:ss")
                ,-25}{externalVisitor.ExitDateTime.ToString("dd MMM yyyy hh:mm:ss tt"),-25}");  //转固定格式化字符串
            }
            Console.WriteLine();
            Console.WriteLine();
        
        }

        public override void OnError(Exception error)
        {
        //当开发人员在代码中调用了一个尚未实现的方法时
            throw new NotImplementedException();
        }

        public override void OnNext(ExternalVisitor value)
        {
            var externalVisitor = value;

            if (externalVisitor.EmployeeContactId == _employee.Id)
            {
            //LINQ  的Lambda表达式 e => e.Id == externalVisitor.Id：这是一个 Lambda 表达式，用于指定查找条件。
            //它表示对于 _externalVisitors 列表中的每个外部访客对象 e，如果 e 的 Id 属性等于当前遍历到的 externalVisitor 对象的 Id 属性，
            //则返回 true，否则返回 false。
                var externalVisitorListItem = _externalVisitors.FirstOrDefault(e => e.Id == externalVisitor.Id);
                 //如果== NULL 表示外部访客头一次到建筑大楼
                if (externalVisitorListItem == null)//没有找到xiangtongid
                
                {
                    _externalVisitors.Add(externalVisitor);
                    
                    OutputFormatter.ChangeOutputTheme(OutputFormatter.TextOutputTheme.Employee);
                    
                    Console.WriteLine($"{_employee.FirstName + " " + _employee.LastName}, your visitor has arrived. Visitor ID({externalVisitor.Id}), FirstName({externalVisitor.FirstName}), LastName({externalVisitor.LastName}), entered the building, DateTime({externalVisitor.EntryDateTime.ToString("dd MMM yyyy hh:mm:ss")})");
                    
                    OutputFormatter.ChangeOutputTheme(OutputFormatter.TextOutputTheme.Normal);
                    
                    Console.WriteLine();
                }
                else
                {
                //如果访客已经离开了
                    if (externalVisitor.InBuilding == false)
                    {
                        //update local external visitor list item with data from the external visitor object passed in from the observable object
                        externalVisitorListItem.InBuilding = false;
                        externalVisitorListItem.ExitDateTime = externalVisitor.ExitDateTime;
                    }
                }

            }
        }

    }

    public class UnSubscriber<ExternalVisitor> : IDisposable //用于消除的 类型使用的是外部访问者
    {
        private List<IObserver<ExternalVisitor>> _observers;  //
        private IObserver<ExternalVisitor> _observer;
        //构造函数两个参数
        public UnSubscriber(List<IObserver<ExternalVisitor>> observers, IObserver<ExternalVisitor> observer)
        {
            _observers = observers;
            _observer = observer;
        }
   //在取消订阅中的类要实现的方法就是dispose
        public void Dispose()
        {
        //observers这个包含了 observers包含他就去删除
            if (_observers.Contains(_observer))
                _observers.Remove(_observer);
        }
    }
    //第二个被观察类  
    public class SecurityNotify : Observer
    {

        public override void OnCompleted()
        {
            string heading = "Security Daily Visitor's Report";
            Console.WriteLine();

            Console.WriteLine(heading);
            Console.WriteLine(new string('-', heading.Length));
            Console.WriteLine();

            foreach (var externalVisitor in _externalVisitors)
            {
                externalVisitor.InBuilding = false;

                Console.WriteLine($"{externalVisitor.Id,-6}{externalVisitor.FirstName,-15}{externalVisitor.LastName,-15}{externalVisitor.EntryDateTime.ToString("dd MMM yyyy hh:mm:ss"),-25}{externalVisitor.ExitDateTime.ToString("dd MMM yyyy hh:mm:ss tt"),-25}");
            }
            Console.WriteLine();
            Console.WriteLine();
        }

        public override void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public override void OnNext(ExternalVisitor value)
        {
            var externalVisitor = value;

            var externalVisitorListItem = _externalVisitors.FirstOrDefault(e => e.Id == externalVisitor.Id);

            if (externalVisitorListItem == null)
            {
                _externalVisitors.Add(externalVisitor);
                
                OutputFormatter.ChangeOutputTheme(OutputFormatter.TextOutputTheme.Security);

                Console.WriteLine($"Security notification: Visitor Id({externalVisitor.Id}), FirstName({externalVisitor.FirstName}), LastName({externalVisitor.LastName}), entered the building, DateTime({externalVisitor.EntryDateTime.ToString("dd MMM yyyy hh:mm:ss tt")})");

                OutputFormatter.ChangeOutputTheme(OutputFormatter.TextOutputTheme.Normal);

                Console.WriteLine();
            }
            else
            {
                if (externalVisitor.InBuilding == false)
                {
                     //update local external visitor list item with data from the external visitor object passed in from the observable object
                    externalVisitorListItem.InBuilding = false; 
                    externalVisitorListItem.ExitDateTime = externalVisitor.ExitDateTime;
                    
                    Console.WriteLine($"Security notification: Visitor Id({externalVisitor.Id}), FirstName({externalVisitor.FirstName}), LastName({externalVisitor.LastName}), exited the building, DateTime({externalVisitor.ExitDateTime.ToString("dd MMM yyyy hh:mm:ss tt")})");
                    Console.WriteLine();

                }
            }

        }
    }



//订阅者类  即安全处理器
    public class SecuritySurveillanceHub : IObservable<ExternalVisitor>
    {
        private List<ExternalVisitor> _externalVisitors;
        private List<IObserver<ExternalVisitor>> _observers;
       //构造器
        public SecuritySurveillanceHub()
        {
            _externalVisitors = new List<ExternalVisitor>();
            _observers = new List<IObserver<ExternalVisitor>>();
        }
        //idisposable 这个接口是用来处理非必要资源的释放非托管资源或执行其他清理操作

        //这个函数是用来这个方法用于订阅外部访客监控的观察者。它接受一个实现了 IObserver<ExternalVisitor> 接口的观察者对象作为参数。
//首先，它检查观察者列表中是否已经包含了传入的观察者，如果没有，则将其添加到观察者列表中。
//然后，它遍历当前已经存在的外部访客列表，对每个外部访客调用观察者的 OnNext 方法，向观察者发送外部访客信息。
//最后，它返回一个 UnSubscriber 对象，用于在需要时取消订阅该观察者。
        public IDisposable Subscribe(IObserver<ExternalVisitor> observer)
        { // 这个方法用于订阅外部访客监控的观察者    
            if (!_observers.Contains(observer))  //观察者序列中包含这个 observer 
                _observers.Add(observer);

            foreach (var externalVisitor in _externalVisitors)
                observer.OnNext(externalVisitor);

            return new UnSubscriber<ExternalVisitor>(_observers, observer);

        }
//这个方法用于确认外部访客进入建筑物。
//它接受外部访客的各种信息作为参数，并将该访客添加到外部访客列表中。
//然后，它遍历观察者列表，并对每个观察者调用 OnNext 方法，向观察者发送新进入建筑物的外部访客信息。
        public void ConfirmExternalVisitorEntersBuilding(int id, string firstName, string lastName, string companyName, string jobTitle, DateTime entryDateTime, int employeeContactId)
        {
        //externalVisitor 是一个生成的对象
            ExternalVisitor externalVisitor = new ExternalVisitor
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                CompanyName = companyName,
                JobTitle = jobTitle,
                EntryDateTime = entryDateTime,
                InBuilding = true,
                EmployeeContactId = employeeContactId
            };

            _externalVisitors.Add(externalVisitor);
//这段代码的作用是通知所有已订阅的观察者对象，即使它们在同一个时间点下也会收到相同的通知。
//这通常用于当外部访客进入或离开建筑物时，通知所有已订阅的观察者。
            foreach (var observer in _observers)
                observer.OnNext(externalVisitor);

        }
        //这个方法用于确认外部访客离开建筑物。
//它接受外部访客的 ID 和离开时间作为参数，根据 ID 在外部访客列表中找到相应的外部访客对象。
//如果找到了对应的外部访客对象，则更新其离开时间和在建筑物状态，并向观察者列表中的每个观察者发送更新后的外部访客信息。
        public void ConfirmExternalVisitorExitsBuilding(int externalVisitorId, DateTime exitDateTime)
        {
        //
//FirstOrDefault 是 LINQ 中的一个方法，它用于从序列中获取第一个元素，如果序列为空，则返回默认值。如果序列不为空，则返回序列中的第一个元素。
            var externalVisitor = _externalVisitors.FirstOrDefault(e => e.Id == externalVisitorId);

            if (externalVisitor != null)
            {
                externalVisitor.ExitDateTime = exitDateTime;
                externalVisitor.InBuilding = false;

                foreach (var observer in _observers)
                    observer.OnNext(externalVisitor);
            }
        }
        public void BuildingEntryCutOffTimeReached()
        {
            if (_externalVisitors.Any(e => e.InBuilding == true))
            {
                return;
            }
//如果有任何一个外部访客仍然在建筑物内，就不执行 foreach 循环中的代码，直接返回到调用点，终止了整个方法或代码块的执行。
            foreach (var observer in _observers)
                observer.OnCompleted();
        }
    }
    //
    public static class OutputFormatter
    {
        public enum TextOutputTheme
        //OutputFormatter 类用于控制台输出的格式化。它定义了不同的文本输出主题，并提供了一个静态方法 ChangeOutputTheme，
        //用于根据指定的主题改变控制台的前景色和背景色，以便在控制台中区分不同类型的输出信息。
        { 
            Security,
            Employee,
            Normal
        }

        public static void ChangeOutputTheme(TextOutputTheme textOutputTheme)
        {
            if (textOutputTheme == TextOutputTheme.Employee)
            {
                Console.BackgroundColor = ConsoleColor.DarkMagenta;
                Console.ForegroundColor = ConsoleColor.White;
            }
            else if (textOutputTheme == TextOutputTheme.Security)
            {
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else
            {
                Console.ResetColor(); //默认值
            }
        
        }

    }

    //外部访问客户实体类
    public class ExternalVisitor
    { 
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CompanyName { get; set; }
        public string JobTitle { get; set; }
        public DateTime EntryDateTime { get; set; }
        public DateTime ExitDateTime { get; set; }
        public bool InBuilding { get; set; }
        public int EmployeeContactId { get; set; }

    }

}
