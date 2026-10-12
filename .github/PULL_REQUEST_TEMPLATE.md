## What this changes



## Safety checklist

- [ ] Build is clean (`dotnet build -c Release -warnaserror`) and all tests pass.
- [ ] No rule targets or can reach a protected system/personal location (catalog safety test passes).
- [ ] New/changed rules have a plain-language `why` and verified `sources`.
- [ ] `docs/RULES.md` regenerated if the catalog changed.
- [ ] `Core` still has no WPF dependency; new OS access is behind an interface with a fake.
- [ ] No empty `catch`; failures become reported skips. Everything cancellable.

## Notes for reviewers


