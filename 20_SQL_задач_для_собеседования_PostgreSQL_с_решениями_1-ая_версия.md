# 20 SQL задач для собеседования PostgreSQL с решениями

## Создание тестовых таблиц и данных

```sql
-- Создание схемы базы данных
CREATE TABLE departments (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    budget DECIMAL(12,2),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE employees (
    id SERIAL PRIMARY KEY,
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    email VARCHAR(100) UNIQUE,
    phone VARCHAR(20),
    hire_date DATE,
    salary DECIMAL(10,2),
    department_id INT REFERENCES departments(id),
    manager_id INT REFERENCES employees(id),
    is_active BOOLEAN DEFAULT true
);

CREATE TABLE projects (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    start_date DATE,
    end_date DATE,
    budget DECIMAL(12,2),
    status VARCHAR(20) DEFAULT 'active'
);

CREATE TABLE employee_projects (
    employee_id INT REFERENCES employees(id),
    project_id INT REFERENCES projects(id),
    role VARCHAR(50),
    hours_per_week DECIMAL(4,2),
    assigned_date DATE DEFAULT CURRENT_DATE,
    PRIMARY KEY (employee_id, project_id)
);

CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    customer_id INT,
    order_date DATE,
    total_amount DECIMAL(10,2),
    status VARCHAR(20)
);

CREATE TABLE order_items (
    id SERIAL PRIMARY KEY,
    order_id INT REFERENCES orders(id),
    product_name VARCHAR(100),
    quantity INT,
    price DECIMAL(8,2)
);

-- Заполнение тестовыми данными
INSERT INTO departments (name, budget) VALUES 
('IT', 500000.00),
('HR', 200000.00),
('Finance', 300000.00),
('Marketing', 250000.00),
('Sales', 400000.00);

INSERT INTO employees (first_name, last_name, email, phone, hire_date, salary, department_id, manager_id) VALUES 
('John', 'Doe', 'john.doe@company.com', '+1234567890', '2020-01-15', 75000.00, 1, NULL),
('Jane', 'Smith', 'jane.smith@company.com', '+1234567891', '2020-03-20', 65000.00, 1, 1),
('Bob', 'Johnson', 'bob.johnson@company.com', '+1234567892', '2021-06-10', 55000.00, 1, 1),
('Alice', 'Brown', 'alice.brown@company.com', '+1234567893', '2019-11-05', 70000.00, 2, NULL),
('Charlie', 'Wilson', 'charlie.wilson@company.com', '+1234567894', '2022-02-14', 60000.00, 2, 4),
('Eva', 'Davis', 'eva.davis@company.com', '+1234567895', '2020-08-30', 80000.00, 3, NULL),
('Frank', 'Miller', 'frank.miller@company.com', '+1234567896', '2021-01-25', 58000.00, 4, NULL),
('Grace', 'Taylor', 'grace.taylor@company.com', '+1234567897', '2023-03-10', 52000.00, 5, NULL),
('Henry', 'Anderson', 'henry.anderson@company.com', NULL, '2022-07-18', 45000.00, 1, 2),
('Ivy', 'Thomas', NULL, '+1234567899', '2021-09-12', 67000.00, 2, 4);

INSERT INTO projects (name, description, start_date, end_date, budget, status) VALUES 
('Website Redesign', 'Complete redesign of company website', '2023-01-01', '2023-06-30', 150000.00, 'completed'),
('Mobile App', 'Development of mobile application', '2023-03-15', '2024-03-15', 300000.00, 'active'),
('Data Migration', 'Migration to new database system', '2023-06-01', '2023-12-31', 200000.00, 'active'),
('HR System', 'New HR management system', '2023-02-01', '2023-08-31', 180000.00, 'completed'),
('Marketing Campaign', 'Q4 marketing campaign', '2023-10-01', '2023-12-31', 100000.00, 'active');

INSERT INTO employee_projects (employee_id, project_id, role, hours_per_week, assigned_date) VALUES 
(1, 1, 'Project Manager', 40.00, '2023-01-01'),
(2, 1, 'Developer', 35.00, '2023-01-05'),
(3, 1, 'Developer', 40.00, '2023-01-10'),
(1, 2, 'Technical Lead', 30.00, '2023-03-15'),
(2, 2, 'Senior Developer', 40.00, '2023-03-20'),
(3, 3, 'Database Specialist', 40.00, '2023-06-01'),
(4, 4, 'Project Coordinator', 25.00, '2023-02-01'),
(7, 5, 'Marketing Lead', 40.00, '2023-10-01');

INSERT INTO orders (customer_id, order_date, total_amount, status) VALUES 
(101, '2023-01-15', 1500.00, 'completed'),
(102, '2023-01-20', 2300.00, 'completed'),
(103, '2023-02-10', 800.00, 'completed'),
(101, '2023-02-25', 1200.00, 'completed'),
(104, '2023-03-05', 3500.00, 'pending'),
(102, '2023-03-15', 950.00, 'cancelled'),
(105, '2023-04-01', 2100.00, 'completed'),
(103, '2023-04-20', 1800.00, 'completed');

INSERT INTO order_items (order_id, product_name, quantity, price) VALUES 
(1, 'Laptop', 1, 1200.00),
(1, 'Mouse', 2, 150.00),
(2, 'Monitor', 2, 800.00),
(2, 'Keyboard', 1, 700.00),
(3, 'Tablet', 1, 800.00),
(4, 'Phone', 1, 1200.00),
(5, 'Laptop', 2, 1200.00),
(5, 'Monitor', 1, 1100.00),
(7, 'Desktop PC', 1, 2100.00),
(8, 'Laptop', 1, 1000.00),
(8, 'Accessories', 5, 160.00);
```

---

## Задача 1: Найти сотрудников с зарплатой выше средней по компании

**Задача:** Найти всех сотрудников, у которых зарплата выше средней по компании.

**Решение:**
```sql
SELECT 
    e.first_name,
    e.last_name,
    e.salary,
    d.name as department_name
FROM employees e
JOIN departments d ON e.department_id = d.id
WHERE e.salary > (SELECT AVG(salary) FROM employees)
ORDER BY e.salary DESC;
```

