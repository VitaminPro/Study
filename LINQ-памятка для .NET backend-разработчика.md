# LINQ Полная Памятка для .NET разработчика

## Основные концепции

**LINQ (Language Integrated Query)** - технология для выполнения запросов к данным в C# с использованием SQL-подобного синтаксиса.

### Два синтаксиса:
- **Query Syntax** (SQL-подобный): `from x in collection select x`
- **Method Syntax** (цепочка методов): `collection.Where(x => x > 0).Select(x => x)`

### Deferred Execution (Отложенное выполнение)
```csharp
var numbers = new[] { 1, 2, 3, 4, 5 };
var query = numbers.Where(x => x > 2); // Запрос НЕ выполняется здесь

// Запрос выполняется только при итерации:
foreach (var num in query) { } // Здесь выполняется
var list = query.ToList();     // Или здесь
```

---

## 1. Фильтрация

### Where - фильтрация элементов
```csharp
var numbers = new[] { 1, 2, 3, 4, 5, 6 };

// Method syntax
var even = numbers.Where(x => x % 2 == 0).ToList();
// Вывод: [2, 4, 6]

// Query syntax  
var evenQuery = (from n in numbers 
                where n % 2 == 0 
                select n).ToList();
// Вывод: [2, 4, 6]

// Несколько условий
var filtered = numbers.Where(x => x > 2 && x < 6).ToList();
// Вывод: [3, 4, 5]

// С индексом
var withIndex = numbers.Where((value, index) => index % 2 == 0).ToList();
// Вывод: [1, 3, 5] (элементы на четных позициях)
```

### OfType - фильтрация по типу
```csharp
var mixed = new object[] { 1, "hello", 2.5, "world", 3, null };

var strings = mixed.OfType<string>().ToList();
// Вывод: ["hello", "world"]

var integers = mixed.OfType<int>().ToList();
// Вывод: [1, 3]

var doubles = mixed.OfType<double>().ToList();
// Вывод: [2.5]
```

---

## 2. Проекция (преобразование)

### Select - преобразование каждого элемента
```csharp
var people = new[] 
{
    new { Name = "John", Age = 25, City = "New York" },
    new { Name = "Jane", Age = 30, City = "London" },
    new { Name = "Bob", Age = 35, City = "Paris" }
};

// Простая проекция
var names = people.Select(p => p.Name).ToList();
// Вывод: ["John", "Jane", "Bob"]

// Вычисления
var ages10Years = people.Select(p => p.Age + 10).ToList();
// Вывод: [35, 40, 45]

// Анонимный тип
var info = people.Select(p => new { 
    p.Name, 
    IsAdult = p.Age >= 18,
    Location = p.City.ToUpper()
}).ToList();
// Вывод: [
//   { Name = "John", IsAdult = true, Location = "NEW YORK" },
//   { Name = "Jane", IsAdult = true, Location = "LONDON" },
//   { Name = "Bob", IsAdult = true, Location = "PARIS" }
// ]

// С индексом
var indexed = people.Select((p, index) => $"{index + 1}: {p.Name}").ToList();
// Вывод: ["1: John", "2: Jane", "3: Bob"]

// Строки
var words = new[] { "hello", "world", "linq" };
var uppercased = words.Select(w => w.ToUpper()).ToList();
// Вывод: ["HELLO", "WORLD", "LINQ"]
```

### SelectMany - развертка коллекций (flatten)
```csharp
var groups = new[]
{
    new { Name = "Group1", Items = new[] { 1, 2, 3 } },
    new { Name = "Group2", Items = new[] { 4, 5, 6 } },
    new { Name = "Group3", Items = new[] { 7, 8 } }
};

// Простая развертка
var allItems = groups.SelectMany(g => g.Items).ToList();
// Вывод: [1, 2, 3, 4, 5, 6, 7, 8]

// С трансформацией
var itemsWithGroup = groups.SelectMany(
    g => g.Items, 
    (group, item) => new { Group = group.Name, Item = item }
).ToList();
// Вывод: [
//   { Group = "Group1", Item = 1 },
//   { Group = "Group1", Item = 2 },
//   { Group = "Group1", Item = 3 },
//   { Group = "Group2", Item = 4 },
//   ...
// ]

// Работа со строками
var sentences = new[] { "Hello world", "LINQ is great", "C# rocks" };
var allWords = sentences.SelectMany(s => s.Split(' ')).ToList();
// Вывод: ["Hello", "world", "LINQ", "is", "great", "C#", "rocks"]
```

---

## 3. Группировка

