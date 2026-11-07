# Long Way Home

[![CI](https://github.com/cgaine/LongWayHome/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/cgaine/LongWayHome/actions/workflows/ci.yml?query=branch%3Amain)

A cozy 3D farming game (working title) built with Godot 4 (.NET / C#) for Windows and Linux, including Steam Deck.

- [Game Design Doc](docs/GAME_DESIGN.md) — pillars, systems, data tables, world and roadmap. This is a living document and will keep changing as the design is fleshed out.
- [Art Direction](docs/ART_DIRECTION.md) — theme, palette, light and the look of each place. Draft, developed on the `Graphics` branch.

## Development

**Tools**

- **Godot 4.7.2 .NET** (the "Godot Engine - .NET" download, not the standard build). The version is pinned: `game/LongWayHome.Game.csproj` references `Godot.NET.Sdk/4.7.2` and `game/project.godot` records `4.7`. Upgrading Godot is its own change.
- **.NET SDK 9** (pinned in `global.json`; any 9.0 feature band rolls forward). The projects target `net8.0`, the framework Godot 4.7 uses.
- Any C# IDE (Rider, VS Code with the C# extension, or Visual Studio).

**Layout**

| Path | What it is |
| --- | --- |
| `src/LongWayHome.Domain` | Entities, value objects and rules. No dependencies. |
| `src/LongWayHome.Application` | Commands, Night processing, ports. References Domain. |
| `src/LongWayHome.Infrastructure` | JSON reference data and saves. References Application and Domain. |
| `game/` | The Godot project (`LongWayHome.Game`). Scenes, node scripts, input. References all three. |
| `tests/LongWayHome.Tests` | xUnit tests. Nothing below `game/` references Godot, so these run without the engine. |

**Run the game:** open `game/project.godot` in the Godot 4.7.2 .NET editor and press Play (F5).

**Run the tests:** `dotnet test` at the repository root. You don't need the Godot editor.

**Build everything:** `dotnet build LongWayHome.sln` at the repository root. `Godot.NET.Sdk` is restored from NuGet, so this works without the editor installed.

**Builds:** CI (`.github/workflows/ci.yml`) runs only when started by hand (Actions → CI → Run workflow, then pick a branch). It builds the solution, runs the tests with coverage and exports the game for Windows and Linux; download both builds from the run's artifacts. The Godot version CI uses is `GODOT_VERSION` in `ci.yml` and must match `Godot.NET.Sdk` in `game/LongWayHome.Game.csproj`.

**Releases:** Actions → Release → Run workflow, on `main`. Each run runs CI on `main`, then publishes the next minor version as a [GitHub release](https://github.com/cgaine/LongWayHome/releases) with `LongWayHome-vX.Y.0-windows.zip` and `LongWayHome-vX.Y.0-linux.tar.gz` attached. The first release is `v0.1.0`; after that it takes the latest `vX.Y.Z` tag and makes `vX.(Y+1).0`. For a major or patch version, push that tag by hand; the next run counts on from it.
