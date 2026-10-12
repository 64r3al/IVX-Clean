---
name: New cleaning rule request
about: Suggest a cache/log location IVX Clean should clean
title: "[rule] "
labels: rule-request
---

**App or component**
Which program or part of Windows creates this data?

**Location(s)**
The folder(s), using tokens where possible (e.g. `{LocalAppData}\Vendor\App\Cache`). It must **not** be
inside the program's install folder, a user's documents, or anywhere with real data.

**Proof it rebuilds itself**
A link or explanation showing this is a pure cache/log that the app or Windows recreates automatically.
Rules without this cannot be accepted.

**Level & risk**
Which level (1 Quick / 2 Standard / 3 Deep / 4 Expert) and whether it should be delete-directly (safe)
or go to Quarantine (caution).
