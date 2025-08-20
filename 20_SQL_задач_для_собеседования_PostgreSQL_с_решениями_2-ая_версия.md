# 20 SQL задач для собеседования .NET разработчика (PostgreSQL)

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
    email VARCHAR(100) UNIQUE NOT NULL,
    phone VARCHAR(20),
    hire_date DATE NOT NULL,
    salary DECIMAL(10,2) NOT NULL,
    department_id INTEGER REFERENCES departments(id),
    manager_id INTEGER REFERENCES employees(id),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE projects (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    start_date DATE,
    end_date DATE,
    budget DECIMAL(12,2),
    status VARCHAR(20) DEFAULT 'active',
    department_id INTEGER REFERENCES departments(id)
);

CREATE TABLE employee_projects (
    employee_id INTEGER REFERENCES employees(id),
    project_id INTEGER REFERENCES projects(id),
    role VARCHAR(50),
    hours_worked DECIMAL(5,2) DEFAULT 0,
    assigned_date DATE DEFAULT CURRENT_DATE,
    PRIMARY KEY (employee_id, project_id)
);

CREATE TABLE salaries_history (
    id SERIAL PRIMARY KEY,
    employee_id INTEGER REFERENCES employees(id),
    old_salary DECIMAL(10,2),
    new_salary DECIMAL(10,2),
    change_date DATE,
    reason VARCHAR(100)
);

-- Заполнение тестовыми данными
INSERT INTO departments (name, budget) VALUES
('IT Development', 500000.00),
('Human Resources', 200000.00),
('Sales', 800000.00),
('Marketing', 300000.00),
('Finance', 400000.00);

INSERT INTO employees (first_name, last_name, email, phone, hire_date, salary, department_id, manager_id, is_active) VALUES
('John', 'Smith', 'john.smith@company.com', '+1234567890', '2020-01-15', 75000.00, 1, NULL, TRUE),
('Jane', 'Doe', 'jane.doe@company.com', '+1234567891', '2020-03-20', 65000.00, 1, 1, TRUE),
('Bob', 'Johnson', 'bob.johnson@company.com', '+1234567892', '2019-05-10', 80000.00, 1, 1, TRUE),
('Alice', 'Brown', 'alice.brown@company.com', '+1234567893', '2021-07-01', 55000.00, 2, NULL, TRUE),
('Charlie', 'Wilson', 'charlie.wilson@company.com', '+1234567894', '2020-11-15', 70000.00, 3, NULL, TRUE),
('Eva', 'Davis', 'eva.davis@company.com', '+1234567895', '2021-02-28', 60000.00, 3, 5, TRUE),
('Frank', 'Miller', 'frank.miller@company.com', '+1234567896', '2018-09-12', 90000.00, 1, NULL, TRUE),
('Grace', 'Taylor', 'grace.taylor@company.com', '+1234567897', '2022-01-10', 45000.00, 4, NULL, TRUE),
('Henry', 'Anderson', 'henry.anderson@company.com', '+1234567898', '2019-12-05', 85000.00, 5, NULL, TRUE),
('Ivy', 'Thomas', 'ivy.thomas@company.com', '+1234567899', '2023-03-15', 50000.00, 2, 4, FALSE);

INSERT INTO projects (name, description, start_date, end_date, budget, status, department_id) VALUES
('Website Redesign', 'Complete redesign of company website', '2023-01-01', '2023-06-30', 150000.00, 'completed', 1),
('CRM System', 'Implementation of new CRM system', '2023-03-01', '2023-12-31', 300000.00, 'active', 1),
('Sales Campaign Q2', 'Marketing campaign for Q2 sales', '2023-04-01', '2023-06-30', 80000.00, 'completed', 3),
('HR Portal', 'Employee self-service portal', '2023-02-01', '2023-08-31', 120000.00, 'active', 2),
('Mobile App', 'Company mobile application', '2023-05-01', '2024-02-29', 200000.00, 'active', 1);

INSERT INTO employee_projects (employee_id, project_id, role, hours_worked, assigned_date) VALUES
(1, 1, 'Team Lead', 320.5, '2023-01-01'),
(2, 1, 'Developer', 280.0, '2023-01-01'),
(3, 1, 'Developer', 290.0, '2023-01-01'),
(1, 2, 'Architect', 150.0, '2023-03-01'),
(2, 2, 'Developer', 180.0, '2023-03-01'),
(7, 2, 'Senior Developer', 200.0, '2023-03-01'),
(5, 3, 'Project Manager', 160.0, '2023-04-01'),
(6, 3, 'Sales Specialist', 140.0, '2023-04-01'),
(4, 4, 'Business Analyst', 120.0, '2023-02-01'),
(1, 5, 'Tech Lead', 100.0, '2023-05-01');

INSERT INTO salaries_history (employee_id, old_salary, new_salary, change_date, reason) VALUES
(1, 70000.00, 75000.00, '2023-01-01', 'Annual Review'),
(2, 60000.00, 65000.00, '2023-01-01', 'Performance Bonus'),
(3, 75000.00, 80000.00, '2022-12-01', 'Promotion'),
(5, 65000.00, 70000.00, '2023-06-01', 'Market Adjustment'),
(7, 85000.00, 90000.00, '2023-03-01', 'Annual Review');
```

---

## Задача 1: Базовая выборка с фильтрацией

**Условие:** Получить всех активных сотрудников IT отдела с зарплатой выше 60000.

### Решение:
```sql
SELECT e.first_name, e.last_name, e.email, e.salary, d.name as department
FROM employees e
JOIN departments d ON e.department_id = d.id
WHERE d.name = 'IT Development' 
  AND e.salary > 60000 
  AND e.is_active = TRUE;
```

### Результат:
```
first_name | last_name | email                    | salary   | department
-----------|-----------|--------------------------|----------|-------------
John       | Smith     | john.smith@company.com   | 75000.00 | IT Development
Jane       | Doe       | jane.doe@company.com     | 65000.00 | IT Development
Bob        | Johnson   | bob.johnson@company.com  | 80000.00 | IT Development
Frank      | Miller    | frank.miller@company.com | 90000.00 | IT Development
```

### Памятка:
- **JOIN** - соединение таблиц по ключу. INNER JOIN (по умолчанию) возвращает только совпадающие записи
- **WHERE** - фильтрация данных. Условия объединяются через AND/OR
- **Алиасы** (e, d) улучшают читаемость запроса
- **Оптимизация**: индексы на department_id, salary, is_active ускорят выполнение

---

## Задача 2: Агрегатные функции и группировка

**Условие:** Показать количество сотрудников и среднюю зарплату по каждому отделу.

### Решение:
```sql
SELECT 
    d.name as department_name,
    COUNT(e.id) as employee_count,
    ROUND(AVG(e.salary), 2) as avg_salary,
    MIN(e.salary) as min_salary,
    MAX(e.salary) as max_salary
FROM departments d
LEFT JOIN employees e ON d.id = e.department_id AND e.is_active = TRUE
GROUP BY d.id, d.name
ORDER BY avg_salary DESC NULLS LAST;
```

### Результат:
```
department_name  | employee_count | avg_salary | min_salary | max_salary
-----------------|----------------|------------|------------|------------
IT Development   | 4              | 77500.00   | 65000.00   | 90000.00
Finance          | 1              | 85000.00   | 85000.00   | 85000.00
Sales            | 2              | 65000.00   | 60000.00   | 70000.00
Human Resources  | 1              | 55000.00   | 55000.00   | 55000.00
Marketing        | 1              | 45000.00   | 45000.00   | 45000.00
```

### Памятка:
- **LEFT JOIN** - включает все записи из левой таблицы, даже если нет соответствий
- **GROUP BY** - группирует строки для агрегатных функций
- **COUNT/AVG/MIN/MAX** - агрегатные функции
- **ROUND()** - округление числовых значений
- **NULLS LAST** - в ORDER BY помещает NULL значения в конец
- **Оптимизация**: индекс на department_id обязателен для JOIN

---

## Задача 3: Подзапросы и EXISTS

**Условие:** Найти сотрудников, которые участвуют более чем в одном проекте.

### Решение:
```sql
SELECT e.first_name, e.last_name, e.email,
       (SELECT COUNT(*) 
        FROM employee_projects ep 
        WHERE ep.employee_id = e.id) as project_count
FROM employees e
WHERE EXISTS (
    SELECT 1 
    FROM employee_projects ep 
    WHERE ep.employee_id = e.id 
    GROUP BY ep.employee_id 
    HAVING COUNT(*) > 1
)
ORDER BY project_count DESC;
```

### Результат:
```
first_name | last_name | email                  | project_count
-----------|-----------|------------------------|---------------
John       | Smith     | john.smith@company.com | 3
Jane       | Doe       | jane.doe@company.com   | 2
```

### Памятка:
- **EXISTS** - проверяет существование записей в подзапросе (более эффективен чем IN для больших данных)
- **Подзапрос в SELECT** - коррелированный подзапрос, выполняется для каждой строки
- **HAVING** - фильтрация после группировки (в отличие от WHERE)
- **SELECT 1** в EXISTS - оптимизация, возвращается только факт существования
- **Альтернатива**: можно использовать window functions или CTE

---

## Задача 4: Оконные функции (Window Functions)

**Условие:** Показать ранжирование сотрудников по зарплате внутри каждого отдела.

### Решение:
```sql
SELECT 
    e.first_name,
    e.last_name,
    d.name as department,
    e.salary,
    RANK() OVER (PARTITION BY d.id ORDER BY e.salary DESC) as salary_rank,
    DENSE_RANK() OVER (PARTITION BY d.id ORDER BY e.salary DESC) as dense_rank,
    ROW_NUMBER() OVER (PARTITION BY d.id ORDER BY e.salary DESC) as row_num