### GroupBy - группировка по ключу
```csharp
var people = new[]
{
    new { Name = "John", Department = "IT", Salary = 5000, Age = 25 },
    new { Name = "Jane", Department = "IT", Salary = 6000, Age = 30 },
    new { Name = "Bob", Department = "HR", Salary = 4000, Age = 35 },
    new { Name = "Alice", Department = "HR", Salary = 4500, Age = 28 },
    new { Name = "Tom", Department = "Marketing", Salary = 5500, Age = 32 }
};

// Простая группировка
var byDept = people.GroupBy(p => p.Department).ToList();
// Каждый элемент в byDept это IGrouping<string, AnonymousType>

foreach (var group in byDept)
{
    Console.WriteLine($"Department: {group.Key}");
    foreach (var person in group)
        Console.WriteLine($"  {person.Name} - {person.Salary}");
}
// Вывод:
// Department: IT
//   John - 5000
//   Jane - 6000
// Department: HR  
//   Bob - 4000
//   Alice - 4500
// Department: Marketing
//   Tom - 5500

// Группировка с агрегацией
var deptSummary = people
    .GroupBy(p => p.Department)
    .Select(g => new 
    {
        Department = g.Key,
        Count = g.Count(),
        TotalSalary = g.Sum(p => p.Salary),
        AvgSalary = g.Average(p => p.Salary),
        MinAge = g.Min(p => p.Age),
        MaxAge = g.Max(p => p.Age),
        People = g.Select(p => p.Name).ToList()
    }).ToList();
// Вывод: [
//   { Department = "IT", Count = 2, TotalSalary = 11000, AvgSalary = 5500, 
//     MinAge = 25, MaxAge = 30, People = ["John", "Jane"] },
//   { Department = "HR", Count = 2, TotalSalary = 8500, AvgSalary = 4250,
//     MinAge = 28, MaxAge = 35, People = ["Bob", "Alice"] },
//   { Department = "Marketing", Count = 1, TotalSalary = 5500, AvgSalary = 5500,
//     MinAge = 32, MaxAge = 32, People = ["Tom"] }
// ]

// Группировка по нескольким ключам
var byDeptAndAgeGroup = people
    .GroupBy(p => new { 
        p.Department, 
        AgeGroup = p.Age < 30 ? "Young" : "Senior" 
    })
    .Select(g => new {
        g.Key.Department,
        g.Key.AgeGroup,
        Count = g.Count(),
        People = g.Select(p => p.Name).ToList()
    }).ToList();
// Вывод: [
//   { Department = "IT", AgeGroup = "Young", Count = 1, People = ["John"] },
//   { Department = "IT", AgeGroup = "Senior", Count = 1, People = ["Jane"] },
//   { Department = "HR", AgeGroup = "Senior", Count = 2, People = ["Bob", "Alice"] },
//   { Department = "Marketing", AgeGroup = "Senior", Count = 1, People = ["Tom"] }
// ]
```

### ToLookup - группировка с немедленным выполнением
```csharp
var lookup = people.ToLookup(p => p.Department);
// ToLookup выполняется сразу (не deferred)

var itPeople = lookup["IT"].ToList();
// Вывод: [{ Name = "John", Department = "IT", ... }, { Name = "Jane", Department = "IT", ... }]

var nonExistentDept = lookup["Finance"].ToList();
// Вывод: [] (пустая коллекция, не exception)
```

---

## 4. Сортировка

### OrderBy / OrderByDescending / ThenBy / ThenByDescending
```csharp
var people = new[]
{
    new { Name = "John", Age = 25, Salary = 5000 },
    new { Name = "Jane", Age = 30, Salary = 4000 },
    new { Name = "Bob", Age = 25, Salary = 6000 },
    new { Name = "Alice", Age = 30, Salary = 5500 }
};

// Простая сортировка
var byAge = people.OrderBy(p => p.Age).ToList();
// Вывод: [
//   { Name = "John", Age = 25, Salary = 5000 },
//   { Name = "Bob", Age = 25, Salary = 6000 },
//   { Name = "Jane", Age = 30, Salary = 4000 },
//   { Name = "Alice", Age = 30, Salary = 5500 }
// ]

var bySalaryDesc = people.OrderByDescending(p => p.Salary).ToList();
// Вывод: [
//   { Name = "Bob", Age = 25, Salary = 6000 },
//   { Name = "Alice", Age = 30, Salary = 5500 },
//   { Name = "John", Age = 25, Salary = 5000 },
//   { Name = "Jane", Age = 30, Salary = 4000 }
// ]

// Множественная сортировка
var sorted = people
    .OrderBy(p => p.Age)              // Сначала по возрасту
    .ThenByDescending(p => p.Salary)  // Потом по зарплате (убывание)
    .ThenBy(p => p.Name)              // Потом по имени
    .ToList();
// Вывод: [
//   { Name = "Bob", Age = 25, Salary = 6000 },   // 25 лет, больше зарплата
//   { Name = "John", Age = 25, Salary = 5000 },  // 25 лет, меньше зарплата
//   { Name = "Alice", Age = 30, Salary = 5500 }, // 30 лет, Alice < Jane
//   { Name = "Jane", Age = 30, Salary = 4000 }   // 30 лет, меньше зарплата
// ]

// Сортировка строк
var words = new[] { "apple", "Banana", "cherry", "Date" };
var sortedWords = words.OrderBy(w => w, StringComparer.OrdinalIgnoreCase).ToList();
// Вывод: ["apple", "Banana", "cherry", "Date"] (игнорирует регистр)
```

### Reverse - обращение порядка
```csharp
var numbers = new[] { 1, 2, 3, 4, 5 };
var reversed = numbers.Reverse().ToList();
// Вывод: [5, 4, 3, 2, 1]
```

---

## 5. Агрегация

### Числовые агрегации
```csharp
var numbers = new[] { 1, 2, 3, 4, 5, 6 };
var prices = new[] { 10.5m, 20.0m, 15.75m, 30.25m };

// Основные агрегации
var sum = numbers.Sum();                    // 21
var avg = numbers.Average();                // 3.5  
var min = numbers.Min();                    // 1
var max = numbers.Max();                    // 6
var count = numbers.Count();                // 6

// С условиями
var countEven = numbers.Count(x => x % 2 == 0);     // 3
var sumLarge = numbers.Where(x => x > 3).Sum();     // 15 (4+5+6)
var avgPrice = prices.Average();                    // 19.125

// LongCount для больших коллекций
var longCount = numbers.LongCount();                // 6L

// Пустые коллекции
var empty = new int[0];
var emptyCount = empty.Count();                     // 0
var emptySum = empty.Sum();                         // 0
// var emptyAvg = empty.Average();                  // Exception!

// Безопасные операции с пустыми коллекциями
var safeAvg = empty.DefaultIfEmpty(0).Average();    // 0

// Агрегация объектов
var products = new[]
{
    new { Name = "Laptop", Price = 1000m, Quantity = 2 },
    new { Name = "Mouse", Price = 25m, Quantity = 5 },
    new { Name = "Keyboard", Price = 75m, Quantity = 3 }
};

var totalValue = products.Sum(p => p.Price * p.Quantity);  // 2350
var avgPrice = products.Average(p => p.Price);             // 366.67
var mostExpensive = products.Max(p => p.Price);            // 1000
var cheapestName = products.OrderBy(p => p.Price).First().Name; // "Mouse"
```

