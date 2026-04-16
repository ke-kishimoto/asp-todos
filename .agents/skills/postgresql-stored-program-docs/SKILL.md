---
name: postgresql-stored-program-docs
description: Create Japanese documentation for PostgreSQL stored procedures and functions from SQL source files. Use when Codex is given an attached `.sql` file or a repository path to a PostgreSQL procedure/function and needs to analyze parameters, return values, referenced tables, updated tables, processing flow, and then write a Markdown document into a sibling `docs/` directory.
---

# PostgreSQL Stored Program Docs

## Overview

Analyze one PostgreSQL stored program source file and produce a Japanese Markdown document next to that source.
Support both procedures and functions, whether the user attaches the file or specifies its path.

## Workflow

1. Read the target SQL file.
2. Identify whether it defines a procedure or a function.
3. Extract the object name, schema when present, parameters, return value, referenced tables, updated tables, and the overall processing flow.
4. Create or update a Markdown document under the source folder's `docs/` directory.
5. Write the document body in Japanese.

## Target Discovery

Accept either of these inputs:

- An attached SQL file
- A file path inside the repository or local workspace

If the user provides multiple files, document only the file explicitly requested. If the request is ambiguous, make the smallest reasonable assumption or ask a short clarification question.

## Output Location and File Name

Create the output file in a `docs/` subdirectory under the directory that contains the SQL source file.

Use this naming rule:

- Procedure: `procedure_<program-name>.md`
- Function: `function_<program-name>.md`

Normalize the program name for filesystem safety:

- Keep letters, digits, and underscores when possible
- Replace spaces, dots, quotes, and schema separators with underscores
- Use the base object name by default
- Append schema only when needed to avoid collisions

Examples:

- `procedure_upsert_order.md`
- `function_get_active_todos.md`

Create the `docs/` directory if it does not already exist.

## Analysis Rules

### Identify the program kind

Look for `CREATE PROCEDURE`, `CREATE OR REPLACE PROCEDURE`, `CREATE FUNCTION`, or `CREATE OR REPLACE FUNCTION`.

### Extract parameters

Capture:

- Parameter name
- Data type
- Mode when present (`IN`, `OUT`, `INOUT`, `VARIADIC`)
- Default value when present
- A short purpose inferred from usage when it is clear
- Whether the parameter is optional or effectively required

### Extract return value

For functions, document the return type from `RETURNS ...`.
For procedures, state that there is no direct return value unless `OUT` parameters provide output semantics.
If the function returns `TABLE (...)`, summarize the returned columns when it is clear from the definition.

### Find referenced tables

List tables that are read by the stored program, including tables used in:

- `FROM`
- `JOIN`
- `USING`
- `SELECT INTO`
- `PERFORM`
- subqueries
- common table expressions

For each table, add the record selection condition when it is easy to infer from `WHERE`, `JOIN`, or key predicates.
If the filtering logic is long, indirect, or highly dynamic, say that the condition is omitted because it is too complex.

### Find updated tables

List tables modified by:

- `INSERT`
- `UPDATE`
- `DELETE`
- `MERGE`
- `INSERT ... ON CONFLICT DO UPDATE`
- DDL only when it is clearly part of runtime behavior and materially important

For each table, document:

- Operation type
- Updated or inserted columns when identifiable
- Update or delete conditions when identifiable
- Notes about conflict handling, soft delete behavior, audit columns, or bulk processing when relevant

### Summarize the processing flow

Describe the major control flow in a short Japanese narrative, for example:

- validation
- local variable preparation
- lookup steps
- branching
- insert or update steps
- exception handling
- returned result construction

Prefer a concise high-signal summary over line-by-line commentary.

### Add notable supplementary information

Add only information worth calling out, such as:

- use of temporary tables
- dynamic SQL
- exception handling
- transaction boundaries
- idempotency considerations
- performance-sensitive joins or loops
- dependence on helper functions
- side effects outside the main tables

## Writing Rules

Write the generated document in Japanese.
Keep section titles stable and easy to scan.
Do not invent business meaning that is not supported by the SQL.
When an inference is uncertain, state it as an inference.
When information is absent, write the Japanese equivalents of "Not applicable" or "Cannot be determined from the SQL" instead of guessing.

## Document Structure

Follow the template in [references/japanese-template.md](references/japanese-template.md).

Always include these sections in this order:

1. Overview section
2. Program kind section
3. Parameter section
4. Return value section
5. Referenced tables section
6. Updated tables section
7. Overall processing summary section
8. Supplementary notes section

## Quality Bar

Before finishing, verify that:

- the output path is under the source file's sibling `docs/` directory
- the file name follows the `procedure_...` or `function_...` rule
- the document is written in Japanese
- every required section exists even when some content is empty
- table names and parameter names match the SQL spelling as closely as practical
