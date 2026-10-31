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
- Editor/PC (testing only): WASD or arrow keys (Co-op: P1 = WASD, P2 = arrows). Phone texts never mention the keyboard (`Input/Platform.cs`).
- Phone: tilt ONLY (neutral position is captured when the 3-2-1 countdown ends). Co-op on one phone: the phone lies flat on the table,
  P1 drags a thumb stick on the left half, P2 on the right half (sticks are drawn on screen, orientation is locked during the round).
- PC / Unity editor / Device Simulator: keyboard only.

## Music (Newgrounds)
Put .mp3 / .ogg / .wav files into sub-folders of `Assets/Resources/Music/`:
`Menu` (menus), `Game` (all modes), optional per mode: `Campaign`, `Classic`, `Hard`, `Dodge`, `Asteroids`, `Coop`, `Online`, `Tutorial`.
Missing folders fall back to `Game`, then to everything in `Music`. Tracks play in random order and cross-fade between menu and game.
Credits: `Assets/Resources/Music/credits.txt` (one line per track) appears as the last card in HOW TO PLAY.
Tip: select the music files in Unity and set Load Type = Streaming (saves memory on the phone).
Own sound effects: `Assets/Resources/Sfx/` named `click`, `pickup`, `launch`, `die`, `shoot`, `crack` (otherwise generated sounds are used;
the generated `die` sound is a scattering-toy-bricks sound made in code, `AudioManager.BrickBreak`).

## Testing on the phone
- Quick test without building: install **Unity Remote 5** on the phone, connect via USB, Edit > Project Settings > Editor > Device = your phone,
  press Play. The game then uses the phone's tilt and touches (`Platform.RemoteConnected`).
- Real build: see the German quick start at the top.

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
| skins + unlock conditions                   | `Core/Skins.cs` |
| settings screen (tabs)                      | `UI/OptionsScreen.cs` |
| wardrobe                                    | `UI/WardrobeScreen.cs` |
| match flow, rounds, Elo handling            | `Core/SessionController.cs` |
| menus / screens                             | `UI/*.cs` |
| real online backend                         | implement `Online/IOnlineService.cs`, assign in `Core/App.cs` |

## Online (Unity Gaming Services)
Without extra packages the game uses `MockOnlineService` (simulated players). Real online play switches on automatically:
1. Unity Hub / Unity: log in, then Edit > Project Settings > Services: link (or create) a Unity Cloud project.
2. Window > Package Manager > Unity Registry: install **Leaderboards** (real leaderboard) and **Lobby**
   (or **Multiplayer Services**) for online PvP. Authentication + Core come automatically.
3. cloud.unity.com > your project > Leaderboards > Create leaderboard: ID `classic`, sort descending, update "Keep best".
   Authentication: enable "Anonymous" sign-in (on by default). Lobby needs no setup.
`Assets/Scripts/TiltRush.asmdef` detects the packages (version defines `TILTRUSH_LEADERBOARDS`, `TILTRUSH_LOBBY`) and
`OnlineFactory` then creates `UgsOnlineService`. Options > Profile shows the online status.
PvP = "same-seed race" through a lobby: similar ELO (+-300), host sets the seed, both write their round times into the lobby.
No opponent within `Config.MatchmakingSeconds` -> bot match. Elo is still calculated on the device (move it to Cloud Code later
to prevent cheating). Player names are free text - moderate them before a public release.

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
- Campaign (`Campaign/Campaign.cs`): 25 levels, each with a theme (`Theme = LAVA` ...). Each has a duration, number of dots (normal/fast/big), walls, swords, rings, speed curve and
  permanent obstacles (grey blocks/pillars that block you and the dots but never hurt). Edit the table to change levels.
- Unlocks: colours by campaign levels (`Core/Unlocks.cs`), skins by their own conditions (`Core/Skins.cs`).
- Tilt setup (Options > Tilt Setup): Auto (pose at round start), presets (Flat / Relaxed / Upright / Lying down) or CALIBRATE NOW
  (current pose becomes the neutral pose). A live preview dot shows the input.
- Look: `Theme` in `UI/UIKit.cs` (colour softening, panels). `Theme.Tone` controls how muted the button colours are.

## Hard and Dodge
- HARD (`Modes.Hard` in `Core/GameModes.cs`): same rules as Classic (pickups, rings after 60 s, walls, swords) but 2.2x as many dots.
- DODGE (`Gameplay/DodgeMode.cs`): the dots never chase you. A director sends patterns: rain, curtain (wall with gaps), ring, conga line, flock,
  smiley (blinks and talks), fireworks (light red warning rings), escape hatch (wall with a sliding doorway inside a short tunnel), pinwheel, heart, windmill.
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

## Menus, settings, skins
- **Main menu** (`UI/MenuScreens.cs`): GD-style logo, big PLAY button, WARDROBE (shows your ship) and LEADERBOARD,
  round buttons for options and help, stats line. The background travels slowly through all theme colours.
