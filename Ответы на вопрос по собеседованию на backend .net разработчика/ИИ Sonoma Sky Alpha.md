### Что такое middleware в ASP.NET Core?

Middleware в ASP.NET Core — это программные компоненты, которые обрабатывают HTTP-запросы и ответы в последовательной цепочке (pipeline). Они позволяют модульно расширять функциональность приложения, вставляя логику обработки на разных этапах жизненного цикла запроса. Middleware не привязан к контроллерам или MVC; он работает на уровне HTTP-уровня, до или после достижения конечного обработчика (например, контроллера).

#### Как работает middleware pipeline?
- **Pipeline** — это последовательность middleware, которая формируется при запуске приложения. Каждый middleware:
  - Получает входящий `HttpContext`.
  - Может обработать запрос (например, логировать, аутентифицировать).
  - Вызвать следующий middleware с помощью `next.Invoke(context)` (или `next()` в некоторых реализациях).
  - Или завершить обработку самостоятельно (short-circuit), не передавая дальше (например, при 404-ошибке).
- Обработка идет **слева направо** для запросов (от внешнего к внутреннему) и **справа налево** для ответов (от внутреннего к внешнему).
- Встроенные middleware: ASP.NET Core предоставляет готовые, такие как `UseAuthentication`, `UseAuthorization`, `UseRouting`, `UseEndpoints`, `UseStaticFiles` и т.д.

Это позволяет гибко настраивать обработку: например, сначала проверить аутентификацию, потом применить CORS, а затем маршрутизировать к контроллеру.

#### Регистрация middleware
Middleware регистрируется в методе `Configure` класса `Startup` (в .NET 5 и ниже) или в `Program.cs` (минимальный хостинг в .NET 6+). Порядок регистрации критически важен — он определяет последовательность выполнения.

Пример в `Program.cs` (.NET 6+):

```csharp
var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы (DI)
builder.Services.AddControllers();

var app = builder.Build();

// Регистрация middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error"); // Обработка ошибок в продакшене
    app.UseHsts();
}

app.UseHttpsRedirection(); // Перенаправление на HTTPS
app.UseStaticFiles(); // Статические файлы
app.UseRouting(); // Маршрутизация
app.UseAuthentication(); // Аутентификация (должен быть перед авторизацией)
app.UseAuthorization(); // Авторизация

app.MapControllers(); // Подключение endpoints (контроллеры)

app.Run();
```

#### Создание собственного middleware
Вы можете написать кастомный middleware как класс, реализующий `IMiddleware`, или как делегат с `RequestDelegate`. Рекомендуется использовать `IMiddleware` для лучшей тестируемости (с DI).

Пример простого middleware для логирования (класс):

```csharp
public class LoggingMiddleware : IMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        _logger.LogInformation("Запрос: {Path}", context.Request.Path);
        
        await next(context); // Передача следующему middleware
        
        _logger.LogInformation("Ответ отправлен: {StatusCode}", context.Response.StatusCode);
    }
}
```

Регистрация в `Program.cs`:

```csharp
builder.Services.AddTransient<LoggingMiddleware>(); // DI
app.UseMiddleware<LoggingMiddleware>(); // В pipeline
```

Или как inline-делегат (для простых случаев):

```csharp
app.Use(async (context, next) =>
{
    Console.WriteLine("Запрос получен");
    await next();
    Console.WriteLine("Ответ отправлен");
});
```

#### Преимущества и актуальность
- **Модульность**: Легко добавлять/удалять функциональность без изменения контроллеров (логирование, кэширование, rate limiting).
- **Производительность**: Pipeline эффективен, так как избегает ненужных вызовов.
- **Актуальность в .NET 8 (2024)**: Middleware остается основой веб-API. В .NET 8 добавлены улучшения, такие как AOT-компиляция и новые middleware для HTTP/3, но концепция не изменилась. В минимальном API (без контроллеров) pipeline используется аналогично.

На собеседовании подчеркните порядок регистрации (например, `UseAuthentication` перед `UseAuthorization`) и short-circuit, чтобы показать глубокое понимание. Если спросят, упомяните, что middleware — это эволюция OWIN в .NET Core, обеспечивающая кросс-платформенность.

### Что такое принципы SOLID?

SOLID — это акроним, обозначающий пять фундаментальных принципов объектно-ориентированного дизайна (ООП), предложенных Робертом Мартином (Uncle Bob) в начале 2000-х. Эти принципы помогают создавать гибкий, поддерживаемый и масштабируемый код, минимизируя coupling (связанность) и повышая cohesion (связность). Они особенно актуальны для backend-разработки в .NET, где SOLID интегрируется с Dependency Injection (DI), интерфейсами и паттернами вроде Repository или Clean Architecture.

В .NET Core/.NET 8+ SOLID используется повсеместно: в сервисах, контроллерах, репозиториях и минимальных API. Соблюдение SOLID упрощает unit-тестирование (с Moq для моков), рефакторинг и эволюцию системы. На собеседовании важно не только перечислить принципы, но и привести примеры, показав, как они решают проблемы (например, в монолитном коде vs. микросервисах).