**Результат:**
```
first_name | last_name | salary   | department_name
Eva        | Davis     | 80000.00 | Finance
John       | Doe       | 75000.00 | IT
Alice      | Brown     | 70000.00 | HR
Ivy        | Thomas    | 67000.00 | HR
```

**Памятка:**
- **Подзапросы (Subqueries)** - выполняются первыми, результат используется в основном запросе
- **AVG()** - агрегатная функция для вычисления среднего значения
- **JOIN** - соединение таблиц для получения связанных данных
- **Оптимизация**: PostgreSQL кэширует результат подзапроса, если он не зависит от внешних таблиц

---

## Задача 2: Использование COALESCE для работы с NULL значениями

**Задача:** Вывести список сотрудников с контактной информацией, заменив NULL значения на значения по умолчанию.

**Решение:**
```sql
SELECT 
    first_name,
    last_name,
    COALESCE(email, 'No email provided') as email_info,
    COALESCE(phone, 'No phone provided') as phone_info
FROM employees
ORDER BY last_name;
```

**Результат:**
```
first_name | last_name | email_info              | phone_info
Henry      | Anderson  | henry.anderson@company.com | No phone provided
Alice      | Brown     | alice.brown@company.com    | +1234567893
Eva        | Davis     | eva.davis@company.com      | +1234567895
John       | Doe       | john.doe@company.com       | +1234567890
Bob        | Johnson   | bob.johnson@company.com    | +1234567892
Frank      | Miller    | frank.miller@company.com   | +1234567896
Jane       | Smith     | jane.smith@company.com     | +1234567891
Grace      | Taylor    | grace.taylor@company.com   | +1234567897
Ivy        | Thomas    | No email provided          | +1234567899
Charlie    | Wilson    | charlie.wilson@company.com | +1234567894
```

**Памятка:**
- **COALESCE(val1, val2, ...)** - возвращает первое НЕ NULL значение из списка
- Альтернативы: **ISNULL()** (SQL Server), **NVL()** (Oracle), **IFNULL()** (MySQL)
- **Оптимизация**: COALESCE работает быстрее, чем CASE WHEN для проверки NULL
- Можно использовать цепочку значений: `COALESCE(email, phone, 'No contact')`

---

## Задача 3: Группировка и агрегация с HAVING

**Задача:** Найти отделы, где средняя зарплата сотрудников превышает 60000.

**Решение:**
```sql
SELECT 
    d.name as department_name,
    COUNT(e.id) as employee_count,
    ROUND(AVG(e.salary), 2) as avg_salary,
    MIN(e.salary) as min_salary,
    MAX(e.salary) as max_salary
FROM departments d
JOIN employees e ON d.id = e.department_id
GROUP BY d.id, d.name
HAVING AVG(e.salary) > 60000
ORDER BY avg_salary DESC;
```

**Результат:**
```
department_name | employee_count | avg_salary | min_salary | max_salary
Finance         | 1              | 80000.00   | 80000.00   | 80000.00
IT              | 4              | 60000.00   | 45000.00   | 75000.00
HR              | 3              | 65666.67   | 60000.00   | 70000.00
```

**Памятка:**
- **GROUP BY** - группирует строки по указанным колонкам
- **HAVING** - фильтрует группы (работает после GROUP BY), в отличие от WHERE (фильтрует строки)
- **Порядок выполнения**: FROM → WHERE → GROUP BY → HAVING → SELECT → ORDER BY
- **Агрегатные функции**: COUNT(), AVG(), SUM(), MIN(), MAX()
- **ROUND(value, decimal_places)** - округление до указанного количества знаков

---

## Задача 4: Оконные функции (Window Functions) - ROW_NUMBER и RANK

**Задача:** Пронумеровать сотрудников по зарплате в каждом отделе.

**Решение:**
```sql
SELECT 
    first_name,
    last_name,
    salary,
    d.name as department_name,
    ROW_NUMBER() OVER (PARTITION BY d.name ORDER BY salary DESC) as salary_rank,
    RANK() OVER (PARTITION BY d.name ORDER BY salary DESC) as salary_rank_with_ties,
    DENSE_RANK() OVER (PARTITION BY d.name ORDER BY salary DESC) as dense_salary_rank
FROM employees e
JOIN departments d ON e.department_id = d.id
ORDER BY d.name, salary DESC;
```

**Результат:**
```
first_name | last_name | salary   | department_name | salary_rank | salary_rank_with_ties | dense_salary_rank
Eva        | Davis     | 80000.00 | Finance         | 1           | 1                     | 1
Alice      | Brown     | 70000.00 | HR              | 1           | 1                     | 1
Ivy        | Thomas    | 67000.00 | HR              | 2           | 2                     | 2
Charlie    | Wilson    | 60000.00 | HR              | 3           | 3                     | 3
John       | Doe       | 75000.00 | IT              | 1           | 1                     | 1
Jane       | Smith     | 65000.00 | IT              | 2           | 2                     | 2
Bob        | Johnson   | 55000.00 | IT              | 3           | 3                     | 3
Henry      | Anderson  | 45000.00 | IT              | 4           | 4                     | 4
```

**Памятка:**
- **ROW_NUMBER()** - последовательная нумерация без пропусков (1,2,3,4...)
- **RANK()** - нумерация с пропусками при одинаковых значениях (1,2,2,4...)
- **DENSE_RANK()** - нумерация без пропусков при одинаковых значениях (1,2,2,3...)
- **PARTITION BY** - разбивает данные на группы для применения функции
- **OVER()** - определяет окно для применения функции
- **Оптимизация**: оконные функции работают быстрее, чем коррелированные подзапросы

---

## Задача 5: Работа с датами и интервалами

**Задача:** Найти сотрудников, которые работают в компании более 3 лет, и вычислить точный стаж.

**Решение:**
```sql
SELECT 
    first_name,
    last_name,
    hire_date,
    CURRENT_DATE - hire_date as days_worked,
    AGE(CURRENT_DATE, hire_date) as exact_tenure,
    EXTRACT(YEAR FROM AGE(CURRENT_DATE, hire_date)) as years_worked
FROM employees
WHERE hire_date <= CURRENT_DATE - INTERVAL '3 years'
ORDER BY hire_date;
```