### Aggregate - кастомная агрегация
```csharp
var numbers = new[] { 1, 2, 3, 4, 5 };

// Произведение всех чисел (нет начального значения)
var product = numbers.Aggregate((acc, x) => acc * x);
// Вывод: 120 (1*2*3*4*5)

// С начальным значением
var result = numbers.Aggregate(10, (acc, x) => acc + x);
// Вывод: 25 (10+1+2+3+4+5)

// С трансформацией результата
var formatted = numbers.Aggregate(
    0,                              // seed
    (acc, x) => acc + x,           // accumulator  
    result => $"Sum: {result}"     // result selector
);
// Вывод: "Sum: 15"

// Конкатенация строк
var words = new[] { "Hello", "World", "LINQ", "Rocks" };
var sentence = words.Aggregate((acc, word) => acc + " " + word);
// Вывод: "Hello World LINQ Rocks"

var sentenceWithSeed = words.Aggregate("Start:", (acc, word) => acc + " " + word);
// Вывод: "Start: Hello World LINQ Rocks"

// Поиск максимума (альтернатива Max)
var maxNumber = numbers.Aggregate((max, current) => current > max ? current : max);
// Вывод: 5

// Сложный пример: группировка в словарь
var items = new[] { "apple", "banana", "apricot", "blueberry", "avocado" };
var groupedByFirstLetter = items.Aggregate(
    new Dictionary<char, List<string>>(),
    (dict, item) => {
        var firstLetter = item[0];
        if (!dict.ContainsKey(firstLetter))
            dict[firstLetter] = new List<string>();
        dict[firstLetter].Add(item);
        return dict;
    }
);
// Вывод: { 
//   'a': ["apple", "apricot", "avocado"], 
//   'b': ["banana", "blueberry"] 
// }
```

---

## 6. Множественные операции

### Distinct - уникальные элементы
```csharp
var numbers = new[] { 1, 2, 2, 3, 3, 3, 4, 1, 2 };
var unique = numbers.Distinct().ToList();
// Вывод: [1, 2, 3, 4]

var words = new[] { "hello", "HELLO", "world", "Hello", "WORLD" };
var uniqueWords = words.Distinct().ToList();
// Вывод: ["hello", "HELLO", "world", "Hello", "WORLD"] (учитывает регистр)

var uniqueIgnoreCase = words.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
// Вывод: ["hello", "world"] (игнорирует регистр)

// Distinct для объектов (по ссылке)
var people = new[]
{
    new { Name = "John", Age = 25 },
    new { Name = "John", Age = 25 }, // Разные объекты!
    new { Name = "Jane", Age = 30 }
};
var uniquePeople = people.Distinct().ToList(); 
// Вывод: все 3 объекта (разные ссылки)

// DistinctBy (.NET 6+) - по определенному свойству
// var distinctByName = people.DistinctBy(p => p.Name).ToList();
```

### Union, Intersect, Except
```csharp
var set1 = new[] { 1, 2, 3, 4, 5 };
var set2 = new[] { 3, 4, 5, 6, 7 };
var set3 = new[] { 1, 1, 2, 2, 3, 3 };

// Union - объединение (уникальные элементы)
var union = set1.Union(set2).ToList();
// Вывод: [1, 2, 3, 4, 5, 6, 7]

// Intersect - пересечение
var intersect = set1.Intersect(set2).ToList();
// Вывод: [3, 4, 5]

// Except - разность (элементы из первого, отсутствующие во втором)
var except = set1.Except(set2).ToList();
// Вывод: [1, 2]

var except2 = set2.Except(set1).ToList();
// Вывод: [6, 7]

// Автоматическое удаление дубликатов
var unionWithDuplicates = set3.Union(set1).ToList();
// Вывод: [1, 2, 3, 4, 5] (дубликаты удалены)

// Строки
var fruits1 = new[] { "apple", "banana", "orange" };
var fruits2 = new[] { "banana", "kiwi", "apple", "grape" };

var allFruits = fruits1.Union(fruits2).ToList();
// Вывод: ["apple", "banana", "orange", "kiwi", "grape"]

var commonFruits = fruits1.Intersect(fruits2).ToList();
// Вывод: ["apple", "banana"]
```

### Zip - попарное объединение
```csharp
var numbers = new[] { 1, 2, 3, 4 };
var words = new[] { "one", "two", "three", "four", "five" };

var zipped = numbers.Zip(words, (n, w) => $"{n}: {w}").ToList();
// Вывод: ["1: one", "2: two", "3: three", "4: four"]
// Примечание: остановится на самой короткой последовательности

var coordinates = new[] { 1, 2, 3 };
var values = new[] { 10, 20, 30 };
var points = coordinates.Zip(values, (x, y) => new { X = x, Y = y }).ToList();
// Вывод: [{ X = 1, Y = 10 }, { X = 2, Y = 20 }, { X = 3, Y = 30 }]
```

---

## 7. Соединения (Joins)

