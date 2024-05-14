![Splash](https://raw.githubusercontent.com/probablykory/valheim-mods/main/CraftedBossDrops/splash.jpg)  
A Valheim mod which adds recipes to craft all boss drops, enabling progression without boss kills.

## Features

Configurable recipes for Hard Antler, Swamp Key, Wishbone, Dragon Tear, Torn Spirit, and "Queen Drop".  The default recipes require a large amount of the summon material plus something else from the respective biome.  Modor, Yagluth and Queen recipes are intentionally annoying.  🙂  
In addition to the recipes, this mod patches two small aspects of the game.
  1) Haldor the Trader will recognize your progression as long as you 'know' the gated material.  Ex: Haldor will sell you Ymir Flesh and Thunderstone after you've crafted the Swamp Key.
  2) Hugin the Raven will recognize your progression if you've crafted a Hard Antler, and won't pester you about the Black Forest being dangerous.

## Rationale 

Sometimes I play Valheim without killing bosses, and it's a very relaxing way to enjoy the world without stressing out about raids or finding bosses, etc.  Although it's technically possible to play this way in vanilla, you will eventually hit a hard stop at Modor, because you need his drop to craft in Plains.  Ditto for Mistlands.  This mod provides a straight-forward way to craft every boss drop in the game, so you can play this way without abusing trolls or clipping through gates.

I was thinking about finding more thematic recipes than what's setup currently, things like crafting a key mould and stuffing that into the smelter which will pop out the swamp key.  But for now, these simpler recipes will work.

## Installation

### Manual

  * Un-zip `CraftedBossDrops.dll` to your `/Valheim/BepInEx/plugins/` folder.

### Thunderstore (manual install)

  * Go to Settings > Import local mod > Select `CraftedBossDrops.zip`.
  * Click "OK/Import local mod" on the pop-up for information.

## Support me

I spend countless hours every day working on, updating, and fixing mods for everyone to enjoy.  While I will never ask for anyone to pay me to make a mod or add a feature, any [support](https://paypal.me/probablyk) is greatly appreciated!

## Changelog

### v1.0.8
 * Minor update for ashlands.  Added recipe for FaderDrop.

### v1.0.7
* Added a setting, off by default, to enable debug logging.

<details>
<summary><i>View changelog history</i></summary>
<br/>

### v1.0.6
 * Config overhaul across mods.
 * Added handler for SettingsChanged - should no longer have warnings of recipes already added.

### v1.0.5
 * Added a new recipe for QueenDrop, to better support progression into ashlands/deepnorth in mods like Warfare/Monstrum.
 * Configuration changes will now immediately take effect.
   - Mod now responds to Configuration Manager if available, or a config file watcher if not.
   - Synced configurations now have proper support.
 * When using Configuration Manager, the requirements field is now drawn as table with multiple inputs. Better usability.
 * Adjustments to the default requirements for all other recipes, in many cases reining in the costs of the summon item

### v1.0.4
 * Fix for Haldor patch - ensure Hildir never sells Haldor's stuff.

### v1.0.3
 * Minor fix for Hildir compat.
 * Patched the Blackforest tutorial to be marked as seen if you craft HardAntler.  No more Hugin nagging.
 * Updated build to require Jotunn 2.12.4

### v1.0.2
 * Minor fix to recipe initialization, it won't attempt to add duplicate recipes after users logout and login repeatedly.
 * Updated build to require Jotunn 2.12.1

### 1.0.1
 * Bugfix for patches to the trader

### 1.0.0
 * Initial Version

 </details>

## Known issues
You can find the github at: https://github.com/probablykory/valheim-mods