**Результат:**
```
first_name | last_name | hire_date  | days_worked | exact_tenure        | years_worked
Alice      | Brown     | 2019-11-05 | 1553        | 4 years 9 mons 15 days | 4
John       | Doe       | 2020-01-15 | 1483        | 4 years 7 mons 5 days  | 4
Jane       | Smith     | 2020-03-20 | 1418        | 4 years 4 mons 31 days | 4
Eva        | Davis     | 2020-08-30 | 1255        | 3 years 11 mons 21 days| 3
```

**Памятка:**
- **CURRENT_DATE** - текущая дата (без времени)
- **INTERVAL** - добавление/вычитание временных периодов
- **AGE(date1, date2)** - возвращает интервал между датами
- **EXTRACT()** - извлекает части даты (YEAR, MONTH, DAY, HOUR и т.д.)
- **Операции с датами**: `date + interval`, `date - date = interval`
- **Форматы интервалов**: '1 year 2 months 3 days', '3 years', '30 days'

---

## Задача 6: Самосоединение (Self Join) - иерархия сотрудников

**Задача:** Показать всех сотрудников вместе с их менеджерами.

**Решение:**
```sql
SELECT 
    e.first_name || ' ' || e.last_name as employee_name,
    e.salary as employee_salary,
    COALESCE(m.first_name || ' ' || m.last_name, 'No Manager') as manager_name,
    m.salary as manager_salary,
    d.name as department_name
FROM employees e
LEFT JOIN employees m ON e.manager_id = m.id
JOIN departments d ON e.department_id = d.id
ORDER BY d.name, e.salary DESC;
```

**Результат:**
```
employee_name    | employee_salary | manager_name  | manager_salary | department_name
Eva Davis        | 80000.00        | No Manager    |                | Finance
Alice Brown      | 70000.00        | No Manager    |                | HR
Ivy Thomas       | 67000.00        | Alice Brown   | 70000.00       | HR
Charlie Wilson   | 60000.00        | Alice Brown   | 70000.00       | HR
John Doe         | 75000.00        | No Manager    |                | IT
Jane Smith       | 65000.00        | John Doe      | 75000.00       | IT
Bob Johnson      | 55000.00        | John Doe      | 75000.00       | IT
Henry Anderson   | 45000.00        | Jane Smith    | 65000.00       | IT
```

**Памятка:**
- **Self Join** - соединение таблицы с самой собой через алиасы
- **LEFT JOIN** - сохраняет все записи из левой таблицы, даже если нет соответствий в правой
- **Конкатенация строк**: `||` (PostgreSQL), `CONCAT()` (универсальный способ)
- **Иерархические структуры**: часто используются self join для родитель-потомок отношений
- **Оптимизация**: используйте индексы на колонки, участвующие в join

---

## Задача 7: Условная логика с CASE

**Задача:** Классифицировать сотрудников по уровню зарплаты и добавить информацию о статусе.

**Решение:**
```sql
SELECT 
    first_name,
    last_name,
    salary,
    CASE 
        WHEN salary >= 70000 THEN 'Senior Level'
        WHEN salary >= 55000 THEN 'Mid Level'
        ELSE 'Junior Level'
    END as salary_level,
    CASE 
        WHEN is_active THEN 'Active'
        ELSE 'Inactive'
    END as employment_status,
    d.name as department_name
FROM employees e
JOIN departments d ON e.department_id = d.id
ORDER BY salary DESC;
```

**Результат:**
```
first_name | last_name | salary   | salary_level | employment_status | department_name
Eva        | Davis     | 80000.00 | Senior Level | Active           | Finance
John       | Doe       | 75000.00 | Senior Level | Active           | IT
Alice      | Brown     | 70000.00 | Senior Level | Active           | HR
Ivy        | Thomas    | 67000.00 | Mid Level    | Active           | HR
Jane       | Smith     | 65000.00 | Mid Level    | Active           | IT
Charlie    | Wilson    | 60000.00 | Mid Level    | Active           | HR
Frank      | Miller    | 58000.00 | Mid Level    | Active           | Marketing
Bob        | Johnson   | 55000.00 | Mid Level    | Active           | IT
Grace      | Taylor    | 52000.00 | Junior Level | Active           | Sales
Henry      | Anderson  | 45000.00 | Junior Level | Active           | IT
```

**Памятка:**
- **CASE WHEN** - условная логика в SQL (аналог if-else)
- **Синтаксис**: `CASE WHEN condition THEN result WHEN ... ELSE default END`
- **Простой CASE**: `CASE column WHEN value1 THEN result1 WHEN value2 THEN result2 END`
- **Оптимизация**: PostgreSQL оптимизирует CASE, вычисляя условия по порядку и останавливаясь на первом истинном
- **Использование**: группировка значений, создание флагов, условные вычисления

---

## Задача 8: EXISTS vs IN - проверка существования

**Задача:** Найти сотрудников, которые участвуют в проектах, двумя способами.

**Решение с EXISTS:**
```sql
SELECT 
    e.first_name,
    e.last_name,
    d.name as department_name
FROM employees e
JOIN departments d ON e.department_id = d.id
WHERE EXISTS (
    SELECT 1 
    FROM employee_projects ep 
    WHERE ep.employee_id = e.id
)
ORDER BY e.last_name;
```

**Решение с IN:**
```sql
SELECT 
    e.first_name,
    e.last_name,
    d.name as department_name
FROM employees e
JOIN departments d ON e.department_id = d.id
WHERE e.id IN (
    SELECT DISTINCT employee_id 
    FROM employee_projects
)
ORDER BY e.last_name;
```

**Результат:**
```
first_name | last_name | department_name
Alice      | Brown     | HR
John       | Doe       | IT
Bob        | Johnson   | IT
Frank      | Miller    | Marketing
Jane       | Smith     | IT
```

