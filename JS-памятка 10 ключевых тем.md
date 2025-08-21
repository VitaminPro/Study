# JS-памятка. 10 ключевых тем на backend .net разработчика

---

## 1) Типы, приведение и сравнение

**Коротко:** в JS примитивы: `number`, `string`, `boolean`, `bigint`, `symbol`, `undefined`, `null`; и объекты. Приведение типов — частая причина багов.

### Что важно помнить

* `typeof null === 'object'` — историческая особенность.
* `==` делает неявные приведения (опасно). `===` — строгое сравнение, безопаснее.
* Пустая строка, `0`, `NaN`, `null`, `undefined`, `false` → при приведении к boolean дают `false` (falsy).
* `+` выполняет сложение или приведение к строке: `1 + '2' === '12'`.

### Примеры и вывод

```js
console.log(typeof null);            // "object"
console.log(null == undefined);      // true
console.log(null === undefined);     // false

console.log('' == 0);                // true  ('' -> 0)
console.log('' === 0);               // false

console.log(+'42');                  // 42  (унарный плюс)
console.log(String(123));            // "123"
```

### Практическая рекомендация

* Используй `===` и `!==` почти всегда.
* Для проверки наличия значения: `value == null` — быстро проверит `null` или `undefined`.
* Для проверки числа: `Number.isNaN(x)` и `Number.isFinite(x)`.

### Упражнение (быстро)

Что выведет?

```js
console.log([] == 0);
console.log([] === 0);
console.log([].toString());
```

Ожидаемый: `true`, `false`, `""`.

---

## 2) `var` / `let` / `const`, hoisting, TDZ

**Коротко:** `var` — функциональная область видимости и hoisting (переменная объявлена сверху, но `undefined` до инициализации). `let` и `const` — блочные, в TDZ до инициализации.

### Ключевые моменты

* `const` запрещает переназначение идентификатора, но не делает объект immutable.
* Hoisting: объявления `var` «поднимаются», но инициализация остаётся на месте.
* TDZ (temporal dead zone): доступ к `let/const` до объявления → `ReferenceError`.

### Примеры

```js
function foo() {
  console.log(a); // undefined
  var a = 1;

  // console.log(b); // ReferenceError
  let b = 2;
}

const obj = {x:1};
obj.x = 2; // OK
// obj = {}; // TypeError
```

### Частые ошибки

* Использование `var` в цикле + замыкание = все функции видят одно и то же значение переменной.
* Ожидание, что `const` делает глубокую защиту объекта.

### Упражнение

Почему такое поведение?

```js
for (var i=0; i<3; i++){
  setTimeout(()=>console.log(i), 0);
}
// вывод: 3,3,3
```

Потому что `var` одна и та же переменная `i` для всех итераций. Правильнее: `let i`.

---

## 3) `this`, стрелочные функции, `call/apply/bind`

**Коротко:** `this` определяется при вызове (runtime) у обычных функций; стрелочные функции берут `this` из внешнего лексического окружения.

### Правила

* Метод объекта: `obj.fn()` — `this` = `obj`.
* Обычная функция (не метод) — `this` = `undefined` в strict mode (или `window` в non-strict).
* `call/apply` задают `this`.
* `bind` фиксирует `this` и возвращает новую функцию.
* Стрелочная функция — нельзя использовать как конструктор, не имеет собственного `this`.

### Примеры и тонкие случаи

```js
const obj = {
  x: 10,
  getX: function(){ return this.x; }
};
console.log(obj.getX()); // 10

const fn = obj.getX;
console.log(fn()); // undefined (в strict) — потерян контекст

const bound = fn.bind(obj);
console.log(bound()); // 10

// Стрелочная функция как метод — осторожно:
const bad = {
  x: 5,
  getX: () => this.x
};
console.log(bad.getX()); // не 5, this взят из внешнего скоупа
```

### Практика для .NET dev

* При передаче метода как callback используйте `.bind(obj)` или функцию-обёртку: `() => obj.method()` чтобы избежать потерянного `this`.

### Упражнение

Что выведет?

```js
const obj = {a:1, f(){ return ()=>this.a; }};
console.log(obj.f()()); // ?
```

Ответ: `1` — потому что стрелочная функция захватила `this` из метода `f`, а у него `this` = `obj`.

---

## 4) Замыкания (closures) и модули