### Join - внутреннее соединение
```csharp
var customers = new[]
{
    new { Id = 1, Name = "John", City = "New York" },
    new { Id = 2, Name = "Jane", City = "London" },
    new { Id = 3, Name = "Bob", City = "Paris" }
};

var orders = new[]
{
    new { Id = 101, CustomerId = 1, Product = "Laptop", Amount = 1000 },
    new { Id = 102, CustomerId = 2, Product = "Phone", Amount = 800 },
    new { Id = 103, CustomerId = 1, Product = "Mouse", Amount = 25 },
    new { Id = 104, CustomerId = 4, Product = "Tablet", Amount = 600 } // CustomerId = 4 не существует
};

// Простое соединение
var customerOrders = customers.Join(
    orders,
    customer => customer.Id,          // внешний ключ (customers)
    order => order.CustomerId,        // внутренний ключ (orders)  
    (customer, order) => new          // результат соединения
    {
        CustomerName = customer.Name,
        City = customer.City,
        Product = order.Product,
        Amount = order.Amount,
        OrderId = order.Id
    }
).ToList();

// Вывод: [
//   { CustomerName = "John", City = "New York", Product = "Laptop", Amount = 1000, OrderId = 101 },
//   { CustomerName = "Jane", City = "London", Product = "Phone", Amount = 800, OrderId = 102 },
//   { CustomerName = "John", City = "New York", Product = "Mouse", Amount = 25, OrderId = 103 }
// ]
// Примечание: заказ с CustomerId = 4 исключен (нет такого клиента)
//             клиент Bob исключен (нет заказов)

// Query syntax для Join
var queryJoin = (from c in customers
                 join o in orders on c.Id equals o.CustomerId
                 select new { c.Name, o.Product, o.Amount }).ToList();
// Тот же результат
```

### GroupJoin - левое соединение с группировкой
```csharp
var customersWithOrders = customers.GroupJoin(
    orders,
    customer => customer.Id,
    order => order.CustomerId,
    (customer, customerOrders) => new
    {
        CustomerName = customer.Name,
        City = customer.City,
        Orders = customerOrders.Select(o => new { o.Product, o.Amount }).ToList(),
        TotalOrders = customerOrders.Count(),
        TotalAmount = customerOrders.Sum(o => o.Amount)
    }
).ToList();

// Вывод: [
//   { CustomerName = "John", City = "New York", 
//     Orders = [{ Product = "Laptop", Amount = 1000 }, { Product = "Mouse", Amount = 25 }],
//     TotalOrders = 2, TotalAmount = 1025 },
//   { CustomerName = "Jane", City = "London",
//     Orders = [{ Product = "Phone", Amount = 800 }],
//     TotalOrders = 1, TotalAmount = 800 },
//   { CustomerName = "Bob", City = "Paris",
//     Orders = [], TotalOrders = 0, TotalAmount = 0 }
// ]
// Примечание: все клиенты включены, даже без заказов (Bob)

// Имитация LEFT JOIN
var leftJoin = customers.GroupJoin(
    orders,
    c => c.Id,
    o => o.CustomerId,
    (customer, orders) => new { customer, orders }
)
.SelectMany(
    co => co.orders.DefaultIfEmpty(), // DefaultIfEmpty для клиентов без заказов
    (co, order) => new
    {
        CustomerName = co.customer.Name,
        Product = order?.Product ?? "No orders",
        Amount = order?.Amount ?? 0
    }
).ToList();

// Вывод: [
//   { CustomerName = "John", Product = "Laptop", Amount = 1000 },
//   { CustomerName = "John", Product = "Mouse", Amount = 25 },
//   { CustomerName = "Jane", Product = "Phone", Amount = 800 },
//   { CustomerName = "Bob", Product = "No orders", Amount = 0 }
// ]
```

---

## 8. Проверки и поиск

### Any, All, Contains
```csharp
var numbers = new[] { 2, 4, 6, 8, 10 };
var mixedNumbers = new[] { 1, 2, 3, 4, 5 };
var emptyList = new int[0];

// Any - есть ли хотя бы один элемент (удовлетворяющий условию)
bool hasElements = numbers.Any();                    // true
bool hasOdd = numbers.Any(x => x % 2 == 1);         // false
bool hasEven = mixedNumbers.Any(x => x % 2 == 0);   // true
bool emptyHasAny = emptyList.Any();                  // false

// All - все ли элементы удовлетворяют условию
bool allEven = numbers.All(x => x % 2 == 0);         // true
bool allPositive = mixedNumbers.All(x => x > 0);     // true  
bool allLarge = numbers.All(x => x > 5);             // false (2, 4 не больше 5)
bool emptyAllPositive = emptyList.All(x => x > 0);   // true! (вакуумная истина)

// Contains - содержит ли определенный элемент
bool hasTwo = numbers.Contains(2);                   // true
bool hasSeven = numbers.Contains(7);                 // false

// Практические примеры
var people = new[]
{
    new { Name = "John", Age = 25, IsActive = true },
    new { Name = "Jane", Age = 17, IsActive = true },
    new { Name = "Bob", Age = 30, IsActive = false }
};

bool hasMinors = people.Any(p => p.Age < 18);        // true
bool allAdults = people.All(p => p.Age >= 18);       // false
bool hasActiveUsers = people.Any(p => p.IsActive);   // true
bool allActive = people.All(p => p.IsActive);        // false

// Строки
var words = new[] { "hello", "world", "linq", "rocks" };
bool hasLongWords = words.Any(w => w.Length > 5);    // false
bool allShort = words.All(w => w.Length <= 5);       // true
bool containsLinq = words.Contains("linq");          // true
```