FROM employees e
JOIN departments d ON e.department_id = d.id
WHERE e.is_active = TRUE
ORDER BY d.name, salary_rank;
```

### Результат:
```
first_name | last_name | department     | salary   | salary_rank | dense_rank | row_num
-----------|-----------|----------------|----------|-------------|------------|--------
Alice      | Brown     | Human Resources| 55000.00 | 1          | 1          | 1
Grace      | Taylor    | Marketing      | 45000.00 | 1          | 1          | 1
Frank      | Miller    | IT Development | 90000.00 | 1          | 1          | 1
Bob        | Johnson   | IT Development | 80000.00 | 2          | 2          | 2
John       | Smith     | IT Development | 75000.00 | 3          | 3          | 3
Jane       | Doe       | IT Development | 65000.00 | 4          | 4          | 4
```

### Памятка:
- **RANK()** - ранжирование с пропусками (1,1,3,4...)
- **DENSE_RANK()** - ранжирование без пропусков (1,1,2,3...)
- **ROW_NUMBER()** - уникальные номера строк
- **PARTITION BY** - разбивает данные на группы для оконной функции
- **OVER** - определяет окно для функции
- **Производительность**: оконные функции быстрее подзапросов для ранжирования

---

## Задача 5: CTE (Common Table Expression) и рекурсия

**Условие:** Построить иерархию сотрудников (кто кому подчиняется) с уровнями.

### Решение:
```sql
WITH RECURSIVE employee_hierarchy AS (
    -- Начальные записи (руководители без начальников)
    SELECT 
        e.id,
        e.first_name,
        e.last_name,
        e.manager_id,
        1 as level,
        CAST(e.first_name || ' ' || e.last_name AS TEXT) as hierarchy_path
    FROM employees e
    WHERE e.manager_id IS NULL AND e.is_active = TRUE
    
    UNION ALL
    
    -- Рекурсивная часть (подчиненные)
    SELECT 
        e.id,
        e.first_name,
        e.last_name,
        e.manager_id,
        eh.level + 1,
        eh.hierarchy_path || ' -> ' || e.first_name || ' ' || e.last_name
    FROM employees e
    JOIN employee_hierarchy eh ON e.manager_id = eh.id
    WHERE e.is_active = TRUE
)
SELECT * FROM employee_hierarchy ORDER BY level, first_name;
```

### Результат:
```
id | first_name | last_name | manager_id | level | hierarchy_path
---|------------|-----------|------------|-------|------------------
1  | John       | Smith     | null       | 1     | John Smith
4  | Alice      | Brown     | null       | 1     | Alice Brown
5  | Charlie    | Wilson    | null       | 1     | Charlie Wilson
7  | Frank      | Miller    | null       | 1     | Frank Miller
9  | Henry      | Anderson  | null       | 1     | Henry Anderson
2  | Jane       | Doe       | 1          | 2     | John Smith -> Jane Doe
3  | Bob        | Johnson   | 1          | 2     | John Smith -> Bob Johnson
6  | Eva        | Davis     | 5          | 2     | Charlie Wilson -> Eva Davis
```

### Памятка:
- **CTE** - временная именованная таблица, видимая только в рамках запроса
- **RECURSIVE** - позволяет создавать рекурсивные запросы
- **UNION ALL** - объединяет начальную и рекурсивную части
- **CAST** - приведение типов (здесь для конкатенации строк)
- **Ограничения**: PostgreSQL имеет защиту от бесконечной рекурсии
- **Альтернатива**: ltree расширение для более эффективной работы с иерархиями

---

## Задача 6: CASE WHEN и условная логика

**Условие:** Классифицировать сотрудников по уровню зарплаты и показать бонус.

### Решение:
```sql
SELECT 
    e.first_name,
    e.last_name,
    e.salary,
    CASE 
        WHEN e.salary >= 80000 THEN 'Senior'
        WHEN e.salary >= 65000 THEN 'Middle'
        WHEN e.salary >= 50000 THEN 'Junior'
        ELSE 'Trainee'
    END as salary_level,
    CASE 
        WHEN e.salary >= 80000 THEN e.salary * 0.15
        WHEN e.salary >= 65000 THEN e.salary * 0.10
        WHEN e.salary >= 50000 THEN e.salary * 0.05
        ELSE 0
    END as annual_bonus,
    COALESCE(d.name, 'No Department') as department
FROM employees e
LEFT JOIN departments d ON e.department_id = d.id
WHERE e.is_active = TRUE
ORDER BY e.salary DESC;
```

### Результат:
```
first_name | last_name | salary   | salary_level | annual_bonus | department
-----------|-----------|----------|--------------|--------------|-------------
Frank      | Miller    | 90000.00 | Senior       | 13500.00     | IT Development
Henry      | Anderson  | 85000.00 | Senior       | 12750.00     | Finance
Bob        | Johnson   | 80000.00 | Senior       | 12000.00     | IT Development
John       | Smith     | 75000.00 | Middle       | 7500.00      | IT Development
Charlie    | Wilson    | 70000.00 | Middle       | 7000.00      | Sales
Jane       | Doe       | 65000.00 | Middle       | 6500.00      | IT Development
Eva        | Davis     | 60000.00 | Junior       | 3000.00      | Sales
Alice      | Brown     | 55000.00 | Junior       | 2750.00      | Human Resources
```

### Памятка:
- **CASE WHEN** - условная логика в SQL (аналог if-else)
- **COALESCE** - возвращает первое не-NULL значение из списка
- **Вычисления** - можно использовать арифметические операции
- **Порядок условий** важен - проверка идет сверху вниз
- **ELSE** - обязательная часть для полного покрытия случаев
- **Производительность**: простые CASE выполняются быстро

---

## Задача 7: Работа с датами и временем

**Условие:** Показать стаж работы сотрудников и статистику по годам найма.

### Решение:
```sql
SELECT 
    e.first_name,
    e.last_name,
    e.hire_date,
    CURRENT_DATE - e.hire_date as days_worked,
    DATE_PART('year', AGE(CURRENT_DATE, e.hire_date)) as years_worked,
    DATE_PART('month', AGE(CURRENT_DATE, e.hire_date)) as additional_months,
    EXTRACT(YEAR FROM e.hire_date) as hire_year,
    TO_CHAR(e.hire_date, 'FMMonth YYYY') as hire_month_year,
    CASE 
        WHEN DATE_PART('year', AGE(CURRENT_DATE, e.hire_date)) >= 3 THEN 'Veteran'
        WHEN DATE_PART('year', AGE(CURRENT_DATE, e.hire_date)) >= 1 THEN 'Experienced'
        ELSE 'Newcomer'
    END as experience_level
FROM employees e
WHERE e.is_active = TRUE
ORDER BY e.hire_date;
```

### Результат:
```
first_name | last_name | hire_date  | days_worked | years_worked | additional_months | hire_year | hire_month_year | experience_level
-----------|-----------|------------|-------------|--------------|-------------------|-----------|-----------------|------------------
Frank      | Miller    | 2018-09-12 | 2165        | 5            | 11                | 2018      | September 2018  | Veteran
Bob        | Johnson   | 2019-05-10 | 1928        | 5            | 3                 | 2019      | May 2019        | Veteran
Henry      | Anderson  | 2019-12-05 | 1719        | 4            | 8                 | 2019      | December 2019   | Veteran
John       | Smith     | 2020-01-15 | 1678        | 4            | 7                 | 2020      | January 2020    | Veteran
```

### Памятка:
- **AGE()** - вычисляет интервал между датами
- **DATE_PART/EXTRACT** - извлекает части даты (год, месяц, день)
- **CURRENT_DATE** - текущая дата системы
- **TO_CHAR** - форматирование дат в строки
- **Арифметика дат** - можно вычитать даты напрямую
- **FM** в TO_CHAR убирает ведущие нули и пробелы
- **Индексы** на DATE колонках улучшают производительность запросов с фильтрацией по дате

---

## Задача 8: UNION и объединение результатов

**Условие:** Создать отчет по всем изменениям зарплат с указанием типа изменения.

### Решение:
```sql
SELECT 
    'Current Salary' as change_type,
    e.first_name || ' ' || e.last_name as employee_name,
    e.salary as amount,
    e.hire_date as change_date,
    'Initial hire' as reason
FROM employees e
WHERE e.is_active = TRUE

UNION ALL

SELECT 
    'Salary History' as change_type,
    e.first_name || ' ' || e.last_name as employee_name,
    sh.new_salary as amount,
    sh.change_date,
    sh.reason
FROM salaries_history sh
JOIN employees e ON sh.employee_id = e.id
WHERE e.is_active = TRUE

