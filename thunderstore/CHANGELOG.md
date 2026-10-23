# Changelog

## 1.0.1
- Fix: revives never regenerated. Reading the game's pause state threw an error every frame (NullReferenceException in pauseStartTime), so the game's normal timer ran instead. The pause state is now read directly.

## 1.0.0
- First release: revives refill over time back to the starting number, using the game's own timer and progress ring.
