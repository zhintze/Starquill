# Starquill

A mobile idle RPG clicker built in Unity 6 (6000.3.8f1) for Android, portrait 1080x1920.

Assemble a party of 4 paper-doll characters who auto-battle waves of enemies while you fire **Verbs**: pooled party abilities that exploit a dual-triangle stat advantage system (Physical: STR > DEX > CON, Mental: INT > WIS > CHA). Loot drops constantly, instantly changing character appearance through a layered compositing pipeline. Equipment carries a primary/secondary stat pair and an awakened ability that levels up over time. Deterministic, math-heavy, exponentially scaling economy with offline earnings.

## Documentation

| Document | Purpose |
|---|---|
| `docs/implemented-systems.md` | Authoritative reference for built systems |
| `docs/roadmap.md` | Remaining MVP roadmap |
| `docs/sprint-review.md` | Sprint history |
| `docs/plans/2026-02-12-idle-rpg-clicker-design.md` | Master design document |
| `CLAUDE.md` | Development guide (architecture, workflow, gotchas) |

## Development

- Open in Unity 6000.3.8f1; the game scene is `Assets/Scenes/ExploreScene.unity`
- Tests: Window > General > Test Runner > EditMode (~262 tests; headless batch mode does not work on Arch Linux)
- Scene rebuild: Tools > Build Explore Scene
- Active branch: `unity-idle-clicker`

The original Godot open-world RPG prototype is archived in `godot-archive/`.
