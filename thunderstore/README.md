# ReviveRegen

In The Spell Brigade, when your team runs out of revives a timer slowly earns **one** back. **ReviveRegen** keeps that timer going until your revives are back to the number the run started with.

- Uses the game's own revive timer. Revives refill one at a time, and each one takes a little longer than the last, just like vanilla (configurable).
- The timer starts as soon as you drop below your starting revives, not only when you hit zero.
- **Taking damage speeds it up.** Each 1% of a player's max health they lose takes 1 second off the timer, so a rough fight refills revives faster.
- **Refill timer** under the revive counter: "+1 in 0:42" while one is coming back (it flashes orange when damage speeds it up), and "+1 Revive!" when it arrives. At 0 revives the game's own ring and animation are used instead.

## Multiplayer

- **Only the host needs this mod.** The host adds the revives, and the count syncs to everyone.
- Players who also have it see the refill timer. Players without it still see the count go up.
- Works with MoreWizards, and refills to MoreWizards' adjusted revive pool for big parties.

## Config

The ReviveRegen config file in `BepInEx\config`, or your mod manager's config editor.

| Setting | Default | What it does |
|---|---|---|
| Enabled | true | Refill to the starting number. Off = vanilla (only earns one back at zero) |
| SecondsPerRevive | 0 | Seconds to earn each revive back. 0 = the game's own timer |
| ExtraSecondsAfterEachRevive | 10 | Each revive earned back makes the next take this much longer (vanilla: 10). 0 = always the same |
| ExtraAboveStart | 0 | Let revives refill this many above the starting number |
| DamageSpeedsUpRecharge | true | Taking damage makes the next revive come back sooner |
| SecondsPerPercentHealthLost | 1 | Seconds taken off the timer per 1% of max health lost (1 = a full health bar saves 100 s) |
| ShowRefillTimer | true | Show the refill timer under the revive counter |

## Multiplayer settings

In multiplayer everyone plays with the **host's** settings: the host's config is sent to every player from the lobby on, and the mod reads it instead of yours while you're connected. Your own whether you see the refill timer stay yours. Your config file is never changed. If the host doesn't have this mod, it stays off for you in that game.