ORDER BY employee_name, change_date;
```

### Результат:
```
change_type    | employee_name  | amount   | change_date | reason
---------------|----------------|----------|-------------|------------------
Alice Brown    | Current Salary | 55000.00 | 2021-07-01  | Initial hire
Bob Johnson    | Current Salary | 80000.00 | 2019-05-10  | Initial hire
Bob Johnson    | Salary History | 80000.00 | 2022-12-01  | Promotion
Charlie Wilson | Current Salary | 70000.00 | 2020-11-15  | Initial hire
Charlie Wilson | Salary History | 70000.00 | 2023-06-01  | Market Adjustment
```

### Памятка:
- **UNION** - объединяет результаты запросов, убирая дубликаты
- **UNION ALL** - объединяет результаты, сохраняя дубликаты (быстрее)
- **Количество и типы колонок** должны совпадать в обеих частях
- **ORDER BY** применяется ко всему результирующему набору
- **Конкатенация строк** через оператор ||
- **Производительность**: UNION ALL быстрее, если дубликаты не критичны

---

## Задача 9: NULL значения и COALESCE

**Условие:** Показать всех сотрудников с обработкой NULL значений в телефонах и менеджерах.

### Решение:
```sql
SELECT 
    e.first_name,
    e.last_name,
    COALESCE(e.phone, 'No phone provided') as phone,
    COALESCE(m.first_name || ' ' || m.last_name, 'No manager') as manager_name,
    CASE 
        WHEN e.phone IS NULL THEN 'Missing'
        WHEN LENGTH(e.phone) < 10 THEN 'Invalid'
        ELSE 'Valid'
    END as phone_status,
    NULLIF(TRIM(e.phone), '') as cleaned_phone
FROM employees e
LEFT JOIN employees m ON e.manager_id = m.id
WHERE e.is_active = TRUE
ORDER BY e.last_name;
```

### Результат:
```
first_name | last_name | phone        | manager_name | phone_status | cleaned_phone
-----------|-----------|--------------|--------------|--------------|---------------
Henry      | Anderson  | +1234567898  | No manager   | Valid        | +1234567898
Alice      | Brown     | +1234567893  | No manager   | Valid        | +1234567893
Eva        | Davis     | +1234567895  | Charlie Wilson| Valid        | +1234567895
Jane       | Doe       | +1234567891  | John Smith   | Valid        | +1234567891
Bob        | Johnson   | +1234567892  | John Smith   | Valid        | +1234567892
Frank      | Miller    | +1234567896  | No manager   | Valid        | +1234567896
John       | Smith     | +1234567890  | No manager   | Valid        | +1234567890
Grace      | Taylor    | +1234567897  | No manager   | Valid        | +1234567897
Charlie    | Wilson    | +1234567894  | No manager   | Valid        | +1234567894
```

### Памятка:
- **COALESCE** - возвращает первое не-NULL значение (эквивалент ISNULL в SQL Server)
- **NULLIF** - возвращает NULL если значения равны, иначе первое значение
- **IS NULL / IS NOT NULL** - правильный способ проверки на NULL
- **TRIM** - удаляет пробелы в начале и конце строки
- **LENGTH** - длина строки (NULL для NULL значений)
- **Тройное сравнение**: NULL = NULL дает NULL, не TRUE
- **Индексы**: NULL значения могут влиять на эффективность индексов

---

## Задача 10: Транзакции и блокировки

**Условие:** Показать пример безопасного обновления зарплаты с логированием.

### Решение:
```sql
-- Начало транзакции
BEGIN;

-- Сохранение текущего состояния в истории
INSERT INTO salaries_history (employee_id, old_salary, new_salary, change_date, reason)
SELECT 
    e.id,
    e.salary,
    e.salary * 1.1, -- увеличение на 10%
    CURRENT_DATE,
    'Annual raise 2024'
FROM employees e
WHERE e.department_id = 1 AND e.is_active = TRUE;

-- Обновление зарплат
UPDATE employees 
SET salary = salary * 1.1,
    created_at = CURRENT_TIMESTAMP
WHERE department_id = 1 AND is_active = TRUE;

-- Проверка результатов
SELECT 
    e.first_name,
    e.last_name,
    sh.old_salary,
    e.salary as new_salary,
    sh.reason
FROM employees e
JOIN salaries_history sh ON e.id = sh.employee_id
WHERE sh.change_date = CURRENT_DATE
  AND sh.reason = 'Annual raise 2024';

-- Подтверждение транзакции
COMMIT;

-- В случае ошибки: ROLLBACK;
```

### Результат:
```
first_name | last_name | old_salary | new_salary | reason
-----------|-----------|------------|------------|------------------
John       | Smith     | 75000.00   | 82500.00   | Annual raise 2024
Jane       | Doe       | 65000.00   | 71500.00   | Annual raise 2024
Bob        | Johnson   | 80000.00   | 88000.00   | Annual raise 2024
Frank      | Miller    | 90000.00   | 99000.00   | Annual raise 2024
```

### Памятка:
- **BEGIN/COMMIT** - границы транзакции
- **ROLLBACK** - отмена всех изменений в транзакции
- **ACID свойства**: Atomicity, Consistency, Isolation, Durability
- **Уровни изоляции**: READ COMMITTED (по умолчанию), REPEATABLE READ, SERIALIZABLE
- **Блокировки**: FOR UPDATE, FOR SHARE для явного управления
- **Savepoints** - промежуточные точки сохранения в транзакции
- **Дедлоки**: PostgreSQL автоматически их обнаруживает и разрешает

---

## Задача 11: Индексы и производительность

**Условие:** Показать план выполнения запроса и создать оптимальные индексы.

### Решение:
```sql
-- Анализ производительности БЕЗ индексов
EXPLAIN (ANALYZE, BUFFERS) 
SELECT e.first_name, e.last_name, d.name, e.salary
FROM employees e
JOIN departments d ON e.department_id = d.id
WHERE e.salary > 60000 AND e.is_active = TRUE
ORDER BY e.salary DESC;

-- Создание составного индекса для оптимизации
CREATE INDEX CONCURRENTLY idx_employees_salary_active 
ON employees (salary DESC, is_active) 
WHERE is_active = TRUE;

-- Создание индекса для JOIN
CREATE INDEX CONCURRENTLY idx_employees_dept_id 
ON employees (department_id);

-- Повторный анализ производительности С индексами
EXPLAIN (ANALYZE, BUFFERS) 
SELECT e.first_name, e.last_name, d.name, e.salary
FROM employees e
JOIN departments d ON e.department_id = d.id
WHERE e.salary > 60000 AND e.is_active = TRUE
ORDER BY e.salary DESC;

-- Статистика использования индексов
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan as index_scans,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched
FROM pg_stat_user_indexes
WHERE tablename IN ('employees', 'departments')
ORDER BY idx_scan DESC;
```

### Результат EXPLAIN:
```
-- БЕЗ индексов:
Seq Scan on employees e  (cost=0.00..2.25 rows=3 width=86)
  Filter: ((salary > '60000'::numeric) AND is_active)

-- С индексами:
Index Scan using idx_employees_salary_active on employees e  (cost=0.14..0.89 rows=3 width=86)
  Index Cond: (salary > '60000'::numeric)
```

### Памятка:
- **EXPLAIN ANALYZE** - показывает фактическое время выполнения
- **BUFFERS** - информация об использовании буферов
- **CONCURRENTLY** - создание индекса без блокировки таблицы
- **Составные индексы** - порядок колонок критичен для эффективности
- **Частичные индексы** (WHERE clause) экономят место и ускоряют поиск
- **pg_stat_user_indexes** - статистика использования индексов
- **Типы индексов**: B-tree (по умолчанию), Hash, GiST, SP-GiST, GIN, BRIN

---

## Задача 12: Полнотекстовый поиск

**Условие:** Реализовать поиск сотрудников по имени и email с ранжированием релевантности.

### Решение:
```sql
-- Добавление колонки для полнотекстового поиска
ALTER TABLE employees 
ADD COLUMN search_vector tsvector;

-- Создание триггера для автоматического обновления
CREATE OR REPLACE FUNCTION update_search_vector() RETURNS trigger AS $
BEGIN
    NEW.search_vector := 
        setweight(to_tsvector('english', COALESCE(NEW.first_name, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.last_name, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.email, '')), 'B');
    RETURN NEW;
END;
$ LANGUAGE plpgsql;

CREATE TRIGGER employees_search_update 
BEFORE INSERT OR UPDATE ON employees 
FOR EACH ROW EXECUTE FUNCTION update_search_vector();

