--Удаление таблиц перед созданием если существуют
-- Платежи по заказам
drop table if exists payments;
--Заказа
drop table if exists orders;
--Позиции заказа
drop table if exists order_items;
--Товары
drop table if exists products;
--Пользователи
drop table if exists users;


--Создание таблиц
--Пользователи
create table users (
user_id INT primary key,
name text,
email text,
created_at date
);

--Продукты
create table products(
product_id int primary key,
name text,
category text
);

--Заказы
create table orders ( 
order_id int primary key,
user_id int references users(user_id),
order_date date,
status text
);

--Позиции заказа
create table order_items( 
order_id int references orders(order_id),
product_id int references products(product_id),
quantity int, --кол-во
unit_price decimal(10,2) --Цена за единицу товара (10 цифр, 2 после запятой)
);

--Платежи по заказам
create table payments( 
payment_id int primary key,
order_id int references orders(order_id),
amount decimal (10,2), --сумма платежа
paid_at date, --дата оплаты
method text --Способ оплаты
);

-- Users
INSERT INTO users VALUES
(1,'Alice','alice@example.com','2023-11-01'),
(2,'Bob','bob@example.com','2023-11-15'),
(3,'Carol','carol@example.com','2024-02-10'),
(4,'Dave','dave@example.com','2024-03-20'),
(5,'Eve','eve@example.com','2024-04-05'),
(6,'Frank','frank@example.com','2024-05-15'),
(7,'Grace','grace@example.com','2024-06-01'),
(8,'Heidi','heidi@example.com','2024-07-10'),
(9,'Ivan','ivan@example.com','2024-07-20');

-- Products
INSERT INTO products VALUES
(10,'Keyboard','Electronics'),
(11,'Mouse','Electronics'),
(12,'Chair','Furniture'),
(13,'Desk','Furniture'),
(14,'Monitor','Electronics'),
(15,'Headphones','Electronics'),
(16,'Lamp','Furniture'),
(17,'Backpack','Accessories'),
(18,'Notebook','Accessories');

-- Orders
INSERT INTO orders VALUES
(100,1,'2024-01-10','paid'),
(101,1,'2024-03-05','paid'),
(102,2,'2023-12-20','paid'),
(103,2,'2024-06-01','cancelled'),
(104,2,'2024-06-15','paid'),
(105,3,'2024-02-15','paid'),
(106,3,'2024-05-01','pending'),
(107,4,'2024-04-10','paid'),
(108,5,'2024-07-12','paid'),
(109,6,'2023-11-22','paid'),
(110,6,'2024-08-05','paid'),
(111,7,'2024-08-15','paid'),
(112,8,'2024-09-01','cancelled'),
(113,9,'2024-09-10','paid');

-- Order Items
INSERT INTO order_items VALUES
(100,10,1,50.00),
(100,11,2,25.00),    -- 100
(101,12,1,200.00),   -- 200
(102,10,2,50.00),    -- 100 (2023)
(104,10,3,50.00),    -- 150
(105,14,1,300.00),   -- 300
(107,13,1,250.00),   -- 250
(108,15,2,80.00),    -- 160
(109,16,1,40.00),    -- 40 (2023)
(110,17,1,60.00),    -- 60
(111,18,5,5.00),     -- 25
(113,14,2,300.00);   -- 600

-- Payments
INSERT INTO payments VALUES
(500,100,100.00,'2024-01-10','card'),
(501,101,200.00,'2024-03-05','card'),
(502,102,100.00,'2023-12-20','card'),
(503,104,150.00,'2024-06-15','paypal'),
(504,105,300.00,'2024-02-15','paypal'),
(505,107,250.00,'2024-04-10','card'),
(506,108,160.00,'2024-07-12','card'),
(507,109,40.00,'2023-11-22','cash'),
(508,110,60.00,'2024-08-05','paypal'),
(509,111,25.00,'2024-08-15','card'),
(510,113,600.00,'2024-09-10','card');


/*Задача 1 
 * Вывести для каждого пользователя за 2024 год:

user_id, name

orders_count_2024 — число оплаченных заказов (status = 'paid', и есть запись в payments)

total_amount_2024 — сумма по позициям заказа (SUM(quantity*unit_price))

Условие: пользователи без заказов 2024 должны отображаться с нулями.
Сортировка: total_amount_2024 DESC, user_id ASC.
 */