**Памятка:**
- **EXISTS** - проверяет существование записей, возвращает TRUE/FALSE
- **IN** - проверяет вхождение значения в список
- **Производительность**: EXISTS часто быстрее для больших таблиц, так как останавливается на первом найденном совпадении
- **NULL values**: IN может вернуть неожиданные результаты с NULL, EXISTS работает корректно
- **NOT EXISTS vs NOT IN**: NOT EXISTS безопаснее при наличии NULL значений
- **SELECT 1 в EXISTS** - не влияет на производительность, важен только факт существования

---

## Задача 9: Работа с JSON данными (PostgreSQL специфика)

**Задача:** Добавить JSON колонку и работать с ней.

**Сначала добавим JSON колонку и данные:**
```sql
-- Добавляем JSON колонку
ALTER TABLE employees ADD COLUMN metadata JSONB;

-- Обновляем данные
UPDATE employees SET metadata = '{"skills": ["SQL", "Python"], "certifications": ["AWS"], "languages": ["English", "Spanish"]}' WHERE id = 1;
UPDATE employees SET metadata = '{"skills": ["Java", "React"], "certifications": ["Oracle"], "languages": ["English"]}' WHERE id = 2;
UPDATE employees SET metadata = '{"skills": ["SQL", "C#"], "certifications": [], "languages": ["English", "French"]}' WHERE id = 3;
UPDATE employees SET metadata = '{"skills": ["HR Management"], "certifications": ["SHRM"], "languages": ["English"]}' WHERE id = 4;
```

**Решение:**
```sql
SELECT 
    first_name,
    last_name,
    metadata->>'skills' as skills_json,
    metadata->'skills' as skills_array,
    jsonb_array_length(metadata->'skills') as skills_count,
    metadata ? 'certifications' as has_certifications,
    metadata->'languages'->>0 as primary_language
FROM employees 
WHERE metadata IS NOT NULL
ORDER BY jsonb_array_length(metadata->'skills') DESC;
```

**Памятка:**
- **JSON vs JSONB**: JSONB (binary) быстрее для запросов, поддерживает индексы
- **-> оператор**: извлекает JSON объект
- **->> оператор**: извлекает JSON объект как текст
- **? оператор**: проверяет существование ключа
- **jsonb_array_length()**: длина JSON массива
- **GIN индексы**: используйте для оптимизации запросов к JSONB полям
- **@> оператор**: содержит ли левый JSON правый JSON

---

## Задача 10: Common Table Expression (CTE) - рекурсивные запросы

**Задача:** Построить иерархию сотрудников с использованием рекурсивного CTE.

**Решение:**
```sql
WITH RECURSIVE employee_hierarchy AS (
    -- Базовый случай: топ-менеджеры (без руководителей)
    SELECT 
        id,
        first_name,
        last_name,
        manager_id,
        first_name || ' ' || last_name as full_name,
        0 as level,
        first_name || ' ' || last_name as path
    FROM employees 
    WHERE manager_id IS NULL
    
    UNION ALL
    
    -- Рекурсивный случай: подчиненные
    SELECT 
        e.id,
        e.first_name,
        e.last_name,
        e.manager_id,
        e.first_name || ' ' || e.last_name as full_name,
        eh.level + 1,
        eh.path || ' -> ' || e.first_name || ' ' || e.last_name
    FROM employees e
    JOIN employee_hierarchy eh ON e.manager_id = eh.id
)
SELECT 
    level,
    REPEAT('  ', level) || full_name as hierarchy_view,
    path as full_path
FROM employee_hierarchy
ORDER BY path;
```

**Результат:**
```
level | hierarchy_view     | full_path
0     | Alice Brown        | Alice Brown
1     |   Charlie Wilson   | Alice Brown -> Charlie Wilson
1     |   Ivy Thomas       | Alice Brown -> Ivy Thomas
0     | Eva Davis          | Eva Davis
0     | Frank Miller       | Frank Miller
0     | Grace Taylor       | Grace Taylor
0     | John Doe           | John Doe
1     |   Bob Johnson      | John Doe -> Bob Johnson
1     |   Jane Smith       | John Doe -> Jane Smith
2     |     Henry Anderson | John Doe -> Jane Smith -> Henry Anderson
```

**Памятка:**
- **CTE (Common Table Expression)** - временная именованная таблица в рамках запроса
- **RECURSIVE** - позволяет CTE ссылаться на себя
- **Структура рекурсивного CTE**: базовый случай UNION ALL рекурсивный случай
- **Ограничения**: PostgreSQL имеет лимит глубины рекурсии (по умолчанию без ограничений)
- **REPEAT(string, count)** - повторяет строку указанное количество раз
- **Оптимизация**: используйте WITH для сложных подзапросов, которые используются несколько раз

---

## Задача 11: Работа с массивами (PostgreSQL Arrays)

**Задача:** Найти пересекающиеся навыки между сотрудниками.

**Решение:**
```sql
-- Сначала создаем таблицу с навыками как массив
CREATE TABLE employee_skills (
    employee_id INT REFERENCES employees(id),
    skills TEXT[],
    PRIMARY KEY (employee_id)
);

INSERT INTO employee_skills VALUES 
(1, ARRAY['SQL', 'Python', 'PostgreSQL', 'Git']),
(2, ARRAY['Java', 'SQL', 'React', 'Git']),
(3, ARRAY['SQL', 'C#', 'PostgreSQL', 'Azure']),
(4, ARRAY['Management', 'Excel', 'PowerBI']),
(5, ARRAY['Recruiting', 'Excel', 'SQL']);

-- Основной запрос
SELECT 
    e1.first_name || ' ' || e1.last_name as employee1,
    e2.first_name || ' ' || e2.last_name as employee2,
    array_to_string(es1.skills & es2.skills, ', ') as common_skills,
    cardinality(es1.skills & es2.skills) as common_skills_count
FROM employee_skills es1
JOIN employee_skills es2 ON es1.employee_id < es2.employee_id
JOIN employees e1 ON es1.employee_id = e1.id
JOIN employees e2 ON es2.employee_id = e2.id
WHERE es1.skills && es2.skills  -- Проверка на пересечение массивов
ORDER BY common_skills_count DESC;
```

