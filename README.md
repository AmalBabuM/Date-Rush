# Date Rush

A Unity 2D catch-and-learn game set inside an Emirati date market.

## Play

Open this folder in Unity **6000.6.8f1**, open `Assets/Scenes/DateRush.unity`, and press **Play**. The **Date Rush > Create or Open Game Scene** menu opens the scene too. Choose **LET'S HARVEST** in the game.

The Windows executable is produced at `Builds/Windows/Date Rush.exe` by **Date Rush > Build Windows Game**. Keep its adjacent data and DLL files together when sharing it.

## Controls

| Action | Keyboard | Mouse / touchscreen |
| --- | --- | --- |
| Move | A / D or left / right arrows | Drag along the floor |
| Jump | Space, W, or up arrow | — |
| Sprint | Hold Shift with a movement key | — |
| Pause / resume | P or Escape | Top-left pause button, then KEEP CATCHING |

Select **Date Rush - Game Controller** in Unity to adjust **Dates Per Wave**, **Belt Speed**, **Upper Belt Drop Chance**, and **Release Edge Margin** under **Conveyor settings**. Dates Per Wave takes effect the next time you enter Play mode. **First Drop Delay** and **Drop Interval** are under **Round settings**.

Under **Custom artwork**, assign a sprite to **Pause Button Sprite** to use your own top-left pause icon.

Adjust **Snake Speed** under **Snake settings**. Jump over the snake's head to avoid a 50-point penalty.

The game announces the target variety, and you can check it in the pause menu. Open **About Dates** there to see pictures and short facts for all six varieties while the round stays paused. Catch **15 matching dates in 90 seconds**. A match earns 100 points, increasing by 25 per consecutive match up to 200. A wrong variety deducts 25 and resets the streak. Missed targets reset the streak. Personal best and discovered dates are saved locally with PlayerPrefs.

## Reference adaptation

The supplied `Fried Eggs - Game.mp4` was reviewed using sampled frames throughout its full 60-second duration, including half-second samples of the last 14 seconds. Its visible design informed the four conveyor sections, moving sources, falling pickups, horizontal movement, jumping and dash-like bursts.

- Fox -> Emirati boy in a kandura, ghutra and agal.
- Chickens in moving nests -> individually labelled dates riding the conveyors.
- Falling eggs -> date fruit falling from the conveyors.
- Rooster moving along the floor -> slithering snake.
- Barn -> warm Arabian date market with an oasis doorway.
- Added the assignment's date recognition, target matching, rewards and learning collection.

This is an original date-themed implementation of the visible reference mechanics. Exact original timing, controls and hidden rules cannot be established from a short gameplay recording; the tuning is exposed on the game controller/player components. No graphics or audio were extracted from the reference video.

## Unity structure

- `DateRushGame.cs`: round state, spawning, scoring, targets, persistence and generated sound.
- `DateRushConveyors.cs` builds the four rounded conveyor sections, rotates the gears, and moves and releases individually labelled dates.
- `DateRushPlayer.cs` handles movement, jumping, sprinting, and catching dates with the plate.
- `DateRushSnake.cs` animates and moves the snake along the floor.
- `DateRushDrop.cs`: falling pickups, rolling obstacle and collision handling.
- `DateRushUI.cs`: native Unity Canvas, text, images, buttons, menus, HUD and collection.
- `DateRushArt.cs` caches sprites and materials, generates conveyor and plate art, and converts coordinates.
- `DateVariety.cs`: six date descriptions and independently verifiable catch/scoring rules.
- `Editor/DateRushEditor.cs`: scene setup and Windows build commands.
- `Editor/DateRushVerification.cs`: repeatable gameplay and collision checks.

The scene stores the camera and configurable game controller. On Play, the controller constructs the market, conveyors, player and native UI as inspectable Unity objects in the Hierarchy. No manual component linking is necessary. Runtime movement uses deterministic, script-driven 2D motion. The plate uses swept collision to catch dates while jumping and at high fall speeds.

The original `SampleScene.unity` is preserved. The dedicated DateRush scene is the build entry point. The project retains the existing URP and Input System packages. Assets and materials work in the 2D renderer without additional lighting setup.

## Verification

Use **Date Rush > Run Gameplay Verification**. Results go to `Documentation/Verification.txt`; screenshots go to `Documentation/Screenshots`. The checks exercise actual falling catches, wrong catches, pause, jump and landing, dash cooldown, target changes, victory, timeout, restart and collection. They also verify swept catches and reject catches outside/below the rim. Verification restores saved progress afterward.

`Documentation/BuildReport.txt` records the Windows build result.

## Artwork and fonts

Market, character and date illustrations were created for this project with the built-in image generation tool. Asset prompts and file mapping are in `Documentation/ArtDirection.md`. Unity code generates the belt loops, catching plate, and wind trails. UI uses Ubuntu Regular and Bold under the included `Documentation/Ubuntu-Font-Licence.txt`.
