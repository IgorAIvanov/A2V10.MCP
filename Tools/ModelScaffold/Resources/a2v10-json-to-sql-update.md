# A2V10: how JSON from the client is mapped to SQL fields (Update pipeline)

This note describes the *reverse direction* compared to `a2v10-sql-dataset-to-json.md`: when a JSON model comes from the client and is persisted in SQL Server.

The rules below are illustrated by the example in `Tools/ModelScaffold/Resources/AgentExample/agent.sql`.

## 1. Core idea

In A2V10, saving is typically not done by sending a raw JSON document to SQL Server.
Instead, JSON is converted on the server side into **table-valued parameters (TVP)**, and then passed into an `...Update` stored procedure.

Therefore, the field mapping is primarily defined by:

- SQL table types (`CREATE TYPE ... AS TABLE (...)`)
- corresponding `...Metadata` procedures (contract/schema for the model)
- `...Update` procedures that consume TVPs and write to tables (often via `MERGE`)

## 2. The contract is defined by `...Metadata` and `...TableType`

In the example:

- `cat.[Agent.Metadata]` declares a variable of type `cat.[Agent.TableType]` and returns it as metadata:

  - `declare @Agent cat.[Agent.TableType];`
  - `select [Agent!Agent!Metadata] = null, * from @Agent;`

- `cat.[Agent.TableType]` defines the **set of fields that can be saved**, their types and sizes:

  - `Id bigint null`
  - `Name nvarchar(255)`
  - `NameEN nvarchar(255)`
  - `CodeUA nvarchar(16)`
  - `Memo nvarchar(255)`
  - `ActedName nvarchar(255)`
  - `Folder bigint`

This is effectively the persistence schema for the root `Agent` object.

### 2.1. Why `...Metadata` matters

In A2V10, the `...Metadata` procedure is the **authoritative contract** between client and server for *what can be saved* and *how it must be serialized* into SQL parameters.

Even though `cat.[Agent.Metadata]` returns no data rows, it returns the **column schema** (names, types, lengths, nullability) of `cat.[Agent.TableType]` through the recordset marked as:

- `select [Agent!Agent!Metadata] = null, * from @Agent;`

This is used by the runtime/client to:

- build the edit/save payload
- validate/conform values to SQL types and constraints (e.g., `nvarchar(16)`)
- serialize the JSON object into the TVP row(s) passed to `cat.[Agent.Update]`

If a property is not present in the metadata/TVP schema, it is *not part of the save contract* for that update call.

## 3. How JSON is converted to `@Agent cat.[Agent.TableType] readonly`

The `cat.[Agent.Update]` procedure signature:

- `@Agent cat.[Agent.TableType] readonly`

means the client/server runtime must supply a TVP.

### Mapping rule

For a single-object save, the TVP usually contains **one row**.
The correspondence is built by **property name equality**:

- JSON property `Id` → TVP column `Id`
- JSON property `Name` → TVP column `Name`
- JSON property `CodeUA` → TVP column `CodeUA`
- JSON property `Folder` → TVP column `Folder`

If a JSON property is missing or `null`, the corresponding TVP column is sent as `NULL`.

### 3.1. Extra JSON properties

If the JSON object contains properties that are not present in the `...Metadata`/`...TableType` schema, those properties are **outside of the save contract** for this call and are typically ignored by the serialization step.

Practical implication:

- adding a field to the UI model is not enough to persist it;
- you must add the column to `schema.[Model.TableType]` and expose it via `schema.[Model.Metadata]`, and then map it in `schema.[Model.Update]`.

### 3.2. Flattening nested references into scalar TVP columns

The save contract often uses scalar foreign key columns (e.g., `Folder bigint`) while the loaded model may contain nested reference objects (e.g., `Folder: { Id, Name }`).

Common patterns:

- The client may send the scalar key property directly (`Folder: 123`).
- Or it may send the nested object (`Folder: { "Id": 123 }`) and the runtime flattens it to the scalar TVP column.