### First, FirstOrDefault, Last, LastOrDefault, Single, SingleOrDefault
```csharp
var numbers = new[] { 1, 2, 3, 4, 5, 6 };
var emptyList = new int[0];
var singleItem = new[] { 42 };
var duplicates = new[] { 1, 2, 2, 3 };

// First - первый элемент (exception если пусто)
var first = numbers.First();                         // 1
var firstEven = numbers.First(x => x % 2 == 0);      // 2
// var firstEmpty = emptyList.First();               // InvalidOperationException!

// FirstOrDefault - первый элемент или значение по умолчанию
var firstOrDefault = numbers.FirstOrDefault();       // 1
var firstEvenOrDefault = numbers.FirstOrDefault(x => x % 2 == 0); // 2
var firstEmptyOrDefault = emptyList.FirstOrDefault(); // 0 (default для int)
var firstLargeOrDefault = numbers.FirstOrDefault(x => x > 10);    // 0

// Last - последний элемент
var last = numbers.Last();                           // 6
var lastOdd = numbers.Last(x => x % 2 == 1);         // 5

// LastOrDefault - последний элемент или значение по умолчанию  
var lastOrDefault = numbers.LastOrDefault();         // 6
var lastEmptyOrDefault = emptyList.LastOrDefault();  // 0

// Single - единственный элемент (exception если 0 или >1)
var single = singleItem.Single();                    // 42
var singleEven = new[] { 2 }.Single(x => x % 2 == 0); // 2
// var multipleSingle = numbers.Single();            // InvalidOperationException! (>1 элемента)
// var emptySingle = emptyList.Single();             // InvalidOperationException! (0 элементов)

// SingleOrDefault - единственный элемент или значение по умолчанию
var singleOrDefault = singleItem.SingleOrDefault();  // 42
var singleEmptyOrDefault = emptyList.SingleOrDefault(); // 0
// var multipleSingleOrDefault = numbers.SingleOrDefault(); // InvalidOperationException! (>1)

// Практические примеры
var users = new[]
{
    new { Id = 1, Name = "John", Email = "john@email.com" },
    new { Id = 2, Name = "Jane", Email = "jane@email.com" },
    new { Id = 3, Name = "Bob", Email = "bob@email.com" }
};

var firstUser = users.First();                       
// { Id = 1, Name = "John", Email = "john@email.com" }

var userById = users.FirstOrDefault(u => u.Id == 2);
// { Id = 2, Name = "Jane", Email = "jane@email.com" }

var nonExistentUser = users.FirstOrDefault(u => u.Id == 99);
// null (для reference types)

var adminUser = users.SingleOrDefault(u => u.Name == "John");
// { Id = 1, Name = "John", Email = "john@email.com" }

// Работа с null для reference types
var strings = new[] { "hello", null, "world" };
var firstString = strings.FirstOrDefault();          // "hello"
var firstNull = strings.FirstOrDefault(s => s == null); // null
```

### ElementAt, ElementAtOrDefault
```csharp
var numbers = new[] { 10, 20, 30, 40, 50 };

var elementAt2 = numbers.ElementAt(2);               // 30 (индекс 2)
var elementAt0 = numbers.ElementAt(0);               // 10 (первый элемент)
// var elementAt10 = numbers.ElementAt(10);          // ArgumentOutOfRangeException!

var elementAtOrDefault = numbers.ElementAtOrDefault(2); // 30
var elementAtOrDefaultLarge = numbers.ElementAtOrDefault(10); // 0 (default)
```

---

## 9. Пропуск и взятие

### Take, Skip, TakeWhile, SkipWhile
```csharp
var numbers = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

// Take - взять первые N элементов
var first3 = numbers.Take(3).ToList();
// Вывод: [1, 2, 3]

var first5 = numbers.Take(5).ToList();
// Вывод: [1, 2, 3, 4, 5]

var takeMore = numbers.Take(15).ToList(); // Больше чем есть
// Вывод: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10] (все элементы)

// Skip - пропустить первые N элементов
var skip3 = numbers.Skip(3).ToList();
// Вывод: [4, 5, 6, 7, 8, 9, 10]

var skipAll = numbers.Skip(10).ToList();
// Вывод: [] (пустая коллекция)

var skipMore = numbers.Skip(15).ToList();
// Вывод: [] (пустая коллекция)

// Пагинация: Skip + Take
var page1 = numbers.Skip(0).Take(3).ToList();  // Страница 1
// Вывод: [1, 2, 3]

var page2 = numbers.Skip(3).Take(3).ToList();  // Страница 2  
// Вывод: [4, 5, 6]

var page3 = numbers.Skip(6).Take(3).ToList();  // Страница 3
// Вывод: [7, 8, 9]

// TakeWhile - брать элементы пока условие истинно
var takeWhileSmall = numbers.TakeWhile(x => x < 5).ToList();
// Вывод: [1, 2, 3, 4] (останавливается на 5)

var takeWhileEven = new[] { 2, 4, 6, 1, 8, 10 }.TakeWhile(x => x % 2 == 0).ToList();
// Вывод: [2, 4, 6] (останавливается на 1, даже если дальше есть четные)

// SkipWhile - пропускать элементы пока условие истинно
var skipWhileSmall = numbers.SkipWhile(x => x < 5).ToList();
// Вывод: [5, 6, 7, 8, 9, 10] (начинает с 5)

var skipWhileOdd = new[] { 1, 3, 5, 2, 7, 9 }.SkipWhile(x => x % 2 == 1).ToList();
// Вывод: [2, 7, 9] (начинает с 2, включает все дальше)

// Практический пример: обработка лога
var logLines = new[]
{
    "INFO: Starting application",
    "INFO: Loading config", 
    "INFO: Connecting to DB",
    "ERROR: Connection failed",
    "INFO: Retrying connection",
    "INFO: Connected successfully"
};

var afterFirstError = logLines.SkipWhile(line => !line.StartsWith("ERROR")).ToList();
// Вывод: [
//   "ERROR: Connection failed",
//   "INFO: Retrying connection", 
//   "INFO: Connected successfully"
// ]

var beforeFirstError = logLines.TakeWhile(line => !line.StartsWith("ERROR")).ToList();
// Вывод: [
//   "INFO: Starting application",
//   "INFO: Loading config",
//   "INFO: Connecting to DB"
// ]
```

