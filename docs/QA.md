# IVX Clean — Manual QA checklist (Windows Sandbox)

Run release builds in **Windows Sandbox** so testing can never touch a real machine.
`tools/sandbox/IVXClean.wsb` maps the `publish/` folder in read-only and opens it.

## Setup
1. `dotnet publish src/IvxClean.App/IvxClean.App.csproj -c Release -r win-x64 --self-contained -o publish`
2. Enable Windows Sandbox (Windows Features) and open `tools/sandbox/IVXClean.wsb`.
3. Inside the sandbox, run `C:\publish\IVXClean.exe` (approve the elevation prompt).

## Screens & flow
- [ ] Dashboard: greeting, level picker, "always protected" chips, **Scan now**.
- [ ] Scanning: progress ring animates, current path scrolls, **Cancel** works, nothing is deleted.
- [ ] Review: category groups, per-item checkboxes, **risk filter** (All/Safe/Caution/Expert),
      per-group select-all (incl. indeterminate), sticky summary with the exact **Clean X** figure,
      restore-point checkbox, **Export list**.
- [ ] Confirm dialog shows the amount and the deleted-vs-quarantine split; **Back to review** cancels.
- [ ] Cleaning runs; Report shows freed total, breakdown, **View log**.
- [ ] Quarantine: a Caution clean appears; **Restore** returns files; **Delete now** confirms first.
- [ ] Unwanted software: tabs populate; drivers/runtimes/anti-cheat/launchers are **absent**; an
      uninstall confirms before running.
- [ ] Settings: theme switch (System/Dark/Light) applies live; temp age & quarantine days persist;
      reset works; open-logs works.
- [ ] About: version, promise checklist, GitHub/Update links.

## Levels & safety
- [ ] Each level scans; Expert hidden unless enabled in Settings; Expert items each warn.
- [ ] Create `Documents\keepme.txt`, a `*.blend`, and a junction from a temp cache into Documents →
      none are ever offered or removed (they show as Protected).
- [ ] Open a browser/Discord → their caches show **Skipped · close X**; closing then rescanning includes them.
- [ ] Restore-point failure path offers **Continue without / Cancel**.
- [ ] Cancel mid-clean → partial report, remaining files intact.

## Presentation & accessibility
- [ ] Light and dark themes both legible; no clipped text.
- [ ] 100% / 150% / 200% display scaling stays crisp.
- [ ] Full keyboard navigation with a visible focus ring; Narrator reads control names.