Which form is used depends on the UI/runtime implementation, but the SQL side still expects the scalar `Folder` column in the TVP.

### 3.3. Data type and length conformance

Because metadata is based on `...TableType`, it carries constraints like:

- `CodeUA nvarchar(16)`
- `Name nvarchar(255)`

The runtime/client typically uses that contract to validate or normalize values before creating the TVP. If values exceed declared lengths or cannot be converted to the declared types, the save call will fail before or during SQL execution.

> Important: Only the fields present in the TVP are available to SQL `...Update`. Nested objects/collections (like `Contacts`, `BankAccounts`, `Contracts`) require their own table types and update procedures.

## 4. How SQL writes fields to database tables

In `cat.[Agent.Update]` the TVP is treated as a source table (`s`) and merged into `cat.Agents` (`t`):

- `merge cat.Agents as t using @Agent as s on t.Id = s.Id`

### Update mapping

Each target column is assigned from the corresponding TVP column:

- `t.[Name] = s.[Name]`
- `t.[Memo] = s.[Memo]`
- `t.[NameEN] = s.[NameEN]`
- `t.CodeUA = s.CodeUA`
- `t.ActedName = s.ActedName`

### Insert mapping

For inserts the mapping is defined explicitly in the `insert (...) values (...)` list:

- `([Name], NameEN, CodeUA, Memo, IsAgent, ActedName, [Parent], UserCreated)`
- `(s.[Name], s.NameEN, s.CodeUA, s.Memo, 1, s.ActedName, s.Folder, @UserId)`

So:

- JSON `Folder` becomes `Parent` in table `cat.Agents`
- `UserCreated` is server-defined (`@UserId`), not taken from JSON

## 5. Why nested JSON (collections) may not be persisted

The JSON produced by `cat.[Agent.Load]` contains nested arrays:

- `Contacts: [...]`
- `BankAccounts: [...]`
- `Contracts: [...]`

But the *save contract* in this example only includes `cat.[Agent.TableType]` (root fields).
Unless there are additional TVPs/procedures like:

- `cat.[Agent.Contacts.TableType]` + `cat.[Agent.Contacts.Update]`
- `cat.[Agent.BankAccounts.TableType]` + `cat.[Agent.BankAccounts.Update]`

those nested objects cannot be saved by `cat.[Agent.Update]`.

### 5.1. Typical pattern for persisting child arrays

To persist child collections (arrays in JSON) the common approach is:

1. Create a dedicated table type for the child rows, including:
   - child `Id`
   - parent key (e.g., `Agent` / `AgentId`)
   - child fields
   - optionally a row state/operation marker (depending on the platform conventions)

2. Add a `...Metadata` procedure for that child type.

3. Add an `...Update` procedure that accepts the child TVP and applies changes to the child table.

4. Or accept multiple TVPs in the root `...Update` if your platform layer supports it (root + children in one roundtrip).

In all cases, the mapping principle remains the same: **JSON → TVP columns (by contract)**, then **TVP → table columns (by SQL mapping in `...Update`)**.

## 6. Practical checklist

1. Add/verify the root TVP type: `schema.[Model.TableType]`.
2. Ensure `schema.[Model.Metadata]` exposes the same fields (so the client knows what is editable/savable).
3. Ensure `schema.[Model.Update]` consumes the TVP and maps TVP columns → table columns.
4. For child arrays, add separate TVP types and update procedures, and map them using parent keys (`AgentId`, etc.).
5. Keep the loaded (`...Load`) and saved (`...TableType`/`...Metadata`) shapes consistent (or explicitly document differences like reference flattening).

## 7. What to inspect in your project to find the exact runtime mapping

To pinpoint the exact code path (where JSON is converted into SQL parameters / TVPs), locate:

- the server-side handler that executes `cat.[Agent.Update]` (often a platform/runtime component)
- the code that builds `SqlParameter` with `SqlDbType.Structured`

If you provide the project where the actual runtime lives (web app / server API), it can be located precisely in the workspace.