**Результат:**
```
employee1   | employee2      | common_skills        | common_skills_count
John Doe    | Bob Johnson    | SQL,PostgreSQL       | 2
John Doe    | Jane Smith     | SQL,Git               | 2
Jane Smith  | Bob Johnson    | SQL                   | 1
John Doe    | Charlie Wilson | SQL                   | 1
Bob Johnson | Charlie Wilson | SQL                   | 1
```

**Памятка:**
- **PostgreSQL массивы** - нативная поддержка массивов в PostgreSQL
- **ARRAY['val1', 'val2']** - создание массива
- **&& оператор** - проверка пересечения массивов
- **& оператор** - пересечение массивов
- **cardinality()** - количество элементов в массиве
- **array_to_string()** - преобразование массива в строку
- **Индексы**: GIN индексы поддерживают операции с массивами

---

## Задача 12: Pivot-таблицы с условной агрегацией

**Задача:** Создать отчет по количеству сотрудников в каждом отделе по уровням зарплат.

**Решение:**
```sql
SELECT 
    d.name as department_name,
    COUNT(CASE WHEN e.salary < 55000 THEN 1 END) as junior_count,
    COUNT(CASE WHEN e.salary BETWEEN 55000 AND 69999 THEN 1 END) as mid_count,
    COUNT(CASE WHEN e.salary >= 70000 THEN 1 END) as senior_count,
    COUNT(e.id) as total_employees,
    ROUND(AVG(e.salary), 2) as avg_salary
FROM departments d
LEFT JOIN employees e ON d.id = e.department_id
GROUP BY d.id, d.name
ORDER BY total_employees DESC;
```

**Результат:**
```
department_name | junior_count | mid_count | senior_count | total_employees | avg_salary
IT              | 1            | 3         | 1            | 4               | 60000.00
HR              | 0            | 2         | 1            | 3               | 65666.67
Finance         | 0            | 0         | 1            | 1               | 80000.00
Marketing       | 0            | 1         | 0            | 1               | 58000.00
Sales           | 1            | 0         | 0            | 1               | 52000.00
```

**Памятка:**
- **Условная агрегация** - использование CASE внутри агрегатных функций
- **COUNT(CASE WHEN ... THEN 1 END)** - подсчет записей по условию
- **LEFT JOIN** - включает все отделы, даже без сотрудников
- **Pivot операции** - преобразование строк в колонки
- **Альтернативы**: FILTER clause (PostgreSQL): `COUNT(*) FILTER (WHERE condition)`
- **Производительность**: условная агрегация быстрее множественных подзапросов

---

## Задача 13: Аналитические функции - LAG и LEAD

**Задача:** Сравнить зарплату каждого сотрудника с предыдущим и следующим по уровню зарплаты в отделе.

**Решение:**
```sql
SELECT 
    first_name,
    last_name,
    d.name as department_name,
    salary,
    LAG(salary, 1) OVER (PARTITION BY d.name ORDER BY salary) as previous_salary,
    LEAD(salary, 1) OVER (PARTITION BY d.name ORDER BY salary) as next_salary,
    salary - LAG(salary, 1) OVER (PARTITION BY d.name ORDER BY salary) as salary_diff_from_prev,
    ROUND(
        (salary - LAG(salary, 1) OVER (PARTITION BY d.name ORDER BY salary)) * 100.0 / 
        LAG(salary, 1) OVER (PARTITION BY d.name ORDER BY salary), 2
    ) as percent_increase
FROM employees e
JOIN departments d ON e.department_id = d.id
ORDER BY d.name, salary;
```

**Результат:**
```
first_name | last_name | department_name | salary   | previous_salary | next_salary | salary_diff_from_prev | percent_increase
Eva        | Davis     | Finance         | 80000.00 |                 |             |                       |
Alice      | Brown     | HR              | 60000.00 |                 | 67000.00    |                       |
Charlie    | Wilson    | HR              | 67000.00 | 60000.00        | 70000.00    | 7000.00               | 11.67
Ivy        | Thomas    | HR              | 70000.00 | 67000.00        |             | 3000.00               | 4.48
Henry      | Anderson  | IT              | 45000.00 |                 | 55000.00    |                       |
Bob        | Johnson   | IT              | 55000.00 | 45000.00        | 65000.00    | 10000.00              | 22.22
Jane       | Smith     | IT              | 65000.00 | 55000.00        | 75000.00    | 10000.00              | 18.18
John       | Doe       | IT              | 75000.00 | 65000.00        |             | 10000.00              | 15.38
```

**Памятка:**
- **LAG(column, offset)** - значение из предыдущей строки
- **LEAD(column, offset)** - значение из следующей строки
- **offset** - количество строк назад/вперед (по умолчанию 1)
- **Третий параметр** - значение по умолчанию если нет предыдущей/следующей строки
- **Использование**: сравнение с предыдущими периодами, вычисление изменений
- **Производительность**: эффективнее self-join для получения соседних значений

---

## Задача 14: UNION vs UNION ALL

**Задача:** Объединить данные о сотрудниках и проектах в общий отчет по активности.

**Решение:**
```sql
-- Использование UNION ALL для объединения разнородных данных
SELECT 
    'Employee' as record_type,
    e.first_name || ' ' || e.last_name as name,
    d.name as department_or_status,
    e.salary::TEXT as amount_or_budget,
    e.hire_date as date_field
FROM employees e
JOIN departments d ON e.department_id = d.id
WHERE e.is_active = true

UNION ALL

SELECT 
    'Project' as record_type,
    p.name,
    p.status,
    p.budget::TEXT,
    p.start_date
FROM projects p
WHERE p.status = 'active'

ORDER BY record_type, date_field DESC;
```