#### 1. **S** — Single Responsibility Principle (Принцип единственной ответственности)
   - **Суть**: Класс должен иметь только одну причину для изменения, то есть одну ответственность. Это предотвращает "god classes" (классы, делающие всё).
   - **Проблема без принципа**: Класс, который и сохраняет данные, и отправляет email, и логирует — трудно тестировать и менять.
   - **Пример на C#** (плохой vs. хороший):
     ```csharp
     // Плохо: Класс с несколькими обязанностями
     public class UserService
     {
         public void CreateUser(User user)
         {
             // Сохранение в БД
             SaveToDatabase(user);
             // Отправка email
             SendEmail(user.Email, "Welcome!");
             // Логирование
             Log("User created");
         }
     }

     // Хорошо: Разделение обязанностей
     public interface IUserRepository { void Save(User user); }
     public interface IEmailService { void SendWelcome(User user); }
     public interface ILogger { void Log(string message); }

     public class UserService
     {
         private readonly IUserRepository _repo;
         private readonly IEmailService _email;
         private readonly ILogger _logger;

         public UserService(IUserRepository repo, IEmailService email, ILogger logger)
         {
             _repo = repo; _email = email; _logger = logger;
         }

         public void CreateUser(User user)
         {
             _repo.Save(user);
             _email.SendWelcome(user);
             _logger.Log("User created");
         }
     }
     ```
   - **Актуальность в .NET**: В DI-контейнере (например, `IServiceCollection`) регистрируем интерфейсы для инверсии зависимостей, что упрощает замену реализаций (например, от SQL к Cosmos DB).

#### 2. **O** — Open-Closed Principle (Принцип открытости/закрытости)
   - **Суть**: Классы должны быть открыты для расширения (через наследование или композицию), но закрыты для модификации (не менять существующий код).
   - **Проблема без принципа**: Добавление новой фичи требует правки старого класса, что рискует сломать всё.
   - **Пример на C#** (расширение обработчика платежей):
     ```csharp
     // Хорошо: Абстрактный базовый класс
     public abstract class PaymentProcessor
     {
         public abstract void Process(Payment payment);
     }

     public class CreditCardProcessor : PaymentProcessor
     {
         public override void Process(Payment payment) => /* Логика для карты */;
     }

     public class PayPalProcessor : PaymentProcessor
     {
         public override void Process(Payment payment) => /* Логика для PayPal */;
     }

     // Использование: Легко добавить новый процессор без изменений в существующих
     public class OrderService
     {
         public void Checkout(Order order, PaymentProcessor processor)
         {
             processor.Process(order.Payment); // Полиморфизм
         }
     }
     ```
   - **Актуальность в .NET**: Используется в плагинах (MEF) или расширениях ASP.NET Core (например, кастомные middleware). В .NET 8 с AOT-компиляцией OCP помогает избегать runtime-ошибок.

#### 3. **L** — Liskov Substitution Principle (Принцип подстановки Барбары Лисков)
   - **Суть**: Объекты производных классов должны быть полностью заменяемыми на объекты базового класса без нарушения поведения программы. Наследники не должны "ломать" ожидания.
   - **Проблема без принципа**: Подкласс меняет контракт (например, бросает исключения, которых не было в базе).
   - **Пример на C#** (птицы, чтобы избежать "квадрат-круг" проблемы):
     ```csharp
     // Хорошо: Базовый интерфейс
     public interface IBird
     {
         void Move(); // Ожидается, что все птицы двигаются
     }

     public class Eagle : IBird { public void Move() => Fly(); } // Летает
     public class Penguin : IBird { public void Move() => Swim(); } // Плавает, но не летает — OK, если Move() не подразумевает полёт

     // Плохо: Если базовый класс ожидает Fly(), Penguin сломает подстановку
     // public class Penguin : FlyingBird { } // Penguin не летает!
     ```
   - **Актуальность в .NET**: Критично для DI и полиморфизма в сервисах (например, разные реализации `IRepository`). В Entity Framework LSP помогает с наследованием сущностей.

#### 4. **I** — Interface Segregation Principle (Принцип разделения интерфейсов)
   - **Суть**: Клиенты не должны зависеть от интерфейсов, которые они не используют. Лучше маленькие, специфичные интерфейсы, чем "жирные".
   - **Проблема без принципа**: Большой интерфейс заставляет реализовывать ненужные методы (метод "fat interface").
   - **Пример на C#** (репозиторий для разных сценариев):
     ```csharp
     // Плохо: Один большой интерфейс
     public interface IRepository
     {
         void Create<T>(T entity);
         void Update<T>(T entity);
         T GetById<T>(int id);
         void Delete<T>(int id); // Не все репозитории поддерживают удаление (read-only)
     }

     // Хорошо: Разделённые интерфейсы
     public interface IReadRepository<T>
     {
         T GetById(int id);
     }

     public interface IWriteRepository<T>
     {
         void Create(T entity);
         void Update(T entity);
         void Delete(int id);
     }

     public class UserRepository : IReadRepository<User>, IWriteRepository<User> { /* Реализация */ }
     public class AuditRepository : IReadRepository<Audit> { /* Только чтение, без Write */ }
     ```
   - **Актуальность в .NET**: В ASP.NET Core интерфейсы инжектируются через DI. ISP снижает boilerplate-код и улучшает тестируемость (моки только нужных методов).

