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
- Create a directory for the model, the name of the directory should begin from small letter and be in singular form (for example, "customer", "order", "product"). The directory can be created in subfolders if necessary, (for example, "sales/order", "inventory/product").
- Get reference information from the A2V10 MCP server.
- Define the set and structure of the model fields and describe them in the `model-name.meta.json` file (example: `agent.meta.json`). This file is used to describe the model data structure. In this file you must also specify data types for each field, as well as any constraints or relations to other models. 
- To get reference information, you can use the A2V10 MCP server resources.
- Based on the model structure, create an SQL file named `model-name.sql` that contains SQL code for creating the table, stored procedures, and table types required for the model.
- The list of required procedures for each model includes (`SchemaName` is the database schema name):
	-- SchemaName.[ModelName.Index]
	-- SchemaName.[ModelName.Load]
	-- SchemaName.[ModelName.Metadata]
	-- SchemaName.[ModelName.Update]
	-- SchemaName.[ModelName.Delete]
	-- SchemaName.[ModelName.Fetch]
- Verify that the created SQL file is registered in the `sql.json` file located in the project root. If not, add it to this file.
- Create the `model.json` file that describes the SQL schema, the actions available in this endpoint, and the XAML views, `.ts`, `.sql` files used.
- Create the XAML views and `.ts` files registered in `model.json` for the model. They will be used to display data and interact with the user.
