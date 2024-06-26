![Splash](https://raw.githubusercontent.com/probablykory/valheim-mods/main/MoreJewelry/splash.png)  
A highly configurable mod for Valheim which adds several new jewelery models for Jewelcrafting.

## Features

New early-game rings & necks, as well as couple late-game jewelry options.  
Yaml-based configuration, allowing you to detail exactly what your rings and necks should look like and behave.  
New perception effect, a customizable version of Guidance/Legacy.  Find your way to any location without a map!  

### Detailed list of new items
Stone ring and Leather cord - very early jewelery made with meadows materials  
Bone ring - use up some of those bone fragments  
Silver band and Polished silver necklace  
Maybe more in the future  

## Rationale 

Jewelcrafting is an amazing mod, but it can sometimes be annoying to play all the way through to Swamps before you can make any rings/necklaces.  This mod changes that, allowing very early access to utility gem effects. 🙂

Going beyond just adding jewelry, I wanted a way to customize which rings used which prefabs (models/colors) and what status effects they had.  This way, I could radically change the jewelry options if I'm playing with a very non-vanilla modpack.

## Installation

> **_NOTE:_**  Users that are upgrading should delete their config file and run the game to let the plugin generate a new one.  Settings can and do change between versions, and old configs may be incompatible.

### Manual

  * Un-zip `MoreJewelry.dll` to your `/Valheim/BepInEx/plugins/` folder.

### Thunderstore (manual install)

  * Go to Settings > Import local mod > Select `MoreJewelry.zip`.
  * Click "OK/Import local mod" on the pop-up for information.

## Translations

This mod has full support for translations, but at the moment only has values for English.  If you wish to add a translation, please do the following:  
 1.  Copy the [default yaml](https://raw.githubusercontent.com/probablykory/valheim-mods/main/MoreJewelry/translations/English.yml) into your `BepInEx/config` and name it... 
 2.  `MoreJewlery.$language.yml`, replacing `$language` part of the new file with the localization you wish to add.  Refer to this [list](https://valheim-modding.github.io/Jotunn/data/localization/language-list.html) of languages; the name must be exact).
 3.  Change the values for all the yml keys to what they should be.
 
 Now when you run Valheim, it should see the translations automatically.  Lastly, consider sending me the yml file (via an issue on [github](https://github.com/probablykory/valheim-mods) or on discord).  I'd like to add as many translations as I can. 🙂

## Contact information
For Questions or Comments, find <span style="color: purple;">probablykory</span> in the Odin Plus Team Discord

[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/v89DHnpvwS)

## Support me

I spend countless hours every day working on, updating, and fixing mods for everyone to enjoy.  While I will never ask for anyone to pay me to make a mod or add a feature, any [support](https://paypal.me/probablyk) is greatly appreciated!

## Changelog

### 1.0.7
 * Updated mod and manager dependencies for Valheim Ashlands 0.218.15
 * Added support for all the new SE_Stat properties for ring effects (Enable external Yaml and look at the everything ring for examples)
 * Fix for case-insensitive name matching of built-in JC effects
 * Fix crash when running on a full Valheim debug build

### 1.0.6
 * Minor update to ensure mocks work with Valheim 0.217.46
 * Small updates to default yaml config which includes a new ring, Humite Ring of Perception
 * Changed default silver ring effect to be Aquatic (gift damage buff while wet)

<details>
<summary><i>View changelog history</i></summary>
<br/>

### 1.0.5
 * Bugfix: Ensure custom skill types work when used in adhoc status effects (ones defined with yaml).  Thanks William!

### 1.0.4
 * Set Config.SaveOnConfigSet to ensure config settings don't unintentionally revert.

### 1.0.3
 * Added soft dep for VNEI, disabling the template prefabs.
 * Bugfix for ConfigManager api, rebuilding settings works.

### 1.0.2
 * Bugfix for JC's original jewelry.  Lumberjacking, etc should all work again.  Thanks Majestic!

### 1.0.1
 * Fixes for the YamlConfigEditor.  Enabled proper color usage when using Azu's Unofficial Config Manager, and fallback when not.

### 1.0.0
 * Initial Version.

</details>

## Known issues
If you find a problem, please visit my [github](https://github.com/probablykory/valheim-mods)
