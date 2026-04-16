---
name: postgresql-stored-program-tests
description: Create E2E test scenarios, seed data, and expected data for PostgreSQL stored programs, mainly functions, inside this repository's E2E project. Use when Codex is given a PostgreSQL procedure/function SQL file or path and should read the source, its sibling `docs/` documentation, and optional table-definition SQL files, then generate a Gauge `.spec` file plus CSV or SQL fixtures under `MyTodo.E2E/specs` and `MyTodo.E2E/fixtures`.
---

# PostgreSQL Stored Program Tests

## Overview

Analyze one stored program and create repository-local E2E artifacts for it.
Prefer function testing via direct SQL execution, but support procedures when the user explicitly asks for them.

## Workflow

1. Read the target stored program SQL file.
2. Read the Markdown document in the source folder's `docs/` directory when available.
3. Read related table-definition SQL files only when needed to infer columns, defaults, keys, or valid seed data.
4. Identify referenced tables, updated tables, parameter behavior, and meaningful test branches.
5. Create the Gauge spec file and seed/expected fixture files inside the E2E project.
6. If the program has multiple plausible behavior branches driven by parameters or conditions, pause and ask the user which cases should be generated.

## Inputs

Accept any of these as the primary source:

- an attached `.sql` file
- a repository path to a procedure/function SQL file
- a local workspace path to a procedure/function SQL file

Use these supporting artifacts when they exist:

- the generated documentation for that stored program under the source folder's `docs/`
- related table schema SQL files
- existing E2E specs or fixtures for naming and style consistency

If the documentation file is missing, continue from SQL analysis alone and mention that assumption in the result.

## Output Targets

Assume the E2E project root is `MyTodo.E2E` unless the repository clearly uses another `*.E2E` project.

Create these files:

- Spec: `MyTodo.E2E/specs/<function-name>.spec`
- Seed fixtures: `MyTodo.E2E/fixtures/<function-name>/seed/<table-name>.csv`
- Seed fixtures when CSV is unsuitable: `MyTodo.E2E/fixtures/<function-name>/seed/<table-name>.sql`
- Expected fixtures: `MyTodo.E2E/fixtures/<function-name>/expected/<table-name>.csv`

Normalize `<function-name>` and `<table-name>` for filesystem safety but preserve recognizability.
Prefer lowercase or the repository's existing naming style consistently within the generated set.

## Scenario Design Rules

Create realistic black-box integration scenarios that verify the stored program's observable effect on database tables.

Always include:

- cleanup of all referenced tables
- cleanup of all updated tables
- seed data loading for all required input tables
- execution of the function or procedure
- assertion of every materially updated table

For function testing, prefer this execution step:

`postgres SQL execution step with SELECT <function-name>(...)`

For procedures, use this when appropriate:

`postgres stored-procedure execution step`

When parameters exist, inline them into the SQL call when testing functions.
Choose parameter values that make the expected table changes easy to verify.

## Ambiguity and User Confirmation

Do not silently generate many scenarios when the behavior branches significantly.
Ask the user for confirmation when any of these apply:

- parameters create two or more materially different branches
- null and non-null inputs both appear important
- success and validation-error cases are both meaningful
- date ranges, flags, or modes imply distinct behaviors
- the docs mention multiple business cases but the request did not choose one

When asking, present a compact proposal with:

1. the candidate scenarios
2. the minimal fixture scope for each
3. your recommended first scenario

If the behavior is simple and one happy-path case is clearly dominant, proceed without asking.

## Fixture Rules

Prefer CSV fixtures.
Use SQL seed files only when CSV cannot express the setup cleanly, such as:

- sequence manipulation
- calling helper functions during setup
- inserting data into complex types
- requiring expressions like `NOW()` or generated UUID logic

Create one seed file per table by default.
Create one expected CSV per updated table by default.
Include only the rows needed for the scenario.
Preserve column names exactly as they exist in PostgreSQL when practical.

When expected verification only needs a subset of columns to prove correctness, include the smallest sufficient set.
When row order matters to the Gauge table comparison, place a stable key column first.

## Spec Authoring Rules

Write the `.spec` file in Japanese.
Follow the existing Gauge step phrases already implemented in `MyTodo.E2E/steps/DbStepImplementation.cs`.
Prefer the PostgreSQL-specific forms that start with `"postgres"` for this skill.
Keep the scenario title concise and descriptive.
Use file references in the existing Gauge style, such as `<table:fixtures/.../seed/...csv>`.

Follow the structure in [references/spec-template.md](references/spec-template.md).

## Analysis Rules

Use the stored program SQL and its docs to infer:

- input tables that must exist before execution
- rows needed to activate the target logic
- output tables and rows that should change
- whether unchanged tables should be ignored
- whether the function returns a value worth asserting separately

When the return value is important and the existing step definitions support it, add a scalar assertion step.
If the return value is not the main contract and table state is the clearer assertion, prioritize table assertions.

## Repository Conventions

For this repository:

- place specs directly under `MyTodo.E2E/specs/` unless the user requests a subfolder
- place fixtures under `MyTodo.E2E/fixtures/<function-name>/`
- consult existing examples such as `MyTodo.E2E/specs/todos/todo-stored.spec`
- reuse the Japanese step wording already implemented in the E2E project

## Quality Bar

Before finishing, verify that:

- the spec path and fixture paths match the requested directory structure
- every referenced fixture file actually exists
- the spec references the created fixture paths correctly
- all referenced and updated tables in the chosen scenario are covered
- CSV headers match intended PostgreSQL column names
- the generated scenario is coherent and executable as written