--Решение Задачи 1
SELECT 
    u.user_id,
    u.name,
    COUNT(DISTINCT o.order_id) AS orders_count_2024,
    COALESCE(SUM(oi.quantity * oi.unit_price), 0) AS total_amount_2024
FROM users u
LEFT JOIN orders o 
    ON o.user_id = u.user_id
   AND o.status = 'paid'
   AND o.order_date >= '2024-01-01'
   AND o.order_date < '2025-01-01'
LEFT JOIN payments p 
    ON p.order_id = o.order_id
LEFT JOIN order_items oi 
    ON oi.order_id = o.order_id
GROUP BY u.user_id, u.name
ORDER BY total_amount_2024 DESC, user_id ASC;
         
--Памятка
/*COALESCE(значение, 0)
Функция COALESCE принимает первый НЕ-NULL аргумент
Если SUM(...) = NULL (нет заказов), возвращает 0
Если SUM(...) = число (есть заказы), возвращает это число

-- Эквивалентные варианты:
CASE WHEN SUM(oi.quantity * oi.unit_price) IS NULL 
     THEN 0 
     ELSE SUM(oi.quantity * oi.unit_price) 
END
*/


/*Задача 2 
 * Нужно найти категории товаров, по которым в 2024 году:

общее число проданных единиц (SUM(quantity)) превышает 2 штуки,

и общая выручка (SUM(quantity * unit_price)) превышает 200.

Вывести:
category | total_qty | total_revenue

Отсортировать по total_revenue DESC.

⚠️ Учитываем только заказы:

status = 'paid',
имеющие запись в payments.
 */

SELECT 
    pr.category,  
    SUM(oi.quantity) AS total_qty, 
    SUM(oi.quantity * oi.unit_price) AS total_revenue
FROM orders o 
JOIN payments p 
    ON p.order_id = o.order_id
JOIN order_items oi 
    ON oi.order_id = o.order_id
JOIN products pr 
    ON pr.product_id = oi.product_id
WHERE o.status = 'paid'
  AND o.order_date >= '2024-01-01'
  AND o.order_date < '2025-01-01'
GROUP BY pr.category
HAVING SUM(oi.quantity) > 2 
   AND SUM(oi.quantity * oi.unit_price) > 200
ORDER BY total_revenue DESC;

--Памятка
-- в SQL сначала идёт WHERE, потом GROUP BY, потом HAVING, потом GROUP BY.


/*Задача 3 (Anti-Join / пользователи без заказов в 2024)

Найти всех пользователей, которые в 2024 году:

не сделали ни одного оплаченного заказа (status = 'paid' и есть запись в payments).

Вывести:
user_id | name | created_at

Отсортировать по user_id. */

SELECT u.user_id, u.name, u.created_at
FROM users u
WHERE NOT EXISTS (
    SELECT 1
    FROM orders o
    JOIN payments p ON p.order_id = o.order_id
    WHERE o.user_id = u.user_id
      AND o.status = 'paid'
      AND o.order_date >= '2024-01-01'
      AND o.order_date < '2025-01-01'
)
ORDER BY u.user_id;


--Памятка. Порядок выполнения команд : WHERE, GROUP BY, HAVING, ORDER BY, LIMIT.

/*Задача 4
Найти топ-2 пользователей по сумме оплаченных заказов в 2024 году.
Учитывать только заказы:
status = 'paid',
имеющие запись в payments.

Вывести:
user_id | name | total_amount_2024

Отсортировать по total_amount_2024 DESC, при равенстве — по user_id ASC. */

SELECT 
    u.user_id, 
    u.name, 
    SUM(oi.quantity * oi.unit_price) AS total_amount_2024
FROM users u 
JOIN orders o  ON o.user_id = u.user_id 
JOIN order_items oi ON oi.order_id = o.order_id
JOIN payments p ON p.order_id = o.order_id 
WHERE o.order_date >= '2024-01-01' 
  AND o.order_date < '2025-01-01'
  AND o.status = 'paid'
