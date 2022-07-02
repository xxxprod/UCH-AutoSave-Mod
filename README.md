# UCH-AutoSave-Mod
This mod enables auto saves for the game Ultimate Chicken Horse. When enabled it will automatically save the current level in any Party, Creative or Freeplay game whenever an item has been placed or destroyed.

The levels are saved as shown below with the timestamp of the start of the session:
```
AutoSave yyyy.MM.dd HHmm
```

So consecutive saves in one game will overwrite the same savegame and NOT create new ones. Only after going back to lobby and restarting will a new timestamp be generated which results in a new savegame.

## Installation
BepInEx v5 needs to be installed and the mod must be placed in the **%UCHRoot%\BepInEx\plugins** folder.

## Configuration
To disable the mod change the file **%UCHRoot%\BepInEx\config\UCHAutoSaveMod.cfg** and set 

```
AutoSave Enabled = false
```

## Credits
- [Clever Endeavour Games](https://www.cleverendeavourgames.com/)
- [BepInEx](https://github.com/BepInEx/BepInEx)