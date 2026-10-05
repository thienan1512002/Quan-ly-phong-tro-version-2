---
name: unit-testing
description: Add focused tests for changed business logic and regressions.
---
# Unit Testing

For bug fixes:
- Reproduce the bug in a test when practical.
- The regression test should fail before the fix and pass after it.

Cover:
- happy path
- boundary values
- null/invalid inputs
- business-rule failures

Avoid brittle tests coupled to irrelevant implementation details.