### TakeLast, SkipLast (.NET Core 2.0+)
```csharp
var numbers = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

var last3 = numbers.TakeLast(3).ToList();
// Вывод: [8, 9, 10]

var skipLast2 = numbers.SkipLast(2).ToList();
// Вывод: [1, 2, 3, 4, 5, 6, 7, 8]
```

---

## 10. Преобразование коллекций

### ToArray, ToList, ToDictionary, ToHashSet, ToLookup
```csharp
// Исходные данные
var numbers = Enumerable.Range(1, 5); // IEnumerable<int>: 1, 2, 3, 4, 5

// ToArray - в массив
var array = numbers.ToArray();
// Вывод: int[5] { 1, 2, 3, 4, 5 }

// ToList - в список
var list = numbers.ToList();
// Вывод: List<int> { 1, 2, 3, 4, 5 }

var people = new[]
{
    new { Id = 1, Name = "John", Department = "IT", Salary = 5000 },
    new { Id = 2, Name = "Jane", Department = "IT", Salary = 6000 },
    new { Id = 3, Name = "Bob", Department = "HR", Salary = 4000 },
    new { Id = 4, Name = "Alice", Department = "HR", Salary = 4500 }
};

// ToDictionary - в словарь
var peopleDict = people.ToDictionary(p => p.Id, p => p.Name);
// Вывод: Dictionary<int, string> { 
//   { 1, "John" }, 
//   { 2, "Jane" }, 
//   { 3, "Bob" }, 
//   { 4, "Alice" } 
// }

var salaryDict = people.ToDictionary(p => p.Name, p => p.Salary);
// Вывод: Dictionary<string, int> {
//   { "John", 5000 },
//   { "Jane", 6000 },
//   { "Bob", 4000 },
//   { "Alice", 4500 }
// }

// ToDictionary с полным объектом
var fullDict = people.ToDictionary(p => p.Id);
// Вывод: Dictionary<int, AnonymousType> - ключ Id, значение весь объект

// ToHashSet - в множество (уникальные элементы)
var duplicateNumbers = new[] { 1, 2, 2, 3, 3, 3, 4 };
var hashSet = duplicateNumbers.ToHashSet();
// Вывод: HashSet<int> { 1, 2, 3, 4 }

var departments = people.Select(p => p.Department).ToHashSet();
// Вывод: HashSet<string> { "IT", "HR" }

// ToLookup - группировка в Lookup (один ключ -> много значений)
var departmentLookup = people.ToLookup(p => p.Department, p => p.Name);
// Вывод: ILookup<string, string>

var itPeople = departmentLookup["IT"].ToList();
// Вывод: ["John", "Jane"]

var hrPeople = departmentLookup["HR"].ToList();
// Вывод: ["Bob", "Alice"]

var nonExistentDept = departmentLookup["Marketing"].ToList();
// Вывод: [] (пустая коллекция, не исключение)
```

### Cast, OfType - преобразование типов
```csharp
// Исходная коллекция object[]
var mixed = new object[] { 1, 2.5, "hello", 3, null, "world", 4.7 };

// OfType - безопасная фильтрация по типу (уже показывали)
var integers = mixed.OfType<int>().ToList();
// Вывод: [1, 3]

var strings = mixed.OfType<string>().ToList();
// Вывод: ["hello", "world"]

var doubles = mixed.OfType<double>().ToList();
// Вывод: [2.5, 4.7]

// Cast - принудительное приведение типов (может бросить исключение)
var onlyNumbers = new object[] { 1, 2, 3, 4, 5 };
var castToInt = onlyNumbers.Cast<int>().ToList();
// Вывод: [1, 2, 3, 4, 5]

// var castMixed = mixed.Cast<int>().ToList(); // InvalidCastException на "hello"!

// Практический пример с ArrayList (legacy code)
var arrayList = new System.Collections.ArrayList { 1, 2, 3, 4, 5 };
var typedList = arrayList.Cast<int>().Where(x => x > 2).ToList();
// Вывод: [3, 4, 5]
```

---

## 11. Полезные методы генерации

### Enumerable статические методы
```csharp
// Range - последовательность чисел
var range = Enumerable.Range(1, 5).ToList();
// Вывод: [1, 2, 3, 4, 5] (начиная с 1, количество 5)

var range10 = Enumerable.Range(10, 3).ToList();
// Вывод: [10, 11, 12] (начиная с 10, количество 3)

// Repeat - повторение значения
var repeat = Enumerable.Repeat("Hello", 3).ToList();
// Вывод: ["Hello", "Hello", "Hello"]

var repeatNumbers = Enumerable.Repeat(42, 5).ToList();
// Вывод: [42, 42, 42, 42, 42]

// Empty - пустая коллекция нужного типа
var emptyInts = Enumerable.Empty<int>().ToList();
// Вывод: [] (List<int>)

var emptyStrings = Enumerable.Empty<string>().ToList();
// Вывод: [] (List<string>)

// Практические применения
// Создание таблицы умножения
var multiplicationTable = Enumerable.Range(1, 10)
    .SelectMany(i => Enumerable.Range(1, 10)
        .Select(j => new { X = i, Y = j, Product = i * j }))
    .Take(10) // Первые 10 для примера
    .ToList();
// Вывод: [
//   { X = 1, Y = 1, Product = 1 },
//   { X = 1, Y = 2, Product = 2 },
//   { X = 1, Y = 3, Product = 3 },
//   ...
// ]

// Инициализация списка значениями по умолчанию
var defaultValues = Enumerable.Repeat(0, 10).ToList();
// Вывод: [0, 0, 0, 0, 0, 0, 0, 0, 0, 0]

// Создание алфавита
var alphabet = Enumerable.Range('A', 26)
    .Select(x => (char)x)
    .ToList();
// Вывод: ['A', 'B', 'C', ..., 'Z']
```