#### 5. **D** — Dependency Inversion Principle (Принцип инверсии зависимостей)
   - **Суть**: Высокоуровневые модули не должны зависеть от низкоуровневых; оба должны зависеть от абстракций. Зависимости инвертируются через интерфейсы.
   - **Проблема без принципа**: Жёсткая привязка к конкретным классам (например, `SqlRepository` напрямую).
   - **Пример на C#** (DI в .NET):
     ```csharp
     // Плохо: Прямая зависимость
     public class UserService
     {
         private SqlUserRepository _repo = new(); // Жёсткая привязка
     }

     // Хорошо: Зависимость от абстракции
     public interface IUserRepository { void Save(User user); }

     public class UserService
     {
         private readonly IUserRepository _repo;

         public UserService(IUserRepository repo) // Инверсия через конструктор
         {
             _repo = repo;
         }

         public void CreateUser(User user) => _repo.Save(user);
     }

     // Регистрация в Program.cs (.NET 6+)
     builder.Services.AddScoped<IUserRepository, SqlUserRepository>();
     ```
   - **Актуальность в .NET**: Встроенный DI-контейнер (`Microsoft.Extensions.DependencyInjection`) реализует DIP из коробки. В .NET 8 с Scoped/Transient lifetimes это ключ к микросервисам и облачным приложениям (легко менять провайдеры, например, на Azure).

#### Преимущества и советы для собеседования
- **Преимущества**: SOLID делает код testable (легче мокать), maintainable (меньше багов при изменениях) и scalable (идеально для enterprise .NET-приложений). В реальных проектах (например, с DDD или CQRS) SOLID — основа архитектуры.
- **Актуальность в 2024**: В .NET 8 SOLID сочетается с новыми фичами вроде Primary Constructors и Native AOT, где DI критичен для производительности. На собеседовании упомяните, как SOLID помогает в TDD (Test-Driven Development) или при миграции на Blazor/MAUI.
- **Совет**: Если спросят пример из практики, опишите рефакторинг legacy-кода или использование в API (например, SRP в контроллерах). Избегайте "книжных" определений — фокусируйтесь на "почему" и "как применяется".

### Когда инициализируется сборка мусора в .NET?

Сборка мусора (Garbage Collection, GC) в .NET — это автоматический механизм управления памятью, реализованный в Common Language Runtime (CLR). GC отвечает за освобождение памяти, занятой объектами, которые больше не используются (unreachable objects). В отличие от языков вроде C++, где память управляется вручную, в .NET GC делает это автоматически, но **non-deterministic** (недетерминировано) — вы не можете точно предсказать, когда именно он запустится. Это упрощает разработку, но требует понимания, чтобы избегать утечек памяти (memory leaks) или чрезмерного давления на GC.

На собеседовании подчеркните, что GC — это не "инициализация" в смысле создания, а **запуск цикла сборки** (collection cycle). CLR инициализирует GC при запуске приложения, но сборка активируется динамически. Актуальность: В .NET 8 GC оптимизирован для облачных сценариев (например, с Ephemeral GC для Gen 0/1), с поддержкой Background GC и конфигурацией через `GCSettings` или переменные окружения.

#### Когда запускается сборка мусора?
GC запускается **автоматически** CLR, когда система замечает проблемы с памятью. Основные триггеры:

1. **Достижение порогов памяти (Heap Thresholds)**:
   - CLR мониторит управляемую кучу (managed heap), разделённую на поколения (Generations): Gen 0 (молодые объекты, ~256 KB), Gen 1 (промежуточные, ~16 MB), Gen 2 (долгожители, остальная память).
   - **Gen 0 Collection**: Запускается, когда Gen 0 заполняется (обычно при аллокации нового объекта). Это быстро и часто (каждые секунды в высоконагруженных apps).
   - **Gen 1 Collection**: Если после Gen 0 выжившие объекты заполняют Gen 1.
   - **Gen 2 Collection**: Редко (каждые минуты/часы), когда вся куча под давлением. Это полноразмерная сборка, очищающая все поколения.
   - Триггер: Когда доступная память падает ниже порога (budget), рассчитанного на основе размера кучи и исторических данных (adaptive GC в .NET 4+).

