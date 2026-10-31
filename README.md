# TILT RUSH – Unity (Android)

## Schnellstart (Deutsch)
1. **Unity Hub > New project** > Vorlage **"2D (Built-In Render Pipeline)"** wählen (NICHT "Universal 2D" / URP).
2. Den Ordner `Assets/Scripts` aus diesem Repo in den `Assets`-Ordner deines Projekts kopieren
   (vorher einen alten `Scripts`-Ordner von TiltRush löschen, damit keine doppelten Dateien entstehen).
3. **Edit > Project Settings > Player > Other Settings > Active Input Handling = "Both"**.
   Unity startet danach einmal neu. Ohne diese Einstellung erscheint in der Console der Fehler
   `TILT RUSH: set ... Active Input Handling to "Both"` – das ist Absicht und genau der Hinweis auf diesen Schritt.
4. **Play** drücken. Im Game-Fenster oben das Seitenverhältnis auf **16:9** stellen und einmal ins Bild klicken (Tastatur: WASD / Pfeile).
5. Handy-Build: **File > Build Settings** (in Unity 6: **File > Build Profiles**) **> Android > Switch Platform**, dann **Build And Run** (Handy per USB, Entwickleroptionen + USB-Debugging an).

---

## Setup (English)

1. Unity 2021.3 LTS or newer (also works in Unity 6): new **2D (Built-in)** project.
2. Copy the folder `Assets/Scripts` into your project's `Assets` folder.
3. **Project Settings > Player > Other Settings > Active Input Handling = "Input Manager (Old)" or "Both"**
   (the code uses the classic `Input` class incl. `Input.acceleration`). Unity 6 defaults to the new Input System only;
   the project then stops with a clear `#error` message telling you to change this setting.
4. Press Play. No scene setup is needed - `App.cs` boots itself in any scene.
   Set the Game view to **16:9** (landscape). Click once into the Game view so it receives keyboard input.
5. Android build: File > Build Settings > Android, orientation Landscape (Auto Rotation) - the game also sets this itself.

## Controls
- Editor/PC: WASD or arrow keys (Co-op: P1 = WASD, P2 = arrows)
- Phone: tilt ONLY (neutral position is captured when the 3-2-1 countdown ends). Co-op on one phone: P1 tilts, P2 uses a touch stick.
- PC / Unity editor / Device Simulator: keyboard only.

