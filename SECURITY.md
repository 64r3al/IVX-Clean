# Security Policy

IVX Clean deletes files, so safety bugs are security bugs. We take them seriously.

## Reporting a vulnerability

Please report privately — do **not** open a public issue for anything that could cause data loss.

- Use GitHub's **private vulnerability reporting**: the *Security* tab → *Report a vulnerability* on
  [64r3al/IVX-Clean](https://github.com/64r3al/IVX-Clean/security/advisories/new).

Include: the IVX Clean version, your Windows version, exact steps to reproduce, and the log file from
`%LOCALAPPDATA%\IVXClean\logs\` if relevant. If you found a way to make the app delete, quarantine or
follow something it should not, a minimal reproduction (a rule or a path layout) is extremely helpful.

## Scope we especially care about

- Any path the SafetyGuard allows that is a protected system/personal location, a reparse-point target,
  or outside a rule's base.
- Any way to make the cleaner act on a file that changed since the scan, or to follow a junction/symlink.
- Classifier mistakes that would let a never-listed item (driver, runtime, anti-cheat) be uninstalled.

## What to expect

We will acknowledge your report, investigate, and fix confirmed issues as a priority, crediting you if
you wish. There is no bug bounty.