2. **Системные события**:
   - **Низкая физическая память**: ОС сигнализирует (Low Memory Notification), и CLR запускает GC.
   - **Выгрузка AppDomain**: При завершении домена приложения (например, в IIS или хостинге).
   - **Финализаторы (Finalizers)**: Объекты с `~Class()` (destructor) перемещаются в Finalization Queue, и GC их обрабатывает в отдельном потоке.

3. **Принудительный запуск**:
   - Через API: `GC.Collect()` (или `GC.Collect(2, GCCollectionMode.Forced)` для Gen 2 с принуждением).
   - **Не рекомендуется в продакшене**: Это нарушает оптимизации CLR (GC знает лучше, когда собирать). Вызывайте только в тестах или редких сценариях (например, перед большим аллоком в играх). Вместо этого используйте `GC.AddMemoryPressure()` для уведомления о неуправляемой памяти.

GC работает в двух режимах:
- **Workstation GC** (по умолчанию для клиентских apps): Однопоточный, подходит для десктопа.
- **Server GC** (включить через `<gcServer enabled="true"/>` в `app.config` или `DOTNET_GCHeapHardLimit`): Многопоточный, для серверов (ASP.NET Core), с отдельными кучами на поток.

В .NET 8 добавлена поддержка **Background GC** (по умолчанию в Workstation), где Gen 2 собирается в фоне, минимизируя паузы (latency). Для облаков (Azure) используйте `GCHeapHardLimit` для лимита кучи.

#### Как работает сборка на примере?
GC использует **Mark-and-Sweep** алгоритм:
1. **Mark Phase**: От корней (stacks, statics, handles) отмечает reachable объекты.
2. **Sweep Phase**: Освобождает unmarked память, компактирует кучу (сдвигает объекты для снижения фрагментации).
3. **Relocation**: Выжившие объекты перемещаются в следующее поколение.

Пример кода, демонстрирующий наблюдение за GC (используйте для мониторинга, не для триггера):

```csharp
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine($"GC поколения: {GC.MaxGeneration}");
        Console.WriteLine($"Размер кучи Gen 0: {GC.CollectionCount(0)} коллекций");

        // Аллокация объектов для триггера Gen 0
        for (int i = 0; i < 100000; i++)
        {
            var obj = new byte[1024]; // Маленькие объекты в Gen 0
        }

        // Принудительный GC (только для примера!)
        GC.Collect(0); // Только Gen 0
        GC.WaitForPendingFinalizers(); // Ждём финализаторы

        Console.WriteLine($"Коллекций Gen 0 после: {GC.CollectionCount(0)}");
    }
}
```

- Вывод: Покажет, как GC автоматически срабатывает при аллокациях, и как `Collect()` увеличивает счётчик.

#### Мониторинг и лучшие практики
- **Инструменты**: Используйте `GC.GetTotalMemory(false)` для размера кучи, PerfView или dotnet-trace для профилинга. В ASP.NET Core добавьте middleware для метрик (Prometheus + GC counters).
- **Избегайте проблем**:
  - **Memory Leaks**: Слабые ссылки (`WeakReference`), события (unsubscribe), статические коллекции.
  - **Частый GC**: Оптимизируйте аллокации (object pooling с `ArrayPool`, Span<T> в .NET Core).
  - **Large Object Heap (LOH)**: Объекты >85 KB идут в Gen 2 без компактации — используйте `GCSettings.LargeObjectHeapCompactionMode` в .NET 4.5+.
- **Актуальность в .NET 8 (2024)**: GC улучшен для контейнеров (Docker) с автоматическим обнаружением лимитов памяти (`ContainerCgroupV1Enabled`). В минимальных API и Blazor GC latency снижена. Для high-throughput (микросервисы) Server GC + ValueTask вместо async для снижения аллокаций.

На собеседовании: Объясните, почему GC non-deterministic (для производительности), и приведите пример, когда вы бы использовали `GC.Collect()` (редко, напр. в batch-обработке). Если спросят о детерминированном GC, упомяните `IDisposable` + `using` для RAII-подобного управления (не GC, но связано). Это покажет глубокое понимание CLR и оптимизации.

### Какие делегаты вы знаете в .NET?

Делегаты в .NET — это типобезопасные указатели на методы (type-safe function pointers), позволяющие передавать методы как параметры, возвращать их или хранить в переменных. Они поддерживают **multicast** (несколько методов в одном делегате, соединённых через `+=`), инвариантность по умолчанию, но с поддержкой **covariance** (out) и **contravariance** (in) с .NET 4+. Делегаты — основа событий (events), LINQ, async/await и функционального программирования.

В backend .NET (ASP.NET Core, EF Core) делегаты используются повсеместно: в middleware (для pipeline), в DI (для фабрик), в Task-based async (Func<Task>), в Expression Trees для динамических запросов. Актуальность в .NET 8: Делегаты оптимизированы для AOT-компиляции, интегрированы в минимальные API (например, `app.MapGet("/users", async (Func<IEnumerable<User>>) handler)`), и используются в новых фичах вроде Primary Constructors для лямбд.