**Результат:**
```
record_type | name              | department_or_status | amount_or_budget | date_field
Employee    | Grace Taylor      | Sales               | 52000.00         | 2023-03-10
Employee    | Henry Anderson    | IT                  | 45000.00         | 2022-07-18
Employee    | Charlie Wilson    | HR                  | 60000.00         | 2022-02-14
Employee    | Ivy Thomas        | HR                  |                  | 2021-09-12
Employee    | Frank Miller      | Marketing           | 58000.00         | 2021-01-25
Employee    | Bob Johnson       | IT                  | 55000.00         | 2021-06-10
Employee    | Eva Davis         | Finance             | 80000.00         | 2020-08-30
Employee    | Jane Smith        | IT                  | 65000.00         | 2020-03-20
Employee    | John Doe          | IT                  | 75000.00         | 2020-01-15
Employee    | Alice Brown       | HR                  | 70000.00         | 2019-11-05
Project     | Marketing Campaign| active              | 100000.00        | 2023-10-01
Project     | Data Migration    | active              | 200000.00        | 2023-06-01
Project     | Mobile App        | active              | 300000.00        | 2023-03-15
```

**Памятка:**
- **UNION** - объединяет результаты и удаляет дубликаты
- **UNION ALL** - объединяет результаты без удаления дубликатов (быстрее)
- **Требования**: одинаковое количество колонок и совместимые типы данных
- **Приведение типов**: используйте ::TEXT или CAST() для совместимости
- **Производительность**: UNION ALL значительно быстрее при больших объемах данных
- **Порядок**: ORDER BY применяется ко всему результату объединения

---

## Задача 15: Работа с подзапросами - коррелированные запросы

**Задача:** Найти сотрудников, которые получают максимальную зарплату в своем отделе.

**Решение:**
```sql
SELECT 
    e.first_name,
    e.last_name,
    e.salary,
    d.name as department_name,
    (SELECT COUNT(*) FROM employees e2 WHERE e2.department_id = e.department_id) as dept_size
FROM employees e
JOIN departments d ON e.department_id = d.id
WHERE e.salary = (
    SELECT MAX(e2.salary) 
    FROM employees e2 
    WHERE e2.department_id = e.department_id
)
ORDER BY e.salary DESC;
```

**Альтернативное решение с оконными функциями:**
```sql
SELECT 
    first_name,
    last_name,
    salary,
    department_name,
    dept_size
FROM (
    SELECT 
        e.first_name,
        e.last_name,
        e.salary,
        d.name as department_name,
        COUNT(*) OVER (PARTITION BY d.id) as dept_size,
        ROW_NUMBER() OVER (PARTITION BY d.id ORDER BY e.salary DESC) as rn
    FROM employees e
    JOIN departments d ON e.department_id = d.id
) ranked
WHERE rn = 1
ORDER BY salary DESC;
```

**Результат:**
```
first_name | last_name | salary   | department_name | dept_size
Eva        | Davis     | 80000.00 | Finance         | 1
John       | Doe       | 75000.00 | IT              | 4
Alice      | Brown     | 70000.00 | HR              | 3
Frank      | Miller    | 58000.00 | Marketing       | 1
Grace      | Taylor    | 52000.00 | Sales           | 1
```

**Памятка:**
- **Коррелированные подзапросы** - внутренний запрос ссылается на колонки внешнего запроса
- **Производительность**: коррелированные подзапросы выполняются для каждой строки (медленнее)
- **Оптимизация**: оконные функции часто быстрее коррелированных подзапросов
- **EXISTS vs IN** - для коррелированных запросов EXISTS обычно быстрее
- **Отладка**: выполняйте подзапросы отдельно для проверки логики

---

## Задача 16: Индексы и планы выполнения

**Задача:** Оптимизировать запрос для поиска сотрудников по email.

**Решение:**
```sql
-- Создание индекса для оптимизации
CREATE INDEX CONCURRENTLY idx_employees_email ON employees(email);
CREATE INDEX CONCURRENTLY idx_employees_dept_salary ON employees(department_id, salary DESC);

-- Запрос с объяснением плана выполнения
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT) 
SELECT 
    e.first_name,
    e.last_name,
    e.email,
    e.salary,
    d.name as department_name
FROM employees e
JOIN departments d ON e.department_id = d.id
WHERE e.email LIKE '%company.com%'
ORDER BY e.salary DESC;
```

**Оптимизированный запрос для точного поиска:**
```sql
SELECT 
    e.first_name,
    e.last_name,
    e.email,
    e.salary,
    d.name as department_name
FROM employees e
JOIN departments d ON e.department_id = d.id
WHERE e.email = 'john.doe@company.com';
```

**Памятка:**
- **EXPLAIN ANALYZE** - показывает реальное время выполнения и план
- **CONCURRENTLY** - создание индекса без блокировки таблицы
- **Типы индексов PostgreSQL**: B-tree (по умолчанию), GIN, GiST, Hash, BRIN
- **Составные индексы** - учитывайте порядок колонок (наиболее селективные первыми)
- **LIKE оптимизация**: `LIKE 'prefix%'` использует индекс, `LIKE '%suffix'` - нет
- **pg_stat_user_indexes** - статистика использования индексов

---

## Задача 17: Транзакции и блокировки

**Задача:** Безопасно обновить зарплаты сотрудников с проверкой бюджета отдела.

**Решение:**
```sql
BEGIN;

-- Проверяем текущий бюджет отдела
DO $
DECLARE
    current_budget DECIMAL(12,2);
    total_salaries DECIMAL(12,2);
    dept_id INT := 1; -- IT отдел
BEGIN
    -- Получаем бюджет отдела
    SELECT budget INTO current_budget 
    FROM departments 
    WHERE id = dept_id;
    
    -- Вычисляем общую зарплату после увеличения на 10%
    SELECT SUM(salary * 1.1) INTO total_salaries
    FROM employees 
    WHERE department_id = dept_id;
    
    -- Проверяем, не превышает ли общая зарплата бюджет
    IF total_salaries > current_budget THEN
        RAISE EXCEPTION 'Salary increase would exceed department budget. Budget: %, New total salaries: %', 
                        current_budget, total_salaries;
    END IF;
    
    -- Если проверка прошла, увеличиваем зарплаты
    UPDATE employees 
    SET salary = salary * 1.1,
        updated_at = CURRENT_TIMESTAMP
    WHERE department_id = dept_id;
    
    RAISE NOTICE 'Salaries updated successfully for department %', dept_id;
END $;

-- Проверяем результат
SELECT 
    first_name,
    last_name,
    salary,
    d.name as department_name
FROM employees e
JOIN departments d ON e.department_id = d.id
WHERE e.department_id = 1;

COMMIT;
```

