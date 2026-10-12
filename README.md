<div align="center">

# IVX Clean

**A safe, honest PC cleaner for everyone.**

Made by **IVX** · Free and open source · MIT

[![CI](https://github.com/64r3al/IVX-Clean/actions/workflows/ci.yml/badge.svg)](https://github.com/64r3al/IVX-Clean/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/64r3al/IVX-Clean?include_prereleases&sort=semver)](https://github.com/64r3al/IVX-Clean/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-informational)](LICENSE)

</div>

---

IVX Clean is a free, open-source Windows 10/11 cleaner that removes **only** files Windows and apps
rebuild by themselves — caches, temporary files, logs, crash dumps, update leftovers. It is built so
that it is **impossible by design** to delete your documents, programs, drivers, games, saves, mods or
system files.

> Screenshots live in [`design/`](design/) (the approved UI reference). Add runtime captures to
> `docs/images/` once you have run the app.

## The safety promise

- **Allowlist only.** It never "hunts" for junk. Every location it can clean is an explicit, reviewed
  [rule](docs/RULES.md). If a path is not covered by a rule, it does not exist to the cleaner.
- **Scan first, delete later.** Scanning is read-only. Nothing is deleted until you review and confirm.
- **Defense in depth.** Even an allowed rule passes through the **SafetyGuard**, which vetoes protected
  system and personal locations, reparse points (junction/symlink redirection), alternate data streams,
  cloud placeholders and protected file types — anywhere, even inside Temp.
- **Fail safe.** Any doubt — locked file, odd path, permission error, something changed since the scan —
  means *skip*, never "try harder". No take-ownership, no ACL changes, no killing processes.
- **Undo where it matters.** Caution items go to **Quarantine** (restorable for N days). A **System
  Restore point** is offered before cleaning (on by default).
- **No snake oil.** No registry cleaner, no "RAM booster", no driver tools, no service disabling, no
  ads, no tracking, no bundled software, and no network calls except the update check you click.

## What IVX Clean never touches

Your files and folders · Documents, Desktop, Downloads, Pictures, Videos, Music, Saved Games · OneDrive ·
Programs · Drivers & GPU software · Games, saves and mods · Passwords, cookies, history & bookmarks ·
Autosave & recovery files (`.blend`, `.psd`, `.aep`, `.prproj`, Office AutoRecover, …) · Windows system
components. Anti-cheat, drivers, runtimes and your game launchers are also hidden from the uninstaller.

## Cleaning levels

| Level | Name | What it adds | Default handling |
|---|---|---|---|
| 1 | Quick | User/Windows temp, thumbnail & icon caches, crash dumps, error-reporting queue | Delete (safe) |
| 2 | Standard *(recommended)* | Browser, app, communication & launcher caches; GPU/DirectX shader caches; old Windows logs | Delete (safe) |
| 3 | Deep | Windows Update download cache (→ Quarantine), all-profile temp, FiveM caches, Adobe media cache (→ Quarantine) | Delete / Quarantine |
| 4 | Expert | Prefetch, DISM component cleanup, clear event logs, empty Recycle Bin | Opt-in, each with a warning |

The full, generated list is in **[docs/RULES.md](docs/RULES.md)**.

## Download & install

1. Download the latest `IVXClean-win-x64.zip` (or `win-arm64`) from the
   [Releases](https://github.com/64r3al/IVX-Clean/releases) page.
2. **Verify the download** against `SHA256SUMS.txt`:
   ```powershell
   Get-FileHash .\IVXClean-win-x64.zip -Algorithm SHA256
   ```
   Compare the hash to the value in `SHA256SUMS.txt`.
3. Unzip and run `IVXClean.exe`. It requests administrator rights (it cleans system locations).

### SmartScreen note

IVX Clean is **not code-signed yet**, so Windows SmartScreen may warn the first time you run it. This is
expected for new, unsigned open-source apps. Choose **More info → Run anyway** — after you have verified
the SHA-256 above. A signing step is prepared in the release workflow for when a certificate is available.

## Build from source

Requires the **.NET 10 SDK** on Windows 10/11.

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

Run a read-only scan from the CLI (never deletes anything):

```bash
dotnet run --project src/IvxClean.Cli -- scan --level 2
```

## FAQ

**Will this break my games or delete my saves?** No. Game install folders, saves, mods and in-game
shader caches are never targeted, and the SafetyGuard blocks personal folders regardless.

**Does it phone home?** No. The only network request is the update check you trigger from the About page.

**Can I undo a clean?** Caution/Expert items go to Quarantine and can be restored. A restore point is
offered first.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) — especially how to add a rule (schema, proof in `sources`, tests).
Security reports: [SECURITY.md](SECURITY.md).

## License

MIT — see [LICENSE](LICENSE). Copyright (c) 2026 IVX. **Made by IVX.**