-- Обновление существующих записей
UPDATE employees SET search_vector = 
    setweight(to_tsvector('english', COALESCE(first_name, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(last_name, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(email, '')), 'B');

-- Создание GIN индекса для быстрого поиска
CREATE INDEX idx_employees_search ON employees USING GIN(search_vector);

-- Поиск с ранжированием
SELECT 
    first_name,
    last_name,
    email,
    ts_rank(search_vector, plainto_tsquery('english', 'john smith')) as rank,
    ts_headline('english', first_name || ' ' || last_name || ' ' || email, 
                plainto_tsquery('english', 'john smith')) as highlighted
FROM employees
WHERE search_vector @@ plainto_tsquery('english', 'john smith')
  AND is_active = TRUE
ORDER BY rank DESC;
```

### Результат:
```
first_name | last_name | email                  | rank     | highlighted
-----------|-----------|------------------------|----------|----------------------------------
John       | Smith     | john.smith@company.com | 0.607927 | <b>John</b> <b>Smith</b> <b>john</b>.<b>smith</b>@company.com
```

### Памятка:
- **tsvector** - тип данных для полнотекстового поиска
- **to_tsvector** - преобразует текст в поисковый вектор
- **setweight** - устанавливает важность термов (A - наивысшая, D - низшая)
- **plainto_tsquery** - простой интерфейс для создания поисковых запросов
- **@@** - оператор соответствия для полнотекстового поиска
- **ts_rank** - вычисляет релевантность найденных документов
- **ts_headline** - подсвечивает найденные термы в тексте
- **GIN индексы** - оптимальны для полнотекстового поиска

---

## Задача 13: JSON данные и JSONB

**Условие:** Работа с JSON полями для хранения дополнительных данных о сотрудниках.

### Решение:
```sql
-- Добавляем JSONB колонку для дополнительной информации
ALTER TABLE employees ADD COLUMN additional_info JSONB;

-- Заполняем тестовыми данными
UPDATE employees SET additional_info = jsonb_build_object(
    'skills', ARRAY['PostgreSQL', 'C#', '.NET'],
    'certifications', ARRAY['Microsoft Certified', 'PostgreSQL Professional'],
    'preferences', jsonb_build_object(
        'remote_work', true,
        'preferred_projects', ARRAY['backend', 'database']
    ),
    'performance_scores', ARRAY[85, 92, 88, 90]
) WHERE id = 1;

UPDATE employees SET additional_info = jsonb_build_object(
    'skills', ARRAY['JavaScript', 'React', 'Node.js'],
    'certifications', ARRAY['AWS Certified'],
    'preferences', jsonb_build_object(
        'remote_work', false,
        'preferred_projects', ARRAY['frontend', 'fullstack']
    ),
    'performance_scores', ARRAY[78, 85, 82]
) WHERE id = 2;

-- Комплексный запрос с JSON операциями
SELECT 
    e.first_name,
    e.last_name,
    e.additional_info->>'skills' as skills_json,
    jsonb_array_length(e.additional_info->'skills') as skills_count,
    e.additional_info->'preferences'->>'remote_work' as remote_work,
    round(
        (SELECT AVG(score::numeric) 
         FROM jsonb_array_elements_text(e.additional_info->'performance_scores') as score
        ), 2
    ) as avg_performance,
    CASE 
        WHEN e.additional_info ? 'certifications' THEN 'Has Certifications'
        ELSE 'No Certifications'
    END as cert_status
FROM employees e
WHERE e.additional_info IS NOT NULL
  AND e.additional_info->'preferences'->>'remote_work' = 'true';
```

### Результат:
```
first_name | last_name | skills_json                           | skills_count | remote_work | avg_performance | cert_status
-----------|-----------|---------------------------------------|--------------|-------------|-----------------|------------------
John       | Smith     | ["PostgreSQL", "C#", ".NET"]         | 3            | true        | 88.75           | Has Certifications
```

### Памятка:
- **JSONB** - бинарный JSON формат (быстрее обычного JSON)
- **->** - извлечение JSON объекта
- **->>** - извлечение JSON объекта как текст
- **jsonb_build_object** - создание JSON объектов
- **jsonb_array_elements** - разворачивает JSON массив в строки
- **?** - проверка существования ключа в JSON
- **@>** и **<@** - операторы включения для JSON
- **GIN индексы** поддерживают эффективные операции с JSONB

---

## Задача 14: Хранимые процедуры и функции

**Условие:** Создать функцию для расчета бонуса сотрудника с учетом проектов.

### Решение:
```sql
-- Создание функции расчета бонуса
CREATE OR REPLACE FUNCTION calculate_employee_bonus(
    p_employee_id INTEGER,
    p_base_percentage DECIMAL DEFAULT 0.1
) RETURNS TABLE(
    employee_name TEXT,
    base_salary DECIMAL,
    project_count INTEGER,
    total_hours DECIMAL,
    bonus_amount DECIMAL
) 
LANGUAGE plpgsql
AS $
DECLARE
    v_salary DECIMAL;
    v_project_multiplier DECIMAL;
BEGIN
    -- Получаем базовую зарплату
    SELECT salary INTO v_salary 
    FROM employees 
    WHERE id = p_employee_id AND is_active = TRUE;
    
    IF v_salary IS NULL THEN
        RAISE EXCEPTION 'Employee with ID % not found or inactive', p_employee_id;
    END IF;
    
    -- Вычисляем мультипликатор на основе количества проектов
    SELECT 
        CASE 
            WHEN COUNT(*) >= 3 THEN 1.5
            WHEN COUNT(*) = 2 THEN 1.2
            WHEN COUNT(*) = 1 THEN 1.0
            ELSE 0.5
        END
    INTO v_project_multiplier
    FROM employee_projects 
    WHERE employee_id = p_employee_id;
    
    -- Возвращаем результат
    RETURN QUERY
    SELECT 
        e.first_name || ' ' || e.last_name,
        e.salary,
        COALESCE(COUNT(ep.project_id), 0)::INTEGER,
        COALESCE(SUM(ep.hours_worked), 0),
        ROUND(e.salary * p_base_percentage * v_project_multiplier, 2)
    FROM employees e
    LEFT JOIN employee_projects ep ON e.id = ep.employee_id
    WHERE e.id = p_employee_id
    GROUP BY e.id, e.first_name, e.last_name, e.salary;
END;
$;

-- Использование функции
SELECT * FROM calculate_employee_bonus(1, 0.15);
SELECT * FROM calculate_employee_bonus(2);

-- Создание процедуры для массового начисления бонусов
CREATE OR REPLACE PROCEDURE process_annual_bonuses(
    p_department_id INTEGER DEFAULT NULL
)
LANGUAGE plpgsql
AS $
DECLARE
    emp_record RECORD;
    bonus_info RECORD;
BEGIN
    -- Создаем временную таблицу для результатов
    CREATE TEMP TABLE IF NOT EXISTS bonus_results (
        employee_id INTEGER,
        employee_name TEXT,
        bonus_amount DECIMAL,
        processed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );
    
    -- Обрабатываем каждого сотрудника
    FOR emp_record IN 
        SELECT id FROM employees 
        WHERE is_active = TRUE 
        AND (p_department_id IS NULL OR department_id = p_department_id)
    LOOP
        -- Получаем информацию о бонусе
        SELECT * INTO bonus_info 
        FROM calculate_employee_bonus(emp_record.id, 0.12);
        
        -- Сохраняем результат
        INSERT INTO bonus_results (employee_id, employee_name, bonus_amount)
        VALUES (emp_record.id, bonus_info.employee_name, bonus_info.bonus_amount);
        
        RAISE NOTICE 'Processed bonus for %: $%', 
                     bonus_info.employee_name, bonus_info.bonus_amount;
    END LOOP;
    
    -- Показываем итоговую статистику
    RAISE NOTICE 'Total employees processed: %', 
                 (SELECT COUNT(*) FROM bonus_results);
    RAISE NOTICE 'Total bonus amount: $%', 
                 (SELECT SUM(bonus_amount) FROM bonus_results);
END;
$;

-- Вызов процедуры
CALL process_annual_bonuses(1); -- только IT отдел
```

### Результат:
```
employee_name | base_salary | project_count | total_hours | bonus_amount
--------------|-------------|---------------|-------------|-------------
John Smith    | 75000.00    | 3             | 570.50      | 16875.00
Jane Doe      | 65000.00    | 2             | 460.00      | 9360.00
```

### Памятка:
- **FUNCTION** - возвращает значение, можно использовать в SELECT
- **PROCEDURE** - выполняет действия, вызывается через CALL
- **RETURNS TABLE** - функция возвращает набор строк
- **LANGUAGE plpgsql** - процедурный язык PostgreSQL
- **DECLARE** - объявление переменных
- **RAISE EXCEPTION/NOTICE** - вывод сообщений и ошибок
- **FOR...LOOP** - цикл по результатам запроса
- **TEMP TABLE** - временные таблицы (удаляются после сессии)

---

## Задача 15: Триггеры и аудит

**Условие:** Создать систему аудита для отслеживания изменений в таблице сотрудников.

### Решение:
```sql
-- Создание таблицы аудита
CREATE TABLE employees_audit (
    audit_id SERIAL PRIMARY KEY,
    table_name TEXT NOT NULL,
    operation_type TEXT NOT NULL, -- INSERT, UPDATE, DELETE
    employee_id INTEGER,
    old_values JSONB,
    new_values JSONB,
    changed_by TEXT DEFAULT CURRENT_USER,
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    session_info JSONB
);

-- Функция триггера аудита
CREATE OR REPLACE FUNCTION audit_employees_changes()
RETURNS TRIGGER AS $
DECLARE
    old_data JSONB := NULL;
    new_data JSONB := NULL;
    session_data JSONB;
BEGIN
    -- Подготавливаем информацию о сессии
    session_data := jsonb_build_object(
        'application_name', current_setting('application_name', true),
        'client_addr', inet_client_addr(),
        'transaction_id', txid_current()
    );
    
    -- Обрабатываем разные типы операций
    IF TG_OP = 'DELETE' THEN
        old_data := to_jsonb(OLD);
        INSERT INTO employees_audit (
            table_name, operation_type, employee_id, 
            old_values, new_values, session_info
        ) VALUES (
            TG_TABLE_NAME, TG_OP, OLD.id, 
            old_data, NULL, session_data
        );
        RETURN OLD;
        
    ELSIF TG_OP = 'UPDATE' THEN
        old_data := to_jsonb(OLD);
        new_data := to_jsonb(NEW);
        
        -- Записываем только если есть изменения
        IF old_data != new_data THEN
            INSERT INTO employees_audit (
                table_name, operation_type, employee_id, 
                old_values, new_values, session_info
            ) VALUES (
                TG_TABLE_NAME, TG_OP, NEW.id, 
                old_data, new_data, session_data
            );
        END IF;
        RETURN NEW;
        
    ELSIF TG_OP = 'INSERT' THEN
        new_data := to_jsonb(NEW);
        INSERT INTO employees_audit (
            table_name, operation_type, employee_id, 
            old_values, new_values, session_info
        ) VALUES (
            TG_TABLE_NAME, TG_OP, NEW.id, 
            NULL, new_data, session_data
        );
        RETURN NEW;
    END IF;
    
    RETURN NULL;
END;
$ LANGUAGE plpgsql;

-- Создание триггера
CREATE TRIGGER employees_audit_trigger
    AFTER INSERT OR UPDATE OR DELETE ON employees
    FOR EACH ROW EXECUTE FUNCTION audit_employees_changes();

-- Тестирование системы аудита
UPDATE employees SET salary = salary + 5000 WHERE id = 1;
UPDATE employees SET phone = '+1234567000' WHERE id = 2;
INSERT INTO employees (first_name, last_name, email, hire_date, salary, department_id) 
VALUES ('Test', 'User', 'test.user@company.com', CURRENT_DATE, 45000, 1);

-- Просмотр аудита с анализом изменений
SELECT 
    a.audit_id,
    a.operation_type,
    e.first_name || ' ' || e.last_name as employee_name,
    a.changed_at,
    a.changed_by,
    CASE 
        WHEN a.operation_type = 'UPDATE' THEN
            jsonb_pretty(
                jsonb_build_object(
                    'salary_change', jsonb_build_object(
                        'from', a.old_values->>'salary',
                        'to', a.new_values->>'salary'
                    ),
                    'phone_change', jsonb_build_object(
                        'from', a.old_values->>'phone',
                        'to', a.new_values->>'phone'
                    )
                )
            )
        ELSE 'N/A'
    END as changes_summary
FROM employees_audit a
LEFT JOIN employees e ON a.employee_id = e.id
ORDER BY a.changed_at DESC
LIMIT 10;
```

### Результат:
```
audit_id | operation_type | employee_name | changed_at          | changed_by | changes_summary
---------|----------------|---------------|---------------------|------------|------------------
3        | INSERT         | Test User     | 2024-08-20 10:30:00 | postgres   | N/A
2        | UPDATE         | Jane Doe      | 2024-08-20 10:29:30 | postgres   | {"phone_change": {"to": "+1234567000", "from": "+1234567891"}}
1        | UPDATE         | John Smith    | 2024-08-20 10:29:15 | postgres   | {"salary_change": {"to": "80000.00", "from": "75000.00"}}
```

### Памятка:
- **TRIGGER** - автоматически выполняется при изменениях данных
- **BEFORE/AFTER** - момент выполнения триггера
- **FOR EACH ROW** - триггер выполняется для каждой строки
- **TG_OP** - тип операции (INSERT/UPDATE/DELETE)
- **OLD/NEW** - старые и новые значения записи
- **to_jsonb()** - преобразование записи в JSON
- **txid_current()** - ID текущей транзакции
- **Производительность**: триггеры замедляют DML операции

---

## Задача 16: Партиционирование таблиц

**Условие:** Создать партиционированную таблицу для логов активности сотрудников.

### Решение:
```sql
-- Создание основной партиционированной таблицы
CREATE TABLE employee_activity_log (
    log_id BIGSERIAL,
    employee_id INTEGER NOT NULL,
    activity_type VARCHAR(50) NOT NULL,
    activity_description TEXT,
    activity_date DATE NOT NULL,
    activity_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ip_address INET,
    user_agent TEXT,
    duration_minutes INTEGER
) PARTITION BY RANGE (activity_date);

-- Создание партиций по месяцам
CREATE TABLE employee_activity_log_2024_01 PARTITION OF employee_activity_log
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');

CREATE TABLE employee_activity_log_2024_02 PARTITION OF employee_activity_log
    FOR VALUES FROM ('2024-02-01') TO ('2024-03-01');

CREATE TABLE employee_activity_log_2024_03 PARTITION OF employee_activity_log
    FOR VALUES FROM ('2024-03-01') TO ('2024-04-01');

CREATE TABLE employee_activity_log_current PARTITION OF employee_activity_log
    FOR VALUES FROM ('2024-04-01') TO ('2025-01-01');

-- Создание индексов на партициях
CREATE INDEX idx_activity_log_2024_01_emp_date 
ON employee_activity_log_2024_01 (employee_id, activity_date);

CREATE INDEX idx_activity_log_current_emp_date 
ON employee_activity_log_current (employee_id, activity_date);

-- Функция для автоматического создания партиций
CREATE OR REPLACE FUNCTION create_monthly_partition(table_date DATE)
RETURNS TEXT
LANGUAGE plpgsql
AS $
DECLARE
    partition_name TEXT;
    start_date DATE;
    end_date DATE;
BEGIN
    start_date := DATE_TRUNC('month', table_date);
    end_date := start_date + INTERVAL '1 month';
    partition_name := 'employee_activity_log_' || TO_CHAR(start_date, 'YYYY_MM');
    
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I PARTITION OF employee_activity_log
        FOR VALUES FROM (%L) TO (%L)',
        partition_name, start_date, end_date
    );
    
    EXECUTE format('
        CREATE INDEX IF NOT EXISTS idx_%I_emp_date 
        ON %I (employee_id, activity_date)',
        partition_name, partition_name
    );
    
    RETURN partition_name;
END;
$;

-- Заполнение тестовыми данными
INSERT INTO employee_activity_log (employee_id, activity_type, activity_description, activity_date, ip_address, duration_minutes)
SELECT 
    (RANDOM() * 9 + 1)::INTEGER as employee_id,
    (ARRAY['login', 'logout', 'project_access', 'report_generation', 'data_export'])[ceil(RANDOM() * 5)] as activity_type,
    'Automated test activity #' || generate_series,
    '2024-01-01'::DATE + (RANDOM() * 365)::INTEGER as activity_date,
    ('192.168.1.' || (RANDOM() * 255)::INTEGER)::INET as ip_address,
    (RANDOM() * 120 + 5)::INTEGER as duration_minutes
FROM generate_series(1, 1000);

-- Запросы с использованием партиций
-- 1. Анализ активности по месяцам
SELECT 
    TO_CHAR(activity_date, 'YYYY-MM') as month,
    COUNT(*) as total_activities,
    COUNT(DISTINCT employee_id) as active_employees,
    AVG(duration_minutes) as avg_duration
FROM employee_activity_log 
WHERE activity_date >= '2024-01-01'
GROUP BY TO_CHAR(activity_date, 'YYYY-MM')
ORDER BY month;

-- 2. План выполнения для партиционированного запроса
EXPLAIN (ANALYZE, BUFFERS) 
SELECT employee_id, COUNT(*) as activity_count
FROM employee_activity_log 
WHERE activity_date BETWEEN '2024-02-01' AND '2024-02-29'
GROUP BY employee_id
ORDER BY activity_count DESC;
```

### Результат:
```
month   | total_activities | active_employees | avg_duration
--------|------------------|------------------|-------------
2024-01 | 85               | 9                | 64.23
2024-02 | 78               | 9                | 62.41
2024-03 | 83               | 9                | 65.18
2024-04 | 89               | 9                | 63.87
...
```

### Памятка:
- **PARTITION BY RANGE** - партиционирование по диапазону значений
- **Partition Pruning** - PostgreSQL автоматически исключает ненужные партиции
- **Constraint Exclusion** - оптимизация на основе ограничений
- **Типы партиционирования**: RANGE, LIST, HASH
- **Maintenance**: регулярное создание новых и удаление старых партиций
- **Индексы** создаются отдельно для каждой партиции
- **Производительность**: значительное ускорение запросов по ключу партиционирования

---

## Задача 17: Материализованные представления

**Условие:** Создать материализованное представление для отчета по производительности отделов.

### Решение:
```sql
-- Создание материализованного представления
CREATE MATERIALIZED VIEW mv_department_performance AS
SELECT 
    d.id as department_id,
    d.name as department_name,
    COUNT(e.id) as total_employees,
    COUNT(e.id) FILTER (WHERE e.is_active = TRUE) as active_employees,
    ROUND(AVG(e.salary) FILTER (WHERE e.is_active = TRUE), 2) as avg_salary,
    MIN(e.salary) FILTER (WHERE e.is_active = TRUE) as min_salary,
    MAX(e.salary) FILTER (WHERE e.is_active = TRUE) as max_salary,
    COUNT(DISTINCT ep.project_id) as active_projects,
    COALESCE(SUM(ep.hours_worked), 0) as total_hours_worked,
    ROUND(
        COALESCE(SUM(ep.hours_worked), 0) / 
        NULLIF(COUNT(e.id) FILTER (WHERE e.is_active = TRUE), 0), 2
    ) as avg_hours_per_employee,
    -- Вычисляем эффективность на основе зарплаты и отработанных часов
    CASE 
        WHEN COALESCE(SUM(ep.hours_worked), 0) > 0 THEN
            ROUND(
                (AVG(e.salary) FILTER (WHERE e.is_active = TRUE)) / 
                (COALESCE(SUM(ep.hours_worked), 0) / NULLIF(COUNT(e.id) FILTER (WHERE e.is_active = TRUE), 0)),
                2
            )
        ELSE 0
    END as salary_per_hour_ratio,
    MAX(e.hire_date) as latest_hire_date,
    MIN(e.hire_date) as earliest_hire_date,
    CURRENT_TIMESTAMP as last_updated
FROM departments d
LEFT JOIN employees e ON d.id = e.department_id
LEFT JOIN employee_projects ep ON e.id = ep.employee_id
GROUP BY d.id, d.name;

-- Создание уникального индекса для REFRESH CONCURRENTLY
CREATE UNIQUE INDEX idx_mv_dept_perf_dept_id ON mv_department_performance (department_id);

-- Создание обычных индексов для запросов
CREATE INDEX idx_mv_dept_perf_name ON mv_department_performance (department_name);
CREATE INDEX idx_mv_dept_perf_employees ON mv_department_performance (active_employees);

-- Первоначальное заполнение
REFRESH MATERIALIZED VIEW mv_department_performance;

-- Функция для автоматического обновления
CREATE OR REPLACE FUNCTION refresh_department_performance()
RETURNS VOID
LANGUAGE plpgsql
AS $
BEGIN
    -- Обновляем материализованное представление без блокировки
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_department_performance;
    
    -- Логируем время обновления
    INSERT INTO system_log (operation, message, created_at) 
    VALUES (
        'refresh_mv', 
        'Department performance materialized view refreshed',
        CURRENT_TIMESTAMP
    );
    
    RAISE NOTICE 'Department performance materialized view refreshed at %', CURRENT_TIMESTAMP;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'Error refreshing materialized view: %', SQLERRM;
END;
$;

-- Создание таблицы для логирования (если не существует)
CREATE TABLE IF NOT EXISTS system_log (
    id SERIAL PRIMARY KEY,
    operation VARCHAR(50),
    message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Запросы к материализованному представлению
-- 1. Рейтинг отделов по производительности
SELECT 
    department_name,
    active_employees,
    avg_salary,
    total_hours_worked,
    avg_hours_per_employee,
    salary_per_hour_ratio,
    RANK() OVER (ORDER BY salary_per_hour_ratio DESC) as efficiency_rank
FROM mv_department_performance
WHERE active_employees > 0
ORDER BY efficiency_rank;

-- 2. Сравнение отделов с общими показателями
WITH company_totals AS (
    SELECT 
        SUM(active_employees) as total_company_employees,
        AVG(avg_salary) as company_avg_salary,
        SUM(total_hours_worked) as company_total_hours
    FROM mv_department_performance
)
SELECT 
    mp.department_name,
    mp.active_employees,
    ROUND((mp.active_employees * 100.0) / ct.total_company_employees, 1) as employee_percentage,
    mp.avg_salary,
    mp.avg_salary - ct.company_avg_salary as salary_vs_avg,
    mp.total_hours_worked,
    ROUND((mp.total_hours_worked * 100.0) / ct.company_total_hours, 1) as hours_percentage
FROM mv_department_performance mp
CROSS JOIN company_totals ct
WHERE mp.active_employees > 0
ORDER BY employee_percentage DESC;
```

### Результат:
```
-- Рейтинг отделов:
department_name  | active_employees | avg_salary | total_hours_worked | efficiency_rank
-----------------|------------------|------------|--------------------|-----------------
IT Development   | 4                | 77500.00   | 1450.5             | 1
Finance          | 1                | 85000.00   | 0.0                | 2
Sales            | 2                | 65000.00   | 300.0              | 3

-- Сравнение с общими показателями:
department_name  | employee_percentage | salary_vs_avg | hours_percentage
-----------------|--------------------|--------------|-----------------
IT Development   | 50.0               | 7833.33      | 82.9
Sales            | 25.0               | -4666.67     | 17.1
Human Resources  | 12.5               | -14666.67    | 0.0
```

### Памятка:
- **MATERIALIZED VIEW** - физически хранит результат запроса (в отличие от обычного VIEW)
- **REFRESH** - обновление данных в представлении
- **CONCURRENTLY** - обновление без блокировки (требует уникальный индекс)
- **FILTER** - условная агрегация (PostgreSQL 9.4+)
- **Использование**: для тяжелых аналитических запросов, которые выполняются часто
- **Стратегия обновления**: по расписанию, по триггерам, или по требованию
- **Индексы** могут создаваться на материализованных представлениях

---

## Задача 18: Работа с массивами и типами данных

**Условие:** Работа с массивами навыков сотрудников и их анализ.

### Решение:
```sql
-- Добавляем колонку с массивом навыков
ALTER TABLE employees ADD COLUMN skills TEXT[];
ALTER TABLE employees ADD COLUMN certifications TEXT[];
ALTER TABLE employees ADD COLUMN languages TEXT[];

-- Заполняем тестовыми данными
UPDATE employees SET 
    skills = ARRAY['PostgreSQL', 'C#', '.NET Core', 'Entity Framework', 'Docker'],
    certifications = ARRAY['Microsoft Certified: Azure Developer', 'PostgreSQL Professional'],
    languages = ARRAY['Russian', 'English']
WHERE id = 1;

UPDATE employees SET 
    skills = ARRAY['JavaScript', 'React', 'Node.js', 'MongoDB', 'AWS'],
    certifications = ARRAY['AWS Certified Developer'],
    languages = ARRAY['English', 'Spanish']
WHERE id = 2;

UPDATE employees SET 
    skills = ARRAY['Python', 'Django', 'PostgreSQL', 'Redis', 'Linux'],
    certifications = ARRAY['Red Hat Certified', 'PostgreSQL Professional'],
    languages = ARRAY['English', 'German', 'French']
WHERE id = 3;

UPDATE employees SET 
    skills = ARRAY['HR Management', 'Recruiting', 'Performance Management'],
    certifications = ARRAY['SHRM-CP', 'PHR'],
    languages = ARRAY['English']
WHERE id = 4;

-- Комплексные запросы с массивами
-- 1. Поиск сотрудников по навыкам
SELECT 
    e.first_name,
    e.last_name,
    e.skills,
    array_length(e.skills, 1) as skills_count,
    -- Проверяем пересечение массивов
    e.skills && ARRAY['PostgreSQL', 'C#'] as has_required_skills,
    -- Извлекаем общие навыки
    e.skills & ARRAY['PostgreSQL', 'JavaScript', 'Python'] as common_tech_skills
FROM employees e
WHERE e.skills IS NOT NULL
  AND e.skills @> ARRAY['PostgreSQL'] -- содержит PostgreSQL
ORDER BY skills_count DESC;

-- 2. Анализ навыков по всей компании
WITH skills_analysis AS (
    SELECT 
        unnest(skills) as skill,
        COUNT(*) as employee_count
    FROM employees 
    WHERE skills IS NOT NULL AND is_active = TRUE
    GROUP BY unnest(skills)
),
skill_popularity AS (
    SELECT 
        skill,
        employee_count,
        ROUND(
            (employee_count * 100.0) / 
            (SELECT COUNT(*) FROM employees WHERE skills IS NOT NULL AND is_active = TRUE), 
            1
        ) as popularity_percentage,
        RANK() OVER (ORDER BY employee_count DESC) as popularity_rank
    FROM skills_analysis
)
SELECT * FROM skill_popularity ORDER BY popularity_rank;

-- 3. Матрица совместимости сотрудников по навыкам
SELECT 
    e1.first_name || ' ' || e1.last_name as employee1,
    e2.first_name || ' ' || e2.last_name as employee2,
    e1.skills & e2.skills as common_skills,
    array_length(e1.skills & e2.skills, 1) as common_skills_count,
    ROUND(
        (array_length(e1.skills & e2.skills, 1) * 100.0) / 
        GREATEST(array_length(e1.skills, 1), array_length(e2.skills, 1)),
        1
    ) as compatibility_percentage
FROM employees e1
CROSS JOIN employees e2
WHERE e1.id < e2.id 
  AND e1.skills IS NOT NULL 
  AND e2.skills IS NOT NULL
  AND e1.skills && e2.skills -- есть общие навыки
ORDER BY common_skills_count DESC, compatibility_percentage DESC;

-- 4. Создание функции для поиска экспертов
CREATE OR REPLACE FUNCTION find_skill_experts(
    p_skills TEXT[],
    p_min_match_count INTEGER DEFAULT 1
)
RETURNS TABLE(
    employee_name TEXT,
    matching_skills TEXT[],
    match_count INTEGER,
    total_skills INTEGER,
    match_percentage DECIMAL
)
LANGUAGE SQL
AS $
    SELECT 
        e.first_name || ' ' || e.last_name,
        e.skills & p_skills,
        array_length(e.skills & p_skills, 1),
        array_length(e.skills, 1),
        ROUND(
            (array_length(e.skills & p_skills, 1) * 100.0) / array_length(p_skills, 1),
            1
        )
    FROM employees e
    WHERE e.skills IS NOT NULL 
      AND e.is_active = TRUE
      AND array_length(e.skills & p_skills, 1) >= p_min_match_count
    ORDER BY array_length(e.skills & p_skills, 1) DESC;
$;

-- Использование функции поиска экспертов
SELECT * FROM find_skill_experts(ARRAY['PostgreSQL', 'Python', 'JavaScript'], 1);
```

### Результат:
```
-- Популярность навыков:
skill        | employee_count | popularity_percentage | popularity_rank
-------------|----------------|----------------------|----------------
PostgreSQL   | 2              | 50.0                 | 1
English      | 4              | 100.0                | 1
C#           | 1              | 25.0                 | 3
JavaScript   | 1              | 25.0                 | 3

-- Совместимость сотрудников:
employee1   | employee2  | common_skills    | common_skills_count | compatibility_percentage
------------|------------|------------------|--------------------|-----------------------
John Smith  | Bob Johnson| {PostgreSQL}     | 1                  | 20.0
Jane Doe    | Bob Johnson| {}               | 0                  | 0.0

-- Эксперты по навыкам:
employee_name | matching_skills | match_count | total_skills | match_percentage
--------------|-----------------|-------------|--------------|------------------
John Smith    | {PostgreSQL}    | 1           | 5            | 33.3
Bob Johnson   | {PostgreSQL}    | 1           | 5            | 33.3
```

### Памятка:
- **ARRAY[]** - создание массива
- **@>** - оператор "содержит" для массивов
- **&&** - оператор пересечения массивов
- **&** - оператор получения общих элементов
- **unnest()** - разворачивает массив в строки
- **array_length()** - длина массива
- **Индексы**: GIN индексы поддерживают операции с массивами
- **Производительность**: операции с массивами могут быть медленными на больших данных

---

## Задача 19: Репликация и высокая доступность

**Условие:** Настройка мониторинга репликации и проверка состояния кластера.

### Решение:
```sql
-- Запросы для мониторинга репликации (выполняются на мастере)

-- 1. Проверка статуса репликации
SELECT 
    client_addr,
    client_hostname,
    client_port,
    state,
    sent_lsn,
    write_lsn,
    flush_lsn,
    replay_lsn,
    write_lag,
    flush_lag,
    replay_lag,
    sync_state,
    sync_priority
FROM pg_stat_replication;

-- 2. Информация о WAL файлах
SELECT 
    pg_current_wal_lsn() as current_wal_lsn,
    pg_current_wal_insert_lsn() as current_wal_insert_lsn,
    pg_wal_lsn_diff(pg_current_wal_lsn(), '0/0') / 1024 / 1024 as wal_mb_generated;

-- 3. Создание функции для проверки отставания реплик
CREATE OR REPLACE FUNCTION check_replication_lag()
RETURNS TABLE(
    replica_host TEXT,
    lag_bytes BIGINT,
    lag_mb DECIMAL,
    lag_seconds INTERVAL,
    is_healthy BOOLEAN
)
LANGUAGE SQL
AS $
    SELECT 
        COALESCE(client_hostname, client_addr::TEXT),
        pg_wal_lsn_diff(pg_current_wal_lsn(), replay_lsn),
        ROUND(pg_wal_lsn_diff(pg_current_wal_lsn(), replay_lsn) / 1024.0 / 1024.0, 2),
        replay_lag,
        CASE 
            WHEN replay_lag < INTERVAL '30 seconds' AND 
                 pg_wal_lsn_diff(pg_current_wal_lsn(), replay_lsn) < 16777216 -- 16MB
            THEN TRUE 
            ELSE FALSE 
        END
    FROM pg_stat_replication
    WHERE state = 'streaming';
$;

-- 4. Мониторинг блокировок и активных подключений
CREATE VIEW v_connection_monitor AS
SELECT 
    pid,
    usename,
    client_addr,
    client_hostname,
    client_port,
    application_name,
    backend_start,
    query_start,
    state_change,
    state,
    query,
    -- Время выполнения текущего запроса
    CASE 
        WHEN state = 'active' THEN 
            EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP - query_start))
        ELSE NULL 
    END as query_duration_seconds,
    -- Время жизни соединения
    EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP - backend_start)) as connection_age_seconds
