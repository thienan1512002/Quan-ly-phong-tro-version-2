# NMV Repository Agent Instructions

## Project Context

Project Name: Quản lý phòng trọ

This is an enterprise business application maintained by Netmarks Vietnam.

## Core Objectives

Prioritize in this order:

1. Correct business logic
2. Data integrity
3. Security
4. Maintainability
5. Regression safety
6. Performance
7. UI consistency

## Working Rules

Before changing non-trivial code:

1. Inspect the current implementation.
2. Identify related modules and dependencies.
3. Reuse existing patterns/components where possible.
4. Identify database and API impact.
5. Write a short implementation plan.
6. Confirm acceptance criteria from available context.

Do not introduce a new architecture when the current architecture can be safely extended.

## Source Control

Never commit directly to:
- `main`
- `master`
- `develop`

Branch naming:

- Feature: `feature/<ticket>-<short-description>`
- Bug: `fix/<ticket>-<short-description>`
- Incident: `fix/INC-<id>-<short-description>`

AI agents may:
- create branches
- edit source
- add tests
- commit
- push non-protected branches
- create pull requests

AI agents must never:
- merge their own pull request
- bypass branch protection
- force push protected branches
- deploy directly to Production

## Database Safety

Production databases are read-only for AI agents.

Never perform destructive Production operations:
- DROP
- TRUNCATE
- DELETE
- ALTER
- uncontrolled UPDATE

Schema changes must be represented as reviewed migrations or SQL scripts.

## Implementation Policy

Prefer the smallest safe change that satisfies the requirement.

Do not:
- perform unrelated refactoring
- duplicate existing services
- hard-code business configuration
- silently swallow exceptions
- weaken validation just to make a test pass

## Verification

Before declaring work complete:

1. Restore dependencies.
2. Build.
3. Run relevant unit tests.
4. Run integration tests when applicable.
5. Review changed files.
6. Check for security/regression risks.
7. Confirm acceptance criteria.
8. Create PR.

A task is not complete if required checks fail.

## Incident Policy

For Production incidents requiring source changes:

1. Gather evidence.
2. Determine likely root cause.
3. Create a dedicated incident branch.
4. Implement a minimal safe fix.
5. Add a regression test where practical.
6. Build and test.
7. Review.
8. Create PR.
9. Notify authorized humans.
10. Wait for explicit human approval.

Do not hot-edit Production source code.

## Definition of Done

- BUILD = PASS
- TEST = PASS
- REVIEW = PASS
- ACCEPTANCE CRITERIA = PASS
- PR CREATED = PASS