**Коротко:** функция «помнит» лексическое окружение, где она была создана. Полезно для скрытия состояния.

### Примеры полезных шаблонов

* Инкапсуляция приватных данных:

```js
function makeCounter() {
  let count = 0;
  return {
    inc(){ return ++count; },
    get(){ return count; }
  };
}
const c = makeCounter();
c.inc(); c.get();
```

* `once`, ленивые вычисления, фабрики.

### Проблемы/производительность

* Замыкания удерживают в памяти переменные окружения — могут неосознанно сохранять большие объекты. Однако JS-движки оптимизируют и сборщик мусора работает корректно, если нет внешних ссылок.

### Упражнение

Реализуй `createLogger(prefix)` возвращающий функцию, которая при вызове печатает `${prefix}: ${message}` и считает сколько раз вызывалась (сохраняет счётчик в замыкании).

---

## 5) Promises, async/await, обработка ошибок

**Коротко:** `Promise` решает проблему вложенных callback. `async` функции возвращают `Promise`. `await` можно применять только в `async`.

### Основы и паттерны

* Создание: `new Promise((resolve,reject)=>{...})`.
* `then`/`catch`/`finally`.
* `async function f(){ return 1 }` → возвращает `Promise.resolve(1)`.
* `await p` — если `p` отклонён, бросается исключение — окружай `try/catch`.

### Promise combinators

* `Promise.all([...])` — отклоняется при первом reject.
* `Promise.allSettled([...])` — возвращает статусы всех.
* `Promise.race([...])` — результат первого settled (resolve/reject).
* `Promise.any([...])` — первый успешно resolved (если все reject — AggregateError).

### Пример с обработкой ошибок (fetch)

```js
async function fetchJson(url) {
  const res = await fetch(url);
  if (!res.ok) throw new Error('HTTP ' + res.status);
  return res.json();
}
```

Не делай `if (!res.ok) return null` без логирования — это может скрыть проблему.

### Важный нюанс: concurrent вызовы and caching

Если функция первый раз возвращает Promise (в процессе выполнения), и вы хотите кэшировать результат — храните **сам Promise**. Если Promise отклоняется и по условию нужно повторно попробовать, не кэшируй отклонённый Promise.

### Упражнение

Напиши `wait(ms)` функцию:

```js
function wait(ms) {
  return new Promise(res => setTimeout(res, ms));
}
```

Используй `await wait(1000); console.log('done');`

---

## 6) Event loop, microtasks и macrotasks

**Коротко:** порядок выполнения — синхронный код в стеке, затем microtasks (Promises, `queueMicrotask`), затем macrotasks (`setTimeout`, I/O callbacks). Это важно для понимания асинхронного поведения и тестов.

### Демонстрация порядка

```js
console.log(1);
setTimeout(()=>console.log(2), 0);
Promise.resolve().then(()=>console.log(3));
console.log(4);
// Вывод: 1,4,3,2
```

### Почему это важно

* Использование `await` не переносит выполнение в macrotask — продолжение `async` функции идёт в микрозадачах.
* При гонках и дедлоках понимание очереди задач помогает избежать неожиданных состояний.

### Упражнение

Предскажите вывод:

```js
console.log('A');
Promise.resolve().then(()=>console.log('B'));
queueMicrotask(()=>console.log('C'));
setTimeout(()=>console.log('D'),0);
console.log('E');
// A, E, B, C, D (microtasks: B then C)
```

---

## 7) Объекты, прототипы, классы, наследование

**Коротко:** JS прототипный; `class` — синтаксический сахар поверх прототипов.

### Полезные вещи

* `Object.create(proto)` создаёт новый объект с заданным прототипом.
* `Object.assign` и spread `{...obj}` → поверхностное копирование.
* `instanceof` проверяет в цепочке прототипов.

### Пример наследования

```js
function Animal(name){ this.name = name; }
Animal.prototype.speak = function(){ return this.name; };

class Dog extends Animal {
  bark(){ return this.name + ' bark'; }
}
```

### Проблемы

* Ошибочно считать `{...}` глубоким клоном.
* Мутирование общего объекта в прототипе (например, массив) — все экземпляры разделяют его.

### Упражнение

Что произойдёт:

