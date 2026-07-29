# Bottle Battle

A mobile logic puzzle game built with Unity 6.3 LTS.

## First launch

1. Sign in to Unity Hub and activate a free Personal license.
2. In Unity Hub, choose **Add > Add project from disk** and select this folder.
3. Open the project with Unity `6000.3.20f1`.
4. `Assets/Scenes/MainMenu.unity` is created and opened automatically.
5. Press Play.

The main menu supports portrait safe areas. The bottle showcase scales dynamically
between 3 and 10 bottles through `MainMenuController.SetBottleCount(int)`.

## Play mode

Press **PLAY** to open the first playable puzzle mode. Drag a colored bottle onto
another bottle on the upper shelf to swap their positions. The lower shelf shows
the target order as plain grey bottles with no symbols. When the order is correct,
the target bottles reveal their colors and the **NEXT LEVEL** button appears.
After every swap, the counter below the instruction reports how many bottles are
currently in the correct position, such as **1 CORRECT** or **2 CORRECT**.

The first 100 levels use this progression:

- Level 1: 2 bottles
- Level 2: 3 bottles
- Levels 3–5: 4 bottles
- Levels 6–10: 5 bottles
- Levels 11–20: 6 bottles
- Levels 21–35: 7 bottles
- Levels 36–50: 8 bottles
- Levels 51–70: 9 bottles
- Levels 71–100: 10 bottles

Progress is stored locally and the next unlocked level opens the next time the
player presses **PLAY**. Early levels use seven strongly differentiated bottle
types. The available visual pool expands to 10 types after level 10, 15 types
after level 30 and all 20 types after level 60. Starting layouts are reshuffled
when too many bottles begin in the correct position.

## Moves and star ratings

Every valid bottle swap counts as one move. The game calculates the mathematically
minimum number of swaps for each generated level from the permutation cycles
between the starting and target orders.

- Minimum moves or minimum + 1: 3 stars
- Minimum + 2 through minimum + 4: 2 stars
- Minimum + 5 or more: 1 star

The live puzzle screen displays the current move count and minimum. On completion,
an English result popup shows the earned stars, moves used, minimum moves and the
player's best move count. The best move count and highest star result are saved
separately for every level.

The completion popup uses a layered blue card, raised banner and radial light rays.
Its stars use the supplied transparent PNG directly, without generated shadows,
outlines or highlight layers. **RETRY**
restarts the same deterministic level from zero moves, while **NEXT** advances to
the next unlocked level.

## Bottle artwork

The game now has a pool of 20 original illustrated bottles split between:

- `Assets/Resources/Bottles/generic-soda-bottles.png`
- `Assets/Resources/Bottles/generic-soda-bottles-extra.png`

The designs cover generic cola, citrus soda, iced tea, ginger, cherry, cream
soda, root-beer-style, sparkling water, berry, tropical and fruit-drink visual
cues without brand names, logos, letters, numbers or copied commercial labels.
Each level deterministically selects its bottles from the full pool, so the
catalog can be reused across hundreds of future levels.

`BottleSpriteCatalog` draws every bottle from measured alpha bounds instead of
equal atlas slices. `BottleTextureImporter` disables mipmaps and compression,
keeps the source at up to 4096 pixels and uses bilinear filtering to prevent
cropped, fragmented or jagged edges. Hidden target bottles remain plain grey
silhouettes so they do not reveal the solution.
