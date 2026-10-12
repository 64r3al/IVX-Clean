<div align="center">

# IVX Clean

**A safe, honest PC cleaner for everyone.**

Made by **IVX** · Free and open source · MIT

</div>

---

> ⚠️ **Work in progress.** IVX Clean is being built in phases. This README is a stub and will be
> completed (features, screenshots, install steps, SmartScreen & SHA-256 notes) in the documentation phase.

IVX Clean is a free, open-source Windows 10/11 cleaner that removes **only** files Windows and apps
rebuild by themselves — caches, temporary files, logs, crash dumps, update leftovers. It is built so
that it is **impossible by design** to delete your documents, programs, drivers, games, saves, mods or
system files.

## The promise

- **Allowlist only.** It never "hunts" for junk. Every location it can clean is an explicit, reviewed rule.
- **Scan first, delete later.** Scanning is read-only. Nothing is deleted until you review and confirm.
- **Fail safe.** Any doubt — locked file, odd path, reparse point, permission error — means *skip*, never "try harder".
- **Undo where it matters.** Caution/Expert items go to Quarantine (restorable for days). A System Restore point is offered first.
- **No snake oil.** No registry cleaner, no "RAM booster", no driver tools, no ads, no tracking, no bundled software.

## What IVX Clean never touches

Your files and folders · Programs · Drivers & GPU software · Games, saves and mods · Passwords, cookies &
history · Autosave & recovery files · Windows system components.

## Build from source

Requires the **.NET 10 SDK** on Windows 10/11.

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

## License

MIT — see [LICENSE](LICENSE). Copyright (c) 2026 IVX.