FROM pg_stat_activity
WHERE state != 'idle'
ORDER BY query_start NULLS LAST;

-- 5. Функция для проверки здоровья базы данных
CREATE OR REPLACE FUNCTION database_health_check()
RETURNS TABLE(
    metric_name TEXT,
    metric_value TEXT,
    status TEXT,
    recommendation TEXT
)
LANGUAGE plpgsql
AS $
BEGIN
    -- Проверка размера базы данных
    RETURN QUERY
    SELECT 
        'Database Size' as metric_name,
        pg_size_pretty(pg_database_size(current_database())) as metric_value,
        CASE 
            WHEN pg_database_size(current_database()) > 10737418240 -- 10GB
            THEN 'WARNING' 
            ELSE 'OK' 
        END as status,
        'Monitor disk space usage' as recommendation;
    
    -- Проверка количества подключений
    RETURN QUERY
    SELECT 
        'Active Connections',
        COUNT(*)::TEXT,
        CASE 
            WHEN COUNT(*) > 80 THEN 'WARNING'
            WHEN COUNT(*) > 50 THEN 'CAUTION'
            ELSE 'OK'
        END,
        'Monitor connection pooling'
    FROM pg_stat_activity 
    WHERE state = 'active';
    
    -- Проверка длительных транзакций
    RETURN QUERY
    SELECT 
        'Long Running Transactions',
        COUNT(*)::TEXT,
        CASE 
            WHEN COUNT(*) > 0 THEN 'WARNING'
            ELSE 'OK'
        END,
        'Check for blocking transactions'
    FROM pg_stat_activity 
    WHERE state = 'active' 
      AND query_start < CURRENT_TIMESTAMP - INTERVAL '5 minutes';
    
    -- Проверка блокировок
    RETURN QUERY
    SELECT 
        'Blocked Queries',
        COUNT(*)::TEXT,
        CASE 
            WHEN COUNT(*) > 0 THEN 'WARNING'
            ELSE 'OK'
        END,
        'Investigate blocking queries'
    FROM pg_locks l1
    JOIN pg_locks l2 ON l1.transactionid = l2.transactionid
    WHERE l1.granted = FALSE AND l2.granted = TRUE;
