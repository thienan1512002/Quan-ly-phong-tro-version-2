---
name: incident-fix
description: Diagnose a Production incident and prepare a safe source-code fix through a PR.
---
# Incident Fix

1. Collect evidence:
   - error
   - stack trace
   - affected endpoint/job
   - first occurrence
   - recent deployment/commit
2. Separate symptom from likely root cause.
3. Create `fix/INC-...` branch.
4. Implement the smallest safe fix.
5. Add regression test where practical.
6. Build/test.
7. Run review.
8. Create PR.
9. Notify human approver.
10. Never merge or deploy automatically.