- **Buttons/panels** (`UI/UIKit.cs`): bright fill, thick dark outline, light bevel; translucent dark panels.
- **Options** (`UI/OptionsScreen.cs`), 5 tabs:
  GRAPHICS (quality preset, particles, light trail, frame rate, background motion, brightness, screen shake),
  SOUND (music, effects, button sounds, vibration, next track),
  INTERFACE (HUD size, pause button left/right, FPS counter, animations),
  CONTROLS (tilt setup, sensitivity, smoothing, invert axes),
  PROFILE (player name, statistics, skins count, reset progress).
- **Skins** (`Core/Skins.cs`): 20 skins. 8 classic ones (free / campaign) + 12 two-colour skins unlocked by
  Classic/Hard scores, Dodge survival time, destroyed dots, pickups, rounds played and online wins.
  New unlocks are announced in the result dialog. The wardrobe shows progress for locked skins.

## Help cards, gadget icons, themed buttons
- **How to play** (`UI/HowToScreen.cs`): full-size cards with large text and pictures (gadgets, enemies, hazards, modes).
  Swipe up/down to change cards; long cards scroll first, pulling past the end switches the card (rubber band).
  PC: arrow buttons, arrow keys / W S / Page Up/Down, mouse wheel. Add content with `Heading`, `Para`, `Row(picture, title, colour, text)`.
- **Gadget pickups** (`Gameplay/Gadgets.cs`): round token in the gadget colour with a white pictogram that shows what it does
  (clock = Slow-Mo, snowflake = Freeze, shield, rings = Shockwave, radiation sign = Nuke, crosshair = Seekers, three beams = Lance,
  lightning = Storm, turret, ghost = Decoy). Pictograms are drawn in `SpriteFactory.Gadget`. Pickups appear often
  (`Config.GadgetSpawnInterval` = 2.5 s).
- **Buttons** (`UIKit.ThemedButton`): keep their own colour but take on some of the background light; grey buttons take the
  background hue. Outline and bevel are tinted with the theme. Brightness/saturation have a minimum so buttons always stand out.
  Panels get a dark tint of the background colour.

## Layered UI and new gadgets
- **Buttons** (`UIKit.MakeButton`, `ButtonFx.ApplyColors`): layered like the gadget tokens / background blocks -
  soft glow, dark frame, dark inside tinted with the button colour + light sheen, thin inner line, bright neon border.
  Colour swatches and locked buttons show their colour inside. Pressing/hovering brightens the inside and the glow.
- **Panels** (`UIKit.Panel`, `PanelTint`): dark translucent layers, thin accent border, inner line and futuristic corner brackets,
  all tinted with the background theme.
- **New gadgets** (14 in total): SHRINK (tiny ship + small hit circle), BOOST (faster), BLACK HOLE (pulls dots in and swallows them,
  `BlackHole` in `GadgetEffects.cs`), ORBIT (three orbs circle you and smash dots).

## Level themes, 25 levels, tutorial
- **Themes** (`Visuals/LevelThemes.cs`): Neon City, Ice Cave, Lava Lake, Toxic Swamp, Desert Ruins, Deep Sea, Outer Space,
  Candy Land, Thunder Storm, Sunset Boulevard, Cyber Grid, Chaos Dimension (rainbow). Each theme sets the background colours,
  the shape of the background blocks (tiles, bricks, crystals, bubbles, stars), floating particles (snow, embers, bubbles,
  stars, rain, dust, sparkles), the obstacle block style (bricks, ice shine, slime dots, rivets, candy stripes, sandstone lines,
  techno frame) and the arena border colour. `GameTheme.Override` is set while a campaign level runs.
- **Campaign**: 25 levels; the level select shows the world colour of each level and its theme name.
- **Tutorial** (`Gameplay/Tutorial.cs`): runs automatically on the very first start (SKIP button, or HOW TO PLAY > PLAY TUTORIAL).
  BLIP, a friendly dot, explains moving (fly through rings), enemies (survive 10 s), gadgets, walls and swords - with jokes.
  You cannot die in the tutorial. The steps are one script method (`Script()`), easy to edit.

## Asteroid field, Freeze, co-op
- **ASTEROID FIELD** (`Modes.Asteroids`, logic in `GameWorld` "asteroid field" section): asteroids, rocks, pebbles and fast shard
  showers drift through space (theme Outer Space). Your cannon fires automatically where your ship's tip points
  (`Config.CannonInterval`, `BulletSpeed`). Big asteroids need 6 hits and split into 2 rocks (3 hits), rocks split into 2 pebbles.
  Gadgets: SPREAD SHOT (5-way cannon, asteroid field only) plus Shield, Shockwave, Nuke, Seekers, Lance, Storm, Shrink, Boost,
  Black Hole, Orbit (`GadgetDef.InAsteroids / InNormal`). No Slow-Mo, Freeze, Turret, Decoy.
- **Freeze**: freezes every dot within `Config.FreezeRadius` (a bit bigger than the shockwave) into ice for `FreezeDuration`;
  frozen dots are harmless and are smashed (with points) when you fly into them.
- **Lives**: `ModeDef.Lives` (Dodge and Asteroid Field = 3). After a hit you lose a heart and blink invulnerable for
  `Config.RespawnInvulnerable` seconds; hearts are shown at the top of the HUD, the last one beats.