### DefaultIfEmpty - значение по умолчанию для пустых коллекций
```csharp
var emptyList = new int[0];
var nonEmptyList = new[] { 1, 2, 3 };

// DefaultIfEmpty без параметра (значение по умолчанию типа)
var defaultEmpty = emptyList.DefaultIfEmpty().ToList();
// Вывод: [0] (default для int)

var defaultNonEmpty = nonEmptyList.DefaultIfEmpty().ToList();
// Вывод: [1, 2, 3] (исходная коллекция не изменилась)

// DefaultIfEmpty с кастомным значением
var customDefault = emptyList.DefaultIfEmpty(99).ToList();
// Вывод: [99]

var customNonEmpty = nonEmptyList.DefaultIfEmpty(99).ToList();
// Вывод: [1, 2, 3] (исходная коллекция)

// Практическое применение: избежание исключений при агрегации
var emptyNumbers = new int[0];
var safeSum = emptyNumbers.DefaultIfEmpty(0).Sum();
// Вывод: 0 (вместо исключения)

var safeAverage = emptyNumbers.DefaultIfEmpty(0).Average();
// Вывод: 0.0 (вместо исключения)
```

---

## 12. Продвинутые сценарии

### Работа с вложенными коллекциями
```csharp
var companies = new[]
{
    new {
        Name = "TechCorp",
        Departments = new[]
        {
            new { Name = "IT", Employees = new[] { "John", "Jane", "Bob" } },
            new { Name = "HR", Employees = new[] { "Alice", "Charlie" } }
        }
    },
    new {
        Name = "DataInc",
        Departments = new[]
        {
            new { Name = "Analytics", Employees = new[] { "David", "Emma" } },
            new { Name = "IT", Employees = new[] { "Frank" } }
        }
    }
};

// Все сотрудники всех компаний
var allEmployees = companies
    .SelectMany(c => c.Departments)
    .SelectMany(d => d.Employees)
    .ToList();
// Вывод: ["John", "Jane", "Bob", "Alice", "Charlie", "David", "Emma", "Frank"]

// Группировка по отделам через все компании
var employeesByDept = companies
    .SelectMany(c => c.Departments)
    .GroupBy(d => d.Name)
    .Select(g => new {
        Department = g.Key,
        AllEmployees = g.SelectMany(d => d.Employees).ToList(),
        CompanyCount = g.Count()
    })
    .ToList();
// Вывод: [
//   { Department = "IT", AllEmployees = ["John", "Jane", "Bob", "Frank"], CompanyCount = 2 },
//   { Department = "HR", AllEmployees = ["Alice", "Charlie"], CompanyCount = 1 },
//   { Department = "Analytics", AllEmployees = ["David", "Emma"], CompanyCount = 1 }
// ]

// Сотрудники с информацией о компании и отделе
var employeesWithContext = companies
    .SelectMany(c => c.Departments,
        (company, dept) => new { company, dept })
    .SelectMany(cd => cd.dept.Employees,
        (cd, employee) => new {
            Employee = employee,
            Company = cd.company.Name,
            Department = cd.dept.Name
        })
    .ToList();
// Вывод: [
//   { Employee = "John", Company = "TechCorp", Department = "IT" },
//   { Employee = "Jane", Company = "TechCorp", Department = "IT" },
//   ...
// ]
```

### Условная агрегация
```csharp
var sales = new[]
{
    new { Product = "Laptop", Category = "Electronics", Amount = 1000, Quarter = "Q1" },
    new { Product = "Phone", Category = "Electronics", Amount = 800, Quarter = "Q1" },
    new { Product = "Desk", Category = "Furniture", Amount = 300, Quarter = "Q1" },
    new { Product = "Tablet", Category = "Electronics", Amount = 600, Quarter = "Q2" },
    new { Product = "Chair", Category = "Furniture", Amount = 150, Quarter = "Q2" }
};

// Условная агрегация с Where
var q1Electronics = sales
    .Where(s => s.Quarter == "Q1" && s.Category == "Electronics")
    .Sum(s => s.Amount);
// Вывод: 1800

// Множественная условная агрегация
var summary = sales
    .GroupBy(s => s.Category)
    .Select(g => new {
        Category = g.Key,
        TotalAmount = g.Sum(s => s.Amount),
        Q1Amount = g.Where(s => s.Quarter == "Q1").Sum(s => s.Amount),
        Q2Amount = g.Where(s => s.Quarter == "Q2").Sum(s => s.Amount),
        ProductCount = g.Count(),
        AvgAmount = g.Average(s => s.Amount)
    })
    .ToList();
// Вывод: [
//   { Category = "Electronics", TotalAmount = 2400, Q1Amount = 1800, Q2Amount = 600, ProductCount = 3, AvgAmount = 800 },
//   { Category = "Furniture", TotalAmount = 450, Q1Amount = 300, Q2Amount = 150, ProductCount = 2, AvgAmount = 225 }
// ]

// Пивот-таблица
var pivot = sales
    .GroupBy(s => s.Category)
    .ToDictionary(
        g => g.Key,
        g => g.GroupBy(s => s.Quarter)
              .ToDictionary(qg => qg.Key, qg => qg.Sum(s => s.Amount))
    );
// Вывод: {
//   "Electronics": { "Q1": 1800, "Q2": 600 },
//   "Furniture": { "Q1": 300, "Q2": 150 }
// }
```