На собеседовании важно показать не только список, но и примеры, multicast и когда использовать (например, Action для void-операций, Func для вычислений). Вот основные встроенные делегаты из `System` namespace (из `mscorlib` или `System.Core`):

#### 1. **Action<T>** (и перегрузки: Action, Action<T1,T2,...>)
   - **Суть**: Делегат без возвращаемого значения (void). Идеален для операций "сделай что-то" (callback'и, события).
   - **Параметры**: До 16 generic (Action<T1,T2,...,T16>).
   - **Пример**:
     ```csharp
     using System;

     class Program
     {
         static void Greet(string name) => Console.WriteLine($"Привет, {name}!");

         static void Main()
         {
             Action<string> action = Greet; // Или лямбда: action = name => Console.WriteLine(name);
             action("Alice"); // Вывод: Привет, Alice!

             // Multicast
             Action<int> math = x => Console.WriteLine(x * 2);
             math += x => Console.WriteLine(x + 10);
             math(5); // Вывод: 10 \n 15
         }
     }
     ```
   - **Использование**: В LINQ (ForEach), Task.Run, event handlers. В ASP.NET: middleware делегаты вроде `app.Use((context, next) => { ... })`.

#### 2. **Func<T, TResult>** (и перегрузки: Func<TResult>, Func<T1,T2,...,T16,TResult>)
   - **Суть**: Делегат с возвращаемым значением. Универсальный для вычислений и преобразований.
   - **Параметры**: До 16 input + TResult (output).
   - **Пример**:
     ```csharp
     using System;

     class Program
     {
         static int Add(int a, int b) => a + b;

         static void Main()
         {
             Func<int, int, int> func = Add; // Или лямбда: func = (a, b) => a + b;
             int result = func(3, 4); // 7

             // В LINQ
             var numbers = new[] { 1, 2, 3 };
             Func<int, int> square = x => x * x;
             var squared = numbers.Select(square).ToArray(); // {1,4,9}
         }
     }
     ```
   - **Использование**: LINQ (Select, Where), async (Func<Task<TResult>>), DI-фабрики (builder.Services.AddScoped<Func<IService>>()). В .NET 8: В минимальных API для handlers.

#### 3. **Predicate<T>**
   - **Суть**: Специализированный Func<T, bool> для фильтрации/проверок (true/false).
   - **Пример**:
     ```csharp
     using System;
     using System.Collections.Generic;

     class Program
     {
         static bool IsEven(int n) => n % 2 == 0;

         static void Main()
         {
             Predicate<int> predicate = IsEven; // Или лямбда: n => n % 2 == 0;
             var numbers = new List<int> { 1, 2, 3, 4 };
             var evens = numbers.FindAll(predicate); // {2,4}
         }
     }
     ```
   - **Использование**: List.FindAll, Array.Find. В EF Core: Для Where-условий в лямбдах.

#### 4. **EventHandler** (и generic EventHandler<TEventArgs>)
   - **Суть**: Стандартный для событий (object sender, EventArgs e). Generic-версия с кастомными args.
   - **Пример**:
     ```csharp
     using System;

     public class Publisher
     {
         public event EventHandler<string> OnMessage; // Generic

         public void Raise(string message)
         {
             OnMessage?.Invoke(this, message); // Null-conditional для multicast
         }
     }

     class Program
     {
         static void Main()
         {
             var pub = new Publisher();
             pub.OnMessage += (sender, msg) => Console.WriteLine($"Событие: {msg}");
             pub.Raise("Hello!"); // Вывод: Событие: Hello!
         }
     }
     ```
   - **Использование**: События в .NET (Button.Click, ASP.NET lifecycle). В SignalR для хабов.

#### 5. **Другие специализированные делегаты**
   - **Comparison<T>**: Для сортировки (int сравнение). Используется в List.Sort.
     ```csharp
     Comparison<int> cmp = (a, b) => a - b; // Асценд
     var list = new List<int> { 3, 1, 2 };
     list.Sort(cmp); // {1,2,3}
     ```
   - **Converter<TInput, TOutput>**: Для преобразований (например, string to int).
     ```csharp
     Converter<string, int> conv = int.Parse;
     int num = conv("123"); // 123
     ```
   - **AsyncCallback**: Для асинхронных операций (IAsyncResult).
     ```csharp
     AsyncCallback callback = result => { /* Обработка завершения */ };
     ```
     Актуально в старом APM (Async Pattern Model), но в .NET Core предпочитают Task-based.

#### 6. **Делегаты для потоков и задач**
   - **ThreadStart**: void () — для Thread.
     ```csharp
     Thread t = new Thread(new ThreadStart(() => Console.WriteLine("Thread!")));
     t.Start();
     ```
   - **ParameterizedThreadStart**: void (object) — с параметром.
   - **WaitCallback**: void (object) — для ThreadPool.QueueUserWorkItem.
   - **TimerCallback**: void (object) — для Timer.

#### 7. **Кастомные делегаты**
   - Определяйте свои для специфичных сценариев.
     ```csharp
     public delegate void CustomDelegate(double value); // void (double)

     CustomDelegate del = x => Console.WriteLine(x * 2);
     del(5.0); // 10
     ```
   - **Совет**: Используйте Action/Func вместо кастомных, если возможно (SRP и читаемость).

#### Дополнительные концепции
- **Multicast**: `del += method;` (цепочка вызовов). Вызов: `del?.Invoke()`. Полезно для событий.
- **Covariance/Contravariance**: `Func<object> = (string s) => s.Length;` (out для возврата).
- **Альтернативы**: Local functions (в методах) или lambdas вместо делегатов для простоты. В .NET 7+ — inline arrays и spans для производительности.
- **Лучшие практики**: Избегайте захвата переменных в лямбдах (closures) для предотвращения leaks. В high-throughput (API) используйте compiled delegates (Expression.Compile()).

На собеседовании: Если спросят разницу Action/Func — "Action для imperative, Func для functional". Приведите пример из практики (например, в контроллере: `services.AddScoped<Func<IUserService, Task<User>>>(... )`). Это покажет понимание функционального стиля в .NET. В .NET 8 делегаты ключевы для performance-critical кода (без boxing).

### Что такое лямбда-выражение в C#?

Лямбда-выражение (lambda expression) в C# — это анонимная функция, которая позволяет создавать делегаты (или expression trees) в компактном синтаксисе. Введено в C# 3.0 (2007), лямбды упрощают функциональное программирование, делая код более читаемым и выразительным. Они — shorthand-нотация для создания экземпляров делегатов (например, `Func<T, TResult>`, `Action<T>` или `Predicate<T>`), без необходимости объявлять именованные методы.

Лямбды часто используются для inline-логики: в LINQ-запросах, асинхронных операциях, событиях и Dependency Injection (DI). Они могут захватывать (capture) переменные из enclosing scope (closures), что делает их мощными для состояний. В backend .NET (ASP.NET Core) лямбды — основа минимальных API, middleware и EF Core-запросов.

Актуальность в .NET 8 (2024): Лямбды оптимизированы для Native AOT (ahead-of-time compilation), интегрированы в Primary Constructors (C# 12) и используются в новых фичах вроде inline arrays. Они снижают аллокации (compilable to delegates без boxing) и поддерживают async/await.

#### Синтаксис лямбда-выражений
Основная форма: `parameters => expression` (expression lambda) или `parameters => { statements; }` (statement lambda).

- **Параметры**: В скобках `(x, y)`. Типы опциональны, если выводимы (C# 10+ улучшило inference).
- **Тело**: Простое выражение (одна строка) или блок `{ }` для множественных statements.
- **Асинхронные**: `async (params) => await ...` (с `Task` возвратом).

Примеры:
1. **Простая лямбда** (замена метода):
   ```csharp
   using System;

   class Program
   {
       static void Main()
       {
           // Лямбда как Func<int, int>
           Func<int, int> square = x => x * x; // Expression lambda
           Console.WriteLine(square(5)); // 25

           // Statement lambda (блок)
           Action<string> greet = name => {
               string message = $"Привет, {name}!";
               Console.WriteLine(message);
           };
           greet("Alice"); // Привет, Alice!
       }
   }
   ```

2. **С захватом переменных (closure)**:
   Лямбда может "захватывать" локальные переменные, делая их доступными внутри.
   ```csharp
   int multiplier = 3;
   Func<int, int> multiply = x => x * multiplier; // Захват multiplier
   Console.WriteLine(multiply(4)); // 12 (multiplier = 3)

   // Внимание: Захват по ссылке для ref-локалов (C# 7+), иначе по значению
   ```

3. **В LINQ** (самое распространённое использование):
   ```csharp
   using System;
   using System.Linq;
   using System.Collections.Generic;

   class Program
   {
       static void Main()
       {
           var numbers = new List<int> { 1, 2, 3, 4, 5 };

           // Фильтрация с Predicate (лямбда)
           var evens = numbers.Where(n => n % 2 == 0).ToList(); // {2,4}

           // Преобразование с Func
           var squared = numbers.Select(n => n * n).ToArray(); // {1,4,9,16,25}

           // Агрегация
           int sum = numbers.Aggregate(0, (acc, n) => acc + n); // 15
       }
   }
   ```
   - В EF Core: `context.Users.Where(u => u.Age > 18)` — лямбда компилируется в SQL.

4. **Асинхронная лямбда**:
   ```csharp
   using System;
   using System.Threading.Tasks;

   static async Task<string> FetchDataAsync() => await Task.FromResult("Data");

   static async Task Main()
   {
       Func<Task<string>> asyncLambda = async () => await FetchDataAsync();
       string result = await asyncLambda(); // "Data"
   }
   ```
   - В ASP.NET Core: Контроллеры с `async (HttpContext ctx) => { ... }`.

#### Связь с делегатами и expression trees
- Лямбды **компилируются в делегаты**: `x => x * 2` становится `Func<int, int>`.
- **Expression Trees**: Если тип — `Expression<Func<T>>` (не `Func<T>`), лямбда строит дерево выражений для runtime-анализа (LINQ to SQL, Dynamic LINQ).
  ```csharp
  Expression<Func<int, int>> expr = x => x * 2;
  // Можно компилировать: var func = expr.Compile(); func(3) => 6
  ```
- Multicast: Лямбды можно комбинировать (`+=`), как делегаты.

#### Преимущества и лучшие практики
- **Преимущества**:
  - **Краткость**: Заменяют verbose методы (например, вместо `public int Square(int x) { return x * x; }`).
  - **Closures**: Доступ к внешним переменным без параметров.
  - **Производительность**: В .NET Core+ лямбды inlined компилятором, минимизируя overhead. Async-лямбды с `ValueTask` снижают аллокации.
  - **Функциональный стиль**: Идеальны для immutable кода и параллелизма (PLINQ).

- **Когда использовать**:
  - Inline-логика в LINQ, events (`button.Click += (s, e) => { ... };`).
  - В DI: `services.AddScoped<Func<IService>>((sp) => sp.GetRequiredService<IService>());`.
  - Async handlers в минимальных API (.NET 6+): `app.MapGet("/users", async (IUserService svc) => await svc.GetUsersAsync());`.

- **Питфаллы и советы**:
  - **Захват переменных**: Может привести к memory leaks (если захват loop-локалов — используйте for вместо foreach в C# 5+). Захватит по ссылке для ref-локалов.
  - **Async**: Избегайте `async void` — возвращайте `Task`.
  - **Альтернативы**: Для сложной логики используйте local functions (C# 7+) вместо лямбд — лучше для debugging (имя, breakpoints).
    ```csharp
    int multiplier = 3;
    int Multiply(int x) => x * multiplier; // Local function, захват как лямбда
    ```
  - **Профилинг**: В high-load API мониторьте аллокации лямбд (dotnet-trace); используйте `ConfiguredTaskAwaitable` для оптимизации.

На собеседовании: Подчеркните, что лямбды — эволюция анонимных методов (C# 2.0), и приведите пример из практики (например, в API-контроллере для фильтрации). Если спросят разницу с методами — "Лямбды для одноразовой логики, методы для reusable". Это покажет понимание функционального программирования в .NET, ключевого для современных backend-приложений.

### Что такое полиморфизм в C# и .NET?

Полиморфизм (polymorphism, от греч. "много форм") — один из фундаментальных принципов объектно-ориентированного программирования (ООП), позволяющий объектам разных классов обрабатываться через единый интерфейс или базовый класс, при этом поведение (метод или свойство) определяется типом объекта в runtime или compile-time. Это обеспечивает гибкость, расширяемость и абстракцию: клиентский код работает с базовым типом, не зная деталей реализации.

В C#/.NET полиморфизм реализуется через **наследование**, **интерфейсы** и **делегаты**. Он тесно связан с SOLID-принципами (особенно LSP — Liskov Substitution Principle), Dependency Injection (DI) и архитектурами вроде CQRS или DDD. Актуальность в .NET 8 (2024): Полиморфизм оптимизирован для Native AOT (ahead-of-time compilation), где virtual calls inlined для производительности. Используется в ASP.NET Core (middleware, контроллеры), EF Core (наследование сущностей, Table Per Hierarchy), Blazor (компоненты) и микросервисах (абстрактные репозитории).

Полиморфизм делится на два вида: **статический (compile-time)** и **динамический (runtime)**.

#### 1. **Статический (Compile-time) Полиморфизм**
   - **Суть**: Разрешается на этапе компиляции. Позволяет иметь несколько методов с одинаковым именем, но разными сигнатурами (параметры, типы).
   - **Реализации**:
     - **Перегрузка методов (Method Overloading)**: Несколько методов в одном классе с одним именем.
     - **Перегрузка операторов (Operator Overloading)**: Для пользовательских типов (например, + для строк).
   - **Пример** (перегрузка методов):
     ```csharp
     using System;

     public class Calculator
     {
         // Перегрузка: разные параметры
         public int Add(int a, int b) => a + b;
         public double Add(double a, double b) => a + b;
         public int Add(int a, int b, int c) => a + b + c; // Три параметра

         // Перегрузка операторов
         public static Complex operator +(Complex c1, Complex c2)
             => new Complex(c1.Real + c2.Real, c1.Imag + c2.Imag);
     }

     public class Complex
     {
         public double Real { get; }
         public double Imag { get; }

         public Complex(double real, double imag) { Real = real; Imag = imag; }
     }

     class Program
     {
         static void Main()
         {
             var calc = new Calculator();
             Console.WriteLine(calc.Add(2, 3)); // 5 (int)
             Console.WriteLine(calc.Add(2.5, 3.7)); // 6.2 (double)

             var c1 = new Complex(1, 2);
             var c2 = new Complex(3, 4);
             var sum = c1 + c2; // Operator overloading
             Console.WriteLine($"{sum.Real}, {sum.Imag}"); // 4, 6
         }
     }
     ```
   - **Использование в .NET**: В контроллерах ASP.NET (перегрузка POST/GET), LINQ (Select с разными типами). В .NET 8 — с Primary Constructors для упрощения.

#### 2. **Динамический (Runtime) Полиморфизм**
   - **Суть**: Разрешается в runtime через виртуальные вызовы. Объект базового типа может вызывать переопределённые методы производного класса.
   - **Реализации**:
     - **Virtual/Override**: Методы в базовом классе помечены `virtual`, в наследнике — `override`.
     - **Абстрактные классы/методы**: `abstract` заставляет наследников реализовывать.
     - **Интерфейсы**: `interface` — контракт без реализации (до C# 8; теперь с default methods).
     - **Generics с Covariance/Contravariance**: Для коллекций (IEnumerable<out T>, IComparer<in T>).
   - **Пример** (virtual/override и интерфейсы):
     ```csharp
     using System;

     // Базовый класс
     public class Shape
     {
         public virtual void Draw() => Console.WriteLine("Рисую фигуру");
         public virtual double Area() => 0;
     }

     // Наследник с override
     public class Circle : Shape
     {
         public double Radius { get; }

         public Circle(double radius) { Radius = radius; }

         public override void Draw() => Console.WriteLine($"Рисую круг радиусом {Radius}");
         public override double Area() => Math.PI * Radius * Radius;
     }

     // Интерфейс (C# 8+ с default method)
     public interface IResizable
     {
         void Resize(double factor);
         double Area() => 0; // Default implementation
     }

     public class Rectangle : Shape, IResizable
     {
         public double Width { get; set; }
         public double Height { get; set; }

         public Rectangle(double w, double h) { Width = w; Height = h; }

         public override void Draw() => Console.WriteLine($"Рисую прямоугольник {Width}x{Height}");
         public override double Area() => Width * Height;

         public void Resize(double factor)
         {
             Width *= factor;
             Height *= factor;
         }
     }

     class Program
     {
         static void Main()
         {
             Shape[] shapes = { new Circle(5), new Rectangle(3, 4) };

             foreach (var shape in shapes)
             {
                 shape.Draw(); // Полиморфизм: Вызов override в runtime
                 Console.WriteLine($"Площадь: {shape.Area()}");
             }

             // Вывод:
             // Рисую круг радиусом 5
             // Площадь: 78.539...
             // Рисую прямоугольник 3x4
             // Площадь: 12

             // Интерфейс: LSP
             IResizable rect = new Rectangle(2, 3);
             rect.Resize(2); // 4x6
             Console.WriteLine(rect.Area()); // 24 (override)
         }
     }
     ```
   - **Covariance/Contravariance в generics** (C# 4+):
     ```csharp
     // Covariance: IEnumerable<out T> — можно присвоить более derived
     IEnumerable<Animal> animals = new List<Dog>(); // OK, если Animal базовый для Dog
     ```

#### Преимущества и использование в .NET
- **Преимущества**:
  - **Гибкость**: Легко добавлять новые типы без изменения клиентского кода (Open-Closed Principle).
  - **Тестируемость**: Моки интерфейсов (Moq) для unit-тестов.
  - **Производительность**: В .NET 8 virtual calls оптимизированы (devirtualization в JIT/AOT), но избегайте в hot paths — используйте sealed для non-polymorphic.
- **В backend .NET**:
  - **DI**: Инжектируем интерфейсы (`IRepository`) — разные реализации (SQL, InMemory) без изменений в сервисах.
  - **ASP.NET Core**: Контроллеры наследуют `ControllerBase`, middleware через `IMiddleware`.
  - **EF Core**: Полиморфные сущности (TPC/TPT/TPH) для inheritance mapping.
  - **Records (C# 9+)**: Immutable полиморфизм: `public record Shape(string Name); public record Circle(string Name, double Radius) : Shape(Name);`.
  - **Default Interface Methods (C# 8)**: Полиморфизм без базового класса, полезно для extensions (например, ILogger с defaults).

#### Питфаллы и советы
- **Проблемы**: Слишком глубокое наследование (fragile base class); предпочитайте композицию (Composition over Inheritance).
- **Актуальные фичи**: В .NET 8 используйте `sealed override` для безопасности, `required` members в records для полиморфных конструкторов.
- **На собеседовании**: Свяжите с LSP ("Производный класс должен заменять базовый без ошибок"). Приведите пример из практики: "В API-сервисе IEmailService с реализациями SmtpEmailService и MockEmailService для тестов". Если спросят разницу — "Статический для overloads (быстро), динамический для runtime (гибко)". Это покажет глубокое понимание ООП в enterprise .NET.
