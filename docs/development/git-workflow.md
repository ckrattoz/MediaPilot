# Git workflow and versioning

This document is the reference for the versioning workflow of MediaPilot. It is
normative: when a contribution and this document disagree, this document wins,
and the change that fixes the disagreement is itself a contribution.

## Ownership of the versioning

In MediaPilot the repository owner administers the versioning end to end: they
decide the branch layout, write the commit messages, create the tags and cut
the releases. Contributors do not push to `main` directly; they open a branch
and a pull request, and the owner merges it.

This is specific to MediaPilot. It does not change how the owner works in any
other repository.

## Branches

`main` is the only long-lived branch. It always contains the state that the
next release is built from, and it is expected to be releasable at any moment.

Work happens in short-lived branches created from `main`:

| Prefix | Use |
| --- | --- |
| `feature/<topic>` | New functionality |
| `fix/<topic>` | Defect correction |
| `refactor/<topic>` | Internal restructuring with no behavior change |
| `docs/<topic>` | Documentation only |
| `chore/<topic>` | Build, CI, tooling or housekeeping |

Branch names are lowercase and use hyphens: `feature/smtc-track-title`.

Rules:

- A branch is created from an up-to-date `main` and merges back into `main`.
  The history stays linear: the merge is a fast-forward or a squash, never a
  merge commit with a wall of conflicts.
- A branch that lives longer than a week is split. A branch that is no longer
  needed is deleted after the merge.
- `main` is never force-pushed. If a mistake lands on `main`, it is reverted
  with a new commit, not rewritten.

## Commits

The format is [Conventional Commits](https://www.conventionalcommits.org/):
`<type>(<scope>): <subject>`, with an optional body and an optional footer.

Types:

| Type | Use |
| --- | --- |
| `feat` | New functionality the user can perceive |
| `fix` | Defect correction |
| `docs` | Documentation only |
| `refactor` | Internal change with no behavior change |
| `perf` | Performance improvement |
| `test` | Tests only |
| `build` | Build system, packaging, dependencies |
| `ci` | Continuous integration |
| `chore` | Housekeeping with no source impact |
| `revert` | Revert of a previous commit |

Rules for the subject line:

- It is written in the imperative mood: `add`, not `added` or `adds`.
- It is in English.
- It does not end with a period.
- It describes *what the change does*, not what it fixes internally.

The body is optional. It is required when the reason is not obvious from the
subject, and it always uses a blank line between the subject and the body:

```
fix(hotkeys): warn when a combination is already registered

RegisterHotKey fails with ERROR_HOTKEY_ALREADY_REGISTERED when another
process owns the combination. The failure was silent: the user pressed the
key and nothing happened.
```

The footer uses the standard trailers when they apply, for example
`Refs: #12` or `BREAKING CHANGE: <description>`.

One commit does one thing. A refactor and a behavior change never share a
commit. Every change that the user can perceive is also recorded in
[`CHANGELOG.md`](../../CHANGELOG.md).

## Versioning

MediaPilot follows [Semantic Versioning 2.0.0](https://semver.org/spec/v2.0.0.html):

- **MAJOR**: incompatible changes to the public behavior or to the settings
  file format.
- **MINOR**: new functionality that is backward compatible.
- **PATCH**: defect corrections that do not change behavior.

While the project is pre-1.0 (`0.x.y`), the minor number carries the breaking
changes: `0.1.0` to `0.2.0` may break things that `0.2.0` to `0.2.1` will not.
The `0.1.x` series is the first functional milestone: global hotkeys, the tray
icon, the volume control, the settings window and the notifications. The track
title integration and the distribution package belong to later milestones.

The version number lives in a single place: the `VersionPrefix` property of
`Directory.Build.props`, which applies to every project of the solution. It is
read from the assembly metadata, so the About window and the executable never
disagree with the tag.

## Releases

A release is cut from `main` with these steps:

1. `CHANGELOG.md` gets the new version section, dated, with every change since
   the previous tag.
2. The version in `MediaPilot.App.csproj` is updated to the same number.
3. The build and the tests pass on `main`:
   `dotnet build MediaPilot.slnx -c Release` and `dotnet test MediaPilot.slnx -c Release`.
4. An annotated tag is created: `git tag -a v1.2.0 -m "MediaPilot 1.2.0"`.
5. The tag is pushed: `git push origin main --tags`.
6. The GitHub release is published with the changelog section as its notes and
   the artifacts produced by `build/publish.ps1` attached.

Tags are never moved or deleted once they are public. A mistake in a release is
fixed with a new patch version.

Pre-release builds, when needed, use a suffix: `v1.2.0-preview.1`.

## Ignoring noise

`.gitignore` already excludes the build outputs, the IDE folders and the local
artifacts. Two rules remain for the owner:

- `artifacts/` is never committed. Release assets are attached to the GitHub
  release, not stored in the repository.
- `%LOCALAPPDATA%\MediaPilot\settings.json` and the log folder are local
  runtime data and never leave the machine.

## Quick reference

```powershell
git switch main
git pull --ff-only
git switch -c feature/my-topic

# ... work ...

dotnet build MediaPilot.slnx -c Release
dotnet test  MediaPilot.slnx -c Release

git add -A
git commit -m "feat(scope): describe what the change does"
git push -u origin feature/my-topic
```
