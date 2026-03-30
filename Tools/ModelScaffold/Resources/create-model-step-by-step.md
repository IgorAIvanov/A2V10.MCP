---
description: 'Instructions for creating a new A2V10 model'
---

# A2V10 model creation rules
- Each model must be created in a separate folder (or nested folders) inside the project. The project name can be obtained from the `appSettings.json` file located in the root of the runnable project, or clarified with the user. The folder name must be unique and reflect the model meaning in singular form (for example, "Customer", "Order", "Product"). 
- Each model must contain the `model.json` file, which describes the SQL schema, the actions available in this endpoint when invoked by URL, and the XAML views, `.ts`, `.sql` files used.
- File examples can be obtained from the A2V10 MCP server resources, as well as from existing models in the project.
- The SQL file must be named after the model and include SQL code to create the table, stored procedures, and other database objects required for the model to work.
- All SQL scripts being created must use an idempotent syntax so that re-running them does not cause errors (for example, use `IF NOT EXISTS` when creating tables and procedures).

# Instructions
- Create a directory for the model. The directory name should be lowercase and singular, and it should match the model name (for example, "customer", "order", "product"). The directory can be created in subfolders if necessary (for example, "sales/order", "inventory/product").
- Create or reuse an existing `modelName.schema.json` file that describes the model schema/contract. You can find an example in the A2V10 MCP server resources at `template://model-scaffold/agent-schema-json`.
- Create or reuse an existing `modelName.meta.json` file that describes the model metadata. You can find an example in the A2V10 MCP server resources at `template://model-scaffold/agent-meta-example`. This file describes the model data structure. You must also specify data types for each field, as well as any constraints or relations to other models.
- When creating `modelName.meta.json`, the `Name` field must match the name of the table that will be created in the database for this model. Existing database objects can be checked using the A2V10 MCP server tool `A2V10.MCP.search_db_objects`. This ensures that the model is correctly linked to the database and can interact with it as expected.
- Based on the model structure, create an SQL file named `modelName.sql` that contains SQL code for creating the table, stored procedures, and table types required for the model. Use the following guidelines when creating the SQL file: 

  `template://model-scaffold/agent-sql-example`, `docs://model-create/sql-dataset-to-json`, `docs://model-create/json-to-sql-update`.
- The list of required procedures for each model includes (`schemaName` is the database schema name):

  ```sql
  -- schemaName.[ModelName.Index]
  -- schemaName.[ModelName.Load]
  -- schemaName.[ModelName.Metadata]
  -- schemaName.[ModelName.Update]
  -- schemaName.[ModelName.Delete]
  -- schemaName.[ModelName.Fetch]
  ```
- Verify that the created SQL file is registered in the `sql.json` file located in the project root. If not, add it to this file.
- Create the `model.json` file that describes the SQL schema, the actions available in this endpoint, and the XAML views, `.ts`, `.sql` files used. Example of the `model.json` file can be found in the A2V10 MCP server resources `template://model-scaffold/model-json`. This file is crucial for the model to work correctly, as it defines how the model interacts with the database and how it is presented to the user. 
- Create the XAML views and `.ts` files registered in `model.json` for the model. They will be used to display data and interact with the user. Examples of these files can be found in the A2V10 MCP server resources 
`template://model-scaffold/index-view`, `template://model-scaffold/edit-dialog`, `template://model-scaffold/browse-dialog`, `template://model-scaffold/index-ts`.