GROUP BY u.user_id, u.name
ORDER BY total_amount_2024 DESC, u.user_id ASC
LIMIT 2;

/*Задача 5
Найти топ-3 продуктов по общей выручке в 2024 году.
Учитывать только заказы со status = 'paid'.

Вывести:
product_id | product_name | total_revenue | rank_position

Требования:
total_revenue = SUM(quantity * unit_price)
Рейтинг формировать с помощью RANK() OVER (ORDER BY total_revenue DESC)
В итоговой выборке должны быть только 3 первых позиции по рангу, 
но если на 3-м месте есть несколько продуктов с одинаковой выручкой, то вывести всех.
 */

WITH product_revenue AS (
    SELECT 
        p.product_id, 
        p.name, 
        SUM(oi.quantity * oi.unit_price) AS total_revenue,
        RANK() OVER (ORDER BY SUM(oi.quantity * oi.unit_price) DESC) AS rank_position       
    FROM order_items oi 
    JOIN products p ON p.product_id = oi.product_id 
    JOIN orders o ON o.order_id = oi.order_id 
    WHERE o.order_date >= '2024-01-01' 
      AND o.order_date < '2025-01-01'
      AND o.status = 'paid'
    GROUP BY p.product_id, p.name
)
SELECT *
FROM product_revenue
WHERE rank_position <= 3
ORDER BY rank_position, product_id;

--Памятка
/*
SQL выполняется в определённом порядке:
	FROM / JOIN
	WHERE
	GROUP BY
	HAVING
	SELECT (алиасы создаются здесь)
	DISTINCT
	ORDER BY
	LIMIT / OFFSET
Алиасы колонок нельзя использовать в WHERE, можно в SELECT, ORDER BY.
Если нужен alias в фильтрации — используем подзапрос или CTE.
*/

--Памятка
/*CTE (WITH) создает временный результат для использования в основном запросе. 
В Postgres 12+ он по умолчанию не материализуется, то есть планировщик просто подставляет его в основной запрос. 
Если использовать MATERIALIZED, CTE сохраняется во временной таблице в памяти или на диске и может использоваться несколько раз.
*/


/*Задача 6
 Найти пользователей, которые никогда не делали заказ со статусом "cancelled", и посчитать их общую выручку за все оплаченные заказы (status = 'paid').

Вывести:
user_id | name | total_revenue

Требования:
Пользователи без никаких cancelled заказов.
Общая сумма — только для заказов со статусом paid.
Если пользователь не делал заказов, total_revenue = 0.
*/

--1 вариант с cte
WITH without_cancel AS (
    SELECT u.user_id
    FROM users u
    WHERE NOT EXISTS (
        SELECT 1 FROM orders o WHERE o.user_id = u.user_id AND o.status = 'cancelled'
    )
)
SELECT u.user_id, u.name, COALESCE(SUM(oi.quantity*oi.unit_price),0) AS total_revenue
FROM users u
JOIN orders o ON o.user_id = u.user_id AND o.status='paid'
JOIN order_items oi ON oi.order_id = o.order_id
JOIN without_cancel w ON w.user_id = u.user_id
GROUP BY u.user_id, u.name;

--2 вариант с NOT EXISTS
SELECT 
    u.user_id, 
    u.name, 
    COALESCE(SUM(oi.quantity * oi.unit_price), 0) AS total_revenue
FROM users u
LEFT JOIN orders o ON o.user_id = u.user_id AND o.status = 'paid' AND o.order_date >= '2024-01-01' AND o.order_date < '2025-01-01'
LEFT JOIN order_items oi ON oi.order_id = o.order_id
WHERE NOT EXISTS (
    SELECT 1 
    FROM orders o2 
    WHERE o2.user_id = u.user_id AND o2.status = 'cancelled'
)
GROUP BY u.user_id, u.name
ORDER BY total_revenue DESC;