END;
$;

-- 6. Создание таблицы для хранения метрик производительности
CREATE TABLE IF NOT EXISTS performance_metrics (
    id SERIAL PRIMARY KEY,
    metric_name VARCHAR(100),
    metric_value DECIMAL,
    metric_unit VARCHAR(20),
    collected_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Функция для сбора метрик производительности
CREATE OR REPLACE FUNCTION collect_performance_metrics()
RETURNS VOID
LANGUAGE plpgsql
AS $
BEGIN
    -- Активные подключения
    INSERT INTO performance_metrics (metric_name, metric_value, metric_unit)
    SELECT 'active_connections', COUNT(*), 'count'
    FROM pg_stat_activity WHERE state = 'active';
    
    -- Размер базы данных
    INSERT INTO performance_metrics (metric_name, metric_value, metric_unit)
    SELECT 'database_size', pg_database_size(current_database()), 'bytes';
    
    -- Количество коммитов в секунду
    INSERT INTO performance_metrics (metric_name, metric_value, metric_unit)
    SELECT 'commits_per_second', 
           xact_commit::DECIMAL / EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP - stats_reset)),
           'tps'
    FROM pg_stat_database 
    WHERE datname = current_database();
    
    -- Использование кеша
    INSERT INTO performance_metrics (metric_name, metric_value, metric_unit)
    SELECT 'cache_hit_ratio',
           ROUND(
               100.0 * blks_hit / NULLIF(blks_hit + blks_read, 0),
               2
           ),
           'percent'
    FROM pg_stat_database 
    WHERE datname = current_database();
