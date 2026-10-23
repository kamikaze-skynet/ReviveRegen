# ReviveRegen

In The Spell Brigade, when your team runs out of revives a timer slowly earns **one** back. **ReviveRegen** keeps that timer going until your revives are back to the number the run started with.

- Uses the game's own revive timer and progress ring. Nothing new to learn.
- Revives refill one at a time. Each one takes a little longer than the last, just like vanilla (configurable).
- The timer starts as soon as you drop below your starting revives, not only when you hit zero.

## Multiplayer

- **Only the host needs this mod.** The host adds the revives, and the count syncs to everyone.
- Players who also have it see the progress ring while revives refill. Players without it still see the count go up.
- Works with MoreWizards, and refills to MoreWizards' adjusted revive pool for big parties.

## Config

The ReviveRegen config file in `BepInEx\config`, or your mod manager's config editor.

| Setting | Default | What it does |
|---|---|---|
| Enabled | true | Refill to the starting number. Off = vanilla (only earns one back at zero) |
| SecondsPerRevive | 0 | Seconds to earn each revive back. 0 = the game's own timer |
| ExtraSecondsAfterEachRevive | 10 | Each revive earned back makes the next take this much longer (vanilla: 10). 0 = always the same |
| ExtraAboveStart | 0 | Let revives refill this many above the starting number |
