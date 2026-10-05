---
name: integration-testing
description: Validate integration boundaries such as database, API and authentication behavior.
---
# Integration Testing

Use when changes cross infrastructure boundaries.

Check:
- DB transaction behavior
- API request/response contract
- authorization
- persistence
- external dependency failure handling

Never point automated tests at Production.

