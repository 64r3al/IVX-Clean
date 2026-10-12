# IVX Clean — Build Progress

Running checklist of the phased build. Updated at the end of each phase.

| Phase | Title | Status | Notes |
|---|---|---|---|
| 1 | Scaffold | ✅ Done | Solution + 3 projects + 2 test projects, central package management, analyzers + warnings-as-errors, lock files, MIT license, stub README, CI workflow. Build green, 3 tests pass. |
| 2 | Core safety | ✅ Done | `IPathRoots` (production + sandbox), `SafetyGuard` with defense-in-depth, reparse/final-path interop, Known Folders, protected patterns (`rules/protected.json`), full adversarial + junction-attack test suites. 71 tests pass. |
| 3 | Rules engine | ⬜ Not started | Schema, loader, rule catalog, scanner, catalog tests. Scan-only run on the real machine. |
| 4 | Cleaner & quarantine | ⬜ Not started | Plan execution, re-validation, quarantine/restore/purge, reports + tests. |
| 5 | System operations | ⬜ Not started | Restore point, services manager + crash marker, DISM, event logs, Recycle Bin, process detection. |
| 6 | Software module | ⬜ Not started | Inventory, classifier, uninstall launcher, tests. |
| 7 | UI | ⬜ Not started | Tokens/themes, shell, all §7 screens + states, viewmodels + tests, en/fr, accessibility. |
| 8 | Polish | ⬜ Not started | Motion, onboarding, settings, update check, CLI, icons, large-tree perf check. |
| 9 | Docs & release | ⬜ Not started | README, RULES.md generator, ARCHITECTURE, QA checklist, sandbox config, templates, release workflow. |
| 10 | Final review | ⬜ Not started | Hostile self-review, re-run tests, then ask before tagging v1.0.0. |

## Environment

- .NET SDK 10.0.401 · runtime 10.0.12 · WindowsDesktop 10.0.12 · Windows 11 · x64
- Repo: https://github.com/64r3al/IVX-Clean (public, `main`)
- Push cadence: push to `origin/main` at the end of each green phase.

## Rules deliberately left out

_(Tracked here as the catalog is built, per the prime directive: when in doubt, leave it out.)_
