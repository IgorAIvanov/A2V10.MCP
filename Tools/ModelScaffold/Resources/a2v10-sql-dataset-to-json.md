# A2V10: how an SQL result (`DataSet`) is transformed into a JSON model

This document describes the rules used by an A2V10 client (UI/SDK) to transform **multiple result sets** (`SELECT ...; SELECT ...; ...`) returned by a stored procedure into the final JSON model object.

For an example, see `Tools/ModelScaffold/Resources/AgentExample/agent.sql` (procedure `cat.[Agent.Load]`) and the expected output in `Tools/ModelScaffold/Resources/AgentExample/agentSQLLoadResult.json`.

## 1. Core idea

The client **does not infer the structure “from tables”** and does not “guess” relationships. The model structure is fully defined by **special markers embedded into column aliases** in SQL.

The markup uses the template:

`[<NodeName>!<TypeName>!<Container>] = null`

and additional markers:

`[Property!!Id]`, `[Property!!Name]`, `[...]!RefId`, `[...]!ParentId`, `[!$System!]`, etc.

The procedure returns a sequence of recordsets. Each recordset, based on its markup, adds/populates nodes in the resulting model.

## 2. Recordset → model node: the first “marker column”

Typically, the first column in a `SELECT` is a special marker that tells the client **what to build**.

Marker examples:

- `[Agent!TAgent!Object] = null`
- `[Contacts!TContacts!Array] = null`
- `[!TOwner!Map] = null`
- `[Folders!TFolder!Tree] = null`
- `[!$System!] = null`

This marker defines:

1) `NodeName` — the JSON node name (`Agent`, `Contacts`, `Owner`, `Folders`, `$System`).

2) `TypeName` — a “logical type” (e.g. `TAgent`, `TOwner`).
   It is used to link recordsets together and build nested objects/collections.

3) `Container` — the container kind (how to interpret recordset rows).

### 2.1. Containers and their meaning

Most commonly used containers:

- `Object` — the recordset forms **a single JSON object**.
  Usually the first (or only) row is used.

- `Array` — the recordset forms **an array** of objects.
  Each row becomes an array element.

- `Map` — the recordset forms **a dictionary (map) of objects keyed by Id**.
  Typically used for lookup/reference entities to later “dereference” `RefId` links.

- `Tree` / `Items` — used for hierarchies (e.g., folder trees).

- `LazyArray` — a marker meaning “the collection is loaded separately (lazy)”.
  Often the initial response contains `null`, but the UI knows children can be loaded via another procedure.

## 3. Property names: regular columns

Any column without special markup is added to JSON as a property with the same name.

Example from `cat.[Agent.Load]`:

- `NameEN` → `"NameEN"`
- `CodeUA` → `"CodeUA"`
- `Memo` → `"Memo"`

## 4. Special modifiers `!!`

### 4.1. `!!Id` — object key

`[Id!!Id] = a.Id`

Means:

- the property name is `Id`;
- this is the object **identifier (key)**.

Id is used:

- as a key in `Map` containers;
- as an anchor for `ParentId` relationships;
- as the target key for `RefId` links.

### 4.2. `!!Name` — display name

`[Name!!Name] = a.[Name]`

Means:

- the property name is `Name`;
- this is a “display name” (often used by the UI and platform conventions).

## 5. Object references: `!RefId` + a separate `Map`

Inside an `Object`/`Array` you can declare a property as a reference to another type:

`[Owner!TOwner!RefId] = a.[UserCreated]`

This means:

- the current object will have an `Owner` property;
- the value will be taken from the `TOwner` `Map` where `Id == a.UserCreated`.

For dereferencing to work, the response must include a recordset that builds the map:

`select [!TOwner!Map] = null, [Id!!Id] = u.Id, ...`

Resulting JSON (schematically):

```json
{
  "Agent": {
    "Owner": { "Id": 123, "Name": "..." }
  }
}
```

Similarly in the `Agent.Load` example:

- `BankAccounts[].Currency` uses `TCurrency!RefId` + `[!TCurrency!Map]`
- `BankAccounts[].Bank` uses `TBank!RefId` + `[!TBank!Map]`

## 6. Child collections: `!ParentId`

To attach rows from another recordset as a **child array** of a parent object, use `ParentId`.

Example (agent contacts):

`[!TContacts!Array] = null, ... , [!TAgent.Contacts!ParentId] = c.Agent, ...`

Rule:

- the current recordset builds items of type `TContacts`;
- the parent property is `TAgent.Contacts` (so `TAgent` ends up with a `Contacts` array);
- `ParentId` tells which `TAgent` object (by `Id`) to attach the item to (`c.Agent`).

Result:

```json
{
  "Agent": {
    "Contacts": [ { "Id": 115, "Name": "..." } ]
  }
}
```

В `Agent.Load` аналогично прикрепляются:

- `BankAccounts` через `[!TAgent.BankAccounts!ParentId]`
- `Contracts` через `[!TAgent.Contracts!ParentId]`
- `Names` через `[!TAgent.Names!ParentId]`

## 7. Иерархии (деревья): `Tree`, `Items`, `SubItems`, `HasChildren`

Для деревьев используются контейнеры `Tree`/`Items` и соглашения `SubItems`, `HasChildren`.
В примере это видно в `cat.[Agent.Index]` и `cat.[Agent.Expand]`.

Типовая схема:

- `Tree` — корневой набор узлов.
- `SubItems`/`Items` — дочерние элементы.
- `HasChildren` (часто размеченное как `!!HasChildren` или `!!HasSubItems`) — признак наличия дочерних элементов.
- `LazyArray` — признак, что детей нужно догружать отдельным вызовом (`Expand`).

Конкретные имена свойств (`SubItems`, `HasSubItems`) задаются именно алиасами в SQL.

## 8. Служебные данные: `[!$System!]`

Последний recordset часто используется для системных параметров модели:

`select [!$System!] = null, [!!ReadOnly] = @readOnly;`

или для пейджинга/фильтров (пример в `cat.[Agent.Agents]`):

- `PageSize`, `Offset`
- `SortOrder`, `SortDir`
- `Permissions`
- значения фильтров (`Fragment` и т.п.)

Это формирует отдельный объект `$System`, который UI/клиент использует для поведения (readonly, права, навигация и т.д.).

## 9. Поля вида `$Title`, `$CanSetAsEmpl` и т.п.

Если в SQL-процедуре нет колонок с такими именами, то они **не возникают из DataSet-разметки напрямую**.
Обычно такие поля добавляются на другом уровне:

- в серверной обработке результата (post-processing модели после выполнения SQL),
- в клиентском коде (например, в файлах `*.ts`/шаблонах),

Чтобы определить точное место в конкретном проекте, нужно искать обработчик, который исполняет `cat.[Agent.Load]` и преобразует `DataSet → Model`.

## 10. Минимальный чек-лист при проектировании SQL под модель

1) Для корневого объекта используйте `[..., ..., !Object]` или `[..., ..., !Array]` если в форме предполагается массив объектов.
2) Для дочерних коллекций используйте `!Array` и обязательно `ParentId`.
3) Для ссылочных объектов используйте `!RefId` + отдельный `!Map`.
4) На объектах обязательно отдавайте `[Id!!Id]`.
5) Для UI имён используйте `[Name!!Name]`.
6) Для служебных параметров используйте recordset `[!$System!]`.
