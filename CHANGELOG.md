# Changelog

All notable changes to IVX Clean are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Core safety engine: `IPathRoots`, the `SafetyGuard` (allowlist + defense in depth), protected
  patterns, reparse-point/final-path resolution, Known Folders.
- Rule catalog (37 rules across 4 levels) with JSON schema, validator and a read-only scanner.
- Cleaner with per-item re-validation, bottom-up empty-directory removal, and partial-report cancel.
- Quarantine with same-volume move, manifest, restore (keep-both/skip) and age-based purge.
- System operations behind interfaces: services manager with crash-recovery marker, WMI restore point,
  DISM component cleanup, event-log clear, Recycle Bin, reboot/servicing probe.
- Unwanted-software module: registry + AppX inventory, classifier with a never-list, uninstall-string
  parser, and a current-user-only uninstaller.
- WPF-UI app: Fluent shell, dark/light design tokens, and the scan → review → clean → report flow,
  plus Quarantine, Unwanted software, Settings and About screens.
- Scan-only CLI, CI workflow, and generated `docs/RULES.md`.

### Known gaps (pre-1.0)
- French localization (`.resx`) and onboarding/visual polish are not yet complete.

## [1.0.0] - TBD
- First public release.