### Работа со строками
```csharp
var text = "The quick brown fox jumps over the lazy dog. The fox was very quick!";

// Анализ слов
var wordAnalysis = text
    .ToLower()
    .Split(new[] { ' ', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
    .GroupBy(word => word)
    .Select(g => new {
        Word = g.Key,
        Count = g.Count(),
        Length = g.Key.Length
    })
    .OrderByDescending(x => x.Count)
    .ThenByDescending(x => x.Length)
    .ToList();
// Вывод: [
//   { Word = "the", Count = 2, Length = 3 },
//   { Word = "fox", Count = 2, Length = 3 },
//   { Word = "quick", Count = 2, Length = 5 },
//   { Word = "brown", Count = 1, Length = 5 },
//   ...
// ]

// Найти самые длинные слова
var longestWords = text
    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
    .Select(w => w.Trim('.', '!', '?'))
    .GroupBy(w => w.Length)
    .OrderByDescending(g => g.Key)
    .First()
    .Distinct()
    .ToList();
// Найдет слова максимальной длины

// Статистика по длине слов
var lengthStats = text
    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
    .Select(w => w.Trim('.', '!', '?').Length)
    .GroupBy(len => len)
    .Select(g => new {
        Length = g.Key,
        Count = g.Count(),
        Percentage = g.Count() * 100.0 / text.Split(' ').Length
    })
    .OrderBy(x => x.Length)
    .ToList();
```

---

## 13. Советы по производительности и лучшие практики

### Deferred Execution (Отложенное выполнение)
```csharp
var numbers = new[] { 1, 2, 3, 4, 5 };

// Запрос создается, но НЕ выполняется
var query = numbers.Where(x => {
    Console.WriteLine($"Checking {x}");
    return x > 2;
});

Console.WriteLine("Query created");

// Запрос выполняется ТУТ при первой итерации
foreach (var num in query) {
    Console.WriteLine($"Result: {num}");
}

// Вывод:
// Query created
// Checking 1
// Checking 2  
// Checking 3
// Result: 3
// Checking 4
// Result: 4
// Checking 5
// Result: 5

// Multiple enumeration - запрос выполняется КАЖДЫЙ раз
query.ToList(); // Выполняется снова!
query.Count();  // И снова!

// Решение: материализация
var materializedQuery = query.ToList(); // Выполняется один раз
var count = materializedQuery.Count;    // Быстро, работает с готовым списком
```

### Оптимизация производительности
```csharp
var largeList = Enumerable.Range(1, 1000000);

// ❌ Плохо - множественная материализация
var bad = largeList.Where(x => x > 500000).ToList()
                   .Select(x => x * 2).ToList()
                   .OrderBy(x => x).ToList()
                   .Take(10).ToList();

// ✅ Хорошо - одна материализация в конце
var good = largeList.Where(x => x > 500000)
                    .Select(x => x * 2)
                    .OrderBy(x => x)
                    .Take(10)
                    .ToList();

// ✅ Еще лучше - фильтрация в начале
var better = largeList.Where(x => x > 500000)     // Сначала фильтруем
                      .Take(10)                   // Берем только нужное количество
                      .Select(x => x * 2)         // Потом трансформируем
                      .OrderBy(x => x)            // Сортируем меньший набор
                      .ToList();

// Проверка существования
// ❌ Плохо
bool hasLargeNumbers = largeList.Where(x => x > 500000).Count() > 0;

// ✅ Хорошо
bool hasLargeNumbersGood = largeList.Any(x => x > 500000);

// Получение первого элемента
// ❌ Плохо
var firstLarge = largeList.Where(x => x > 500000).First();

// ✅ Хорошо  
var firstLargeGood = largeList.First(x => x > 500000);
```

### Работа с null и безопасность
```csharp
var people = new[]
{
    new { Name = "John", Email = "john@email.com" },
    new { Name = "Jane", Email = (string)null },
    new { Name = "Bob", Email = "bob@email.com" }
};

// Безопасная работа с null
var validEmails = people
    .Where(p => !string.IsNullOrEmpty(p.Email))
    .Select(p => p.Email.ToLower())
    .ToList();
// Вывод: ["john@email.com", "bob@email.com"]

// Использование ?. для безопасности
var emailLengths = people
    .Select(p => new {
        Name = p.Name,
        EmailLength = p.Email?.Length ?? 0
    })
    .ToList();
// Вывод: [
//   { Name = "John", EmailLength = 15 },
//   { Name = "Jane", EmailLength = 0 },
//   { Name = "Bob", EmailLength = 14 }
// ]
```

### Лучшие практики
```csharp
// 1. Используйте осмысленные имена переменных в лямбдах
var products = GetProducts();

// ❌ Плохо
var result1 = products.Where(x => x.Price > 100).Select(x => x.Name);

// ✅ Хорошо  
var expensiveProductNames = products
    .Where(product => product.Price > 100)
    .Select(product => product.Name);

// 2. Разбивайте сложные запросы на этапы
// ❌ Плохо - сложно читать и отлаживать
var complex = products
    .Where(p => p.Category == "Electronics" && p.Price > 500 && p.InStock)
    .GroupBy(p => p.Brand)
    .Where(g => g.Count() > 3)
    .Select(g => new { Brand = g.Key, AvgPrice = g.Average(p => p.Price) })
    .OrderByDescending(x => x.AvgPrice);

// ✅ Хорошо - пошаговое построение
var expensiveElectronics = products
    .Where(p => p.Category == "Electronics")
    .Where(p => p.Price > 500)
    .Where(p => p.InStock);

var popularBrands = expensiveElectronics
    .GroupBy(p => p.Brand)
    .Where(g => g.Count() > 3);

var brandAverages = popularBrands
    .Select(g => new { 
        Brand = g.Key, 
        AvgPrice = g.Average(p => p.Price),
        ProductCount = g.Count()
    })
    .OrderByDescending(x => x.AvgPrice)
    .ToList();

// 3. Кешируйте результаты дорогих операций
var processedData = rawData
    .Where(item => ExpensiveOperation(item))  // Дорогая операция
    .ToList();                               // Материализуем один раз

var result1 = processedData.Where(item => item.Type == "A");
var result2 = processedData.Where(item => item.Value > 100);
```