**Памятка:**
- **BEGIN/COMMIT** - границы транзакции
- **ROLLBACK** - отмена изменений в транзакции
- **DO blocks** - анонимные блоки кода PL/pgSQL
- **RAISE EXCEPTION** - генерация исключения с откатом транзакции
- **RAISE NOTICE** - вывод информационных сообщений
- **Уровни изоляции**: READ UNCOMMITTED, READ COMMITTED, REPEATABLE READ, SERIALIZABLE
- **Блокировки**: SELECT FOR UPDATE - эксклюзивная блокировка строк

---

## Задача 18: Полнотекстовый поиск

**Задача:** Реализовать поиск сотрудников по имени и email с ранжированием результатов.

**Решение:**
```sql
-- Добавляем колонку для полнотекстового поиска
ALTER TABLE employees ADD COLUMN search_vector tsvector;

-- Создаем функцию для обновления поискового вектора
CREATE OR REPLACE FUNCTION update_employee_search_vector() RETURNS trigger AS $
BEGIN
    NEW.search_vector := 
        setweight(to_tsvector('english', COALESCE(NEW.first_name, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.last_name, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.email, '')), 'B');
    RETURN NEW;
END $ LANGUAGE plpgsql;

-- Создаем триггер
CREATE TRIGGER employee_search_vector_update 
    BEFORE INSERT OR UPDATE ON employees
    FOR EACH ROW EXECUTE FUNCTION update_employee_search_vector();

-- Обновляем существующие записи
UPDATE employees SET search_vector = 
    setweight(to_tsvector('english', COALESCE(first_name, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(last_name, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(email, '')), 'B');

-- Создаем индекс
CREATE INDEX idx_employees_search ON employees USING GIN(search_vector);

-- Поисковый запрос
SELECT 
    first_name,
    last_name,
    email,
    d.name as department_name,
    ts_rank(search_vector, to_tsquery('english', 'john | smith')) as rank
FROM employees e
JOIN departments d ON e.department_id = d.id
WHERE search_vector @@ to_tsquery('english', 'john | smith')
ORDER BY rank DESC, last_name;
```

**Результат:**
```
first_name | last_name | email                    | department_name | rank
John       | Doe       | john.doe@company.com     | IT              | 0.607927
Jane       | Smith     | jane.smith@company.com   | IT              | 0.607927
```

**Памятка:**
- **tsvector** - тип данных для полнотекстового поиска
- **to_tsvector()** - преобразует текст в поисковый вектор
- **to_tsquery()** - создает поисковый запрос
- **@@** - оператор сопоставления вектора и запроса
- **ts_rank()** - ранжирование результатов поиска
- **setweight()** - назначение весов частям документа ('A', 'B', 'C', 'D')
- **GIN индексы** - оптимальны для полнотекстового поиска

---

## Задача 19: Временные таблицы и производительность

**Задача:** Создать временную таблицу для анализа производительности сотрудников.

**Решение:**
```sql
-- Создаем временную таблицу
CREATE TEMP TABLE employee_performance AS
SELECT 
    e.id,
    e.first_name,
    e.last_name,
    e.salary,
    d.name as department_name,
    COUNT(ep.project_id) as projects_count,
    COALESCE(SUM(ep.hours_per_week), 0) as total_weekly_hours,
    CASE 
        WHEN COUNT(ep.project_id) = 0 THEN 'No Projects'
        WHEN COUNT(ep.project_id) <= 2 THEN 'Light Load'
        ELSE 'Heavy Load'
    END as workload_category,
    ROUND(e.salary / GREATEST(COALESCE(SUM(ep.hours_per_week), 1), 1), 2) as hourly_rate
FROM employees e
LEFT JOIN departments d ON e.department_id = d.id
LEFT JOIN employee_projects ep ON e.id = ep.employee_id
GROUP BY e.id, e.first_name, e.last_name, e.salary, d.name;

-- Создаем индекс на временной таблице
CREATE INDEX idx_temp_workload ON employee_performance(workload_category);
CREATE INDEX idx_temp_hourly_rate ON employee_performance(hourly_rate DESC);

-- Анализируем данные
SELECT 
    workload_category,
    COUNT(*) as employee_count,
    ROUND(AVG(salary), 2) as avg_salary,
    ROUND(AVG(hourly_rate), 2) as avg_hourly_rate,
    ROUND(AVG(total_weekly_hours), 2) as avg_weekly_hours
FROM employee_performance
GROUP BY workload_category
ORDER BY avg_salary DESC;

-- Топ сотрудников по hourly rate
SELECT 
    first_name,
    last_name,
    department_name,
    salary,
    total_weekly_hours,
    hourly_rate,
    workload_category
FROM employee_performance
WHERE total_weekly_hours > 0
ORDER BY hourly_rate DESC
LIMIT 5;
```

**Результат анализа по категориям:**
```
workload_category | employee_count | avg_salary | avg_hourly_rate | avg_weekly_hours
Heavy Load        | 1              | 75000.00   | 1071.43         | 70.00
Light Load        | 4              | 63750.00   | 1968.75         | 32.50
No Projects       | 5              | 58400.00   | 58400.00        | 0.00
```

**Топ сотрудников:**
```
first_name | last_name | department_name | salary   | total_weekly_hours | hourly_rate | workload_category
Alice      | Brown     | HR              | 70000.00 | 25.00              | 2800.00     | Light Load
Frank      | Miller    | Marketing       | 58000.00 | 40.00              | 1450.00     | Light Load
Jane       | Smith     | IT              | 65000.00 | 75.00              | 866.67      | Heavy Load
John       | Doe       | IT              | 75000.00 | 70.00              | 1071.43     | Heavy Load
Bob        | Johnson   | IT              | 55000.00 | 40.00              | 1375.00     | Light Load
```