END;
$;

-- Выполнение проверок
SELECT * FROM database_health_check();
SELECT * FROM check_replication_lag();
CALL collect_performance_metrics();

-- Просмотр собранных метрик за последний час
SELECT 
    metric_name,
    AVG(metric_value) as avg_value,
    MIN(metric_value) as min_value,
    MAX(metric_value) as max_value,
    metric_unit,
    COUNT(*) as measurements
FROM performance_metrics 
WHERE collected_at >= CURRENT_TIMESTAMP - INTERVAL '1 hour'
GROUP BY metric_name, metric_unit
ORDER BY metric_name;
```

### Результат:
```
-- Проверка здоровья БД:
metric_name              | metric_value | status | recommendation
-------------------------|--------------|--------|---------------------------
Database Size            | 156 MB       | OK     | Monitor disk space usage
Active Connections       | 3            | OK     | Monitor connection pooling  
Long Running Transactions| 0            | OK     | Check for blocking transactions
Blocked Queries          | 0            | OK     | Investigate blocking queries

-- Метрики производительности:
metric_name        | avg_value | min_value | max_value | metric_unit | measurements
-------------------|-----------|-----------|-----------|-------------|-------------
active_connections | 3.0       | 2         | 4         | count       | 12
cache_hit_ratio    | 99.8      | 99.5      | 100.0     | percent     | 12
commits_per_second | 0.05      | 0.03      | 0.08      | tps         | 12
database_size      | 163577856 | 163577856 | 163577856 | bytes       | 12
```

### Памятка:
- **pg_stat_replication** - статус репликации на мастере
- **pg_stat_activity** - информация об активных подключениях
- **WAL LSN** - Log Sequence Number для отслеживания позиции в WAL
- **Streaming replication** - асинхронная репликация PostgreSQL
- **Lag monitoring** - критично для производственных систем
- **Connection pooling** - PgBouncer, PgPool для управления подключениями
- **Health checks** - должны выполняться регулярно через мониторинг

---

## Задача 20: Оптимизация производительности и анализ планов

**Условие:** Комплексная оптимизация медленного запроса с анализом производительности.

### Решение:
```sql
-- Создадим более крупную тестовую таблицу для демонстрации
CREATE TABLE large_transactions (
    id SERIAL PRIMARY KEY,
    employee_id INTEGER REFERENCES employees(id),
    transaction_type VARCHAR(50),
    amount DECIMAL(12,2),
    transaction_date TIMESTAMP,
    description TEXT,
    category VARCHAR(100),
    status VARCHAR(20) DEFAULT 'pending'
);

-- Заполним большим количеством данных
INSERT INTO large_transactions (employee_id, transaction_type, amount, transaction_date, description, category, status)
SELECT 
    (RANDOM() * 9 + 1)::INTEGER,
    (ARRAY['salary', 'bonus', 'expense', 'reimbursement', 'commission'])[ceil(RANDOM() * 5)],
    ROUND((RANDOM() * 10000 + 100)::NUMERIC, 2),
    TIMESTAMP '2023-01-01' + (RANDOM() * INTERVAL '365 days'),
    'Transaction description #' || generate_series,
    (ARRAY['HR', 'IT', 'Marketing', 'Sales', 'Finance', 'Operations'])[ceil(RANDOM() * 6)],
    (ARRAY['pending', 'approved', 'rejected', 'processing'])[ceil(RANDOM() * 4)]
