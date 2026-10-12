# Contributing to IVX Clean

Thanks for helping make a safe, honest cleaner. The prime directive is simple: **when in doubt, skip —
never delete.** Shipping fewer rules is fine; shipping one dangerous rule is not.

## Adding a cleaning rule

1. Add the rule to the right file under [`rules/catalog/`](../rules/catalog/) following
   [`rules/schema.json`](../rules/schema.json). Required fields: `id`, `name`, `category`, `level`,
   `risk`, `action`, `why`, `sources`.
2. **Prove it is a self-rebuilding cache or log** in `sources` (a URL or a clear rationale). If you
   cannot prove it, do not add it.
3. Use path tokens (`{LocalAppData}`, `{Temp}`, `{EachUserProfile}`, …) — never hard-coded absolute
   paths, and never anything inside a program's install folder or a user's documents.
4. Run the tests:
   ```bash
   dotnet test
   ```
   The catalog tests check the schema, unique ids, `why`/`sources` presence, and — crucially — that the
   rule's resolved targets are **not** a protected location (the SafetyGuard must allow a benign probe).
5. Regenerate the docs:
   ```bash
   dotnet run --project tools/RulesDocGen -- docs/RULES.md
   ```

## Code style

- `Core` must not reference WPF. Put anything that touches the OS behind an interface with a fake.
- `async`/`await` end to end; no `.Result`/`.Wait()`; everything cancellable.
- No empty `catch`. Every catch logs and converts to a reported skip.
- Nullable enabled, warnings as errors, analyzers on. Keep files focused (aim < 400 lines).

## Pull request checklist

- [ ] Build is clean (`dotnet build -c Release -warnaserror`) and all tests pass.
- [ ] New/changed rules have `why` + verified `sources` and pass the catalog safety test.
- [ ] `docs/RULES.md` regenerated if the catalog changed.
- [ ] No rule targets or can reach a protected/system/personal location.
- [ ] Conventional Commit message (`feat:`, `fix:`, `test:`, `docs:`, …).
