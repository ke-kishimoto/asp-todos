# Comment Guidelines

## Purpose

Add comments to MSSQL code only where the PostgreSQL function already conveys useful intent.
The goal is migration traceability and maintainability, not blanket annotation.

## Good Comment Targets

- why a parameter exists
- why a filter is applied
- why a branch exists
- why a transaction starts or ends
- what an update is intended to change
- why a returned value matters
- why error handling takes a specific path

## Poor Comment Targets

- obvious syntax
- line-by-line paraphrases
- comments copied onto mismatched logic
- comments that hide a behavioral gap instead of reporting it

## Placement

Use standard SQL comments that fit the surrounding file.
Prefer short comment blocks immediately above the relevant statement block.

## Safety

If the PostgreSQL comment depends on logic not present in MSSQL, do not transplant it.
Instead, mention the missing logic in the gap report.
