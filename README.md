# UCH-AutoSave-Mod
This mod enables auto saves for the game Ultimate Chicken Horse. When enabled it will automatically save the current level in any Party, Creative or Freeplay game whenever an item has been placed or destroyed.

The levels are saved as shown below in with an auto generated name which contains the original level name, the names of the players (if more than 1) and the timestamp of the start of the session:
```
AutoSave [OriginalName] player1, playerN yyyy.MM.dd_HHmm
```

So consecutive saves in one game will overwrite the same savegame and NOT create new ones. Only after going back to lobby and restarting will a new timestamp be generated which results in a new savegame.

## Configuration
To disable the mod change the file **%UCHRoot%\BepInEx\config\UCHAutoSaveMod.cfg** and set 

```
AutoSave Enabled = false
```

## Credits
- [Clever Endeavour Games](https://www.cleverendeavourgames.com/)
- [BepInEx](https://github.com/BepInEx/BepInEx)