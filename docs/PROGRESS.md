# IVX Clean — Build Progress

Running checklist of the phased build. Updated at the end of each phase.

| Phase | Title | Status | Notes |
|---|---|---|---|
| 1 | Scaffold | ✅ Done | Solution + 3 projects + 2 test projects, central package management, analyzers + warnings-as-errors, lock files, MIT license, stub README, CI workflow. Build green, 3 tests pass. |
| 2 | Core safety | ✅ Done | `IPathRoots` (production + sandbox), `SafetyGuard` with defense-in-depth, reparse/final-path interop, Known Folders, protected patterns (`rules/protected.json`), full adversarial + junction-attack test suites. 71 tests pass. |
| 3 | Rules engine | ✅ Done | Rule model + JSON catalog (7 files, 30 rules), `RuleValidator` + `schema.json`, read-only `Scanner` (no reparse following, SafetyGuard per item, progress/cancellation), CLI `scan`. Real-machine scan found ~7.2 GB includable, nothing deleted. 81 tests pass. |
| 4 | Cleaner & quarantine | ✅ Done | `Cleaner` (guard re-check → exists → not reparse → unchanged → delete/quarantine; empty-dir removal bottom-up, never the base; partial report on cancel), `QuarantineStore` (same-volume move, central manifest, restore with keep-both/skip, purge). 92 tests pass. |
| 5 | System operations | ✅ Done | `ServicesManager` (stop→action→restore-in-finally + crash-recovery marker), `IServiceController` (Windows + fake), WMI restore point (with frequency-limit/protection-disabled detection), DISM StartComponentCleanup runner, event-log clear, Recycle Bin (SHQuery/SHEmpty), reboot/servicing probe. All behind interfaces. 100 tests pass. |
| 6 | Software module | ✅ Done | Registry inventory (HKLM 64/WOW/HKCU) + filter (SystemComponent/ParentKeyName/ReleaseType/DisplayName), AppX source (frameworks/resource/system excluded), `SoftwareClassifier` (bloatware → never-list → maybe → keep) with `software.json`, `UninstallCommand` parser (CommandLineToArgvW, MSI /I→/X), registry + AppX uninstaller (current user only). 135 tests pass. |
| 7 | UI | ⬜ Not started | Tokens/themes, shell, all §7 screens + states, viewmodels + tests, en/fr, accessibility. |
| 8 | Polish | ⬜ Not started | Motion, onboarding, settings, update check, CLI, icons, large-tree perf check. |
| 9 | Docs & release | ⬜ Not started | README, RULES.md generator, ARCHITECTURE, QA checklist, sandbox config, templates, release workflow. |
| 10 | Final review | ⬜ Not started | Hostile self-review, re-run tests, then ask before tagging v1.0.0. |

## Environment

- .NET SDK 10.0.401 · runtime 10.0.12 · WindowsDesktop 10.0.12 · Windows 11 · x64
- Repo: https://github.com/64r3al/IVX-Clean (public, `main`)
- Push cadence: push to `origin/main` at the end of each green phase.

## Rules deliberately left out (per the prime directive: when in doubt, leave it out)

- **Steam web cache & per-game shadercache** — live inside the Steam install folder, which is usually `Program Files (x86)` (a protected, no-touch tree) and whose location varies per machine. Cleaning inside a program's install directory violates the guard. Left out of v1.
- **Riot / Rockstar launcher caches** — cache/log locations are inconsistent across versions and not reliably a pure cache. Left out pending verification.
- **New Teams, Zoom caches** — packaged (MSIX) `LocalCache` / uncertain Zoom cache path; risk of touching app state. Classic Teams is included; new Teams deferred.
- **Blender `%TEMP%` cache folders** — the general user-temp rule already prunes stale temp files, and `.blend`/autosave files are guard-protected; a dedicated rule added risk without benefit.
- **DaVinci Resolve CacheClip** — location not reliably provable; left out as the prompt directs.
- **Delivery Optimization cache** — best cleared via the official `Delete-DeliveryOptimizationCache` cmdlet; deferred to the command-runner phase.
- **npm / pip / yarn / dotnet nuget dev caches** — these are `command` rules that shell out to each tool's own cleaner; deferred to the system-operations/command phase (Phase 5) so they are executed safely, not half-implemented.
