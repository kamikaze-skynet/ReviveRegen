# ReviveRegen

A [BepInEx](https://github.com/BepInEx/BepInEx) IL2CPP mod for **The Spell Brigade**. When revives run low, they refill over time back to the number the run started with, using the game's own "earn a revive back" timer.

**Download:** on [Thunderstore](https://thunderstore.io/c/the-spell-brigade/) (search "ReviveRegen"). Only the host needs it. See [`thunderstore/README.md`](thunderstore/README.md) for the config options.

## Building

Requirements: [.NET SDK 8](https://dotnet.microsoft.com/download), plus the game with **BepInExPack IL2CPP 6.0.788** installed and launched once.

```powershell
dotnet build src\ReviveRegen.csproj -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\The Spell Brigade"
# r2modman/Gale profile:  -p:BepInExDir="<profile>\BepInEx"     copy into plugins: -p:CopyToPlugins=true
.\package.ps1 -GameDir "D:\SteamLibrary\steamapps\common\The Spell Brigade"   # -> dist\ReviveRegen-<version>.zip
```

## How it works

`ReviveManager.HandleReviveTokenEarning` runs every frame on every player's game. In vanilla it does this:

```
if (CurrentRevives > 0 || paused) return;
progress = (ServerTime - earningTokenStartTime) / timeUntilReviveTokenEarned;
OnReviveTokenEarningChanged(progress);                    // progress ring
if (IsOwner && progress >= 1) { timeUntilReviveTokenEarned += 10; CurrentRevives += 1; }
```

`UseReviveRpc` sets `earningTokenStartTime = ServerTime` when the pool reaches 0.

ReviveRegen replaces that method with the same logic, but changes when it earns:
- **The limit:** it earns while `CurrentRevives` is below the starting total. The total comes from `InitializeOnClientsRpc(totalRevives)`, which MoreWizards also calls when it adjusts the pool.
- **Starting the timer:** the host starts it when the pool first drops below full.
- **Restarting the timer:** it restarts after each revive earned, if more are needed.

Every value lives in the game's own `NetworkVariable`s, which the host owns. That's why only the host needs the mod, while everyone's counter and ring stay in step. If anything goes wrong, the patch falls back to the vanilla method.

```
src/Plugin.cs        config
src/RegenPatches.cs  the HandleReviveTokenEarning replacement and starting-total tracking
thunderstore/        manifest, icon, README, CHANGELOG for the Thunderstore zip
```

## License

[MIT](LICENSE). The Spell Brigade is © Bolt Blaster Games. This is an unofficial fan mod.
