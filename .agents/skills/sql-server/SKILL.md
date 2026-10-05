---
name: sql-server
description: Analyze and change SQL Server data access with strong data-integrity safeguards.
---
# SQL Server

Before query changes:
1. Check keys, constraints and indexes.
2. Check transaction boundaries.
3. Check nullability and concurrency behavior.
4. Estimate result cardinality.

Production access is read-only.

Never execute destructive SQL in Production.
Prefer parameterized queries.
Document index/schema changes explicitly.

