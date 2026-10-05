---
name: dotnet-backend
description: Implement maintainable .NET backend changes using the repository's existing architecture.
---
# .NET Backend

- Follow existing dependency injection and layering.
- Keep business logic out of controllers when the project uses service layers.
- Use async APIs consistently where the surrounding code is async.
- Preserve cancellation tokens when present.
- Validate external input.
- Add logging for operationally relevant failures.
- Avoid catch-all exception handling unless rethrowing or converting at an application boundary.