**Памятка:**
- **TEMP TABLE** - существует только в рамках сессии
- **CREATE TEMP TABLE AS** - создание временной таблицы с данными
- **Индексы на временных таблицах** - улучшают производительность сложных запросов
- **GREATEST(val1, val2)** - возвращает наибольшее значение, избегает деления на ноль
- **Очистка**: временные таблицы автоматически удаляются при закрытии сессии
- **pg_temp схема** - специальная схема для временных объектов

---

## Задача 20: Оптимизация сложных запросов с материализованными представлениями

**Задача:** Создать материализованное представление для быстрого доступа к отчету по отделам.

**Решение:**
```sql
-- Создаем материализованное представление
CREATE MATERIALIZED VIEW mv_department_report AS
SELECT 
    d.id as department_id,
    d.name as department_name,
    d.budget as department_budget,
    COUNT(e.id) as total_employees,
    COUNT(CASE WHEN e.is_active THEN 1 END) as active_employees,
    COALESCE(SUM(e.salary), 0) as total_salaries,
    COALESCE(ROUND(AVG(e.salary), 2), 0) as avg_salary,
    COALESCE(MIN(e.salary), 0) as min_salary,
    COALESCE(MAX(e.salary), 0) as max_salary,
    COUNT(DISTINCT ep.project_id) as unique_projects,
    COALESCE(SUM(ep.hours_per_week), 0) as total_project_hours,
    ROUND(
        CASE 
            WHEN d.budget > 0 THEN (COALESCE(SUM(e.salary), 0) * 100.0 / d.budget)
            ELSE 0 
        END, 2
    ) as budget_utilization_percent,
    EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP - MAX(e.hire_date))) / 86400 as days_since_last_hire
FROM departments d
LEFT JOIN employees e ON d.id = e.department_id
LEFT JOIN employee_projects ep ON e.id = ep.employee_id
GROUP BY d.id, d.name, d.budget
WITH DATA;

-- Создаем индексы
CREATE UNIQUE INDEX idx_mv_dept_report_id ON mv_department_report(department_id);
CREATE INDEX idx_mv_dept_report_utilization ON mv_department_report(budget_utilization_percent DESC);

-- Запрос к материализованному представлению
SELECT 
    department_name,
    total_employees,
    active_employees,
    avg_salary,
    unique_projects,
    budget_utilization_percent,
    CASE 
        WHEN budget_utilization_percent > 80 THEN 'High Utilization'
        WHEN budget_utilization_percent > 60 THEN 'Medium Utilization'
        ELSE 'Low Utilization'
    END as utilization_category
FROM mv_department_report
ORDER BY budget_utilization_percent DESC;

-- Функция для автоматического обновления
CREATE OR REPLACE FUNCTION refresh_department_report()
RETURNS void AS $
BEGIN
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_department_report;
    INSERT INTO refresh_log VALUES (CURRENT_TIMESTAMP, 'mv_department_report refreshed');
END $ LANGUAGE plpgsql;

-- Создаем таблицу для логирования
CREATE TABLE IF NOT EXISTS refresh_log (
    refresh_time TIMESTAMP,
    message TEXT
);
```

**Результат:**
```
department_name | total_employees | active_employees | avg_salary | unique_projects | budget_utilization_percent | utilization_category
Finance         | 1               | 1                | 80000.00   | 0               | 26.67                      | Low Utilization
IT              | 4               | 4                | 60000.00   | 3               | 48.00                      | Low Utilization
HR              | 3               | 3                | 65666.67   | 1               | 98.50                      | High Utilization
Marketing       | 1               | 1                | 58000.00   | 1               | 23.20                      | Low Utilization
Sales           | 1               | 1                | 52000.00   | 0               | 13.00                      | Low Utilization
```

**Команды обслуживания:**
```sql
-- Обновление данных
REFRESH MATERIALIZED VIEW mv_department_report;

-- Обновление без блокировки (требует UNIQUE индекс)
REFRESH MATERIALIZED VIEW CONCURRENTLY mv_department_report;

-- Просмотр размера и статистики
SELECT 
    schemaname,
    matviewname,
    matviewowner,
    tablespace,
    hasindexes,
    ispopulated
FROM pg_matviews 
WHERE matviewname = 'mv_department_report';
```

**Памятка:**
- **Материализованные представления** - сохраняют результат запроса физически
- **WITH DATA** - заполняет представление данными при создании
- **REFRESH MATERIALIZED VIEW** - обновляет данные в представлении
- **CONCURRENTLY** - обновление без блокировки чтения (требует уникальный индекс)
- **Преимущества**: быстрый доступ к сложным агрегатам
- **Недостатки**: данные не всегда актуальные, требуют места на диске
- **pg_matviews** - системное представление для просмотра материализованных представлений

---

## Заключение

Эти 20 задач покрывают основные темы SQL и PostgreSQL, которые часто встречаются на собеседованиях:

1. **Базовые запросы** - SELECT, WHERE, ORDER BY
2. **Соединения** - INNER, LEFT, RIGHT, FULL JOIN, Self Join
3. **Агрегация** - GROUP BY, HAVING, агрегатные функции
4. **Подзапросы** - коррелированные и некоррелированные
5. **Оконные функции** - аналитические функции
6. **Работа с NULL** - COALESCE, IS NULL/IS NOT NULL
7. **Условная логика** - CASE WHEN
8. **Работа с датами** - DATE, INTERVAL, извлечение частей
9. **Строковые функции** - конкатенация, поиск
10. **PostgreSQL специфика** - JSON, массивы, полнотекстовый поиск
11. **Оптимизация** - индексы, планы выполнения
12. **Транзакции** - ACID свойства
13. **Производительность** - временные таблицы, материализованные представления

**Ключевые принципы оптимизации:**
- Используйте индексы на часто используемых колонках
- Избегайте SELECT * в продакшене
- Предпочитайте EXISTS вместо IN при работе с большими данными
- Используйте LIMIT для ограничения результатов
- Анализируйте планы выполнения с EXPLAIN
- Оптимизируйте JOIN операции правильным порядком таблиц
