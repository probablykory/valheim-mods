![Splash](https://raw.githubusercontent.com/probablykory/valheim-mods/main/BronzeAgeChest/splash.jpg)  

## Features

![Showcase](https://raw.githubusercontent.com/probablykory/valheim-mods/main/BronzeAgeChest/showcase.jpg)  

Adds a distinctive chest that fits into the world of Valheim.  Sized between Wood and Iron, at 3 rows, 5 columns.  The build recipe is configurable, but the default is 15x Wood, 1x Bronze, and 10x Bronze nails.

## Rationale 

I really enjoy bronze age in Valheim, but I hate fighting with wooden chests; they just don't cut it.  Instead of pushing swamp early to upgrade to reinforced chests, I figured I could make a new bronze age chest, and have a great time exploring and enjoying a new world.  

If you're looking for bigger options, or configurable looks, there are a bunch of other mod options out there.  This one's just to fill the tiny little gap in the vanilla gameplay. 🙂

## Installation

### Manual

  * Un-zip `BronzeAgeChest.dll` to your `/Valheim/BepInEx/plugins/` folder.

### Thunderstore (manual install)

  * Go to Settings > Import local mod > Select `BronzeAgeChest.zip`.
  * Click "OK/Import local mod" on the pop-up for information.

## Translations

This mod has full support for translations, but at the moment only has values for English & Russian.  If you wish to add a translation, please do the following:  
 1.  Copy the json file located at `$your_valheim_folder/BepInEx/plugins/BronzeAgeChest/Translations/English/english.json` into a new directory within `Translations`
 2.  Name that directory the same as the language you wish to translate (refer to this [list](https://valheim-modding.github.io/Jotunn/data/localization/language-list.html) of languages; the name must be exact).
 3.  Change the values for all the json keys to what they should be.
 
 Now when you run Valheim, it should see the translations automatically.  Lastly, consider sending me the json file (via an issue on [github](https://github.com/probablykory/valheim-mods) or on discord).  I'd like to add as many translations as I can. 🙂

## Support me

I spend countless hours every day working on, updating, and fixing mods for everyone to enjoy.  While I will never ask for anyone to pay me to make a mod or add a feature, any [support](https://paypal.me/probablyk) is greatly appreciated!

## Changelog

### 1.0.7
 * Updated for ashlands.
 * Fix error related to asset loading, now works with 0.217.46.

### 1.0.6
 * Added a setting, off by default, to enable debug logging.

<details>
<summary><i>View changelog history</i></summary>
<br/>

### 1.0.5
 * Config overhaul across mods.
 * Config changes will now immediately take effect.
 * Mod now responds to Configuration Manager if available, or a config file watcher if not.
 * Added custom drawers when using Configuration Manager.

### 1.0.4
 * Minor fix for Hildir compat.
 * Updated build to require Jotunn 2.12.4

### 1.0.3
 * Adjustment to localization naming
 * Added Russian translation
 * Updated build to require Jotunn 2.12.1

### 1.0.2
 * Minor tweak to the BronzeChest prefab

### 1.0.1
 * Localization revisions.  Plugin now writes default translation to disk, allowing users to modify/add languages.

### 1.0.0
 * Initial Version

 </details>

## Known issues
You can find the github at: https://github.com/probablykory/valheim-mods