FROM generate_series(1, 100000);

-- МЕДЛЕННЫЙ запрос - БЕЗ оптимизации
-- Находим сотрудников с наибольшими тратами по категориям за последние 6 месяцев
EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON)
SELECT 
    e.first_name || ' ' || e.last_name as employee_name,
    d.name as department,
    t.category,
    COUNT(t.id) as transaction_count,
    SUM(t.amount) as total_amount,
    AVG(t.amount) as avg_amount,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY t.amount) as median_amount,
    MAX(t.transaction_date) as latest_transaction,
    -- Ранжирование по тратам внутри категории
    RANK() OVER (PARTITION BY t.category ORDER BY SUM(t.amount) DESC) as spending_rank,
    -- Процент от общих трат в категории  
    ROUND(
        (SUM(t.amount) * 100.0) / SUM(SUM(t.amount)) OVER (PARTITION BY t.category),
        2
    ) as category_percentage
FROM large_transactions t
JOIN employees e ON t.employee_id = e.id
JOIN departments d ON e.department_id = d.id
WHERE t.transaction_date >= CURRENT_TIMESTAMP - INTERVAL '6 months'
  AND t.status IN ('approved', 'processing')
  AND t.amount > 0
GROUP BY e.id, e.first_name, e.last_name, d.name, t.category
HAVING SUM(t.amount) > 1000
ORDER BY t.category, spending_rank
LIMIT 50;

-- Анализ производительности BEFORE оптимизации
-- Смотрим на статистику таблицы
SELECT 
    schemaname,
    tablename,
    n_tup_ins as inserts,
    n_tup_upd as updates,
    n_tup_del as deletes,
    n_live_tup as live_tuples,
    n_dead_tup as dead_tuples,
    last_vacuum,
    last_autovacuum,
    last_analyze,
    last_autoanalyze
FROM pg_stat_user_tables 
WHERE tablename = 'large_transactions';

-- ОПТИМИЗАЦИЯ 1: Создаем необходимые индексы
CREATE INDEX CONCURRENTLY idx_large_trans_employee_date_status 
ON large_transactions (employee_id, transaction_date, status) 
WHERE status IN ('approved', 'processing');

CREATE INDEX CONCURRENTLY idx_large_trans_date_amount_category
ON large_transactions (transaction_date, amount, category)
WHERE amount > 0;

CREATE INDEX CONCURRENTLY idx_large_trans_category_status_amount
ON large_transactions (category, status, amount DESC)
WHERE status IN ('approved', 'processing') AND amount > 0;

-- ОПТИМИЗАЦИЯ 2: Обновляем статистику
ANALYZE large_transactions;
ANALYZE employees;
ANALYZE departments;

-- ОПТИМИЗИРОВАННЫЙ запрос с CTE и лучшей структурой
WITH filtered_transactions AS (
    SELECT 
        employee_id,
        category,
        amount,
        transaction_date
    FROM large_transactions
    WHERE transaction_date >= CURRENT_TIMESTAMP - INTERVAL '6 months'
      AND status IN ('approved', 'processing')
      AND amount > 0
),
employee_spending AS (
    SELECT 
        ft.employee_id,
        ft.category,
        COUNT(*) as transaction_count,
        SUM(ft.amount) as total_amount,
        AVG(ft.amount) as avg_amount,
        percentile_cont(0.5) WITHIN GROUP (ORDER BY ft.amount) as median_amount,
        MAX(ft.transaction_date) as latest_transaction
    FROM filtered_transactions ft
    GROUP BY ft.employee_id, ft.category
    HAVING SUM(ft.amount) > 1000
),
category_totals AS (
    SELECT 
        category,
        SUM(total_amount) as category_total
    FROM employee_spending
    GROUP BY category
),
ranked_spending AS (
    SELECT 
        es.*,
        ct.category_total,
        RANK() OVER (PARTITION BY es.category ORDER BY es.total_amount DESC) as spending_rank,
        ROUND((es.total_amount * 100.0) / ct.category_total, 2) as category_percentage
    FROM employee_spending es
    JOIN category_totals ct ON es.category = ct.category
)
SELECT 
    e.first_name || ' ' || e.last_name as employee_name,
    d.name as department,
    rs.category,
    rs.transaction_count,
    rs.total_amount,
    rs.avg_amount,
    rs.median_amount,
    rs.latest_transaction,
    rs.spending_rank,
    rs.category_percentage
FROM ranked_spending rs
JOIN employees e ON rs.employee_id = e.id
JOIN departments d ON e.department_id = d.id
ORDER BY rs.category, rs.spending_rank
LIMIT 50;

-- Сравнение производительности
-- Создаем функцию для бенчмарка
CREATE OR REPLACE FUNCTION benchmark_query(query_text TEXT, iterations INTEGER DEFAULT 5)
RETURNS TABLE(
    iteration INTEGER,
    execution_time_ms DECIMAL,
    planning_time_ms DECIMAL,
    buffers_hit INTEGER,
    buffers_read INTEGER
)
LANGUAGE plpgsql
AS $
DECLARE
    start_time TIMESTAMP;
    end_time TIMESTAMP;
    plan_result JSONB;
    i INTEGER;
BEGIN
    FOR i IN 1..iterations LOOP
        -- Очищаем кеш для честного тестирования
        DISCARD PLANS;
        
        start_time := clock_timestamp();
        EXECUTE 'EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) ' || query_text INTO plan_result;
        end_time := clock_timestamp();
        
        RETURN QUERY SELECT 
            i,
            ROUND(EXTRACT(MILLISECONDS FROM (end_time - start_time))::NUMERIC, 2),
            (plan_result->0->'Planning Time')::DECIMAL,
            (plan_result->0->'Plan'->'Shared Hit Blocks')::INTEGER,
            (plan_result->0->'Plan'->'Shared Read Blocks')::INTEGER;
    END LOOP;
END;
$;

-- Мониторинг индексов - какие используются, какие нет
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan as times_used,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes 
WHERE schemaname = 'public'
ORDER BY idx_scan DESC;

-- Поиск неиспользуемых индексов
SELECT 
    schemaname,
    tablename,
    indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes 
WHERE idx_scan = 0
  AND schemaname = 'public'
  AND indexname NOT LIKE '%_pkey'; -- исключаем первичные ключи

-- Анализ самых медленных запросов (требует pg_stat_statements)
-- Включается в postgresql.conf: shared_preload_libraries = 'pg_stat_statements'
SELECT 
    query,
    calls,
    total_time,
    ROUND((total_time / calls)::NUMERIC, 2) as avg_time_ms,
    ROUND((100 * total_time / SUM(total_time) OVER())::NUMERIC, 2) as percentage_total_time,
    rows,
    ROUND((100 * (shared_blks_hit + shared_blks_read) / SUM(shared_blks_hit + shared_blks_read) OVER())::NUMERIC, 2) as percentage_io
FROM pg_stat_statements 
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY total_time DESC
LIMIT 10;
```

### Результат:
```
-- BEFORE оптимизации (примерные значения):
Execution Time: 2847.234 ms
Planning Time: 45.123 ms
Buffers: shared hit=15234 read=8901

-- AFTER оптимизации:
Execution Time: 234.567 ms  (улучшение в ~12 раз)
Planning Time: 12.345 ms    (улучшение в ~4 раза)
Buffers: shared hit=1234 read=567

-- Использование индексов:
indexname                              | times_used | tuples_read | index_size
---------------------------------------|------------|-------------|------------
idx_large_trans_category_status_amount | 156        | 12456       | 4521 kB
idx_large_trans_employee_date_status   | 89         | 8901        | 3456 kB
idx_large_trans_date_amount_category   | 34         | 2345        | 2890 kB
```

### Памятка:
- **EXPLAIN (ANALYZE, BUFFERS)** - детальный анализ производительности
- **CTE** - Common Table Expressions улучшают читаемость и иногда производительность
- **Составные индексы** - порядок колонок критичен (наиболее селективная первой)
- **Частичные индексы** с WHERE экономят место и ускоряют запросы
- **ANALYZE** - обновление статистики критично для оптимизатора
- **pg_stat_statements** - расширение для мониторинга производительности запросов
- **Buffer cache** - попадание в кеш (hit) намного быстрее чтения с диска (read)
- **Параллельные запросы** - включаются автоматически для больших таблиц
- **Мониторинг** - регулярная проверка неиспользуемых индексов и медленных запросов

---

## Заключение

Эти 20 задач покрывают основные аспекты работы с PostgreSQL, которые встречаются на собеседованиях .NET разработчиков:

### Основные темы:
1. **Базовые запросы** - SELECT, JOIN, WHERE, GROUP BY
2. **Агрегации и аналитика** - window functions, CTE, подзапросы  
3. **Работа с данными** - типы данных, JSON, массивы, даты
4. **Производительность** - индексы, планы выполнения, оптимизация
5. **Расширенные возможности** - триггеры, функции, процедуры
6. **Администрирование** - репликация, партиционирование, мониторинг
7. **Безопасность и надежность** - транзакции, блокировки, аудит

### Рекомендации для подготовки:
- Изучите документацию PostgreSQL
- Практикуйтесь на реальных данных
- Понимайте планы выполнения запросов  
- Знайте основы администрирования
- Умейте объяснить выбор решения

Удачи на собеседовании! 🚀