--Памятка
/*
CTE (WITH) — временный результат для использования в основном запросе.

Postgres ≤11:
CTE материализуется по умолчанию → создаётся временная структура (память или диск).
Может быть медленнее, если используется один раз.

Postgres ≥12:
CTE не материализуется по умолчанию → планировщик вставляет его прямо в запрос (inline).
Производительность почти такая же, как если бы использовался подзапрос.

MATERIALIZED:
Явно сохраняет результат CTE во временной таблице.
Полезно, если CTE используется несколько раз.

NOT EXISTS vs CTE:
NOT EXISTS компактнее и эффективнее, когда проверка нужна один раз.

В Postgres 12+ CTE без MATERIALIZED почти эквивалентен NOT EXISTS по производительности.

Совет:
Фильтруйте данные внутри CTE, чтобы передавать меньше строк.
Используйте MATERIALIZED только при повторном использовании CTE.
*/


/*Задача 7

Найти топ-2 самых дорогих заказа для каждого пользователя за 2024 год.

Вывести:
user_id | name | order_id | total_amount

Условия:
Сумма заказа = SUM(quantity * unit_price) по всем позициям.
Только заказы со статусом paid.
Сортировать по пользователю и по сумме заказа по убыванию.
 */

with cte as (
select 
	u.name, 
	o.order_id, 
	sum(oi.quantity * oi.unit_price) as total_amount,
	ROW_NUMBER() OVER(PARTITION BY u.name ORDER BY sum(oi.quantity * oi.unit_price) DESC) as rank
from users u
join orders o on o.user_id  = u.user_id
join order_items oi on oi.order_id =o.order_id
where o.status = 'paid'
group by u.name, o.order_id
)
select name, order_id, total_amount
from cte 
where rank < 3

/*Задача 8:
Вывести пользователей, у которых общая сумма оплаченных заказов за 2024 год больше, 
чем средняя сумма оплаченных заказов среди всех пользователей за тот же период.
*/

with sum_cte as
	(
	select u.name, sum(oi.quantity * oi.unit_price) as total_amount
	from users u 
	join orders o on o.user_id = u.user_id 
	join order_items oi on oi.order_id  = o.order_id 
	where o.order_date >= '2024-01-01'
	  and o.order_date  < '2025-01-01'
	  and status ='paid'
	group by u.name
	),
avg_total as 
	( 
	select avg(total_amount)
	from sum_cte
	)
select name, total_amount, t.avg
from sum_cte
cross join avg_total t
where total_amount > t.avg

--Памятка
/*
в PostgreSQL оконные функции нельзя использовать в HAVING (и в WHERE тоже). 
Их можно применять только в списке SELECT или в ORDER BY.
*/

/*Задача 9:
Найти для каждого пользователя его самый дорогой заказ (по сумме товаров), 
и вывести имя пользователя, номер заказа и сумму.
*/
WITH cte AS (
    SELECT 
        u.name, 
        o.order_id, 
        SUM(oi.quantity * oi.unit_price) AS total_amount,
        ROW_NUMBER() OVER(
            PARTITION BY u.name 
            ORDER BY SUM(oi.quantity * oi.unit_price) DESC
        ) AS rn
    FROM users u
    JOIN orders o ON o.user_id = u.user_id
    JOIN order_items oi ON oi.order_id = o.order_id
    WHERE o.status = 'paid'
    GROUP BY u.name, o.order_id
)
SELECT name, order_id, total_amount
FROM cte 
WHERE rn = 1;


/*Задача 10
Найти пользователей, которые сделали заказы во все месяцы 2024 года, и посчитать общую сумму их покупок за этот период.
 */
with sum_count_cte as
(
    select 
         u.name,
         sum(oi.quantity * oi.unit_price) as total_amount,
         count(distinct date_trunc('month', o.order_date::date)) as months_count
    from users u 
    join orders o on o.user_id = u.user_id 
    join order_items oi on oi.order_id  = o.order_id 
    where o.order_date >= '2024-01-01'
      and o.order_date  < '2025-01-01'
      and status = 'paid'
    group by u.name
)
select
     name
from 
     sum_count_cte
where  
     months_count = 12;
    
    
--Памятка
/*date_trunc — это функция в PostgreSQL, которая обрезает дату или время до нужной границы.

Например:
date_trunc('month', '2024-08-21'::date) → 2024-08-01 00:00:00
date_trunc('year', '2024-08-21'::date) → 2024-01-01 00:00:00
*/
     
  
  
  
		

























