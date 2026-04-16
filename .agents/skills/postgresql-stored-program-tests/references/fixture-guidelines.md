# Fixture Guidelines

## Seed Data

Create only the rows required to make the scenario understandable and deterministic.
Prefer readable primary keys and values over large realistic datasets.
When foreign keys exist, include the minimum parent rows needed for insertion to succeed.

## Expected Data

Create expected CSV files for each updated table.
Include the rows expected after the function or procedure runs.
If the table already contains seeded rows that remain unchanged, either:

- include the full final table when that is easiest to reason about, or
- include only the rows that should exist after a cleanup-and-seed flow

Choose whichever approach matches the spec's setup steps.

## Column Selection

Include enough columns to prove the behavior:

- key columns
- columns changed by the stored program
- columns needed to distinguish rows

Avoid adding irrelevant columns that make maintenance noisy.

## SQL Seed Exceptions

Use a `.sql` seed file instead of CSV when you need:

- expressions or database functions in inserted values
- temporary setup objects
- sequence resets
- multi-statement setup logic
- custom types that are awkward in CSV

## Parameter Cases

When parameters naturally split the logic into multiple meaningful cases, prepare a short proposal before generating files.
Example categories:

- matching rows exist vs no matching rows
- null parameter vs explicit parameter
- range includes target rows vs excludes them
- flag enabled vs disabled

Recommend the smallest high-value case first.