```js
function A(){ this.arr = [] }
A.prototype.push = function(v){ this.arr.push(v) };
const a1 = new A(), a2 = new A();
a1.push(1);
console.log(a2.arr); // ?
```

Ответ: `[]` — потому что `arr` создаётся в конструкторе на каждом экземпляре; если бы `arr` был в prototype, тогда общий.

---

## 8) Spread, rest, destructuring

**Коротко:** удобный синтаксис для работы с массивами и объектами.

### Примеры

* Spread array:

```js
const a = [1,2];
const b = [...a,3]; // [1,2,3]
```

* Rest in function:

```js
function sum(...nums){ return nums.reduce((s,n)=>s+n,0); }
```

* Destructure:

```js
const {id, ...rest} = {id:1, name:'a'}; // id=1, rest={name:'a'}
```

### Подводные камни

* Spread/assign — поверхностная копия. Ссылки на вложенные объекты сохраняются.
* Не путать rest и spread: `...` в параметрах — rest (сбор), в выражении — spread (распаковка).

### Упражнение

Скопируйте объект поверхностно и измените вложенный объект — что изменится в копии?

---

## 9) Array методы: map/filter/reduce и асинхронность

**Коротко:** `map` трансформирует, `filter` фильтрует, `reduce` агрегирует. Обратите внимание на асинхронные операции внутри `map`.

### Пример синхронного использования

```js
[1,2,3].map(x => x*2); // [2,4,6]
[1,2,3].reduce((s,x)=>s+x,0); // 6
```

### Асинхронность (очень важный момент)

Нельзя использовать `await` напрямую в `Array.prototype.map` так, как ожидают многие:

```js
const results = await Promise.all(arr.map(async x => await fetchAndProcess(x)));
```

Нужно обернуть `map` в `Promise.all` чтобы дождаться всех `async` операций.

### Частые ошибки

* Использовать `map` для сайд-эффектов (лучше `forEach`).
* Ожидать последовательного выполнения от `map` с async функциями (по умолчанию они запускаются параллельно).

### Упражнение

Как выполнить async операции последовательно на массиве?
Ответ: использовать `for...of` с `await` внутри, либо reduce-паттерн:

```js
for (const item of items) {
  await process(item);
}
```

---

## 10) Fetch / HTTP / JSON / CORS — интеграция фронт-бэк

**Коротко:** `fetch` возвращает `Promise`. Важно проверять `response.ok` и работать с заголовками и CORS.

### Правила работы с fetch

```js
async function callApi(url, opts) {
  const res = await fetch(url, opts);
  if (!res.ok) {
    // обработка или бросание ошибки
    const text = await res.text().catch(()=>null);
    throw new Error(`HTTP ${res.status}: ${text}`);
  }
  // если пустой ответ — не вызывать res.json()
  return res.headers.get('Content-Type')?.includes('application/json') ? res.json() : res.text();
}
```

### CORS — кратко для .NET dev

* Браузер блокирует cross-origin запросы, если сервер не отвечает заголовками:

  * `Access-Control-Allow-Origin: https://your.frontend` или `*` (в тестах).
  * Для preflight (OPTIONS) добавьте `Access-Control-Allow-Methods` и `Access-Control-Allow-Headers` и поддерживайте OPTIONS ответ на сервере.
* В ASP.NET Core: `services.AddCors(...)` и `app.UseCors(...)`.

### Ошибки, которые тупо ломают интеграцию

* Не добавили `Content-Type: application/json` при отправке JSON.
* Сервер возвращает cookie, но фронт не отправляет его из-за `credentials`/CORS настроек.
* Двойной `res.json()` — вызовет ошибку (поток уже прочитан).

### Упражнение

Напиши `fetchWithRetry(url, opts, retries=3)` — вызывает fetch и в случае сетевой ошибки или 5xx статуса пробует снова с экспоненциальной задержкой.

---

# Быстрый чеклист перед интервью (на памётку)

* Всегда используй `===` (если не нужен coercion).
* `const` по умолчанию, `let` при необходимости.
* Не теряй `this` при передаче метода — `.bind()` или лямбда-обёртка.
* Обрабатывай `Promise` — `try/catch` внутри `async`.
* Для параллельных async операций — `Promise.all([...])`.
* В тестах и примерах помни: microtasks (Promises) выполняются до setTimeout.
* При интеграции — проверь CORS и заголовки (Content-Type, Authorization).

---

