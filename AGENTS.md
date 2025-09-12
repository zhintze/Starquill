# Repository Guidelines

## Project Structure & Module Organization
- `scenes/` — Godot scenes and UI. Avoid renaming/moving outside the editor to preserve UIDs.
- `scripts/` — GDScript source (e.g., `core/`, `equipment/`). Autoload singletons live in `autoload/`.
- `assets/` — art, audio, data (CSV). Non-runtime docs in `documents/` and `diagrams/`.
- `addons/` — third‑party/editor plugins. `.godot/` is editor metadata. Exports go to `build/`.

Target engine: Godot 4.4 (see `project.godot`). Main scene is configured there.

## Build, Test, and Development Commands
- Run editor: `godot4 --path .` (opens the project).
- Run game: `godot4 --path . --editor --quit --run` or press Play in editor.
- Export (Windows preset):
  - Debug/Release headless: `godot4 --headless --path . --export-release "Windows Desktop" build/Starquill.exe`
  - Presets are defined in `export_presets.cfg`.

## Coding Style & Naming Conventions
- Language: GDScript 2.0. Indent with 4 spaces; UTF‑8 per `.editorconfig`.
- Files and variables: `snake_case`. Classes, nodes, and scenes: `PascalCase`. Constants: `SCREAMING_SNAKE_CASE`.
- One script per scene/node where possible. Group by domain under `scripts/<domain>/`.
- Prefer typed GDScript and explicit signals. Keep functions under ~40 lines.

## Testing Guidelines
- No formal automated test suite yet. Use in‑editor playtesting for features.
- If adding tests, create `scripts/tests/` and name files `*_test.gd`; provide a minimal runner scene under `scenes/tests/`.
- Aim for coverage of core logic in `scripts/core/` and `scripts/equipment/` with small, deterministic units.

## Commit & Pull Request Guidelines
- Commits: imperative mood, concise scope first. Examples: `Fix: prevent duplicate equipment slots`, `Feat: add PNG export for randomizer`, `Refactor: unify color manager API`. Link issues with `#123` when relevant.
- PRs: include summary, screenshots/GIFs for visual changes, reproduction/verification steps, and any data or export preset updates. Keep PRs focused and small.

## Editor & Asset Tips
- Manage scenes, resources, and renames inside Godot to avoid broken `uid://` references.
- Large binaries (exports) stay out of Git; artifacts go in `build/` (already used by Windows preset).
- Exports exclude `documents/**` and `*.csv` per `export_presets.cfg`—adjust if runtime needs change.

