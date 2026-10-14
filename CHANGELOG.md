# Changelog

## 1.0.5.4

- Added an opt-in HT server-bar button. Click it to open settings; its tooltip shows the next enabled house needing a visit. Optional yellow/red colors indicate your reminder interval, not the game's demolition timer.
- Added an Echo confirmation when an interior visit is recorded, with a settings toggle.
- Added export and merge import of tracked characters, house addresses and visit dates. Import preserves newer matching-address visits and existing conflicting addresses. Backups are saved in this user's plugin configuration folder.
- Renamed Reset Warnings to Show dismissed reminders again. It restores dismissed reminders without resetting recorded visits.

## 1.0.5.3

- Updated compatibility for FFXIV patch 7.57.

## 1.0.5.2

- Add `/house` as an alternative to `/htimeout`.
- Add an Enable FC estate reminders setting, enabled by default. Turning it off hides FC reminders and FC settings for all saved characters while preserving their saved housing details and personal-house reminders.

## 1.0.5.1

- Make the reminder window draggable by its title bar and give it an opaque background. Save its position after dragging.
- Skip FC-estate reminders for characters whose rank lacks Estate Hall Access, using the actual permission instead of rank names or position.
- Cache FC permissions per character after reading their own row in the game's FC Members tab. Display unknown permissions explicitly until the data is available.
- Refresh automatically detected FC addresses when a character changes FC estate.

## 1.0.5.0

- Automatically detect the logged-in character's owned personal house and FC estate, including district, ward, and plot.
- Remember each character after login and show reminders for saved alts by default. Keep personal-house visit records separate across characters.
- Record visits only inside the matching estate. Standing outside, entering an apartment, or entering an FC private chamber does not count.
- Remove apartment reminders and configuration.
- Simplify housing settings with detected addresses, clear visit information, a Detect my housing button, and manual address editing when needed.
- Fix the all-character reminder toggle, warning pagination, and repeated warning-window growth.

## 1.0.4.0

- Update for Dalamud API 15 and .NET 10.
