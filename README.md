# StormWatch

A small BepInEx 5 plugin that adds a polished storm counter called StormWatch directly to
Magic: The Gathering Arena's Unity UI.

The plugin observes Arena's typed GRE game-state messages. It never changes a
message or game object:

- increments for every `CastSpell` zone-transfer annotation;
- counts spells cast by either player, as the storm rules require;
- resets when Arena advances to a new turn or game;
- ignores lands, activated/triggered abilities, and copied spells;
- keeps countered spells in the count; and
- deduplicates annotations that Arena sends more than once.

The overlay fades in only during games. Its accent changes from cyan to violet
to gold as the spell count climbs, and the number pulses on every cast.

## Linux / Steam / Proton installation

### Requirements

- MTG Arena installed through Steam.
- BepInEx 5 **Windows x64** extracted into Arena's game directory.
- A current .NET SDK, for building from source.

If MTGA Enhancement Suite is already working, its BepInEx installation can be
reused. Do not install a second loader.

Arena's default Steam location is:

```text
~/.local/share/Steam/steamapps/common/MTGA
```

Build and install:

```bash
./scripts/install-linux.sh
```

For a Steam library in another location:

```bash
MTGA_GAME_DIR=/path/to/steamapps/common/MTGA ./scripts/install-linux.sh
```

When installing BepInEx for the first time, prepend this to Arena's existing
Steam launch options:

```text
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

Keep any other launch wrappers you already use; the override only needs to
appear before the command that starts Arena.

After launching once, configuration is written to:

```text
MTGA/BepInEx/config/dev.deep.mtga.stormwatch.cfg
```

The configuration controls visibility, offsets above Arena's action controls, scale,
opacity, and the toggle shortcut. In Arena, open **Options → Gameplay** and use
the native-looking **StormWatch Overlay** switch to show or hide it. Press **F8**
during a match for the same action; the choice is saved and F8 can be changed in
the configuration.

## Build and test

The project builds against the assemblies from the installed Arena client:

```bash
dotnet build src/StormWatch/StormWatch.csproj -c Release
dotnet run --project tests/StormWatch.Tests/StormWatch.Tests.csproj -c Release
```

Override `GameDir` when Arena is installed elsewhere:

```bash
dotnet build src/StormWatch/StormWatch.csproj -c Release \
  -p:GameDir=/path/to/steamapps/common/MTGA
```

The compiled plugin is:

```text
src/StormWatch/bin/Release/netstandard2.1/StormWatch.dll
```

## Compatibility

The current build is verified against:

- MTG Arena `2026.61.30.13636`
- Unity `2022.3.62`
- BepInEx `5.4.23.2`
- Proton on Linux

Arena updates can rename internal types. The Harmony patch is deliberately
limited to the single read-only `MatchManager.OnMessageReceived` observer so a
compatibility update stays small.
