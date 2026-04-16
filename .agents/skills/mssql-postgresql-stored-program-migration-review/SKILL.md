---
name: mssql-postgresql-stored-program-migration-review
description: Review a SQL Server stored procedure or function during PostgreSQL-to-MSSQL migration work. Use when Codex is given an MSSQL stored-program SQL file or path and should compare it with the same-named PostgreSQL function, copy meaningful comments into the MSSQL source, and produce a Markdown report for any behavior-changing differences in control flow, data updates, filtering, transactions, return values, or side effects.
---

# MSSQL PostgreSQL Stored Program Migration Review

## Overview

Annotate an MSSQL stored program by reusing useful comments from the matching PostgreSQL function.
Identify behavior-changing gaps between the two implementations and summarize them in a separate Markdown report without modifying the PostgreSQL source or silently fixing the MSSQL code.

## Workflow

1. Read the target MSSQL stored-program SQL file.
2. Find the same-named PostgreSQL function that represents the migration source of truth.
3. Read both files carefully and align corresponding sections by behavior, not by syntax.
4. Add comments to the MSSQL source where the PostgreSQL function already explains the same logical step and the MSSQL source lacks that comment.
5. Detect any behavior-changing differences that could alter runtime results.
6. Write a separate Markdown gap report with the differences and proposed MSSQL-side fixes.
7. Do not modify the PostgreSQL file.

## Inputs

Accept any of these as the starting point:

- an attached MSSQL `.sql` file
- a repository path to an MSSQL stored procedure or function
- a local workspace path to an MSSQL stored procedure or function

Use additional context when available:

- the same-named PostgreSQL function file
- generated docs for either implementation
- related table definition SQL files
- migration notes in the repository

## Matching Rules

Treat the PostgreSQL function as the comparison baseline.
Try to match by object name first.
If more than one PostgreSQL candidate exists, prefer the one that:

- has the same base object name
- lives in the expected migration source area
- shares the same parameter purpose
- updates the same main tables

If the match is still ambiguous, stop and ask the user which PostgreSQL function is authoritative.

## Comment Transfer Rules

Edit only the MSSQL file.
Never edit the PostgreSQL file.
Copy or adapt comments only when the surrounding logic is substantively the same.
Do not copy comments for logic that is missing or materially different in MSSQL.
Place comments immediately above or alongside the corresponding MSSQL block using the prevailing SQL Server comment style.
Prefer concise comments that explain intent, business meaning, boundary handling, and non-obvious update logic.

Good candidates for transferred comments include:

- parameter intent
- filtering purpose
- join purpose
- branch meaning
- transaction intent
- update purpose
- returned value meaning
- exception handling intent

Do not add comments that merely restate syntax.

## Difference Detection Rules

Ignore purely syntactic or stylistic differences.
Report only differences that could change execution results, operational safety, or externally visible behavior.

Examples of reportable differences:

- different filter predicates
- different join cardinality or missing joins
- different insert, update, delete targets
- different column assignments
- missing transaction boundaries
- different error handling behavior
- different null handling
- different date or timezone behavior
- different return values or output parameters
- missing side effects such as audit writes or status changes
- branching logic present in PostgreSQL but absent in MSSQL
- loops, cursors, or set-based logic that change affected rows

If a difference is uncertain, say that it is a likely gap or a potential gap and explain why.

## Output Files

Always update the MSSQL source file in place with comments when appropriate.

Also create a Markdown gap report in a sibling `docs/` directory under the MSSQL file's folder.
Create the `docs/` directory if it does not exist.

Use this naming rule for the gap report:

- `<mssql-object-name>_migration_gap_report.md`

If a same-folder naming conflict exists, append a short suffix such as `_v2`.

## Gap Report Contents

Follow the template in [references/gap-report-template.md](references/gap-report-template.md).

The report should include:

1. target MSSQL object
2. matched PostgreSQL object
3. summary judgment
4. behavior-changing differences
5. recommended MSSQL-side fixes
6. open questions or assumptions

Write the gap report in Japanese unless the user asks otherwise.

## Editing Rules

Keep the MSSQL program executable after comment insertion.
Do not reorder logic just to make comments easier to place.
Do not implement the proposed fixes unless the user explicitly asks for code changes beyond comment insertion.
When no behavior-changing gap is found, still create the report and state that no material difference was detected.

## Analysis Heuristics

Compare the two implementations by logical stages:

- input validation
- preparation and variable assignment
- lookup queries
- branching
- DML operations
- transaction handling
- error handling
- final return or output

Use table definitions only when needed to understand keys, nullability, or update semantics.

## Quality Bar

Before finishing, verify that:

- only the MSSQL source file was edited
- PostgreSQL source files remain untouched
- every added comment maps to a corresponding PostgreSQL explanation
- the report excludes syntax-only differences
- each reported difference includes a concrete MSSQL-side fix proposal
