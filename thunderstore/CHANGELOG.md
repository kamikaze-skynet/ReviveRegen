# Changelog

## 1.1.1
- At 0 revives the refill timer stays hidden, so the game's own refill ring and "revive earned" animation show for 0 -> 1. The timer still shows for refills above 1.

## 1.1.0
- Taking damage makes revives come back sooner: each 1% of a player's max health lost takes 1 second off the timer (`DamageSpeedsUpRecharge`, `SecondsPerPercentHealthLost`).
- A refill timer under the revive counter ("+1 in 0:42", "+1 Revive!" when one arrives). The game's own ring only shows at 0 revives, so there was no sign revives were coming back. It flashes orange when damage speeds it up (`ShowRefillTimer`).
- Fix: when revives were set from outside (e.g. RunSaves restoring a run), a revive could come back instantly.

## 1.0.1
- Fix: revives never regenerated. Reading the game's pause state threw an error every frame (NullReferenceException in pauseStartTime), so the game's normal timer ran instead. The pause state is now read directly.

## 1.0.0
- First release: revives refill over time back to the starting number, using the game's own timer and progress ring.