## Music (Newgrounds)
Download tracks from Newgrounds (check the artist's licence and credit them!) and put the files into
`Assets/Resources/Music/`. They play automatically in random order. The current track name is shown in Options.
Own sound effects: `Assets/Resources/Sfx/` named `click`, `pickup`, `launch`, `die` (otherwise generated beeps are used).

## Where do I change what?
| I want to change...                         | File |
|---------------------------------------------|------|
| speeds, spawn rates, difficulty curve, Elo, best-of | `Core/Config.cs` |
| modes (add / tweak)                         | `Core/GameModes.cs` |
| gadgets (add new ones in 1 line + effect)   | `Gameplay/Gadgets.cs` |
| enemy types                                 | `Gameplay/Enemy.cs` |
| launch ring behaviour                       | `Gameplay/LaunchRing.cs` |
| round logic / spawning / collisions         | `Gameplay/GameWorld.cs` |
| controls                                    | `Input/MoveInputs.cs` |
| colours (difficulty colour journey)         | `Visuals/GameTheme.cs` |
| background (gradient, blocks, shapes)       | `Visuals/NeonBackground.cs` |
| shapes (new player shapes)                  | `Visuals/SpriteFactory.cs` |
| match flow, rounds, Elo handling            | `Core/SessionController.cs` |
| menus / screens                             | `UI/*.cs` |
| real online backend                         | implement `Online/IOnlineService.cs`, assign in `Core/App.cs` |

## Online PvP
Implemented as a "same-seed race": both players play the identical map (same seed) on their own device;
the longer survivor wins the round, first to 3 wins the match (best of 5). The included `MockOnlineService`
simulates an opponent so the full flow (matchmaking, rounds, Elo change) is testable offline.
For real online play, implement `IOnlineService` (Unity Gaming Services, Photon, Nakama, Firebase...).
Do the Elo calculation on the server later to prevent cheating.

## Difficulty progression
Dots from the start -> red walls from difficulty 0.3 -> swords (with light red warning lane) from 0.55. All values in `Core/Config.cs`.
The arena automatically fills the whole screen (safe area aware); `Config.CameraSize` controls how small everything looks.

## Score, leaderboard, shake
- Score = points per second survived (`Config.ScorePerSecond`), multiplied up to 3x with difficulty (`ScoreDifficultyBonus`).
- Classic scores are submitted to the leaderboard (`IOnlineService.SubmitScore / GetLeaderboard`). The mock service
  uses made-up players; plug in a real backend (e.g. Unity Gaming Services Leaderboards) to compare with real people.
  Player names are free text - a real backend should filter/moderate them.
- Launch rings appear after `Config.LaunchRingStartTime` (60 s).
- Screen shake (nuke, shockwave, death) can be switched off in Options; strength is set in the `App.I.Shake(...)` calls in `GameWorld.cs`.
- Bonus points: dots destroyed by shield / shockwave / nuke (`Config.KillPoints`, nuke kills count `NukeKillMultiplier` = 25%) and
  every pickup (`Config.GadgetPoints`). All bonuses are multiplied by the difficulty factor.

## Gadgets, campaign, tilt setup, design
- Gadgets (`Gameplay/Gadgets.cs`): Slow-Mo, Freeze, Shield, Shockwave, Nuke, Seekers (homing darts), Lance (3 beams), Storm (chain lightning),
  Turret, Decoy (dots chase the hologram). New gadget = one entry in `GadgetLibrary.All` + an effect method in `GameWorld`.
- Campaign (`Campaign/Campaign.cs`): 12 levels. Each has a duration, number of dots (normal/fast/big), walls, swords, rings, speed curve and
  permanent obstacles (grey blocks/pillars that block you and the dots but never hurt). Edit the table to change levels.
- Unlocks (`Core/Unlocks.cs`): skins and colours are unlocked by completed campaign levels. Tables are in the same order as `Config.PlayerShapes` / `Config.Palette`.
- Tilt setup (Options > Tilt Setup): Auto (pose at round start), presets (Flat / Relaxed / Upright / Lying down) or CALIBRATE NOW
  (current pose becomes the neutral pose). A live preview dot shows the input.
- Look: `Theme` in `UI/UIKit.cs` (colour softening, panels). `Theme.Tone` controls how muted the button colours are.

## Hard and Dodge
- HARD (`Modes.Hard` in `Core/GameModes.cs`): same rules as Classic (pickups, rings after 60 s, walls, swords) but 2.2x as many dots.
- DODGE (`Gameplay/DodgeMode.cs`): the dots never chase you. A director sends patterns: rain, curtain (wall with gaps), ring, conga line, flock,
  smiley (blinks and talks), fireworks (light red warning rings), elevator (wall with a moving, piped doorway), pinwheel, heart, windmill.
  A pattern is one function that adds dots with a movement formula - to add a gag write a method like `Smiley()` and register it in the constructor
  (name, difficulty at which it appears, announce on screen).

## Stability notes
- **Sub-steps:** long frames are split into steps of at most `Config.MaxSubStep` (1/50 s) and capped at `Config.MaxFrameTime`,
  so nothing tunnels through walls or swords after a hiccup.
- **Auto pause:** the game pauses when the app loses focus (call, home button, notification shade). RESUME restarts with a
  short 2-1 countdown, and the tilt pose is re-read in AUTO mode.
- **Tilt:** the accelerometer is low-pass filtered (`TiltInput.Smoothing`) with a soft dead zone; the landscape side is detected
  with fallbacks (`TiltInput.Landscape()`), so the axes are never swapped.
- **Pooling:** dots, Dodge dots and effects are reused instead of being created/destroyed all the time (less GC stutter on Android).
- **Online fairness:** online rounds use the same arena size on every phone (`Config.OnlineArenaSize`); the camera zooms to fit.
  Spawns use the seeded RNGs and game time only, so both players get the same dots at the same places.

## Visual effects
- **Sparks** (`SparkSystem` in `Visuals/Fx.cs`, call `Fx.Sparks(...)`): destroyed dots, player death, pickups, launch rings.
  One component with a fixed ring buffer of 320 streaks - cheap even when a nuke destroys hundreds of dots.
- **Light trail** (`PlayerController`): a tapered tail of soft dots in your ship's colour; longer when you are fast or launched.
  Tune `TrailLen` / `TrailSample`.
- **Pop-in** (`Enemy`): new dots pop in with a little overshoot and bump once when they become dangerous. Dodge dots pop in too.
- **Living arena border** (`Visuals/ArenaView.cs`): colour goes from cool cyan to hot red with the difficulty and pulses with the
  music (`AudioManager.Beat`); without music it beats like a calm heart that speeds up.
- **Screen transitions** (`ScreenFade` in `UI/UIManager.cs`): menus fade and grow softly into place, the old screen fades out.

## Look (Geometry-Dash style)
- **Colours** (`Visuals/GameTheme.cs`): turquoise-blue at the start -> blue -> purple -> magenta -> red -> glowing orange.
  The harder it gets, the more saturated and brighter everything becomes. `Hues` = colour journey, the `Lerp` values = how bright.
- **Background** (`Visuals/NeonBackground.cs`): colour gradient, two layers of drifting blocks (parallax), big floating outline shapes;
  pulses with the music and drifts faster at high difficulty.
- **Enemies** (`Gameplay/Enemy.cs`): dark bodies with glowing rims (readable on every colour). Normal = spinning saw with a glowing eye,
  Fast = small spiky star, Big = spiky ball with angry eyes that look where it is going. The spikes stick out a bit beyond the
  hit circle (forgiving). Dodge dots are little saws too.
- **Readability**: the player ship has a thick dark outline, walls have a dark core with a red rim, the sword lane is dark with light red edges.
