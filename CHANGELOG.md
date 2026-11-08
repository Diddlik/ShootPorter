# Changelog

All notable ShootPorter changes intended for release notes are tracked here.

## [Unreleased]

## [0.2.1] - 2026-11-08

### Fixed

- "Already downloaded" now only needs the same name and size at the destination. Files copied earlier with the copy time as file time were shown as Pending.

## [0.2.0] - 2026-11-08

### Added

- Files that already exist at their destination with the same size and time are shown as "Already downloaded" and skipped.
- After Download options: create subfolders next to downloaded files, convert RAW files to DNG with Adobe DNG Converter.
- Job code and sequence start are saved; the sequence continues after a download; the job code box suggests recently used codes.
- Command line: `--source <path>`, `--profile <name>` and `--jobcode <code>` are applied at startup.

### Fixed

- Downloads no longer overwrite existing files silently: when a target already exists, a dialog asks to overwrite this file, overwrite all, add a suffix (`_01`, `_02`), add a suffix to all, or cancel the download. Overwriting writes to a temp file first, so a failed copy keeps the original.
- Auto-delete now requires a SHA-256 match between the source and every copy (primary and backups) before deleting the source.
- Token values are made safe for file names: `/ \ : * ? " < > |` in values such as aperture, shutter speed or locale dates become `-` instead of creating subfolders or failing.
- Each file's final status is shown as soon as it finishes; cancelling a download keeps the results of finished files.
- Settings are saved atomically, saved on exit, and a corrupt settings file is kept as `settings.json.corrupt-*` instead of being overwritten by defaults.
- Auto-update now checks `https://github.com/Diddlik/ShootPorter`.
- Overall transfer progress is now based on bytes across all files instead of the current file index, and never jumps backwards during parallel copies.
- File status in the transfer queue now moves from Pending to Downloading when a file starts and shows its progress live.
- Video dates: new setting "Video time is local time" (on by default). Many cameras write local time into the QuickTime header; turn it off for phones, which write UTC.

## [0.1.3] - 2026-05-18

### Added

- Wired the existing `AutoDeleteSource` setting into the active app download flow.
- Added source disposition reporting for transfers: deleted, retained, and delete failed.
- Added transfer queue status labels and tooltips for source deletion outcomes.
- Added focused coverage for source auto-delete success, verification-disabled retention, backup-failure retention, and delete-failure handling.

### Changed

- Source files are deleted only after the primary destination copy and all configured backup copies complete successfully with verification enabled.
- Source files are retained when transfer verification is disabled, when any backup copy fails, or when the transfer is cancelled or fails.
- Delete failures now keep destination files intact and mark the transfer result as an error.
- Updated the open implementation tracker to mark source auto-delete as implemented and move the next recommended slice to destination duplicate detection.

### Verified

- `dotnet test tests\ShootPorter.Core.Tests --filter "FullyQualifiedName~DownloadOrchestratorTests"`: 8/8 passed.
- `dotnet build ShootPorter.slnx`: succeeded with 0 warnings and 0 errors.
- `dotnet test ShootPorter.slnx --no-build`: 143/143 passed.
